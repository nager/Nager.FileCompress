using Microsoft.Extensions.Options;
using Nager.FileCompressService.Helpers;
using Nager.FileCompressService.Models;
using Nager.FileCompressService.Optimizer;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nager.FileCompressService.Services
{
    /// <summary>
    /// Provides services for scanning directories and executing image compression and image optimization tasks.
    /// </summary>
    public class ImageCompressionService : IImageCompressionService
    {
        private readonly ILogger<ImageCompressionService> _logger;
        private readonly FileProcessorOptions _options;
        private readonly IImageOptimizer _imageOptimizer;
        private readonly IFileCompressionHistoryService _fileCompressionHistoryService;
        private readonly EnumerationOptions _enumerationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCompressionService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used for tracking the processing execution.</param>
        /// <param name="options">The options accessor containing configuration for directory paths, parallelism, and compression criteria.</param>
        /// <param name="imageOptimizer">The optimization engine responsible for executing the compression algorithms.</param>
        /// <param name="fileCompressionHistoryService">The optimization engine responsible for executing the compression algorithms.</param>
        public ImageCompressionService(
            ILogger<ImageCompressionService> logger,
            IOptions<FileProcessorOptions> options,
            IImageOptimizer imageOptimizer,
            IFileCompressionHistoryService fileCompressionHistoryService)
        {
            this._logger = logger;
            this._options = options.Value;
            this._imageOptimizer = imageOptimizer;
            this._fileCompressionHistoryService = fileCompressionHistoryService;

            this._enumerationOptions = new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                ReturnSpecialDirectories = false,
                IgnoreInaccessible = true,               // Skips inaccessible folders without throwing an exception
                AttributesToSkip = FileAttributes.System // Skips system files, e.g. hidden system files
            };
        }

        /// <inheritdoc/>
        public async Task<CompressSummary[]> ProcessDirectoryAsync(
            string directoryPath,
            int currentDepth,
            string[] fileExtensions,
            CancellationToken cancellationToken = default)
        {
            var compressReports = new List<CompressSummary>();

            if (cancellationToken.IsCancellationRequested)
            {
                return [.. compressReports];
            }

            var compressSummaryFiles = await this.ProcessFilesAsync(directoryPath, fileExtensions, cancellationToken);
            compressReports.AddRange(compressSummaryFiles);

            var directories = Directory.EnumerateDirectories(directoryPath);
            foreach (var directory in directories)
            {
                var compressSummaryDirectory = await this.ProcessDirectoryAsync(directory, ++currentDepth, fileExtensions, cancellationToken);
                compressReports.AddRange(compressSummaryDirectory);
            }

            if (currentDepth == 1)
            {
                foreach (var compressSummaryReport in compressReports)
                {
                    if (compressSummaryReport.TotalSavingsPercentage <= 20)
                    {
                        continue;
                    }

                    this._logger.LogInformation($"{compressSummaryReport.CompressDescription} - {compressSummaryReport.Path} - possible saving {FileSizeHelper.FormatBytes(compressSummaryReport.TotalSavingsSize)} [{compressSummaryReport.TotalSavingsPercentage}%]");
                }
            }

            return [.. compressReports];
        }

        private async Task<CompressSummary[]> ProcessFilesAsync(
            string directoryPath,
            string[] fileExtensions,
            CancellationToken cancellationToken = default)
        {
            this._logger.LogDebug("{Method} - Process - {DirectoryPath}", nameof(ProcessFilesAsync), directoryPath);

            var maxDegreeOfParallelism = this._options.MaxDegreeOfParallelism == 0 ? Environment.ProcessorCount : this._options.MaxDegreeOfParallelism;
            var fileReports = new ConcurrentBag<FileReport>();

            await Parallel.ForEachAsync(
                Directory.EnumerateFiles(directoryPath, "*", this._enumerationOptions)
                .Where(file => fileExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    CancellationToken = cancellationToken,
                },
                async (file, cancellationToken) =>
                {
                    var fileReport = await CompressFileAsync(file, cancellationToken);
                    fileReports.Add(fileReport);
                });

            if (fileReports.IsEmpty)
            {
                return [];
            }

            var compressSummaryReports = fileReports
                .SelectMany(o => o.CompressResults)
                .GroupBy(o => o.CompressDescription)
                .Select(group => new CompressSummary
                {
                    Path = directoryPath,
                    CompressDescription = group.Key,
                    TotalSourceSize = group.Sum(r => r.SourceSize),
                    TotalCompressedSize = group.Sum(r => r.CompressedSize)
                })
                .OrderByDescending(summary => summary.TotalSavingsSize)
                .ToList();

            return [.. compressSummaryReports];
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
            try
            {

                var directoryPath = Path.GetDirectoryName(filePath);
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    return new FileReport
                    {
                        FilePath = filePath
                    };
                }

                if (this._options.ImageOptimizer is null)
                {
                    return new FileReport
                    {
                        FilePath = filePath
                    };
                }

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var outputFilePathWithoutExtension = Path.Combine(directoryPath, $"{fileName}");

                if (this._options.AnalyzeOnly)
                {
                    var stopwatch = new Stopwatch();

                    stopwatch.Start();

                    if (this._options.ImageOptimizer.OutputFormat == "webp")
                    {
                        var compressResult = await this._imageOptimizer.CompressWebpAsync(
                            filePath,
                            $"{outputFilePathWithoutExtension}_analyzeonly.webp",
                            quality: this._options.ImageOptimizer.Quality,
                            analyzeOnly: true);

                        stopwatch.Stop();
                        var compressionTime = stopwatch.Elapsed.TotalMilliseconds;

                        this._logger.LogDebug("{Method} - {FilePath}, webp compression time:{CompressionTime:0} ms", nameof(CompressFileAsync), filePath, compressionTime);

                        return new FileReport
                        {
                            FilePath = filePath,
                            CompressResults = [compressResult]
                        };
                    }
                    else if (this._options.ImageOptimizer.OutputFormat == "jpeg")
                    {
                        var compressResult = await this._imageOptimizer.CompressJpegAsync(
                            filePath,
                            $"{outputFilePathWithoutExtension}_analyzeonly.jpg",
                            quality: this._options.ImageOptimizer.Quality,
                            analyzeOnly: true);

                        stopwatch.Stop();
                        var compressionTime = stopwatch.Elapsed.TotalMilliseconds;

                        this._logger.LogDebug("{Method} - {FilePath}, jpeg compression time:{CompressionTime:0} ms", nameof(CompressFileAsync), filePath, compressionTime);

                        return new FileReport
                        {
                            FilePath = filePath,
                            CompressResults = [compressResult]
                        };
                    }

                    return new FileReport
                    {
                        FilePath = filePath
                    };
                }

                if (await this._fileCompressionHistoryService.IsCompressedAsync(filePath, cancellationToken))
                {
                    return new FileReport
                    {
                        FilePath = filePath
                    };
                }

                var optimizedSuffix = "_optimized";

                if (this._options.ImageOptimizer.OutputFormat == "webp")
                {
                    var newImagePath = $"{outputFilePathWithoutExtension}{optimizedSuffix}.webp";

                    var compressResult = await this._imageOptimizer.CompressWebpAsync(
                        filePath,
                        newImagePath,
                        quality: this._options.ImageOptimizer.Quality,
                        analyzeOnly: this._options.AnalyzeOnly);

                    await this._fileCompressionHistoryService.MarkAsCompressedAsync(filePath, cancellationToken);
                    await this._fileCompressionHistoryService.MarkAsCompressedAsync(newImagePath, cancellationToken);

                    if (this._options.ImageOptimizer.KeepOriginal == false)
                    {
                        File.Delete(filePath);

                        var movePath = Path.Combine(
                            Path.GetDirectoryName(newImagePath)!,
                            fileName + Path.GetExtension(newImagePath)
                        );

                        File.Move(newImagePath, movePath);
                    }

                    return new FileReport
                    {
                        FilePath = filePath,
                        CompressResults = [compressResult]
                    };
                }
                else if (this._options.ImageOptimizer.OutputFormat == "jpeg")
                {
                    var newImagePath = $"{outputFilePathWithoutExtension}{optimizedSuffix}.jpg";

                    var compressResult = await this._imageOptimizer.CompressJpegAsync(
                        filePath,
                        newImagePath,
                        quality: this._options.ImageOptimizer.Quality,
                        analyzeOnly: this._options.AnalyzeOnly);

                    await this._fileCompressionHistoryService.MarkAsCompressedAsync(filePath, cancellationToken);
                    await this._fileCompressionHistoryService.MarkAsCompressedAsync(newImagePath, cancellationToken);

                    if (this._options.ImageOptimizer.KeepOriginal == false &&
                        compressResult.NewFileCreated)
                    {
                        File.Delete(filePath);

                        var movePath = Path.Combine(
                            Path.GetDirectoryName(newImagePath)!,
                            fileName + Path.GetExtension(newImagePath)
                        );

                        File.Move(newImagePath, movePath);
                    }

                    return new FileReport
                    {
                        FilePath = filePath,
                        CompressResults = [compressResult]
                    };
                }

                return new FileReport { FilePath = filePath };
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, "{Method} - Error process image, path:{FilePath}", nameof(CompressFileAsync), filePath);
                return new FileReport { FilePath = filePath };
            }
        }
    }
}
