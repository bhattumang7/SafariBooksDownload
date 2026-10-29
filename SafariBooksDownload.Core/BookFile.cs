using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafariBooksDownload
{
    public class BookFile
    {
        
        public string ourn { get; set; }
        public string url { get; set; }
        public string full_path { get; set; }
        public string filename { get; set; }
        public string filename_ext { get; set; }
        public string media_type { get; set; }
        public bool has_mathml { get; set; }
        public string kind { get; set; }
        public DateTime created_time { get; set; }
        public DateTime last_modified_time { get; set; }
        public int? virtual_pages { get; set; }
        public int? file_size { get; set; }
        public string epub_archive { get; set; }
    }
}
