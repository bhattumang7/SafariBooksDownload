
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SafariBooksDownload
{
    internal class Config
    {
        static Config(){
            if(DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                var downloadsPath = FileSystem.Current.AppDataDirectory;
                var epubPath = Path.Combine(downloadsPath, "epub");

                // Create the "epub" directory if it doesn't exist
                if (!Directory.Exists(epubPath))
                {
                    Directory.CreateDirectory(epubPath);
                }
            }
            else if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                BooksPath = @"C:\Umang\NewDownloader\";
            }
            else
            {
                BooksPath = @"C:\Umang\NewDownloader\";
            }
            
        }
        private static string pATH = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string COOKIES_FILE = Path.Combine(PATH, "cookies.json");
        public static string ORLY_BASE_HOST = "oreilly.com";  // Insert URL here

        public static string SAFARI_BASE_HOST = "learning." + ORLY_BASE_HOST;
        public static string API_ORIGIN_HOST = "api." + ORLY_BASE_HOST;

        public static string ORLY_BASE_URL = "https://www." + ORLY_BASE_HOST;
        public static string SAFARI_BASE_URL = "https://" + SAFARI_BASE_HOST;
        public static string API_ORIGIN_URL = "https://" + API_ORIGIN_HOST;
        public static string PROFILE_URL = SAFARI_BASE_URL + "/profile/";

        public static string PATH { get => pATH; set => pATH = value; }

        public static string BooksPath ;
    }
}
