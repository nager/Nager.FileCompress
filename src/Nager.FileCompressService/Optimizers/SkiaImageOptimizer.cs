using Nager.FileCompressService.Helpers;
using Nager.FileCompressService.Models;
using SkiaSharp;

// Skia is not used because:
// - EXIF metadata is removed during processing, causing loss of image orientation information.
// - Images may appear incorrectly rotated when the EXIF orientation data is missing.
namespace Nager.FileCompressService.Optimizer
{
    /// <summary>
    /// An image optimization engine implementing <see cref="IImageOptimizer"/> that utilizes SkiaSharp 
    /// for high-performance, synchronous, and asynchronous image optimization tasks.
    /// </summary>
    /// <remarks>
    /// Note: SkiaSharp strips out EXIF orientation metadata by default during decoding/encoding. 
    /// For projects requiring automatic orientation fixes based on camera metadata, consider using the ImageSharp implementation instead.
    /// </remarks>
    public class SkiaImageOptimizer : IImageOptimizer
    {
        /// <inheritdoc/>
        /// <exception cref="Exception">Thrown when the source image cannot be decoded by SkiaSharp.</exception>
        public void OptimizeImage(string inputPath, string outputPath, int maxWidth, int maxHeight, int quality)
        {
            // 1. Load image from file
            using var inputStream = File.OpenRead(inputPath);
            using var originalBitmap = SKBitmap.Decode(inputStream);
            if (originalBitmap is null)
            {
                throw new Exception("Image could not be loaded.");
            }

            // 2. Calculate new dimensions proportionally (preserving aspect ratio)
            var (newWidth, newHeight) = CalculateResizedDimensions(originalBitmap.Width, originalBitmap.Height, maxWidth, maxHeight);

            // 3. Create a new, empty bitmap with target dimensions
            var imageInfo = new SKImageInfo(newWidth, newHeight, originalBitmap.ColorType, originalBitmap.AlphaType);
            using var resizedBitmap = new SKBitmap(imageInfo);

            // Resample the original bitmap into the new, smaller image canvas (High Quality)
            originalBitmap.ScalePixels(resizedBitmap, SKSamplingOptions.Default);

            // 4. Convert and save the optimized image
            using var image = SKImage.FromBitmap(resizedBitmap);

            // Encode using JPEG (or SKEncodedImageFormat.Webp for even better compression ratios)
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            using var outputStream = File.OpenWrite(outputPath);

            // Write compressed bytes to file
            data.SaveTo(outputStream);
        }

        /// <summary>
        /// Calculates proportional dimensions for resizing an image while preserving its aspect ratio.
        /// </summary>
        /// <param name="originalWidth">The current width of the image in pixels.</param>
        /// <param name="originalHeight">The current height of the image in pixels.</param>
        /// <param name="maxWidth">The maximum allowed width constraint in pixels.</param>
        /// <param name="maxHeight">The maximum allowed height constraint in pixels.</param>
        /// <returns>A tuple containing the calculated <c>Width</c> and <c>Height</c> adhering to the constraints.</returns>
        private static (int Width, int Height) CalculateResizedDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            double ratioX = (double)maxWidth / originalWidth;
            double ratioY = (double)maxHeight / originalHeight;
            double ratio = Math.Min(ratioX, ratioY);

            // If the image is already smaller than the maximum dimensions, do not upscale it
            if (ratio >= 1.0)
            {
                return (originalWidth, originalHeight);
            }

            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            return (newWidth, newHeight);
        }

        /// <inheritdoc/>
        /// <exception cref="Exception">Thrown when the source image cannot be decoded by SkiaSharp.</exception>
        public Task<CompressResult> CompressJpegAsync(
            string inputPath,
            string outputPath,
            int quality = 80,
            bool analyzeOnly = false)
        {
            var compressDescription = $"Jpeg [{quality}%]";

            // 1. Load image from file
            using var inputStream = File.OpenRead(inputPath);
            long originalFileSize = inputStream.Length;

            if (originalFileSize < 200)
            {
                return Task.FromResult(new CompressResult { CompressDescription = compressDescription });
            }

            using var originalBitmap = SKBitmap.Decode(inputStream);
            if (originalBitmap is null)
            {
                throw new Exception("Image could not be loaded.");
            }

            // 2. Create an SKImage directly from the bitmap (without resizing)
            using var image = SKImage.FromBitmap(originalBitmap);

            // 3. Encode as JPEG with the desired quality level
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            var compressedFileSize = data.Size;

            if (!analyzeOnly)
            {
                var saving = CompressionHelper.GetSavingsPercentage(originalFileSize, compressedFileSize);
                if (saving > 20) // min 20% saving required
                {
                    using var outputStream = File.Create(outputPath);

                    // 4. Save the compressed data
                    data.SaveTo(outputStream);

                    FileTimeHelper.SyncFiles(inputPath, outputPath);
                }
            }

            var compressResult = new CompressResult
            {
                CompressDescription = compressDescription,
                SourceSize = originalFileSize,
                CompressedSize = compressedFileSize,
            };

            return Task.FromResult(compressResult);
        }

        /// <inheritdoc/>
        /// <exception cref="Exception">Thrown when the source image cannot be decoded by SkiaSharp.</exception>
        public Task<CompressResult> CompressWebpAsync(
            string inputPath,
            string outputPath,
            int quality = 80,
            bool analyzeOnly = false)
        {
            var compressDescription = $"Webp [{quality}%]";

            // 1. Load image from file
            using var inputStream = File.OpenRead(inputPath);
            long originalFileSize = inputStream.Length;

            if (originalFileSize < 200)
            {
                return Task.FromResult(new CompressResult { CompressDescription = compressDescription });
            }

            using var originalBitmap = SKBitmap.Decode(inputStream);
            if (originalBitmap is null)
            {
                throw new Exception("Image could not be loaded.");
            }

            // 2. Create an SKImage directly from the bitmap (without resizing)
            using var image = SKImage.FromBitmap(originalBitmap);

            // 3. Encode as WebP with the desired quality level
            using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
            var compressedFileSize = data.Size;

            if (!analyzeOnly)
            {
                var saving = CompressionHelper.GetSavingsPercentage(originalFileSize, compressedFileSize);
                if (saving > 20) // min 20% saving required
                {
                    using var outputStream = File.Create(outputPath);

                    // 4. Save the compressed data
                    data.SaveTo(outputStream);

                    FileTimeHelper.SyncFiles(inputPath, outputPath);
                }
            }

            var compressResult = new CompressResult
            {
                CompressDescription = compressDescription,
                SourceSize = originalFileSize,
                CompressedSize = compressedFileSize,
            };

            return Task.FromResult(compressResult);
        }
    }
}
