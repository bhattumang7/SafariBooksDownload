using System.IO.Compression;
using System.Text;

namespace SafariBooksDownload.Tests;

public class BookDownloaderTests
{
    private const string Id = "123";
    private const string FilesUrl = "https://learning.oreilly.com/api/v2/epubs/urn:orm:book:123/files/?page=1";
    private const string FilesUrl2 = "https://learning.oreilly.com/api/v2/epubs/urn:orm:book:123/files/?page=2";

    private static string FileJson(string path, string mediaType) =>
        "{\"url\":\"" + Samples.Base + path + "\",\"full_path\":\"" + path + "\",\"media_type\":\"" + mediaType + "\"}";

    private static string Page(int count, string? next, params string[] fileJson) =>
        "{\"count\":" + count + ",\"next\":" + (next == null ? "null" : "\"" + next + "\"") +
        ",\"results\":[" + string.Join(",", fileJson) + "]}";

    private static string Details() =>
        "{\"isbn\":\"i\",\"table_of_contents\":\"t\",\"files\":\"" + FilesUrl + "\"}";

    private static string Chapters() =>
        "{\"count\":1,\"results\":[{\"content_url\":\"" + Samples.Base + "OEBPS/ch01.xhtml\",\"related_assets\":{\"stylesheets\":[\"" +
        Samples.Base + "OEBPS/style.css\"]}}]}";

    /// <summary>A two-page book: opf, chapter, css, image.</summary>
    private static FakeClient BookClient()
    {
        var css = ".h{display:none}.bg{background-image:url(\"../images/p.png\")}";
        return new FakeClient()
            .Add(OReillyApi.BookDetailsUrl(Id), Details())
            .Add(FilesUrl, Page(4, FilesUrl2, FileJson("OEBPS/content.opf", "application/oebps-package+xml"), FileJson("OEBPS/ch01.xhtml", "application/xhtml+xml")))
            .Add(FilesUrl2, Page(4, null, FileJson("OEBPS/style.css", "text/css"), FileJson("OEBPS/images/p.png", "image/png")))
            .Add(OReillyApi.ChaptersUrl(Id), Chapters())
            .Add(Samples.Base + "OEBPS/content.opf", "<?xml version=\"1.0\"?><package xmlns=\"http://www.idpf.org/2007/opf\"><manifest/></package>")
            .Add(Samples.Base + "OEBPS/ch01.xhtml", "<p>Hello</p><img src=\"images/p.png\"/>")
            .Add(Samples.Base + "OEBPS/style.css", css)
            .Add(Samples.Base + "OEBPS/images/p.png", [1, 2, 3, 4]);
    }

    private static Book NewBook() => new() { title = "My: Book", product_id = Id };

    [Fact]
    public async Task DownloadBook_ProducesAnEpubWithAllPieces_AndCleansUp()
    {
        using var dir = new TempDir();
        var downloader = new BookDownloader(BookClient(), new DownloadViewModel());

        var epub = await downloader.DownloadBookAsync(NewBook(), dir.Path, "/* override */", retainFolder: false);

        Assert.Equal(dir.Combine("My_ Book.epub"), epub);
        Assert.False(Directory.Exists(dir.Combine(Id)));
        using var zip = ZipFile.OpenRead(epub);
        var names = zip.Entries.Select(e => e.FullName).ToHashSet();
        Assert.Superset(new HashSet<string>
        {
            "OEBPS/content.opf", "OEBPS/ch01.xhtml", "OEBPS/style.css", "OEBPS/images/p.png",
            "override_v1.css", "META-INF/container.xml",
        }, names);
        Assert.DoesNotContain(WorkFolders.Marker, names);

        Assert.Contains("OEBPS/content.opf", Read(zip, "META-INF/container.xml"));
        Assert.Contains(EpubManifest.OverrideItemId, Read(zip, "OEBPS/content.opf"));
        Assert.Contains("override", Read(zip, "override_v1.css"));
        Assert.Contains("visibility: hidden", Read(zip, "OEBPS/style.css"));
        Assert.Contains("<link href=\"style.css\"", Read(zip, "OEBPS/ch01.xhtml"));
        Assert.Contains("<p>Hello</p>", Read(zip, "OEBPS/ch01.xhtml"));
    }

