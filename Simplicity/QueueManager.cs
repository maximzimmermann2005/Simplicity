using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Simplicity
{
    public class QueueManager : INotifyPropertyChanged
    {
        public event Action<Song>? SongChanged;

        public ObservableCollection<Song> FullPlaybackList { get; } = new();

        private int currentIndex = -1;
        private int queueStartIndex = -1;
        private int queueEndIndexExclusive = -1;

        public int QueueStartIndex => queueStartIndex;
        public int QueueEndIndexExclusive => queueEndIndexExclusive;

        public int CurrentIndex
        {
            get => currentIndex;
            set
            {
                if (value >= 0 && value < FullPlaybackList.Count && currentIndex != value)
                {
                    currentIndex = value;
                    OnPropertyChanged(nameof(CurrentIndex));
                    OnPropertyChanged(nameof(CurrentSong));
                }
            }
        }

        public Song? CurrentSong => (CurrentIndex >= 0 && CurrentIndex < FullPlaybackList.Count)
            ? FullPlaybackList[CurrentIndex]
            : null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetPlaybackList(List<Song> songs)
        {
            FullPlaybackList.Clear();
            foreach (var song in songs)
                FullPlaybackList.Add(song);

            CurrentIndex = 0;
            PlayCurrent();
        }

        public void EnqueueNext(Song song)
        {
            int insertAt = CurrentIndex + 1;

            FullPlaybackList.Insert(insertAt, song);

            UpdateQueueBoundsAfterInsert(insertAt);
        }

        public void Enqueue(Song song)
        {
            int insertAt = (queueEndIndexExclusive > CurrentIndex)
                ? queueEndIndexExclusive
                : CurrentIndex + 1;

            FullPlaybackList.Insert(insertAt, song);

            UpdateQueueBoundsAfterInsert(insertAt);
        }

        private void UpdateQueueBoundsAfterInsert(int insertedIndex)
        {
            if (queueStartIndex == -1 || insertedIndex < queueStartIndex)
                queueStartIndex = insertedIndex;

            if (queueEndIndexExclusive == -1 || insertedIndex >= queueEndIndexExclusive)
                queueEndIndexExclusive = insertedIndex + 1;
            else
                queueEndIndexExclusive++; // Push end forward if inserted inside queue
        }

        public void Next()
        {
            if (CurrentIndex + 1 < FullPlaybackList.Count)
            {
                CurrentIndex++;

                // If we just played through the queue, shift the bounds
                if (queueStartIndex != -1 && CurrentIndex >= queueStartIndex)
                {
                    if (CurrentIndex >= queueEndIndexExclusive)
                    {
                        queueStartIndex = -1;
                        queueEndIndexExclusive = -1;
                    }
                    else
                    {
                        queueStartIndex = CurrentIndex + 1;
                    }
                }

                PlayCurrent();
            }
        }

        public void Back()
        {
            if (CurrentIndex > 0)
            {
                CurrentIndex--;
                PlayCurrent();
            }
        }

        public void PlayCurrent()
        {
            if (CurrentSong != null)
            {
                SongChanged?.Invoke(CurrentSong);
            }
        }

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}