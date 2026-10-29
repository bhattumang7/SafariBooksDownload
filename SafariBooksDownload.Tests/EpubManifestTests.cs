using System.Xml.Linq;

namespace SafariBooksDownload.Tests;

public class EpubManifestTests
{
    private const string Opf = """<?xml version="1.0"?><package xmlns="http://www.idpf.org/2007/opf"><manifest><item id="c1" href="ch01.xhtml" media-type="application/xhtml+xml"/></manifest></package>""";

    [Fact]
    public void GetOpfFileFullPath_ReturnsFirstOpf()
    {
        var files = new[]
        {
            Samples.File("OEBPS/ch01.xhtml", "application/xhtml+xml"),
            Samples.File("OEBPS/content.opf", "application/oebps-package+xml"),
            Samples.File("other.opf", "application/oebps-package+xml"),
        };

        Assert.Equal("OEBPS/content.opf", EpubManifest.GetOpfFileFullPath(files));
    }

    [Fact]
    public void GetOpfFileFullPath_WithoutOpf_ReturnsEmpty() =>
        Assert.Equal("", EpubManifest.GetOpfFileFullPath([Samples.File("a.css", "text/css")]));

    [Fact]
    public void BuildContainerXml_PointsAtTheOpf()
    {
        var doc = XDocument.Parse(EpubManifest.BuildContainerXml("OPS/book.opf"));
        XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";

        var rootfile = doc.Descendants(ns + "rootfile").Single();
        Assert.Equal("OPS/book.opf", rootfile.Attribute("full-path")!.Value);
        Assert.Equal("application/oebps-package+xml", rootfile.Attribute("media-type")!.Value);
    }

    [Fact]
    public void AddOverrideCss_AddsManifestItem()
    {
        var doc = XDocument.Parse(Opf);

        Assert.True(EpubManifest.AddOverrideCss(doc, "override_v1.css"));

        XNamespace ns = "http://www.idpf.org/2007/opf";
        var item = doc.Descendants(ns + "item").Single(i => i.Attribute("id")!.Value == EpubManifest.OverrideItemId);
        Assert.Equal("override_v1.css", item.Attribute("href")!.Value);
        Assert.Equal("text/css", item.Attribute("media-type")!.Value);
    }

    [Fact]
    public void AddOverrideCss_WithoutManifest_Throws()
    {
        var doc = XDocument.Parse("""<package xmlns="http://www.idpf.org/2007/opf"/>""");

        Assert.Throws<InvalidOperationException>(() => EpubManifest.AddOverrideCss(doc, "x.css"));
    }

    [Fact]
    public void AddOverrideCss_Twice_CurrentlyAddsADuplicate()
    {
        // Documents existing behaviour: the "already added" lookup uses un-namespaced element names,
        // so it never finds the namespaced item it added earlier. Flip this test when that is fixed.
        var doc = XDocument.Parse(Opf);

        EpubManifest.AddOverrideCss(doc, "override_v1.css");
        var second = EpubManifest.AddOverrideCss(doc, "override_v1.css");

        XNamespace ns = "http://www.idpf.org/2007/opf";
        Assert.True(second);
        Assert.Equal(2, doc.Descendants(ns + "item").Count(i => i.Attribute("id")!.Value == EpubManifest.OverrideItemId));
    }

    [Fact]
    public void AddOverrideCssToFile_PersistsTheChange()
    {
        using var dir = new TempDir();
        var path = dir.Combine("content.opf");
        File.WriteAllText(path, Opf);

        EpubManifest.AddOverrideCssToFile(path, "override_v1.css");

        Assert.Contains(EpubManifest.OverrideItemId, File.ReadAllText(path));
    }

    [Fact]
    public void AddOverrideCssToFile_MissingFile_Throws()
    {
        using var dir = new TempDir();

        Assert.Throws<FileNotFoundException>(() => EpubManifest.AddOverrideCssToFile(dir.Combine("none.opf"), "x.css"));
    }
}

public class EpubManifestExistingItemTests
{
    [Fact]
    public void AddOverrideCss_WhenAnUnnamespacedItemWithTheIdExists_ReturnsFalse()
    {
        var doc = XDocument.Parse($$"""<package xmlns="http://www.idpf.org/2007/opf"><manifest><item xmlns="" id="{{EpubManifest.OverrideItemId}}" href="x.css"/></manifest></package>""");

        Assert.False(EpubManifest.AddOverrideCss(doc, "override_v1.css"));
    }

    [Fact]
    public void AddOverrideCssToFile_WhenNothingToAdd_LeavesTheFileUntouched()
    {
        using var dir = new TempDir();
        var path = dir.Combine("content.opf");
        var original = $$"""<package xmlns="http://www.idpf.org/2007/opf"><manifest><item xmlns="" id="{{EpubManifest.OverrideItemId}}" href="x.css"/></manifest></package>""";
        File.WriteAllText(path, original);

        EpubManifest.AddOverrideCssToFile(path, "override_v1.css");

        Assert.Equal(original, File.ReadAllText(path));
    }
}
