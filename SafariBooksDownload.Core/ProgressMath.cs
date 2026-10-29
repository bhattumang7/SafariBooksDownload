namespace SafariBooksDownload
{
    public static class ProgressMath
    {
        /// <summary>Whole-number percentage of files processed so far (integer division, as shown in the UI).</summary>
        public static double FilePercent(int currentFileNo, int totalFileCount) =>
            totalFileCount <= 0 ? 0 : (currentFileNo * 100) / totalFileCount;

        /// <summary>Fraction (0..1) of file-list pages fetched. Safe for books with fewer than 20 files (zero pages).</summary>
        public static double PageFraction(int downloaded, int pageCount) =>
            pageCount <= 0 ? 0 : Math.Clamp((double)downloaded / pageCount, 0, 1);
    }
}
