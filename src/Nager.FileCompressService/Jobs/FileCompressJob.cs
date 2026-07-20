using Microsoft.Extensions.Options;
using Nager.FileCompressService.Helpers;
using Nager.FileCompressService.Models;
using Nager.FileCompressService.Optimizer;
using Quartz;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nager.FileCompressService.Jobs
{
    /// <summary>
    /// A Quartz.NET job that scans directories and handles parallel compression and optimization of image files.
    /// </summary>
    /// <remarks>
    /// This job supports an analysis-only mode as well as direct compression into JPEG or WebP formats. 
    /// It uses Windows Alternate Data Streams (ADS) to mark files as processed and avoid redundant optimization cycles.
    /// </remarks>
    public class FileCompressJob : IJob
    {
        private readonly ILogger<FileCompressJob> _logger;
        private readonly FileProcessorOptions _options;
        private readonly IImageOptimizer _imageOptimizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCompressJob"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used for tracking the pipeline execution state.</param>
        /// <param name="options">The configuration options for directory paths, parallelism, and compression criteria.</param>
        /// <param name="imageOptimizer">The optimization engine responsible for executing the JPEG/WebP compression algorithms.</param>
        public FileCompressJob(
            ILogger<FileCompressJob> logger,
            IOptions<FileProcessorOptions> options,
            IImageOptimizer imageOptimizer)
        {
            this._logger = logger;
            this._options = options.Value;
            this._imageOptimizer = imageOptimizer;
        }

        /// <summary>
        /// Executes the background compression and scanning workload.
        /// </summary>
        /// <param name="context">The execution context provided by the Quartz scheduler, containing execution details and cancellation tokens.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous orchestration of the background job.</returns>
        public async Task Execute(IJobExecutionContext context)
        {
            if (this._logger.IsEnabled(LogLevel.Information))
            {
                this._logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            var options = new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                ReturnSpecialDirectories = false,
                IgnoreInaccessible = true,               // Skips inaccessible folders without throwing an exception
                RecurseSubdirectories = true,            // Replaces SearchOption.AllDirectories
                AttributesToSkip = FileAttributes.System // Skips system files, e.g. hidden system files
            };

            var maxDegreeOfParallelism = this._options.MaxDegreeOfParallelism == 0 ? Environment.ProcessorCount : this._options.MaxDegreeOfParallelism;

            var compressReports = new List<CompressSummary>();

            //TODO: Currently we are ignore the source directory files, we are process only the subfolders
            var directories = Directory.EnumerateDirectories(this._options.SourceDirectory);
            foreach (var directory in directories)
            {
                this._logger.LogInformation($"Process - {directory}");

                var extensions = this._options.ImageOptimizer.FileExtensions;
                var fileReports = new ConcurrentBag<FileReport>();

                await Parallel.ForEachAsync(
                    Directory.EnumerateFiles(directory, "*", options).Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)),
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = maxDegreeOfParallelism,
                        CancellationToken = context.CancellationToken,
                    },
                    async (file, cancellationToken) =>
                    {
                        var fileReport = await CompressFileAsync(file, cancellationToken);
                        fileReports.Add(fileReport);
                    });

                if (fileReports.Count == 0)
                {
                    continue;
                }

                var compressSummaryReports = fileReports
                    .SelectMany(o => o.CompressResults)
                    .GroupBy(o => o.CompressDescription)
                    .Select(group => new CompressSummary
                    {
                        Path = directory,
                        CompressDescription = group.Key,
                        TotalSourceSize = group.Sum(r => r.SourceSize),
                        TotalCompressedSize = group.Sum(r => r.CompressedSize)
                    })
                    .OrderByDescending(summary => summary.TotalSavingsSize)
                    .ToList();

                compressReports.AddRange(compressSummaryReports);
            }


            foreach (var compressSummaryReport in compressReports)
            {
                this._logger.LogInformation($"{compressSummaryReport.Path} - {compressSummaryReport.CompressDescription}, possible saving {FileSizeHelper.FormatBytes(compressSummaryReport.TotalSavingsSize)} [{compressSummaryReport.TotalSavingsPercentage}%]");
            }

            this._logger.LogInformation("Job done");
        }

        /// <summary>
        /// Processes and compresses a single image file based on the configured compression strategy.
        /// </summary>
        /// <param name="filePath">The absolute path to the target image file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests during lengthy compression IO operations.</param>
        /// <returns>A <see cref="FileReport"/> containing details about the operation metrics and compression ratios.</returns>
        /// <remarks>
        /// If <see cref="FileProcessorOptions.AnalyzeOnly"/> is active, this method performs mock runs for both JPEG and WebP to collect benchmarks.
        /// Otherwise, it acts upon the configured <see cref="ImageOptimizerOptions.OutputFormat"/> and writes an indicator stream to NTFS ADS to prevent re-processing.
        /// </remarks>
        private async Task<FileReport> CompressFileAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return new FileReport
                {
                    FilePath = filePath
                };
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileNameExtension = Path.GetExtension(filePath);
            var outputFilePathWithoutExtension = Path.Combine(directoryPath, $"{fileName}");

            if (this._options.AnalyzeOnly)
            {
                var stopwatch = new Stopwatch();

                stopwatch.Start();

                var compressResultJpg = await this._imageOptimizer.CompressJpegAsync(
                    filePath,
                    $"{outputFilePathWithoutExtension}_o.jpg",
                    quality: this._options.ImageOptimizer.Quality,
                    analyzeOnly: this._options.AnalyzeOnly);

                stopwatch.Stop();
                var compressJpgElapsed = stopwatch.Elapsed.TotalMilliseconds;

                stopwatch.Restart();

                var compressResultWebp = await this._imageOptimizer.CompressWebpAsync(
                    filePath,
                    $"{outputFilePathWithoutExtension}_o.webp",
                    quality: this._options.ImageOptimizer.Quality,
                    analyzeOnly: true);

                stopwatch.Stop();
                var compressWebpElapsed = stopwatch.Elapsed.TotalMilliseconds;

                return new FileReport
                {
                    FilePath = filePath,
                    CompressResults = [compressResultJpg, compressResultWebp]
                };
            }

            var adsStreamName = $"nagerfilecompress";
            var adsPath = $"{filePath}:{adsStreamName}";
            if (File.Exists(adsPath))
            {
                return new FileReport
                {
                    FilePath = filePath
                };
            }

            if (this._options.ImageOptimizer.OutputFormat == "webp")
            {
                var newImagePath = $"{outputFilePathWithoutExtension}_optimized.webp";

                var compressResult = await this._imageOptimizer.CompressWebpAsync(
                    filePath,
                    newImagePath,
                    quality: this._options.ImageOptimizer.Quality,
                    analyzeOnly: this._options.AnalyzeOnly);

                File.Create(adsPath).Close();
                File.Create($"{newImagePath}:{adsStreamName}").Close();

                return new FileReport
                {
                    FilePath = filePath,
                    CompressResults = [compressResult]
                };
            }
            else if (this._options.ImageOptimizer.OutputFormat == "jpeg")
            {
                var newImagePath = $"{outputFilePathWithoutExtension}_optimized.jpg";

                var compressResult = await this._imageOptimizer.CompressJpegAsync(
                    filePath,
                    newImagePath,
                    quality: this._options.ImageOptimizer.Quality,
                    analyzeOnly: this._options.AnalyzeOnly);

                File.Create(adsPath).Close();
                File.Create($"{newImagePath}:{adsStreamName}").Close();

                return new FileReport
                {
                    FilePath = filePath,
                    CompressResults = [compressResult]
                };
            }

            return new FileReport { FilePath = filePath };
        }
    }
}
