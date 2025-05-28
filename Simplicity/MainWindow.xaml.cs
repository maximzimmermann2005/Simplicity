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

        private DateTime lastBackClickTime = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();

            queueManager = new QueueManager();
            folderView = new FolderView();
            folderView.SetQueueManager(queueManager);
            queueView = new QueueView(queueManager);

            MainRegionContent.Content = folderView;
            SideRegionContent.Content = queueView;

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

            NowPlayingPanel.ShowMetadata(song);
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
            if (waveOut != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Paused)
                {
                    waveOut.Play(); // Resume
                }
                // Do nothing if already playing
            }
            else
            {
                queueManager.PlayCurrent(); // Only load new audio if nothing is active
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            waveOut?.Pause();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            var timeSinceLastClick = now - lastBackClickTime;

            lastBackClickTime = now;

            if (timeSinceLastClick.TotalMilliseconds < 500)
            {
                // Double-click: go to previous song
                queueManager.Back();
            }
            else if (audioFile != null)
            {
                // Single click: restart current song
                audioFile.CurrentTime = TimeSpan.Zero;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            queueManager.Next();
        }
    }
}