namespace Nager.FileCompressService.Helpers
{
    /// <summary>
    /// Provides helper methods for synchronizing file metadata and timestamps.
    /// </summary>
    public static class FileTimeHelper
    {
        /// <summary>
        /// Synchronizes the creation and last modification timestamps from a source file to a destination file.
        /// </summary>
        /// <param name="sourceFilePath">The absolute or relative path of the file to copy the timestamps from.</param>
        /// <param name="destinationFilePath">The absolute or relative path of the file to apply the timestamps to.</param>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the source file or destination file cannot be found.
        /// </exception>
        /// <remarks>
        /// This method operates entirely in UTC (<see cref="DateTimeKind.Utc"/>) to prevent 
        /// daylight saving time shifts and time zone discrepancies.
        /// </remarks>
        public static void SyncFiles(string sourceFilePath, string destinationFilePath)
        {
            DateTime sourceCreationTime = File.GetCreationTimeUtc(sourceFilePath);
            DateTime sourceModificationTime = File.GetLastWriteTimeUtc(sourceFilePath);
            File.SetCreationTimeUtc(destinationFilePath, sourceCreationTime);
            File.SetLastWriteTimeUtc(destinationFilePath, sourceModificationTime);
        }
    }
}
