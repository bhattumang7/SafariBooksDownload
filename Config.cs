using System.IO;

namespace SafariBooksDownload
{
    internal class Config
    {
        public const string WorkInProgressMarker = WorkFolders.Marker;

        static Config()
        {
#if ANDROID
            var downloads = Android.OS.Environment.GetExternalStoragePublicDirectory("Download")!.AbsolutePath;
            BooksPath = Path.Combine(downloads, "SafariBooks");
            COOKIES_FILE = Path.Combine(FileSystem.Current.AppDataDirectory, "cookies.json");
#elif WINDOWS
            BooksPath = @"C:\Umang\NewDownloader\";
            COOKIES_FILE = Path.Combine(BooksPath, "cookies.json");
#endif
        }

        public static void EnsureBooksPathExists()
        {
            if (!Directory.Exists(BooksPath))
                Directory.CreateDirectory(BooksPath);
        }

        public static void SweepOrphanWorkFolders() => WorkFolders.SweepOrphans(BooksPath);

        private static string pATH = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        public static string COOKIES_FILE = "";
        public static string ORLY_BASE_HOST = "oreilly.com";

        public static string SAFARI_BASE_HOST = "learning." + ORLY_BASE_HOST;
        public static string API_ORIGIN_HOST = "api." + ORLY_BASE_HOST;

        public static string ORLY_BASE_URL = "https://www." + ORLY_BASE_HOST;
        public static string SAFARI_BASE_URL = "https://" + SAFARI_BASE_HOST;
        public static string API_ORIGIN_URL = "https://" + API_ORIGIN_HOST;
        public static string PROFILE_URL = SAFARI_BASE_URL + "/profile/";

        public static string PATH { get => pATH; set => pATH = value; }

        public static string BooksPath = "";
    }
}
