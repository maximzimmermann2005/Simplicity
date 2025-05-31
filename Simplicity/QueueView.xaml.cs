using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using System.Windows.Documents;

namespace Simplicity
{
    public partial class QueueView : UserControl
    {
        private readonly QueueManager manager;
        private Point _dragStartPoint;

        private InsertionAdorner? _insertionAdorner;
        private int _insertionIndex = -1;
        private bool _insertionAbove;

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

            // Find the item under the mouse
            var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item != null)
            {
                // If the item is already selected and we're not holding Ctrl/Shift, prevent selection change
                if (PlaybackTimeline.SelectedItems.Contains(item.DataContext) &&
                    (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == 0)
                {
                    e.Handled = true; // Prevent ListBox from changing selection
                }
            }
        }

        private void PlaybackTimeline_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (PlaybackTimeline.SelectedItems.Count > 0)
                    {
                        var items = PlaybackTimeline.SelectedItems.Cast<Song>().ToList();
                        DragDrop.DoDragDrop(PlaybackTimeline, items, DragDropEffects.Move);
                    }
                }
            }
        }

        private void PlaybackTimeline_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            _insertionIndex = -1;
            _insertionAbove = false;

            if (e.Data.GetDataPresent(typeof(List<Song>)) || e.Data.GetDataPresent(typeof(IList)))
            {
                Point pos = e.GetPosition(PlaybackTimeline);
                int index = -1;
                bool above = false;

                for (int i = 0; i < PlaybackTimeline.Items.Count; i++)
                {
                    var item = (ListBoxItem)PlaybackTimeline.ItemContainerGenerator.ContainerFromIndex(i);
                    if (item != null)
                    {
                        var bounds = VisualTreeHelper.GetDescendantBounds(item);
                        var topLeft = item.TranslatePoint(new Point(0, 0), PlaybackTimeline);
                        var rect = new Rect(topLeft, bounds.Size);

                        if (pos.Y < rect.Top + rect.Height / 2)
                        {
                            index = i;
                            above = true;
                            break;
                        }
                        else if (pos.Y < rect.Bottom)
                        {
                            index = i + 1;
                            above = true;
                            break;
                        }
                    }
                }

                if (index == -1)
                {
                    // Dropping below the last item
                    index = PlaybackTimeline.Items.Count;
                    above = true;
                }

                _insertionIndex = index;
                _insertionAbove = above;

                ShowInsertionAdorner(index, above);
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        private void PlaybackTimeline_Drop(object sender, DragEventArgs e)
        {
            RemoveInsertionAdorner();

            IList? draggedItems = null;
            if (e.Data.GetDataPresent(typeof(List<Song>)))
                draggedItems = e.Data.GetData(typeof(List<Song>)) as IList;
            else if (e.Data.GetDataPresent(typeof(IList)))
                draggedItems = e.Data.GetData(typeof(IList)) as IList;

            if (draggedItems != null && _insertionIndex >= 0)
            {
                var songs = draggedItems.Cast<Song>().ToList();
                var list = manager.FullPlaybackList;

                // Count how many dragged items are before the insertion index
                int adjust = songs.Select(song => list.IndexOf(song))
                                  .Where(idx => idx >= 0 && idx < _insertionIndex)
                                  .Count();

                // Remove all dragged items first
                foreach (var song in songs)
                    list.Remove(song);

                // Adjust insertion index
                int insertAt = _insertionIndex - adjust;
                if (insertAt < 0) insertAt = 0;
                if (insertAt > list.Count) insertAt = list.Count;

                foreach (var song in songs)
                {
                    list.Insert(insertAt, song);
                    insertAt++;
                }
                HighlightCurrent();
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

        private void PlaybackTimeline_DragLeave(object sender, DragEventArgs e)
        {
            RemoveInsertionAdorner();
        }

        private void ShowInsertionAdorner(int itemIndex, bool above)
        {
            RemoveInsertionAdorner();

            if (itemIndex < PlaybackTimeline.Items.Count)
            {
                var item = (ListBoxItem)PlaybackTimeline.ItemContainerGenerator.ContainerFromIndex(itemIndex);
                if (item != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(item);
                    if (adornerLayer != null)
                    {
                        _insertionAdorner = new InsertionAdorner(item, true);
                        adornerLayer.Add(_insertionAdorner);
                    }
                }
            }
            else if (PlaybackTimeline.Items.Count > 0)
            {
                // Add adorner to the last item for end-of-list drop
                var item = (ListBoxItem)PlaybackTimeline.ItemContainerGenerator.ContainerFromIndex(PlaybackTimeline.Items.Count - 1);
                if (item != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(item);
                    if (adornerLayer != null)
                    {
                        _insertionAdorner = new InsertionAdorner(item, false);
                        adornerLayer.Add(_insertionAdorner);
                    }
                }
            }
        }

        private void RemoveInsertionAdorner()
        {
            if (_insertionAdorner != null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(_insertionAdorner.AdornedElement);
                if (adornerLayer != null)
                    adornerLayer.Remove(_insertionAdorner);
                _insertionAdorner = null;
            }
        }
    }
}