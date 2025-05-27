using System.Windows.Controls;
using System.Windows.Media;

namespace Simplicity
{
    public partial class QueueView : UserControl
    {
        private readonly QueueManager manager;

        public QueueView(QueueManager manager)
        {
            InitializeComponent();
            this.manager = manager;

            QueueList.ItemsSource = manager.Queue;
            PlaybackList.ItemsSource = manager.PlaybackList;

            PlaybackList.LayoutUpdated += (_, __) => HighlightCurrentSong();

            manager.PropertyChanged += (_, __) => HighlightCurrentSong();
        }

        private void HighlightCurrentSong()
        {
            foreach (Song song in PlaybackList.Items)
            {
                var item = (ListBoxItem?)PlaybackList.ItemContainerGenerator.ContainerFromItem(song);
                if (item != null)
                {
                    item.Background = (song == manager.CurrentSong)
                        ? new SolidColorBrush(Color.FromRgb(200, 255, 200))
                        : Brushes.Transparent;
                }
            }
        }
    }
}