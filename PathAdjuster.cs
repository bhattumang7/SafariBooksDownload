using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafariBooksDownload
{
    public class PathAdjuster
    {
        private readonly string _baseUrl;
        private readonly string _localBasePath;

        public PathAdjuster(string productId)
        {
            _baseUrl = "/api/v2/epubs/urn:orm:book:" + productId + "/files/";
            _localBasePath = "";
        }

        public string AdjustPathsInHtml(string htmlContent)
        {
            // Replace the base URL with the local base path
            string  adjustedHtml = htmlContent.Replace("https://learning.oreilly.com" + _baseUrl, "");
            adjustedHtml = adjustedHtml.Replace(_baseUrl, _localBasePath);
            return adjustedHtml;
        }
    }
}
