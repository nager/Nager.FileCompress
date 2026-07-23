namespace Nager.FileCompressService.Services
{
    /// <summary>
    /// Service to track and manage the compression state of files.
    /// </summary>
    public interface IFileCompressionHistoryService
    {
        /// <summary>
        /// Checks whether the specified file has already been compressed.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the file is already compressed; otherwise, false.</returns>
        Task<bool> IsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the specified file as compressed.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task MarkAsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optional: Removes the compressed status from a file (e.g., if decompressed or reverted).
        /// </summary>
        Task UnmarkAsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }
}
