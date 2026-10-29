using System.IO.Compression;

namespace SafariBooksDownload.Tests;

public class WorkFoldersTests
{
    [Fact]
    public void SweepOrphans_RemovesOnlyFoldersWithTheMarker()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Combine("orphan"));
        File.WriteAllText(dir.Combine("orphan", WorkFolders.Marker), "");
        File.WriteAllText(dir.Combine("orphan", "chapter.xhtml"), "x");
        Directory.CreateDirectory(dir.Combine("finished"));
        File.WriteAllText(dir.Combine("finished", "chapter.xhtml"), "x");
        File.WriteAllText(dir.Combine("book.epub"), "x");

        WorkFolders.SweepOrphans(dir.Path);

        Assert.False(Directory.Exists(dir.Combine("orphan")));
        Assert.True(Directory.Exists(dir.Combine("finished")));
        Assert.True(File.Exists(dir.Combine("book.epub")));
    }

    [Fact]
    public void SweepOrphans_MissingBooksPath_IsANoOp()
    {
        using var dir = new TempDir();

        var missing = dir.Combine("does-not-exist");

        WorkFolders.SweepOrphans(missing);

        Assert.False(Directory.Exists(missing));
    }
}

public class EpubPackagerTests
{
    private static string MakeFolder(TempDir dir)
    {
        var folder = dir.Combine("123");
        Directory.CreateDirectory(Path.Combine(folder, "META-INF"));
        File.WriteAllText(Path.Combine(folder, "META-INF", "container.xml"), "<c/>");
        File.WriteAllText(Path.Combine(folder, "ch01.xhtml"), "<p/>");
        return folder;
    }

    [Fact]
    public void Package_CreatesEpubWithPreferredName_AndLeavesNoZip()
    {
        using var dir = new TempDir();
        var folder = MakeFolder(dir);

        var epub = EpubPackager.Package(folder, "123", "My Book");

        Assert.Equal(dir.Combine("My Book.epub"), epub);
        Assert.True(File.Exists(epub));
        Assert.False(File.Exists(dir.Combine("123.zip")));
        Assert.False(File.Exists(dir.Combine("123.epub")));
        using var zip = ZipFile.OpenRead(epub);
        Assert.Contains(zip.Entries, e => e.FullName == "ch01.xhtml");
        Assert.Contains(zip.Entries, e => e.FullName == "META-INF/container.xml");
    }

    [Fact]
    public void Package_OverwritesAnExistingEpub()
    {
        using var dir = new TempDir();
        var folder = MakeFolder(dir);
        File.WriteAllText(dir.Combine("My Book.epub"), "stale");

        var epub = EpubPackager.Package(folder, "123", "My Book");

        using var zip = ZipFile.OpenRead(epub);
        Assert.NotEmpty(zip.Entries);
    }

    [Fact]
    public void Package_WhenRenameFails_FallsBackToProductIdName()
    {
        using var dir = new TempDir();
        var folder = MakeFolder(dir);

        // A directory squatting on the target name makes the rename fail.
        Directory.CreateDirectory(dir.Combine("Blocked.epub"));
        File.WriteAllText(dir.Combine("Blocked.epub", "keep"), "x");

        var epub = EpubPackager.Package(folder, "123", "Blocked");

        Assert.Equal(dir.Combine("123.epub"), epub);
        Assert.True(File.Exists(epub));
    }
}
