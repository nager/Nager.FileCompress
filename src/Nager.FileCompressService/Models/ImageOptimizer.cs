namespace Nager.FileCompressService.Models
{
    /// <summary>
    /// Configuration options specifically tailored for tuning the image optimization and encoding engine.
    /// </summary>
    public class ImageOptimizer
    {
        /// <summary>
        /// Gets or sets the collection of file extensions targeted for processing (e.g., [ ".jpg", ".png" ]).
        /// </summary>
        /// <remarks>
        /// Comparisons using this filter are executed case-insensitively.
        /// </remarks>
        public string[] FileExtensions { get; set; } = [];

        /// <summary>
        /// Gets or sets the compression quality level, typically ranging from 1 to 100.
        /// </summary>
        /// <value>
        /// An integer where higher values prioritize image quality and lower values optimize for maximum file size savings.
        /// </value>
        public int Quality { get; set; }

        /// <summary>
        /// Gets or sets the target output format for the optimized images (e.g., "jpeg" or "webp").
        /// </summary>
        public string OutputFormat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the original source files should be deleted after successful compression.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if original files should be purged post-compression; otherwise, <see langword="false"/>.
        /// </value>
        public bool DeleteOriginal { get; set; }
    }
}
