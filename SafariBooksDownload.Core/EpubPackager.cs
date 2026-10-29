using System.IO.Compression;

namespace SafariBooksDownload
{
    public static class EpubPackager
    {
        /// <summary>
        /// Zips <paramref name="epubFolder"/> next to itself as {folder name}.epub, then tries to rename it to
        /// <paramref name="preferredFileName"/>.epub. Falls back to the first name if the rename fails.
        /// </summary>
        public static string Package(string epubFolder, string productId, string preferredFileName)
        {
            var parentDirectory = Path.GetDirectoryName(epubFolder)
                                  ?? throw new ArgumentException("Folder has no parent directory.", nameof(epubFolder));
            var zipPath = Path.Combine(parentDirectory, productId + ".zip");
            var epubPath = Path.Combine(parentDirectory, productId + ".epub");

            DeleteFileIfExists(zipPath);
            CreateZip(epubFolder, zipPath);

            DeleteFileIfExists(epubPath);
            File.Move(zipPath, epubPath);

            try
            {
                string newEpubPath = Path.Combine(parentDirectory, preferredFileName + ".epub");
                DeleteFileIfExists(newEpubPath);
                File.Move(epubPath, newEpubPath);
                return newEpubPath;
            }
            catch (Exception)
            {
                // keep using the older path
                return epubPath;
            }
        }

        // Zips the folder without the in-progress marker, which has to stay on disk until packaging succeeds.
        private static void CreateZip(string folder, string zipPath)
        {
            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/');
                if (entryName == WorkFolders.Marker)
                {
                    continue;
                }

                zip.CreateEntryFromFile(file, entryName);
            }
        }

        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
