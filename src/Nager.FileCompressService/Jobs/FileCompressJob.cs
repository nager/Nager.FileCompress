using Microsoft.Extensions.Options;
using Nager.FileCompressService.Models;
using Nager.FileCompressService.Services;
using Quartz;

namespace Nager.FileCompressService.Jobs
{
    /// <summary>
    /// A Quartz.NET job that triggers the file compression service.
    /// </summary>
    public class FileCompressJob : IJob
    {
        private readonly ILogger<FileCompressJob> _logger;
        private readonly IImageCompressService _imageCompressService;
        private readonly FileProcessorOptions _fileProcessorOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCompressJob"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used for logging the job execution status.</param>
        /// <param name="imageCompressService">The service responsible for executing the file compression operations.</param>
        public FileCompressJob(
            ILogger<FileCompressJob> logger,
            IImageCompressService imageCompressService,
            IOptions<FileProcessorOptions> fileProcessorOptions)
        {
            this._logger = logger;
            this._imageCompressService = imageCompressService;
            this._fileProcessorOptions = fileProcessorOptions.Value;
        }

        /// <summary>
        /// Executes the job to process file compression asynchronously.
        /// </summary>
        /// <param name="context">The execution context provided by the Quartz scheduler.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Execute(IJobExecutionContext context)
        {
            if (this._logger.IsEnabled(LogLevel.Information))
            {
                this._logger.LogInformation("Job started at: {time}", DateTimeOffset.Now);
            }

            if (this._fileProcessorOptions.ImageOptimizer is null)
            {
                this._logger.LogError("No ImageOptimizer config available");
                return;
            }

            await this._imageCompressService.ProcessDirectoryAsync(
                this._fileProcessorOptions.SourceDirectory,
                this._fileProcessorOptions.ImageOptimizer.FileExtensions,
                context.CancellationToken);

            this._logger.LogInformation("Job done");
        }
    }
}