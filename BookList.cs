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
        public String title { get; set; }
        public String product_id { get; set; }
        public String cover_image { get; set; }
        public string authors { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public List<String> chapters { get; set; }

        public String isbn { get; set; }

        public string flat_toc { get; set; }

        /// <summary>
        /// Table of contents
        /// </summary>
        public String toc { get; set; }
        public string getTitle_file_name_safe()
        {
            return string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
        }

    
    }
}
