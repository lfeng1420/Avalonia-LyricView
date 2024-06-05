using ReactiveUI;

namespace LyricView.Controls
{
    /// <summary>
    /// 歌词
    /// </summary>
    public class Lyric : ReactiveObject
    {
        /// <summary>
        /// 起始时间
        /// </summary>
        private double _timeStamp;
        public double TimeStamp
        {
            get => _timeStamp;
            set => this.RaiseAndSetIfChanged(ref _timeStamp, value);
        }

        /// <summary>
        /// 结束时间
        /// </summary>
        private double _endTimeStamp;
        public double EndTimeStamp
        {
            get => _endTimeStamp;
            set => this.RaiseAndSetIfChanged(ref _endTimeStamp, value);
        }

        /// <summary>
        /// 歌词文本
        /// </summary>
        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set => this.RaiseAndSetIfChanged(ref _text, value);
        }
    }
}
