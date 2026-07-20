namespace Nager.FileCompressService.Models
{
    /// <summary>
    /// Represents a detailed processing report for an individual file, tracking its path and compression outcomes.
    /// </summary>
    public class FileReport
    {
        /// <summary>
        /// Gets or sets the full, absolute file path of the processed or analyzed image file.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of individual compression results and format benchmarks generated for this file.
        /// </summary>
        /// <value>
        /// An array of <see cref="CompressResult"/> objects. In analysis mode, this typically contains benchmarks for multiple 
        /// formats (e.g., both JPEG and WebP) to allow comparison.
        /// </value>
        public CompressResult[] CompressResults { get; set; } = [];
    }
}
