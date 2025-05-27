using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Simplicity
{
    public partial class FolderView : UserControl
    {
        public event Action<Song>? SongSelected;
        public event Action<List<Song>>? FolderScanned;

        public List<Song> LoadedSongs { get; private set; } = new();

        public FolderView()
        {
            InitializeComponent();
        }

        private async void ScanFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var files = Directory.GetFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".mp3") || f.EndsWith(".wav") || f.EndsWith(".flac"))
                .ToList();

            LoadedSongs.Clear();
            LibraryList.ItemsSource = null;

            ScanProgressBar.Visibility = Visibility.Visible;
            ScanStatusText.Visibility = Visibility.Visible;
            ScanProgressBar.Minimum = 0;
            ScanProgressBar.Maximum = files.Count;
            ScanProgressBar.Value = 0;

            int completed = 0;
            var readySongs = new ConcurrentBag<Song>();
            var progress = new Progress<int>(i =>
            {
                ScanProgressBar.Value = i;
                ScanStatusText.Text = $"Loading songs... ({i}/{files.Count})";
            });

            await Task.Run(() =>
            {
                Parallel.ForEach(files, file =>
                {
                    var song = LoadAndDecodeSong(file);
                    readySongs.Add(song);
                    ((IProgress<int>)progress).Report(++completed);
                });
            });

            var sorted = readySongs.OrderBy(s => s.Title).ToList();
            LoadedSongs.AddRange(sorted);
            LibraryList.ItemsSource = LoadedSongs;

            ScanProgressBar.Visibility = Visibility.Collapsed;
            ScanStatusText.Visibility = Visibility.Collapsed;

            // Notify that the folder scan is complete
            FolderScanned?.Invoke(LoadedSongs);
        }

        private Song LoadAndDecodeSong(string path)
        {
            var song = new Song { FilePath = path };

            try
            {
                var tagFile = TagLib.File.Create(path);
                song.Title = tagFile.Tag.Title ?? song.Title;
                song.Artist = tagFile.Tag.FirstPerformer ?? song.Artist;
                song.Album = tagFile.Tag.Album ?? song.Album;
                song.Duration = tagFile.Properties.Duration;

                if (tagFile.Tag.Pictures.Length > 0)
                {
                    var picData = tagFile.Tag.Pictures[0].Data.Data;
                    using var ms = new MemoryStream(picData);
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    song.AlbumArt = image;
                }
            }
            catch { }

            return song;
        }

        private void LibraryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LibraryList.SelectedItem is Song song)
            {
                SongSelected?.Invoke(song);
            }
        }
    }
}