    [Fact]
    public async Task DownloadBook_WithRetainFolder_KeepsTheWorkFolderWithoutMarker()
    {
        using var dir = new TempDir();
        var downloader = new BookDownloader(BookClient(), new DownloadViewModel());

        await downloader.DownloadBookAsync(NewBook(), dir.Path, "", retainFolder: true);

        Assert.True(File.Exists(dir.Combine(Id, "OEBPS", "ch01.xhtml")));
        Assert.False(File.Exists(dir.Combine(Id, WorkFolders.Marker)));
    }

    [Fact]
    public async Task DownloadBook_WhenAFileFails_RemovesPartialFolder_AndRethrows()
    {
        using var dir = new TempDir();
        var client = new FakeClient()
            .Add(OReillyApi.BookDetailsUrl(Id), Details())
            .Add(FilesUrl, Page(1, null, FileJson("OEBPS/content.opf", "application/oebps-package+xml")))
            .Add(OReillyApi.ChaptersUrl(Id), "{\"count\":0,\"results\":[]}");
        // content.opf itself is not served -> the download throws mid-way
        var downloader = new BookDownloader(client, new DownloadViewModel());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            downloader.DownloadBookAsync(NewBook(), dir.Path, "", retainFolder: false));

        Assert.False(Directory.Exists(dir.Combine(Id)));
        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact]
    public async Task LoadFileList_FollowsNextLinks_AndCollectsEveryFile()
    {
        var client = BookClient();
        var book = NewBook();
        book.files_URL = FilesUrl;

        await new BookDownloader(client, new DownloadViewModel()).LoadFileListAsync(book);

        Assert.Equal(4, book.fileList.Count);
        Assert.Equal([FilesUrl, FilesUrl2], client.Requests);
    }

    [Fact]
    public async Task LoadFileList_FewerThanOnePageOfFiles_DoesNotThrow()
    {
        // count < 20 means zero "pages"; this used to divide by zero.
        var client = new FakeClient().Add(FilesUrl, Page(2, null, FileJson("a.css", "text/css")));
        var book = NewBook();
        book.files_URL = FilesUrl;
        var progress = new DownloadViewModel();

        await new BookDownloader(client, progress).LoadFileListAsync(book);

        Assert.Single(book.fileList);
        Assert.Equal(0, progress.ProgressBarValue);
    }

    [Fact]
    public async Task DownloadFiles_ReportsProgress_ThroughTheUiDispatcher()
    {
        using var dir = new TempDir();
        var client = BookClient();
        var book = NewBook();
        book.files_URL = FilesUrl;
        var progress = new DownloadViewModel();
        var dispatched = 0;
        var downloader = new BookDownloader(client, progress, a => { dispatched++; a(); });
        await downloader.LoadFileListAsync(book);
        dispatched = 0;

        await downloader.DownloadFilesAsync(book, [], dir.Path);

        Assert.True(dispatched > 0);
        Assert.Contains("downloading files", progress.DownloadLabel);
        Assert.Contains("4", progress.ProgressLabel);
    }

    [Fact]
    public async Task DownloadSingleFile_ExistingFile_IsNotFetchedAgain()
    {
        using var dir = new TempDir();
        var client = new FakeClient();
        var file = Samples.File("OEBPS/img.png", "image/png");
        Directory.CreateDirectory(dir.Combine("OEBPS"));
        File.WriteAllBytes(dir.Combine("OEBPS", "img.png"), [9]);

        await new BookDownloader(client, new DownloadViewModel())
            .DownloadSingleFileAsync(Samples.BookWith(file), [], dir.Path, file);

        Assert.Empty(client.Requests);
        Assert.Equal([9], File.ReadAllBytes(dir.Combine("OEBPS", "img.png")));
    }

    [Fact]
    public async Task DownloadSingleFile_BinaryFile_IsWrittenByteForByte()
    {
        using var dir = new TempDir();
        var file = Samples.File("OEBPS/images/p.png", "image/png");
        var client = new FakeClient().Add(file.url, [0, 255, 7, 128]);

        await new BookDownloader(client, new DownloadViewModel())
            .DownloadSingleFileAsync(Samples.BookWith(file), [], dir.Path, file);

        Assert.Equal([0, 255, 7, 128], File.ReadAllBytes(dir.Combine("OEBPS", "images", "p.png")));
    }

    private static string Read(ZipArchive zip, string name)
    {
        using var reader = new StreamReader(zip.GetEntry(name)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
