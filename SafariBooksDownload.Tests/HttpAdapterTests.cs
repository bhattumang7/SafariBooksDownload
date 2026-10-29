using System.Net;
using System.Text;

namespace SafariBooksDownload.Tests;

public class ORiellyHttpClientAdapterTests
{
    private static string CookieFile(TempDir dir, string json = """{"existing":"1"}""")
    {
        var path = dir.Combine("cookies.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static void Write(HttpListenerResponse response, string body, int status = 200)
    {
        response.StatusCode = status;
        var bytes = Encoding.UTF8.GetBytes(body);
        response.OutputStream.Write(bytes);
    }

    [Fact]
    public void Constructor_MissingCookieFile_Throws()
    {
        using var dir = new TempDir();

        Assert.Throws<FileNotFoundException>(() => new ORiellyHttpClientAdapter(dir.Combine("none.json")));
    }

    [Fact]
    public void Constructor_NullJsonCookieFile_IsTreatedAsEmpty()
    {
        using var dir = new TempDir();

        Assert.NotNull(new ORiellyHttpClientAdapter(CookieFile(dir, "null")));
    }

    [Fact]
    public async Task GetAsync_ReturnsBody_AndSendsRefererAndAcceptHeaders()
    {
        using var dir = new TempDir();
        using var server = new LoopbackServer((_, response) => Write(response, "hello"));

        var response = await new ORiellyHttpClientAdapter(CookieFile(dir)).GetAsync(server.BaseUrl + "x");

        Assert.NotNull(response);
        Assert.Equal("hello", await response.Content.ReadAsStringAsync());
        var request = Assert.Single(server.Requests);
        Assert.Equal("https://learning.oreilly.com/", request.Headers["Referer"]);
        Assert.Equal("*/*", request.Headers["Accept"]);
    }

    [Theory]
    [InlineData(302)]
    [InlineData(404)]
    public async Task GetAsync_StatusOf300OrMore_ThrowsSessionExpired(int status)
    {
        using var dir = new TempDir();
        using var server = new LoopbackServer((_, response) => Write(response, "", status));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ORiellyHttpClientAdapter(CookieFile(dir)).GetAsync(server.BaseUrl));

        Assert.Contains(status.ToString(), error.Message);
    }

    [Fact]
    public async Task GetAsync_SetCookie_IsPersistedAlongsideExistingCookies()
    {
        using var dir = new TempDir();
        var file = CookieFile(dir);
        using var server = new LoopbackServer((_, response) =>
        {
            response.AppendHeader("Set-Cookie", "sid=abc123; Path=/; HttpOnly");
            response.AppendHeader("Set-Cookie", "malformed=a=b");
            Write(response, "ok");
        });

        await new ORiellyHttpClientAdapter(file).GetAsync(server.BaseUrl);

        var saved = File.ReadAllText(file);
        Assert.Contains("\"sid\":\"abc123\"", saved);
        Assert.Contains("\"existing\":\"1\"", saved);
        Assert.DoesNotContain("malformed", saved);
    }

    [Fact]
    public async Task GetAsync_ResponseWithoutCookies_LeavesTheFileUntouched()
    {
        using var dir = new TempDir();
        var file = CookieFile(dir, """{"existing": "1"}""");
        using var server = new LoopbackServer((_, response) => Write(response, "ok"));

        await new ORiellyHttpClientAdapter(file).GetAsync(server.BaseUrl);

        Assert.Equal("""{"existing": "1"}""", File.ReadAllText(file));
    }

    [Fact]
    public async Task GetAsync_ServerDown_ReturnsNull()
    {
        using var dir = new TempDir();

        var response = await new ORiellyHttpClientAdapter(CookieFile(dir))
            .GetAsync($"http://127.0.0.1:{LoopbackServer.FreePort()}/");

        Assert.Null(response);
    }

    [Fact]
    public async Task PostAsync_SendsBody_AndReturnsResponse()
    {
        using var dir = new TempDir();
        string? received = null;
        using var server = new LoopbackServer((request, response) =>
        {
            received = new StreamReader(request.InputStream).ReadToEnd();
            Write(response, "posted");
        });

        var response = await new ORiellyHttpClientAdapter(CookieFile(dir))
            .PostAsync(server.BaseUrl, new StringContent("payload"));

        Assert.Equal("posted", await response!.Content.ReadAsStringAsync());
        Assert.Equal("payload", received);
    }

    [Fact]
    public async Task PostAsync_SetCookie_IsPersisted()
    {
        using var dir = new TempDir();
        var file = CookieFile(dir);
        using var server = new LoopbackServer((_, response) =>
        {
            response.AddHeader("Set-Cookie", "token=t1");
            Write(response, "ok");
        });

        await new ORiellyHttpClientAdapter(file).PostAsync(server.BaseUrl, new StringContent(""));

        Assert.Contains("\"token\":\"t1\"", File.ReadAllText(file));
    }

    [Fact]
    public async Task PostAsync_ServerDown_ReturnsNull()
    {
        using var dir = new TempDir();

        var response = await new ORiellyHttpClientAdapter(CookieFile(dir))
            .PostAsync($"http://127.0.0.1:{LoopbackServer.FreePort()}/", new StringContent(""));

        Assert.Null(response);
    }
}

public class OReillyClientTests
{
    [Fact]
    public async Task GetBytesAsync_ReturnsTheResponseBytes()
    {
        using var dir = new TempDir();
        var cookies = dir.Combine("c.json");
        File.WriteAllText(cookies, "{}");
        using var server = new LoopbackServer((_, response) => response.OutputStream.Write([1, 2, 3]));

        var bytes = await new OReillyClient(cookies).GetBytesAsync(server.BaseUrl);

        Assert.Equal([1, 2, 3], bytes);
    }

    [Fact]
    public async Task GetBytesAsync_WhenUnreachable_ThrowsWithTheUrl()
    {
        using var dir = new TempDir();
        var cookies = dir.Combine("c.json");
        File.WriteAllText(cookies, "{}");
        var url = $"http://127.0.0.1:{LoopbackServer.FreePort()}/";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new OReillyClient(cookies).GetBytesAsync(url));

        Assert.Contains(url, error.Message);
    }
}
