using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafariBooksDownload
{
    public class Book
    {
        public Book() 
        {
            fileList = new List<BookFile>();
        }
        public String title { get; set; }
        public String product_id { get; set; }
        public String cover_image { get; set; }
        public string authors { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public List<String> chapters { get; set; }

        public String isbn { get; set; }

        public string flat_toc { get; set; }
        public string files_URL { get; set; }
        public string publication_date { get; set; }

        public List<BookFile> fileList {get;set;}
        public List<JsonNodeInfo> nextedTOC { get; set; }
        /// <summary>
        /// Table of contents
        /// </summary>
        public String table_of_contents { get; set; }
        public string getTitle_file_name_safe()
        {
            char[] invalidChars = Path.GetInvalidFileNameChars().Concat(new char[] { ':' }).ToArray();
            return string.Join("_", title.Split(invalidChars));
        }
    }
}
