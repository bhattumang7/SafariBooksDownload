namespace SafariBooksDownload
{
    internal static class Config
    {
        public const string WorkInProgressMarker = WorkFolders.Marker;

        public static readonly string BooksPath = ResolveBooksPath();

        public static readonly string COOKIES_FILE = ResolveCookiesFile();

        private static string ResolveBooksPath()
        {
#if ANDROID
            var downloads = Android.OS.Environment.GetExternalStoragePublicDirectory("Download")!.AbsolutePath;
            return Path.Combine(downloads, "SafariBooks");
#elif WINDOWS
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SafariBooks");
#else
            return "";
#endif
        }

        private static string ResolveCookiesFile()
        {
#if ANDROID
            return Path.Combine(FileSystem.Current.AppDataDirectory, "cookies.json");
#elif WINDOWS
            return Path.Combine(BooksPath, "cookies.json");
#else
            return "";
#endif
        }

        public static void EnsureBooksPathExists()
        {
            if (!Directory.Exists(BooksPath))
                Directory.CreateDirectory(BooksPath);
        }

        public static void SweepOrphanWorkFolders() => WorkFolders.SweepOrphans(BooksPath);
    }
}
