namespace SafariBooksDownload.Tests;

public class OReillyApiTests
{
    private const string SearchJson = """
        {"data":{"products":[
          {"title":"Clean Code","product_id":"9780132350884","cover_image":"https://c/1.jpg","description":"d","url":"https://u/1",
           "authors":["Robert Martin","Someone Else"],"custom_attributes":{"publication_date":"2008-08-01"}},
          {"title":"Second","product_id":"2","cover_image":"c2","description":"d2","url":"u2",
           "authors":[],"custom_attributes":{"publication_date":"2020-01-01"}}
        ]}}
        """;

    [Fact]
    public void SearchUrl_EncodesTheTerm() =>
        Assert.Contains("q=c%23+%26+f%23", OReillyApi.SearchUrl("c# & f#"));

    [Fact]
    public void Urls_EmbedTheProductId()
    {
        Assert.Equal("https://learning.oreilly.com/api/v2/epubs/urn:orm:book:42/", OReillyApi.BookDetailsUrl("42"));
        Assert.EndsWith("urn:orm:book:42", OReillyApi.ChaptersUrl("42"));
    }

    [Fact]
    public void ParseSearchResults_MapsEveryField()
    {
        var books = OReillyApi.ParseSearchResults(SearchJson);

        Assert.Equal(2, books.Count);
        Assert.Equal("Clean Code", books[0].title);
        Assert.Equal("9780132350884", books[0].product_id);
        Assert.Equal("Robert Martin, Someone Else", books[0].authors);
        Assert.Equal("2008-08-01", books[0].publication_date);
        Assert.Equal("", books[1].authors);
    }

    [Fact]
    public void ParseSearchResults_NoProducts_ReturnsEmpty() =>
        Assert.Empty(OReillyApi.ParseSearchResults("""{"data":{"products":[]}}"""));

    [Fact]
    public void ParseSearchResults_UnexpectedShape_Throws() =>
        Assert.ThrowsAny<Exception>(() => OReillyApi.ParseSearchResults("""{"nope":1}"""));

    [Fact]
    public void PopulateBookDetails_SetsIsbnTocAndFilesUrl()
    {
        var book = new Book();

        OReillyApi.PopulateBookDetails(book, """{"isbn":"123","table_of_contents":"toc-url","files":"files-url"}""");

        Assert.Equal("123", book.isbn);
        Assert.Equal("toc-url", book.table_of_contents);
        Assert.Equal("files-url", book.files_URL);
    }

    [Fact]
    public void ParseChapters_ReadsContentUrlAndStylesheets()
    {
        var chapters = OReillyApi.ParseChapters("""
            {"count":1,"results":[{"content_url":"https://x/ch1.xhtml","related_assets":{"stylesheets":["https://x/a.css"]}}]}
            """);

        var chapter = Assert.Single(chapters);
        Assert.Equal("https://x/ch1.xhtml", chapter.content_url);
        Assert.Equal(["https://x/a.css"], chapter.related_assets.stylesheets);
    }

    [Fact]
    public void ParseFilesPage_ReadsCountFilesAndNext()
    {
        var page = OReillyApi.ParseFilesPage("""
            {"count":41,"next":"https://x/page2","results":[
              {"url":"https://x/a.css","full_path":"a.css","media_type":"text/css","created_time":"2020-01-01T00:00:00Z","virtual_pages":null}]}
            """);

        Assert.Equal(41, page.Count);
        Assert.Equal("https://x/page2", page.Next);
        Assert.Equal("a.css", Assert.Single(page.Files).full_path);
    }

    [Theory]
    [InlineData("""{"count":1,"next":null,"results":[]}""")]
    [InlineData("""{"count":1,"results":[]}""")]
    public void ParseFilesPage_LastPage_HasNoNext(string json) =>
        Assert.Null(OReillyApi.ParseFilesPage(json).Next);
}
