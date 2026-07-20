namespace Nager.FileCompressService.Models
{
    /// <summary>
    /// Configuration options for orchestrating the file discovery, parallelism, and compression service pipelines.
    /// </summary>
    public class FileProcessorOptions
    {
        /// <summary>
        /// The key name of the configuration section within appsettings.json used to bind these options.
        /// </summary>
        public const string SectionName = "FileProcessor";

        /// <summary>
        /// Gets or sets the maximum number of concurrent file compression operations.
        /// </summary>
        /// <value>
        /// The maximum degree of parallelism. A value of 0 dynamically falls back to <see cref="Environment.ProcessorCount"/>.
        /// </value>
        public int MaxDegreeOfParallelism { get; set; } = 0;

        /// <summary>
        /// Gets or sets the target directory path containing the source folders and image files to be optimized.
        /// </summary>
        public string SourceDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the service runs in a simulation mode (Dry Run).
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the service should only benchmark and log potential savings without writing files or setting alternate data streams; 
        /// otherwise, <see langword="false"/>.
        /// </value>
        public bool AnalyzeOnly { get; set; } = true;

        /// <summary>
        /// Gets or sets the specific encoder configurations, target quality levels, and format outputs for the underlying image optimizer engine.
        /// </summary>
        public ImageOptimizer? ImageOptimizer { get; set; }
    }
}
