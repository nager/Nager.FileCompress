using Nager.FileCompressService.Helpers;
using Nager.FileCompressService.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Nager.FileCompressService.Optimizer
{
    /// <summary>
    /// An image optimization engine implementing <see cref="IImageOptimizer"/> that utilizes SixLabors.ImageSharp 
    /// for asynchronous formatting and SkiaSharp for synchronous high-performance image resizing.
    /// </summary>
    public class ImageSharpImageOptimizer : IImageOptimizer
    {
        /// <inheritdoc/>
        /// <exception cref="ImageFormatException">Thrown when the source image has an invalid or unsupported format.</exception>
        public void OptimizeImage(string inputPath, string outputPath, int maxWidth, int maxHeight, int quality)
        {
            // 1. Load image from file using ImageSharp
            using var image = Image.Load(inputPath);

            // 2. Normalize orientation using EXIF metadata and apply fluent mutations
            image.Mutate(context =>
            {
                context.AutoOrient();

                // 3. Resize proportionally using the integrated BoxPad/Max constraints to preserve aspect ratio
                context.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth, maxHeight)
                });
            });

            // 4. Configure encoder options and write the optimized file
            var encoder = new JpegEncoder
            {
                Quality = quality
            };

            image.Save(outputPath, encoder);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Automatically normalizes image orientation using EXIF metadata. Files smaller than 200 bytes are skipped.
        /// In active mode, files are only saved if the compression yields more than 20% storage savings.
        /// </remarks>
        public async Task<CompressResult> CompressJpegAsync(
            string inputPath,
            string outputPath,
            int quality = 80,
            bool analyzeOnly = false)
        {
            var compressDescription = $"Jpeg [{quality}%]";

            // 1. Determine original file size
            long originalFileSize = new FileInfo(inputPath).Length;

            if (originalFileSize < 200)
            {
                return new CompressResult { CompressDescription = compressDescription };
            }

            // 2. Load image using ImageSharp
            using var image = await Image.LoadAsync(inputPath);

            // Correct EXIF orientation (physically rotates the image if required)
            image.Mutate(x => x.AutoOrient());

            // 3. Configure encoder with desired quality level
            var encoder = new JpegEncoder
            {
                Quality = quality
            };

            long compressedFileSize = 0;
            var newFileCreated = false;

            if (analyzeOnly)
            {
                using var memoryStream = new MemoryStream();
                await image.SaveAsync(memoryStream, encoder);
                compressedFileSize = memoryStream.Length;
            }
            else
            {
                // Compress in memory first to determine the actual potential savings
                using var memoryStream = new MemoryStream();
                await image.SaveAsync(memoryStream, encoder);
                var simulatedSize = memoryStream.Length;

                var saving = CompressionHelper.GetSavingsPercentage(originalFileSize, simulatedSize);
                if (saving > 20) // min 20% saving required
                {
                    memoryStream.Position = 0;
                    using var fileStream = File.OpenWrite(outputPath);
                    await memoryStream.CopyToAsync(fileStream);

                    compressedFileSize = new FileInfo(outputPath).Length;
                    FileTimeHelper.SyncFiles(inputPath, outputPath);

                    newFileCreated = true;
                }
            }

            return new CompressResult
            {
                CompressDescription = compressDescription,
                SourceSize = originalFileSize,
                CompressedSize = compressedFileSize,
                NewFileCreated = newFileCreated,
            };
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Automatically normalizes image orientation using EXIF metadata and enforces a lossy WebP encoder profile. 
        /// Files smaller than 200 bytes are skipped. In active mode, files are only saved if compression yields more than 20% savings.
        /// </remarks>
        public async Task<CompressResult> CompressWebpAsync(
            string inputPath,
            string outputPath,
            int quality = 80,
            bool analyzeOnly = false)
        {
            var compressDescription = $"Webp [{quality}%]";

            long originalFileSize = new FileInfo(inputPath).Length;

            if (originalFileSize < 200)
            {
                return new CompressResult { CompressDescription = compressDescription };
            }

            using var image = await Image.LoadAsync(inputPath);

            // Correct EXIF orientation
            image.Mutate(x => x.AutoOrient());

            // 2. Configure WebP Encoder with quality and lossy compression profile
            var encoder = new WebpEncoder
            {
                Quality = quality,
                FileFormat = WebpFileFormatType.Lossy // Ensures the image is actually being compressed
            };

            long compressedFileSize = 0;

            if (analyzeOnly)
            {
                using var memoryStream = new MemoryStream();
                await image.SaveAsync(memoryStream, encoder);
                compressedFileSize = memoryStream.Length;
            }
            else
            {
                // Compress in memory first to determine the actual potential savings
                using var memoryStream = new MemoryStream();
                await image.SaveAsync(memoryStream, encoder);
                var simulatedSize = memoryStream.Length;

                var saving = CompressionHelper.GetSavingsPercentage(originalFileSize, simulatedSize);
                if (saving > 20) // min 20% saving required
                {
                    memoryStream.Position = 0;
                    using var fileStream = File.OpenWrite(outputPath);
                    await memoryStream.CopyToAsync(fileStream);

                    compressedFileSize = new FileInfo(outputPath).Length;
                    FileTimeHelper.SyncFiles(inputPath, outputPath);
                }
            }

            return new CompressResult
            {
                CompressDescription = compressDescription,
                SourceSize = originalFileSize,
                CompressedSize = compressedFileSize,
            };
        }
    }
}
