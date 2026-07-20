namespace Nager.FileCompressService.Helpers
{
    /// <summary>
    /// Provides helper methods for formatting and converting file sizes into human-readable strings.
    /// </summary>
    public static class FileSizeHelper
    {
        private static readonly string[] Suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        /// <summary>
        /// Converts a byte count into a human-readable string with the appropriate binary suffix (e.g., KiB, MiB).
        /// </summary>
        /// <param name="bytes">The number of bytes to format. Can be negative.</param>
        /// <returns>
        /// A formatted string representing the file size with up to two decimal places and the appropriate unit suffix 
        /// (e.g., "12.34 MB"). If <paramref name="bytes"/> is negative, the formatted string will be prefixed with a minus sign.
        /// </returns>
        /// <remarks>
        /// This method uses the binary system (base 1024) for calculation. 
        /// Supported units range from Bytes (B) up to Exabytes (EB).
        /// </remarks>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) return "-" + FormatBytes(-bytes);
            if (bytes == 0) return "0 B";

            // Berechnet, welche Einheit (Index im Array) genutzt werden muss
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));

            // Berechnet den Wert in der entsprechenden Einheit
            double num = Math.Round(bytes / Math.Pow(1024, place), 2);

            // Gibt das Ganze formatiert zurück (z. B. "12.34 MB")
            return $"{num} {Suffixes[place]}";
        }
    }
}
