namespace Nager.FileCompressService.Helpers
{
    /// <summary>
    /// Provides helper methods for calculating file compression metrics and statistics.
    /// </summary>
    public static class CompressionHelper
    {
        /// <summary>
        /// Calculates the compressed size as a percentage of the original size.
        /// </summary>
        /// <param name="originalSize">The original file size in bytes.</param>
        /// <param name="compressedSize">The compressed file size in bytes.</param>
        /// <returns>
        /// The percentage of the original size (e.g., 40.0 for a file compressed to 40% of its size). 
        /// Returns 0 if <paramref name="originalSize"/> is less than or equal to 0.
        /// </returns>
        public static double GetPercentageOfOriginal(long originalSize, long compressedSize)
        {
            if (originalSize <= 0)
            {
                return 0;
            }

            return ((double)compressedSize / originalSize) * 100;
        }

        /// <summary>
        /// Calculates the percentage of space saved after compression.
        /// </summary>
        /// <param name="originalSize">The original file size in bytes.</param>
        /// <param name="compressedSize">The compressed file size in bytes.</param>
        /// <returns>
        /// The reduction percentage (e.g., 60.0 if the file is 60% smaller). 
        /// Returns 0 if <paramref name="originalSize"/> is less than or equal to 0.
        /// </returns>
        public static double GetSavingsPercentage(long originalSize, long compressedSize)
        {
            if (originalSize <= 0)
            {
                return 0;
            }

            return 100 - GetPercentageOfOriginal(originalSize, compressedSize);
        }
    }
}
