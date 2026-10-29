namespace SafariBooksDownload.Tests;

public class UrlResolverTests
{
    private static readonly BookFile[] Files =
    [
        Samples.File("OEBPS/ch01.xhtml", "application/xhtml+xml"),
        Samples.File("OEBPS/images/fig1.png", "image/png"),
        Samples.File("OEBPS/styles/a.css", "text/css"),
    ];

    [Fact]
    public void AbsoluteUrl_IsMadeRelativeToTheSourceFile()
    {
        var result = UrlResolver.GetRelativePath(Files, Samples.Base + "OEBPS/ch01.xhtml", Samples.Base + "OEBPS/images/fig1.png");
        Assert.Equal("images/fig1.png", result);
    }

    [Fact]
    public void RelativePath_MatchingTailOfAKnownFileUrl_IsResolved()
    {
        var result = UrlResolver.GetRelativePath(Files, Samples.Base + "OEBPS/ch01.xhtml", "images/fig1.png");
        Assert.Equal("images/fig1.png", result);
    }

    [Fact]
    public void RelativePath_FromNestedFile_WalksUp()
    {
        var result = UrlResolver.GetRelativePath(Files, Samples.Base + "OEBPS/styles/a.css", "images/fig1.png");
        Assert.Equal("../images/fig1.png", result);
    }

    [Fact]
    public void UnknownRelativePath_IsReturnedUnchanged()
    {
        var result = UrlResolver.GetRelativePath(Files, Samples.Base + "OEBPS/ch01.xhtml", "nope/missing.png");
        Assert.Equal("nope/missing.png", result);
    }

    [Fact]
    public void EscapedCharacters_AreUnescaped()
    {
        var files = new[] { Samples.File("OEBPS/my image.png", "image/png") };
        files[0].url = Samples.Base + "OEBPS/my%20image.png";
        var result = UrlResolver.GetRelativePath(files, Samples.Base + "OEBPS/ch01.xhtml", files[0].url);
        Assert.Equal("my image.png", result);
    }
}
