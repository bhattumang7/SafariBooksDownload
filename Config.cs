
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

#if ANDROID
        private static string GetDownloadFolderForAndroid10AndAbove()
        {
         string downloadsPath =  Android.OS.Environment.GetExternalStoragePublicDirectory("Download").AbsolutePath;
         return downloadsPath;
         /*if(File.Exists(downloadsPath )){
         var i =  20 ;
         }
         if(Directory.Exists(downloadsPath)){
         var i = 25 ;
         var myFile = Path.Join(downloadsPath, "my.txt");
         File.WriteAllText(myFile,"asd");
         }
            var docsDirectory = Android.App.Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDownloads);
            string s = docsDirectory.AbsolutePath;*/
            /*string epubPath1 = Path.Combine(s, "/epub/");
            string filepath = Path.Combine(s, "my.html");*/
           // return s;
        }
#endif
        static Config(){
           

#if ANDROID
            /*  var downloadsPath = FileSystem.Current.AppDataDirectory;
              var epubPath = Path.Combine(Android.OS.Environment.StorageDirectory.AbsolutePath, "/epub/");

              // Create the "epub" directory if it doesn't exist
              if (!Directory.Exists(epubPath))
              {
                  Directory.CreateDirectory(epubPath);
              }

              BooksPath = epubPath;
              COOKIES_FILE = Path.Combine(epubPath, "cookies.json");*/

           
            // Use MediaStore to get the Downloads folder
            BooksPath = GetDownloadFolderForAndroid10AndAbove();
          
            COOKIES_FILE = Path.Combine(BooksPath, "cookies.json");
#elif WINDOWS
                BooksPath = @"C:\Umang\NewDownloader\";
                COOKIES_FILE = Path.Combine(PATH, "cookies.json");
#endif

        }

        private static string pATH = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string COOKIES_FILE;
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
