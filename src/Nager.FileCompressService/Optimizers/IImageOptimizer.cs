using Nager.FileCompressService.Models;

namespace Nager.FileCompressService.Optimizer
{
    /// <summary>
    /// Defines the contract for an image optimization engine capable of compressing and resizing different image formats.
    /// </summary>
    public interface IImageOptimizer
    {
        /// <summary>
        /// Compresses a source image into the JPEG format asynchronously.
        /// </summary>
        /// <param name="inputPath">The absolute path to the source image file.</param>
        /// <param name="outputPath">The target path where the optimized JPEG file should be written.</param>
        /// <param name="quality">The compression quality level, ranging from 1 to 100. Default is 80.</param>
        /// <param name="analyzeOnly">
        /// If set to <see langword="true"/>, the engine benchmarks the compression and returns metrics without writing the output file to disk.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> wrapping a <see cref="CompressResult"/> that contains metadata about the original and compressed file sizes.
        /// </returns>
        Task<CompressResult> CompressJpegAsync(
            string inputPath,
            string outputPath,
            int quality = 80,
            bool analyzeOnly = false);

        /// <summary>
        /// Compresses a source image into the WebP format asynchronously.
        /// </summary>
        /// <param name="inputPath">The absolute path to the source image file.</param>
        /// <param name="outputPath">The target path where the optimized WebP file should be written.</param>
        /// <param name="quality">The compression quality level, ranging from 1 to 100. Default is 80.</param>
        /// <param name="analyzeOnly">
        /// If set to <see langword="true"/>, the engine benchmarks the compression and returns metrics without writing the output file to disk.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> wrapping a <see cref="CompressResult"/> that contains metadata about the original and compressed file sizes.
        /// </returns>
        Task<CompressResult> CompressWebpAsync(
            string inputPath,
            string outputPath,
            int quality = 80,
            bool analyzeOnly = false);

        /// <summary>
        /// Resizes and optimizes an image synchronously based on specified maximum dimensions and quality.
        /// </summary>
        /// <param name="inputPath">The absolute path to the source image file.</param>
        /// <param name="outputPath">The target path where the resized and optimized image should be written.</param>
        /// <param name="maxWidth">The maximum allowed width of the output image in pixels. Aspect ratio should be maintained.</param>
        /// <param name="maxHeight">The maximum allowed height of the output image in pixels. Aspect ratio should be maintained.</param>
        /// <param name="quality">The compression quality level, ranging from 1 to 100.</param>
        void OptimizeImage(
            string inputPath,
            string outputPath,
            int maxWidth,
            int maxHeight,
            int quality);
    }
}
