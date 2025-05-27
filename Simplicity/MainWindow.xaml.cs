using NAudio.Wave;
using System;
using System.Windows;

namespace Simplicity
{
    public partial class MainWindow : Window
    {
        private IWavePlayer? waveOut;
        private AudioFileReader? audioFile;
        private FolderView folderView;
        private QueueView queueView;
        private QueueManager queueManager;

        public MainWindow()
        {
            InitializeComponent();

            queueManager = new QueueManager();
            folderView = new FolderView();
            folderView.SetQueueManager(queueManager);
            queueView = new QueueView(queueManager);

            MainRegionContent.Content = folderView;
            SideRegionContent.Content = queueView;

            folderView.SongSelected += song => queueManager.Play(song);
            folderView.FolderScanned += songs => queueManager.SetPlaybackList(songs);

            queueManager.SongChanged += PlaySong;
        }

        private void PlaySong(Song song)
        {
            DisposeAudio();

            audioFile = new AudioFileReader(song.FilePath);
            waveOut = new WaveOutEvent();
            waveOut.Init(audioFile);
            waveOut.Play();

            NowPlayingPanel.ShowMetadata(song); // Now handled globally
        }

        private void DisposeAudio()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;

            audioFile?.Dispose();
            audioFile = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            DisposeAudio();
            base.OnClosed(e);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            queueManager.PlayCurrent();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            waveOut?.Pause();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            queueManager.Back();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            queueManager.Next();
        }
    }
}