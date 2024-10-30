using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafariBooksDownload
{
    public class ChappterInfo
    {
        public String content_url { get; set; }
        public Assets related_assets {get;set;}
    }
    public class Assets
    {
        public List<string>? stylesheets { get; set; }
    }
}
