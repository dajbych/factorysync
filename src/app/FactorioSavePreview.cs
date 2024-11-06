using System.Drawing;
using System.IO.Compression;
using System.Linq;

namespace Dajbych.FactorySync {

    /// <summary>
    /// Factorio save game screen loader.
    /// </summary>
    public class FactorioSavePreview {

        public FactorioSavePreview(ZipArchive archive) {
            var preview = archive.Entries.Where(e => e.Name == "preview.jpg").SingleOrDefault() ?? archive.Entries.Where(e => e.Name == "preview.png").SingleOrDefault();
            if (preview != null) {
                using (var bitmapStream = preview.Open()) {
                    Image = new Bitmap(bitmapStream);
                }
            }
        }

        public Bitmap Image { get; }

    }

}