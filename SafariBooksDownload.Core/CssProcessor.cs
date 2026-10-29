using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;

namespace SafariBooksDownload
{
    public static class CssProcessor
    {
        // Properties that can contain URLs
        private static readonly string[] PropertiesWithUrls =
        [
            "background",
            "background-image",
            "border-image",
            "content",
            "cursor",
            "list-style-image"
        ];

        /// <summary>
        /// Hides rather than removes <c>display:none</c> rules, and rewrites url("...") references
        /// to paths relative to the stylesheet's own location.
        /// </summary>
        public static string Process(string css, IEnumerable<BookFile> bookFiles, string cssFileUrl)
        {
            var config = Configuration.Default.WithCss();
            var context = BrowsingContext.New(config);
            var parser = context.GetService<ICssParser>()
                         ?? throw new InvalidOperationException("ICssParser is not available.");

            var stylesheet = parser.ParseStyleSheet(css);
            RemoveVisibilityNone(stylesheet);
            AdjustUrls(bookFiles, cssFileUrl, stylesheet);
            return stylesheet.ToCss();
        }

        public static void RemoveVisibilityNone(ICssStyleSheet stylesheet)
        {
            foreach (var rule in stylesheet.Rules)
            {
                if (rule is ICssStyleRule styleRule && styleRule.Style.GetPropertyValue("display") == "none")
                {
                    styleRule.Style.RemoveProperty("display");
                    styleRule.Style.SetProperty("visibility", "hidden");
                }
            }
        }

        public static void AdjustUrls(IEnumerable<BookFile> bookFiles, string cssFileUrl, ICssStyleSheet stylesheet)
        {
            var files = bookFiles.ToList();
            foreach (var rule in stylesheet.Rules)
            {
                if (rule is not ICssStyleRule styleRule)
                {
                    continue;
                }

                foreach (var propertyName in PropertiesWithUrls)
                {
                    var propertyValue = styleRule.Style.GetPropertyValue(propertyName);
                    if (string.IsNullOrWhiteSpace(propertyValue) || !propertyValue.Contains("url("))
                    {
                        continue;
                    }

                    var match = Regex.Match(propertyValue, @"url\(\""(.*?)\""\)");
                    if (!match.Success)
                    {
                        continue;
                    }

                    string extractedUrl = match.Groups[1].Value;
                    var updatedPath = UrlResolver.GetRelativePath(files, cssFileUrl, extractedUrl);
                    var escapedUpdatedPath = updatedPath.Replace("\\", "\\\\");
                    styleRule.Style.SetProperty(propertyName, propertyValue.Replace(extractedUrl, escapedUpdatedPath));
                }
            }
        }
    }
}
