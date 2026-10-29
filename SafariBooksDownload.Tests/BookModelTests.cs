namespace SafariBooksDownload.Tests;

public class BookTests
{
    [Fact]
    public void FileNameSafeTitle_ReplacesInvalidCharactersAndColons()
    {
        var book = new Book { title = "C#: A/B Guide" };

        Assert.Equal("C#_ A_B Guide", book.getTitle_file_name_safe());
    }

    [Fact]
    public void NewBook_HasEmptyFileList() => Assert.Empty(new Book().fileList);
}

public class PathAdjusterTests
{
    private readonly PathAdjuster _adjuster = new("123");

    [Fact]
    public void FullUrls_ArePointedAtTheBookRoot()
    {
        var html = "<img src=\"https://learning.oreilly.com/api/v2/epubs/urn:orm:book:123/files/img/a.png\"/>";

        Assert.Equal("<img src=\"img/a.png\"/>", _adjuster.AdjustPathsInHtml(html));
    }

    [Fact]
    public void RootRelativeUrls_AreRebasedToSlash()
    {
        var html = "<a href=\"/api/v2/epubs/urn:orm:book:123/files/ch2.xhtml\">x</a>";

        Assert.Equal("<a href=\"/ch2.xhtml\">x</a>", _adjuster.AdjustPathsInHtml(html));
    }
}

public class JsonNodeProcessorTests
{
    private static JsonNodeInfo Node(string url, int depth, params JsonNodeInfo[] children) =>
        new() { url = url, depth = depth, children = children.ToList() };

    [Fact]
    public void AssignOrder_NumbersNodesDepthFirst_AndTracksDepthAndUrls()
    {
        var tree = new List<JsonNodeInfo>
        {
            Node("a", 1, Node("a1", 2, Node("a1x", 3)), Node("a2", 2)),
            Node("b", 1),
        };
        var processor = new JsonNodeProcessor();

        processor.AssignOrder(tree);

        Assert.Equal(0, tree[0].order);
        Assert.Equal(1, tree[0].children[0].order);
        Assert.Equal(2, tree[0].children[0].children[0].order);
        Assert.Equal(3, tree[0].children[1].order);
        Assert.Equal(4, tree[1].order);
        Assert.Equal(3, processor.getMaxDepth());
        Assert.Equal(5, processor.getUniquePageList().Count);
    }
}

public class ProgressMathTests
{
    [Theory]
    [InlineData(1, 100, 1)]
    [InlineData(50, 100, 50)]
    [InlineData(8, 30, 26)]
    [InlineData(5, 0, 0)]
    public void FilePercent_UsesIntegerDivision(int current, int total, double expected) =>
        Assert.Equal(expected, ProgressMath.FilePercent(current, total));

    [Fact]
    public void PageFraction_WithZeroPages_DoesNotDivideByZero() => Assert.Equal(0, ProgressMath.PageFraction(1, 0));

    [Theory]
    [InlineData(1, 4, 0.25)]
    [InlineData(4, 4, 1)]
    [InlineData(9, 4, 1)]
    public void PageFraction_IsClampedToUnitRange(int done, int pages, double expected) =>
        Assert.Equal(expected, ProgressMath.PageFraction(done, pages));
}
