namespace SafariBooksDownload.Tests;

public class CssProcessorTests
{
    private static readonly BookFile[] Files =
    [
        Samples.File("OEBPS/styles/a.css", "text/css"),
        Samples.File("OEBPS/images/bg.png", "image/png"),
    ];

    private static string Run(string css) => CssProcessor.Process(css, Files, Samples.Base + "OEBPS/styles/a.css");

    [Fact]
    public void DisplayNone_BecomesVisibilityHidden()
    {
        var output = Run(".hide { display: none; }");

        Assert.DoesNotContain("display", output);
        Assert.Contains("visibility: hidden", output);
    }

    [Fact]
    public void OtherDisplayValues_AreLeftAlone()
    {
        var output = Run(".box { display: block; }");

        Assert.Contains("display: block", output);
        Assert.DoesNotContain("visibility", output);
    }

    [Fact]
    public void BackgroundUrl_IsRewrittenRelativeToTheStylesheet()
    {
        var output = Run(".hero { background-image: url(\"images/bg.png\"); }");

        Assert.Contains("../images/bg.png", output);
    }

    [Fact]
    public void UrlThatMatchesNoBookFile_IsKept()
    {
        var output = Run(".hero { background-image: url(\"other/missing.png\"); }");

        Assert.Contains("other/missing.png", output);
    }
}
