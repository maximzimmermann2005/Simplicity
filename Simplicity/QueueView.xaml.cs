using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Simplicity
{
    public partial class QueueView : UserControl
    {
        private readonly QueueManager manager;
        private Point _dragStartPoint;

        public QueueView(QueueManager manager)
        {
            InitializeComponent();
            this.manager = manager;

            PlaybackTimeline.ItemsSource = manager.FullPlaybackList;
            PlaybackTimeline.LayoutUpdated += (_, __) => HighlightCurrent();
            manager.PropertyChanged += (_, __) => HighlightCurrent();
        }

        private void HighlightCurrent()
        {
            for (int i = 0; i < PlaybackTimeline.Items.Count; i++)
            {
                var item = (ListBoxItem?)PlaybackTimeline.ItemContainerGenerator.ContainerFromIndex(i);
                if (item != null)
                {
                    int relative = i - manager.CurrentIndex;

                    if (relative == 0)
                    {
                        item.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200)); // Green
                    }
                    else if (relative > 0 && relative <= manager.QueuedCount)
                    {
                        item.Background = new SolidColorBrush(Color.FromRgb(230, 230, 255)); // Purple
                    }
                    else
                    {
                        item.Background = Brushes.Transparent;
                    }
                }
            }
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

        private void PlaybackTimeline_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void PlaybackTimeline_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
                    if (listBoxItem != null)
                    {
                        DragDrop.DoDragDrop(listBoxItem, listBoxItem.DataContext, DragDropEffects.Move);
                    }
                }
            }
        }

        private void PlaybackTimeline_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void PlaybackTimeline_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Song)))
            {
                var dragged = e.Data.GetData(typeof(Song)) as Song;
                var target = ((FrameworkElement)e.OriginalSource).DataContext as Song;

                if (dragged != null && target != null && dragged != target)
                {
                    var list = manager.FullPlaybackList;
                    int oldIndex = list.IndexOf(dragged);
                    int newIndex = list.IndexOf(target);

                    if (oldIndex == -1 || newIndex == -1 || oldIndex == newIndex)
                        return;

                    manager.MoveAndAdjustQueue(dragged, newIndex);
                    HighlightCurrent();
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T correctlyTyped)
                    return correctlyTyped;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}