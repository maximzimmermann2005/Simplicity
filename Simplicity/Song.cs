using System;
using System.Windows.Media.Imaging;

namespace Simplicity
{
    public class Song
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = "Unknown Title";
        public string Artist { get; set; } = "Unknown Artist";
        public string Album { get; set; } = "Unknown Album";
        public TimeSpan Duration { get; set; }

        public BitmapImage? AlbumArt { get; set; } // fully decoded, safe for UI

        public override string ToString() => $"{Title} - {Artist}";

    }
}