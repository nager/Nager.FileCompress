namespace Nager.FileCompressService.Models
{
    /// <summary>
    /// Represents the aggregated compression metrics and storage savings for an entire directory.
    /// </summary>
    public class CompressSummary
    {
        /// <summary>
        /// Gets or sets the absolute or relative directory path where the compressed files are located.
        /// </summary>
        public required string Path { get; set; }

        /// <summary>
        /// Gets or sets the description of the compression target or format applied (e.g., "JPEG" or "WebP").
        /// </summary>
        public string CompressDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the combined original size of all processed source files in bytes.
        /// </summary>
        public long TotalSourceSize { get; set; }

        /// <summary>
        /// Gets or sets the combined size of all processed files after compression in bytes.
        /// </summary>
        public long TotalCompressedSize { get; set; }

        /// <summary>
        /// Gets the total aggregated storage space saved in bytes across the directory.
        /// </summary>
        /// <value>
        /// The difference between <see cref="TotalSourceSize"/> and <see cref="TotalCompressedSize"/>.
        /// </value>
        public long TotalSavingsSize => TotalCompressedSize == 0 ? 0 : TotalSourceSize - TotalCompressedSize;

        /// <summary>
        /// Gets the total percentage of storage space saved relative to the combined original file size.
        /// </summary>
        /// <value>
        /// The overall reduction percentage rounded to two decimal places. 
        /// Returns 0 if <see cref="TotalSourceSize"/> is 0.
        /// </value>
        public double TotalSavingsPercentage => TotalCompressedSize == 0
            ? 0
            : Math.Round((double)TotalSavingsSize / TotalSourceSize * 100, 2);
    }
}
