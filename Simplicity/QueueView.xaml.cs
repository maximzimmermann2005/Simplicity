using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Simplicity
{
    /// <summary>
    /// Interaction logic for QueueView.xaml
    /// </summary>
    public partial class QueueView : UserControl
    {
        private readonly QueueManager manager;

        public QueueView(QueueManager manager)
        {
            InitializeComponent();
            this.manager = manager;

            PlaybackTimeline.ItemsSource = manager.FullPlaybackList;
        }

        private void PlaybackTimeline_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selected = PlaybackTimeline.SelectedItems.Cast<Song>().ToList();

            if (PlaybackTimelineContextMenu != null)
            {
                var playFromHere = (MenuItem)PlaybackTimelineContextMenu.Items[0];
                var playNext = (MenuItem)PlaybackTimelineContextMenu.Items[1];
                var moveToQueue = (MenuItem)PlaybackTimelineContextMenu.Items[2];

                bool allInQueue = selected.All(song =>
                {
                    int relative = manager.FullPlaybackList.IndexOf(song) - manager.CurrentIndex;
                    return relative > 0 && relative <= manager.QueuedCount;
                });

                playFromHere.Visibility = selected.Count == 1 ? Visibility.Visible : Visibility.Collapsed;
                moveToQueue.Visibility = allInQueue ? Visibility.Collapsed : Visibility.Visible;
                playNext.Visibility = Visibility.Visible;
            }
        }

        private void PlayFromHere_Click(object sender, RoutedEventArgs e)
        {
            if (PlaybackTimeline.SelectedItems.Count == 1 &&
                PlaybackTimeline.SelectedItem is Song selected)
            {
                manager.PlayFrom(selected);
            }
        }

        private void PlayNext_Click(object sender, RoutedEventArgs e)
        {
            var selected = PlaybackTimeline.SelectedItems.Cast<Song>().ToList();

            foreach (var song in selected.Reverse<Song>())
            {
                manager.EnqueueNext(song);
            }
        }

        private void AddToQueue_Click(object sender, RoutedEventArgs e)
        {
            var selected = PlaybackTimeline.SelectedItems.Cast<Song>().ToList();

            foreach (var song in selected)
            {
                manager.Enqueue(song);
            }
        }

        private void RemoveFromPlaybackList_Click(object sender, RoutedEventArgs e)
        {
            var selected = PlaybackTimeline.SelectedItems.Cast<Song>().ToList();
            foreach (var song in selected)
            {
                manager.Remove(song);
            }
        }
    }
}
