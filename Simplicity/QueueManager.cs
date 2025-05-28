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

        private int queuedCount = 0;
        public int QueuedCount
        {
            get => queuedCount;
            private set
            {
                if (queuedCount != value)
                {
                    queuedCount = value;
                    OnPropertyChanged(nameof(QueuedCount));
                }
            }
        }

        public Song? CurrentSong =>
            (CurrentIndex >= 0 && CurrentIndex < FullPlaybackList.Count)
                ? FullPlaybackList[CurrentIndex] : null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetPlaybackList(List<Song> songs)
        {
            FullPlaybackList.Clear();
            foreach (var song in songs)
                FullPlaybackList.Add(song);

            CurrentIndex = 0;
            QueuedCount = 0;
            PlayCurrent();
        }

        public void EnqueueNext(Song song)
        {
            if (!FullPlaybackList.Contains(song))
                return;

            FullPlaybackList.Remove(song);
            int insertAt = CurrentIndex + 1;
            FullPlaybackList.Insert(insertAt, song);
            QueuedCount++;
        }

        public void Enqueue(Song song)
        {
            if (!FullPlaybackList.Contains(song))
                return;

            FullPlaybackList.Remove(song);
            int insertAt = CurrentIndex + QueuedCount + 1;
            insertAt = Math.Min(insertAt, FullPlaybackList.Count);
            FullPlaybackList.Insert(insertAt, song);
            QueuedCount++;
        }

        public void PlayFrom(Song song)
        {
            int newIndex = FullPlaybackList.IndexOf(song);
            if (newIndex == -1)
                return;

            int relative = newIndex - CurrentIndex;

            if (relative > 0 && relative <= QueuedCount)
            {
                QueuedCount -= relative;
            }
            else
            {
                QueuedCount = 0;
            }

            CurrentIndex = newIndex;
            PlayCurrent();
        }

        public void Remove(Song song)
        {
            int index = FullPlaybackList.IndexOf(song);
            if (index == -1) return;

            int relative = index - CurrentIndex;
            FullPlaybackList.RemoveAt(index);

            if (index < CurrentIndex)
                CurrentIndex--;

            if (relative > 0 && relative <= QueuedCount)
                QueuedCount = Math.Max(QueuedCount - 1, 0);
        }

        public void MoveAndAdjustQueue(Song song, int newIndex)
        {
            int oldIndex = FullPlaybackList.IndexOf(song);
            if (oldIndex == -1 || newIndex == -1 || oldIndex == newIndex)
                return;

            int oldRelative = oldIndex - CurrentIndex;
            int newRelative = newIndex - CurrentIndex;

            FullPlaybackList.Move(oldIndex, newIndex);

            if (CurrentIndex == oldIndex)
                CurrentIndex = newIndex;

            if (oldRelative > 0 && oldRelative <= QueuedCount && (newRelative <= 0 || newRelative > QueuedCount))
                QueuedCount = Math.Max(QueuedCount - 1, 0);
            else if ((oldRelative <= 0 || oldRelative > QueuedCount) && newRelative > 0 && newRelative <= QueuedCount + 1)
                QueuedCount++;
        }

        public void Next()
        {
            if (CurrentIndex + 1 < FullPlaybackList.Count)
            {
                CurrentIndex++;
                if (QueuedCount > 0)
                    QueuedCount--;
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
                SongChanged?.Invoke(CurrentSong);
        }

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}