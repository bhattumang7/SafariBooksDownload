using System.Text.Json;

namespace SafariBooksDownload
{
    public record FilesPage(int Count, string? Next, List<BookFile> Files);

    public static class OReillyApi
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private static string Text(JsonElement element, string property) =>
            element.GetProperty(property).GetString() ?? "";

        public static string SearchUrl(string searchTerm) =>
            "https://www.oreilly.com/search/api/search/?q=" + System.Web.HttpUtility.UrlEncode(searchTerm) +
            "&type=book&rows=20&language_with_transcripts=en&tzOffset=-5.5&feature_flags=improveSearchFilters&report=true&isTopics=false";

        public static string BookDetailsUrl(string productId) =>
            "https://learning.oreilly.com/api/v2/epubs/urn:orm:book:" + productId + "/";

        public static string ChaptersUrl(string productId) =>
            "https://learning.oreilly.com/api/v2/epub-chapters/?epub_identifier=urn:orm:book:" + productId;

        public static List<Book> ParseSearchResults(string json)
        {
            using var document = JsonDocument.Parse(json);
            var products = document.RootElement.GetProperty("data").GetProperty("products");

            var books = new List<Book>();
            foreach (var entry in products.EnumerateArray())
            {
                books.Add(new Book
                {
                    title = Text(entry, "title"),
                    product_id = Text(entry, "product_id"),
                    cover_image = Text(entry, "cover_image"),
                    description = Text(entry, "description"),
                    url = Text(entry, "url"),
                    authors = string.Join(", ",
                        entry.GetProperty("authors").EnumerateArray().Select(author => author.GetString() ?? "")),
                    publication_date = Text(entry.GetProperty("custom_attributes"), "publication_date")
                });
            }

            return books;
        }

        public static void PopulateBookDetails(Book book, string json)
        {
            using var document = JsonDocument.Parse(json);
            book.isbn = Text(document.RootElement, "isbn");
            book.table_of_contents = Text(document.RootElement, "table_of_contents");
            book.files_URL = Text(document.RootElement, "files");
        }

        public static List<ChappterInfo> ParseChapters(string json)
        {
            using var document = JsonDocument.Parse(json);
            var results = document.RootElement.GetProperty("results");
            return JsonSerializer.Deserialize<List<ChappterInfo>>(results, JsonOptions)
                   ?? throw new InvalidOperationException("Chapter list was null.");
        }

        public static FilesPage ParseFilesPage(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            int count = root.GetProperty("count").GetInt32();
            var files = JsonSerializer.Deserialize<List<BookFile>>(root.GetProperty("results"), JsonOptions)
                        ?? new List<BookFile>();

            string? next = null;
            if (root.TryGetProperty("next", out var nextElement) && nextElement.ValueKind == JsonValueKind.String)
            {
                next = nextElement.GetString();
            }

            return new FilesPage(count, next, files);
        }
    }
}
