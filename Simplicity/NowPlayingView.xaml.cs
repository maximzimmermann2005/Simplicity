using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Simplicity
{
    public partial class NowPlayingView : UserControl
    {
        public NowPlayingView()
        {
            InitializeComponent();
        }

        public void ShowMetadata(Song song)
        {
            TitleText.Text = song.Title;
            ArtistText.Text = $"{song.Artist} - {song.Album}";
            AlbumArtImage.Source = song.AlbumArt;
        }

        public void Clear()
        {
            TitleText.Text = "—";
            ArtistText.Text = "—";
            AlbumArtImage.Source = null;
        }
    }
}