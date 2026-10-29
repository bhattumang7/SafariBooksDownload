using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafariBooksDownload
{
    public class JsonNodeInfo
    {
        public int depth { get; set; }
        public string url { get; set; }
        public string fragment { get; set; }

        public String filename { get; set; }
        public int order { get; set; }
        public string label { get; set; }
        public String full_path { get; set; }
        public String href { get; set; }
        public String media_type { get; set; }

        public List<JsonNodeInfo> children { get; set; }
    }
}
