namespace Nager.FileCompressService.Models
{
    /// <summary>
    /// Represents the statistical metrics and results of an individual file compression or analysis operation.
    /// </summary>
    public class CompressResult
    {
        /// <summary>
        /// Gets or sets the descriptive identifier or engine name used for the compression process (e.g., "JPEG" or "WebP").
        /// </summary>
        public required string CompressDescription { get; set; }

        /// <summary>
        /// Gets or sets the original size of the source file in bytes.
        /// </summary>
        public long SourceSize { get; set; }

        /// <summary>
        /// Gets or sets the final size of the compressed file in bytes.
        /// </summary>
        public long CompressedSize { get; set; }

        /// <summary>
        /// Gets the absolute amount of storage space saved in bytes.
        /// </summary>
        /// <value>
        /// The difference between <see cref="SourceSize"/> and <see cref="CompressedSize"/>. 
        /// A positive value indicates space saved, while a negative value suggests the file grew during processing.
        /// </value>
        public long Savings => SourceSize - CompressedSize;

        /// <summary>
        /// Gets the percentage of storage space saved relative to the original file size.
        /// </summary>
        /// <value>
        /// The percentage reduction rounded to two decimal places (e.g., 45.55). 
        /// Returns 0 if <see cref="SourceSize"/> is 0.
        /// </value>
        public double SavingsPercentage
        {
            get
            {
                if (SourceSize == 0) return 0;
                return Math.Round((double)Savings / SourceSize * 100, 2);
            }
        }
    }
}
