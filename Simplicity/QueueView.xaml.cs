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
                        item.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200)); // Green highlight
                    }
                    else if (i >= manager.QueueStartIndex && i < manager.QueueEndIndexExclusive)
                    {
                        item.Background = new SolidColorBrush(Color.FromRgb(230, 230, 255)); // Light blue for queue
                    }
                    else
                    {
                        item.Background = Brushes.Transparent;
                    }
                }
            }
        }
    }
}