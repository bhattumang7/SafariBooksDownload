using HtmlAgilityPack;

namespace SafariBooksDownload
{
    public static class ChapterHtmlBuilder
    {
        public const string OverrideCssFileName = "override_v1.css";

        private const string StyleSheetSection = """
            <style type="text/css">
              body {
                margin: 1em;
                background-color: transparent !important;
              }
              #sbo-rt-content * {
                text-indent: 0pt !important;
              }
              #sbo-rt-content .bq {
                margin-right: 1em !important;
              }
              {%- if should_support_kindle -%}
              #sbo-rt-content * {
                word-wrap: break-word !important;
                word-break: break-word !important;
              }
              #sbo-rt-content table,
              #sbo-rt-content pre {
                overflow-x: unset !important;
                overflow: unset !important;
                overflow-y: unset !important;
                white-space: pre-wrap !important;
              }
              {%- endif -%}
            </style>
            """;

        public static bool IsHtml(BookFile file) =>
            file.media_type == "text/html" || file.media_type == "application/xhtml+xml";

        /// <summary>
        /// Wraps a downloaded chapter in a full XHTML page that links the chapter's stylesheets plus the
        /// override css, and rewrites img src attributes to paths relative to the chapter file.
        /// </summary>
        public static string Build(Book book, BookFile file, ChappterInfo? chapter, string chapterHtml)
        {
            var extraCssInfo = "";
            if (chapter?.related_assets?.stylesheets is { Count: > 0 } stylesheets)
            {
                var allStylesheets = stylesheets.Append(
                    "https://learning.oreilly.com/api/v2/epubs/urn:orm:book:" + book.product_id + "/files/" +
                    OverrideCssFileName);
                foreach (var styleSheetUrl in allStylesheets)
                {
                    var path = UrlResolver.GetRelativePath(book.fileList, file.url, styleSheetUrl);
                    extraCssInfo += $"<link href=\"{path}\" rel=\"stylesheet\" type=\"text/css\" />\n";
                }
            }

            var page = $"""
                <!DOCTYPE html>
                <html lang="en" xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.w3.org/2002/06/xhtml2/ http://www.w3.org/MarkUp/SCHEMA/xhtml2.xsd" xmlns:epub="http://www.idpf.org/2007/ops">
                 <title> {book.title} </title>
                <head>
                <meta charset="utf-8" />
                {extraCssInfo}
                {StyleSheetSection}
                </head>
                <body><div class="ucvMode-white"><div id="book-content">{chapterHtml}</div></div></body>
                </html>
                """;

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(page);
            var imgNodes = htmlDoc.DocumentNode.SelectNodes("//img");
            if (imgNodes != null)
            {
                foreach (var imgNode in imgNodes)
                {
                    var src = imgNode.GetAttributeValue("src", null);
                    if (src != null)
                    {
                        imgNode.SetAttributeValue("src", UrlResolver.GetRelativePath(book.fileList, file.url, src));
                    }
                }
            }

            using var writer = new StringWriter();
            htmlDoc.Save(writer);
            return writer.ToString();
        }
    }
}
