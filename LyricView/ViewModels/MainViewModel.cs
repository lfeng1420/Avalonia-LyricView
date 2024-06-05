using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using LyricView.Controls;
using ReactiveUI;

namespace LyricView.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private ObservableCollection<Lyric>? _lyrics;
        public ObservableCollection<Lyric>? Lyrics
        {
            get => _lyrics;
            set => this.RaiseAndSetIfChanged(ref _lyrics, value);
        }

        private double _currentTime = 924;
        public double CurrentTime
        {
            get => _currentTime;
            set => this.RaiseAndSetIfChanged(ref _currentTime, value);
        }

        public string TimerBtnText
        {
            get => _timer.IsEnabled ? "Stop timer" : "Start timer";
        }


        private readonly string[] strings =
            {
                "No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same, No two people are the same",
                "Stand beside you, but just far enough away",
                "Last night we were drinking",
                "Tried to think away the pain",
                "Made that age-old mistake",
                "Tried to disconnect my body",
                "From my soul, from my soul",
                "See, I feel alright already on my own",
                "Can you let me be",
                "Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?Intoxicated on my own?",
                "Do I need to answer",
                "Or right my wrongs?",
                "Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?Am I home if I don't know this place?",
                "And I've been feeling alienated",
                "On my spaceship alone",
                "Say goodbye to the past",
                "Leave it all with a laugh",
                "'Cause you always was right all along'Cause you always was right all along'Cause you always was right all along'Cause you always was right all along'Cause you always was right all along'Cause you always was right all along'Cause you always was right all along'Cause you always was right all along'Cause you always was right all along",
                "Know my reasons for the pain",
                "But if you brought it in front of me",
                "I know I'd do it all again",
                "Call them beer can gains",
                "I know from all the years",
                "That my feelings never change",
                "Can you let me be",
                "Intoxicated on my own?",
                "Do I need to answer",
                "Or right my wrongs?",
                "Am I home if I don't know this place?",
                "And I've been feeling alienated",
                "On my spaceship alone",
                "Say goodbye to the past",
                "Leave it all with a laugh",
                "'Cause you always was right all along",
                "Did the winds make the noise of change?",
                "Can the wings on your skin help you fly away?",
                "'Cause it's always raining",
                "And the clouds are always grey when you're away",
                "Yeah, I've been feeling alienated",
                "On my spaceship alone",
                "Say goodbye to the past",
                "Leave it all with a laugh",
                "'Cause you always was right all along",
                "All along",
                "Say goodbye to the past",
                "Say goodbye",
            };

        private DispatcherTimer _timer = new(DispatcherPriority.ApplicationIdle);

        public MainViewModel()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += (s, e) => PushTimeCommand(null);
            ReloadCommand(1);
        }

        public void Shuffle<T>(IList<T> list, Random r)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = r.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public void ReloadCommand(object? param)
        {
            Random r = new Random();
            var finalList = new List<Lyric>();
            for (int index = 0; index < 20; ++index)
            {
                finalList.Add(new() { TimeStamp = 0, EndTimeStamp = 0 });
            }

            for (int index = 0; index < 10; ++index)
            {
                var list = new List<string>(strings);
                Shuffle(list, r);
                list.ForEach(x => finalList.Add(new() { TimeStamp = 8 * (finalList.Count - 20), EndTimeStamp = 8 * ((finalList.Count - 20) + 1), Text = $"{(finalList.Count - 20)} - {x} - {8 * (finalList.Count - 20)}" }));
            }

            for (int index = 0; index < 20; ++index)
            {
                finalList.Add(new() { TimeStamp = 0, EndTimeStamp = 0 });
            }

            Lyrics = new(finalList);
            if (param is null)
            {
                CurrentTime = 0;
            }
        }

        public void PushTimeCommand(object? param)
        {
            CurrentTime += Random.Shared.Next(1, 9);
            _timer.Stop();
            _timer.Interval = TimeSpan.FromMilliseconds(Random.Shared.Next(50, 1000));
            _timer.Start();
        }

        public void SwitchTimerCommand(object? param)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
            else
            {
                _timer.Start();
            }

            this.RaisePropertyChanged(nameof(TimerBtnText));
        }
    }
}
