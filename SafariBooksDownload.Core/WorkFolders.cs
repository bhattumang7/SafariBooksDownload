namespace SafariBooksDownload
{
    public static class WorkFolders
    {
        public const string Marker = ".in_progress";

        /// <summary>Deletes every immediate sub-folder of <paramref name="booksPath"/> that still carries the in-progress marker.</summary>
        public static void SweepOrphans(string booksPath)
        {
            if (!Directory.Exists(booksPath))
                return;

            foreach (var dir in Directory.EnumerateDirectories(booksPath).Where(d => File.Exists(Path.Combine(d, Marker))))
            {
                try { Directory.Delete(dir, recursive: true); }
                catch { /* best-effort cleanup */ }
            }
        }
    }
}
