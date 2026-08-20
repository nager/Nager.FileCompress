using Nager.FileCompressService.Models;

namespace Nager.FileCompressService.Services
{
    public interface IImageCompressionService
    {
        Task<CompressSummary[]> ProcessDirectoryAsync(
            string directoryPath,
            int currentDepth,
            string[] fileExtensions,
            CancellationToken cancellationToken = default);
    }
}
