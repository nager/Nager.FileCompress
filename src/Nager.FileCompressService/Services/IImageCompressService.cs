using Nager.FileCompressService.Models;

namespace Nager.FileCompressService.Services
{
    public interface IImageCompressService
    {
        Task<CompressSummary[]> ProcessDirectoryAsync(
            string directoryPath,
            string[] fileExtensions,
            CancellationToken cancellationToken = default);
    }
}
