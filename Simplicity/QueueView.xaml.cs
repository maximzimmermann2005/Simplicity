using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Simplicity
{
    public partial class QueueView : UserControl
    {
        private readonly QueueManager manager;
        private Point _dragStartPoint;

        private InsertionAdorner? _insertionAdorner;
        private int _insertionIndex = -1;

        private int _lastAdornerIndex = -1;

        public QueueView(QueueManager manager)
        {
            InitializeComponent();
            this.manager = manager;

            PlaybackTimeline.ItemsSource = manager.FullPlaybackList;
            PlaybackTimeline.LayoutUpdated += (_, __) => HighlightCurrent();
            manager.PropertyChanged += (_, __) => HighlightCurrent();
        }

        private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
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

            var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item != null)
            {
                if (PlaybackTimeline.SelectedItems.Contains(item.DataContext) &&
                    (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == 0)
                {
                    e.Handled = true;
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
            // --- Step 1: Determine the new insertion index using a single, unified method ---
            int newInsertionIndex = -1;
            Point pos = e.GetPosition(PlaybackTimeline);

            if (PlaybackTimeline.Items.Count == 0)
            {
                newInsertionIndex = 0;
            }
            else
            {
                for (int i = 0; i < PlaybackTimeline.Items.Count; i++)
                {
                    var item = (ListBoxItem)PlaybackTimeline.ItemContainerGenerator.ContainerFromIndex(i);
                    if (item != null)
                    {
                        // The boundary is always the midpoint of the item.
                        double itemMidPointY = item.TranslatePoint(new Point(0, item.ActualHeight / 2), PlaybackTimeline).Y;
                        if (pos.Y < itemMidPointY)
                        {
                            newInsertionIndex = i;
                            break;
                        }
                    }
                }

                if (newInsertionIndex == -1)
                {
                    // If the loop finished, we are below the midpoint of the last item.
                    newInsertionIndex = PlaybackTimeline.Items.Count;
                }
            }

            // --- Step 2: Compare NEW index with LAST index and update visuals ---
            if (newInsertionIndex != _lastAdornerIndex)
            {
                RemoveInsertionAdorner();

                if (newInsertionIndex != -1)
                {
                    ListBoxItem? itemToAdorn = null;
                    bool adornAbove = true;

                    if (newInsertionIndex < PlaybackTimeline.Items.Count)
                    {
                        itemToAdorn = (ListBoxItem)PlaybackTimeline.ItemContainerGenerator.ContainerFromIndex(newInsertionIndex);
                        adornAbove = true;
                    }
                    else if (PlaybackTimeline.Items.Count > 0)
                    {
                        itemToAdorn = (ListBoxItem)PlaybackTimeline.ItemContainerGenerator.ContainerFromIndex(PlaybackTimeline.Items.Count - 1);
                        adornAbove = false;
                    }

                    if (itemToAdorn != null)
                    {
                        ShowInsertionAdorner(itemToAdorn, adornAbove);
                    }
                }
                _lastAdornerIndex = newInsertionIndex;
            }

            // --- Step 3: Handle Drop Effect & Scrolling ---
            _insertionIndex = newInsertionIndex;
            e.Effects = (_insertionIndex != -1) ? DragDropEffects.Move : DragDropEffects.None;

            var scrollViewer = FindVisualChild<ScrollViewer>(PlaybackTimeline);
            if (scrollViewer != null)
            {
                double tolerance = 25.0;
                double verticalPos = e.GetPosition(scrollViewer).Y;
                if (verticalPos < tolerance) scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 5);
                else if (verticalPos > scrollViewer.ActualHeight - tolerance) scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 5);
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

                int adjust = songs.Select(song => list.IndexOf(song))
                                  .Where(idx => idx >= 0 && idx < _insertionIndex)
                                  .Count();

                foreach (var song in songs)
                    list.Remove(song);

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
            _lastAdornerIndex = -1;
        }

        private void ShowInsertionAdorner(UIElement item, bool isAbove)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(item);
            if (adornerLayer != null)
            {
                _insertionAdorner = new InsertionAdorner(item, isAbove);
                adornerLayer.Add(_insertionAdorner);
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