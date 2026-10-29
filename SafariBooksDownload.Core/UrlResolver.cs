namespace SafariBooksDownload
{
    public static class UrlResolver
    {
        /// <summary>
        /// Resolves <paramref name="toPath"/> (absolute URL, or a path that is the tail of one of the
        /// book's file URLs) relative to <paramref name="fromPath"/>. Unknown relative paths are returned unchanged.
        /// </summary>
        public static string GetRelativePath(IEnumerable<BookFile> bookFiles, string fromPath, string toPath)
        {
            var fromUri = new Uri(fromPath);

            if (!Uri.IsWellFormedUriString(toPath, UriKind.Absolute))
            {
                var match = bookFiles.FirstOrDefault(file => file.url.EndsWith(toPath));
                if (match == null)
                {
                    return toPath;
                }

                toPath = match.url;
            }

            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString())
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}
