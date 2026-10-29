namespace SafariBooksDownload
{
    /// <summary>Production <see cref="IOReillyClient"/>: cookie-authenticated requests via <see cref="ORiellyHttpClientAdapter"/>.</summary>
    public class OReillyClient : IOReillyClient
    {
        public async Task<byte[]> GetBytesAsync(string url)
        {
            var response = await new ORiellyHttpClientAdapter().GetAsync(url)
                           ?? throw new InvalidOperationException($"Failed to fetch {url} — null response (session may be expired).");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
