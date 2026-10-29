using System.Xml.Linq;

namespace SafariBooksDownload
{
    public static class EpubManifest
    {
        public const string OverrideItemId = "Oververyowncustomoverrideumang";
        private static readonly XNamespace OpfNamespace = "http://www.idpf.org/2007/opf";

        public static string GetOpfFileFullPath(IEnumerable<BookFile> bookFiles) =>
            bookFiles.FirstOrDefault(file => file.full_path.EndsWith(".opf"))?.full_path ?? "";

        public static string BuildContainerXml(string opfPath) =>
            $"""<?xml version="1.0"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="{opfPath}" media-type="application/oebps-package+xml" /></rootfiles></container>""";

        /// <summary>Adds the override css item to the OPF manifest. Returns false if it was already present.</summary>
        public static bool AddOverrideCss(XDocument opf, string cssFileName)
        {
            var manifest = opf.Root!.Element(OpfNamespace + "manifest")
                           ?? throw new InvalidOperationException("OPF has no manifest element.");

            // Note: matches un-namespaced "item" elements, so items in the OPF namespace are never found.
            var existingItem = manifest.Elements("item")
                .FirstOrDefault(e => e.Attribute("id")?.Value == OverrideItemId);
            if (existingItem != null)
            {
                return false;
            }

            manifest.Add(new XElement(OpfNamespace + "item",
                new XAttribute("id", OverrideItemId),
                new XAttribute("href", cssFileName),
                new XAttribute("media-type", "text/css")));
            return true;
        }

        public static void AddOverrideCssToFile(string opfFilePath, string cssFileName)
        {
            if (!File.Exists(opfFilePath))
            {
                throw new FileNotFoundException(opfFilePath);
            }

            var opf = XDocument.Load(opfFilePath);
            if (AddOverrideCss(opf, cssFileName))
            {
                using var stream = File.Create(opfFilePath);
                opf.Save(stream);
            }
        }
    }
}
