namespace FileDedupe
{
    public static class FileSizes
    {
        public static long KB(this int bytes) => bytes * 1024L;
        public static long MB(this int bytes) => bytes * 1024L * 1024L;
        public static long GB(this int bytes) => bytes * 1024L * 1024L * 1024L;

        private static string[] suffixes = new[] { "B", "KB", "MB", "GB", "TB" };
        public static string ToFileSize(this int fileSize) => ToFileSize((long)fileSize);
        public static string ToFileSize(this long fileSize)
        {
            var multiplier = 0;
            var remainder = 0L;
            while (fileSize > 1024)
            {
                remainder = fileSize % 1024;
                multiplier += 1;
                fileSize /= 1024L;
            }

            var decimalRemainder = (remainder * 1000 / 1024).ToString("D3");

            return $"{fileSize}.{decimalRemainder}{suffixes[multiplier]}";
        }
    }
}
