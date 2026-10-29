namespace SafariBooksDownload
{
    /// <summary>Authenticated GET access to the O'Reilly APIs. Implementations throw on non-success responses.</summary>
    public interface IOReillyClient
    {
        Task<byte[]> GetBytesAsync(string url);
    }

    public static class OReillyClientExtensions
    {
        public static async Task<string> GetStringAsync(this IOReillyClient client, string url) =>
            System.Text.Encoding.UTF8.GetString(await client.GetBytesAsync(url));
    }
}
