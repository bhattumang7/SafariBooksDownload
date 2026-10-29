namespace SafariBooksDownload.Tests;

public class ChapterHtmlBuilderTests
{
    private static readonly BookFile Chapter = Samples.File("OEBPS/ch01.xhtml", "application/xhtml+xml");

    private static Book Book() => Samples.BookWith(
        Chapter,
        Samples.File("OEBPS/images/fig1.png", "image/png"),
        Samples.File("OEBPS/styles/a.css", "text/css"));

    [Theory]
    [InlineData("text/html", true)]
    [InlineData("application/xhtml+xml", true)]
    [InlineData("text/css", false)]
    [InlineData("image/png", false)]
    public void IsHtml_ByMediaType(string mediaType, bool expected) =>
        Assert.Equal(expected, ChapterHtmlBuilder.IsHtml(new BookFile { media_type = mediaType }));

    [Fact]
    public void WrapsChapterBodyInBookContentContainer()
    {
        var html = ChapterHtmlBuilder.Build(Book(), Chapter, null, "<p>Hello</p>");

        Assert.Contains("<div id=\"book-content\"><p>Hello</p></div>", html);
        Assert.Contains("My: Book/Title", html);
    }

    [Fact]
    public void ImageSources_AreMadeRelative()
    {
        var html = ChapterHtmlBuilder.Build(Book(), Chapter, null, "<img src=\"images/fig1.png\" />");

        Assert.Contains("src=\"images/fig1.png\"", html);
        var abs = ChapterHtmlBuilder.Build(Book(), Chapter, null, $"<img src=\"{Samples.Base}OEBPS/images/fig1.png\" />");
        Assert.Contains("src=\"images/fig1.png\"", abs);
        Assert.DoesNotContain("https://learning.oreilly.com", abs.Split("<body>")[1]);
    }

    [Fact]
    public void ChapterStylesheets_AreLinked_AlongWithOverrideCss()
    {
        var chapter = new ChappterInfo
        {
            content_url = Chapter.url,
            related_assets = new Assets { stylesheets = [Samples.Base + "OEBPS/styles/a.css"] },
        };

        var html = ChapterHtmlBuilder.Build(Book(), Chapter, chapter, "<p/>");

        Assert.Contains("<link href=\"styles/a.css\" rel=\"stylesheet\"", html);
        Assert.Contains("override_v1.css", html);
    }

    [Fact]
    public void Build_DoesNotMutateTheChaptersStylesheetList()
    {
        var sheets = new List<string> { Samples.Base + "OEBPS/styles/a.css" };
        var chapter = new ChappterInfo { content_url = Chapter.url, related_assets = new Assets { stylesheets = sheets } };

        ChapterHtmlBuilder.Build(Book(), Chapter, chapter, "<p/>");
        ChapterHtmlBuilder.Build(Book(), Chapter, chapter, "<p/>");

        Assert.Single(sheets);
    }

    [Fact]
    public void ChapterWithoutStylesheets_GetsNoLinkTags()
    {
        var chapter = new ChappterInfo { content_url = Chapter.url, related_assets = new Assets { stylesheets = [] } };

        var html = ChapterHtmlBuilder.Build(Book(), Chapter, chapter, "<p/>");

        Assert.DoesNotContain("<link", html);
    }
}
