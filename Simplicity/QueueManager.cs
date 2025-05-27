using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Simplicity
{
    public class QueueManager : INotifyPropertyChanged
    {
        public ObservableCollection<Song> Queue { get; } = new();
        public ObservableCollection<Song> PlaybackList { get; } = new();

        private int currentPlaybackIndex = -1;
        private Song? currentSong;
        public Song? CurrentSong
        {
            get => currentSong;
            private set
            {
                currentSong = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSong)));
            }
        }

        public event Action<Song>? SongChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetPlaybackList(List<Song> songs)
        {
            Queue.Clear();
            PlaybackList.Clear();
            foreach (var song in songs)
                PlaybackList.Add(song);
            currentPlaybackIndex = -1;
            Next();
        }

        public void Play(Song song)
        {
            CurrentSong = song;
            SongChanged?.Invoke(song);
        }

        public void PlayCurrent()
        {
            if (CurrentSong != null)
                SongChanged?.Invoke(CurrentSong);
        }

        public void Next()
        {
            if (Queue.Count > 0)
            {
                var next = Queue[0];
                Queue.RemoveAt(0);
                Play(next);
                return;
            }

            if (currentPlaybackIndex + 1 < PlaybackList.Count)
            {
                currentPlaybackIndex++;
                Play(PlaybackList[currentPlaybackIndex]);
            }
            else
            {
                CurrentSong = null;
            }
        }

        public void Back()
        {
            if (currentPlaybackIndex > 0)
            {
                currentPlaybackIndex--;
                Play(PlaybackList[currentPlaybackIndex]);
            }
        }
        public void Enqueue(Song song)
        {
            Queue.Add(song);
        }

        public void EnqueueNext(Song song)
        {
            Queue.Insert(0, song);
        }

    }
}