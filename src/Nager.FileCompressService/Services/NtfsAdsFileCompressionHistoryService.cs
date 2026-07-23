namespace Nager.FileCompressService.Services
{
    public class NtfsAdsFileCompressionHistoryService : IFileCompressionHistoryService
    {
        private readonly string _adsStreamName = "nagerfilecompress";

        public Task<bool> IsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var adsPath = $"{filePath}:{this._adsStreamName}";
            if (File.Exists(adsPath))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task MarkAsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var adsPath = $"{filePath}:{this._adsStreamName}";
            if (!File.Exists(adsPath))
            {
                File.Create(adsPath).Close();
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task UnmarkAsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var adsPath = $"{filePath}:{this._adsStreamName}";
            if (File.Exists(adsPath))
            {
                File.Delete(adsPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
