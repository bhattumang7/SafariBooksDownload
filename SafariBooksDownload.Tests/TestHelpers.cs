using System.Text;

namespace SafariBooksDownload.Tests;

internal sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sbd-tests-" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Path);

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
    }
}

/// <summary>In-memory <see cref="IOReillyClient"/> keyed by URL; records every request.</summary>
internal sealed class FakeClient : IOReillyClient
{
    private readonly Dictionary<string, byte[]> _responses = new();
    public List<string> Requests { get; } = new();

    public FakeClient Add(string url, string body) { _responses[url] = Encoding.UTF8.GetBytes(body); return this; }
    public FakeClient Add(string url, byte[] body) { _responses[url] = body; return this; }

    public Task<byte[]> GetBytesAsync(string url)
    {
        lock (Requests) Requests.Add(url);
        return _responses.TryGetValue(url, out var body)
            ? Task.FromResult(body)
            : throw new HttpRequestException($"No fake response for {url}");
    }
}

internal static class Samples
{
    public const string Base = "https://learning.oreilly.com/api/v2/epubs/urn:orm:book:123/files/";

    public static BookFile File(string path, string mediaType) => new()
    {
        url = Base + path,
        full_path = path,
        media_type = mediaType,
    };

    public static Book BookWith(params BookFile[] files)
    {
        var book = new Book { title = "My: Book/Title", product_id = "123" };
        book.fileList.AddRange(files);
        return book;
    }
}
