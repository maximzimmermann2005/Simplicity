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
                    if (i == manager.CurrentIndex)
                    {
                        item.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200));
                    }
                    else if (i >= manager.QueueStartIndex && i < manager.QueueEndIndexExclusive)
                    {
                        item.Background = new SolidColorBrush(Color.FromRgb(230, 230, 255));
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
                playFromHere.IsEnabled = selected.Count == 1;
            }
        }

        private void PlayFromHere_Click(object sender, RoutedEventArgs e)
        {
            if (PlaybackTimeline.SelectedItems.Count == 1 &&
                PlaybackTimeline.SelectedItem is Song selected)
            {
                int index = manager.FullPlaybackList.IndexOf(selected);
                if (index != -1)
                {
                    manager.CurrentIndex = index;
                    manager.PlayCurrent();
                }
            }
        }

        private void PlayNext_Click(object sender, RoutedEventArgs e)
        {
            foreach (Song song in PlaybackTimeline.SelectedItems)
                manager.EnqueueNext(song);
        }

        private void AddToQueue_Click(object sender, RoutedEventArgs e)
        {
            foreach (Song song in PlaybackTimeline.SelectedItems)
                manager.Enqueue(song);
        }

        private void RemoveFromPlaybackList_Click(object sender, RoutedEventArgs e)
        {
            var selected = PlaybackTimeline.SelectedItems.Cast<Song>().ToList();
            foreach (var song in selected)
                manager.FullPlaybackList.Remove(song);
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
                var droppedData = e.Data.GetData(typeof(Song)) as Song;
                var target = ((FrameworkElement)e.OriginalSource).DataContext as Song;

                if (droppedData is Song dragged && target is Song dropTarget)
                {
                    int oldIndex = manager.FullPlaybackList.IndexOf(dragged);
                    int newIndex = manager.FullPlaybackList.IndexOf(dropTarget);

                    if (oldIndex != newIndex && oldIndex != -1 && newIndex != -1)
                    {
                        manager.FullPlaybackList.Move(oldIndex, newIndex);
                    }
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