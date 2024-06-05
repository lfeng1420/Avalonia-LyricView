using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ReactiveUI;

namespace LyricView.Controls
{
    public partial class LyricView : UserControl
    {
        public static readonly StyledProperty<double> CurrentTimeProperty = AvaloniaProperty.Register<LyricView, double>(nameof(CurrentTime), -1, false, Avalonia.Data.BindingMode.TwoWay);
        public static readonly StyledProperty<double> ScrollDurationProperty = AvaloniaProperty.Register<LyricView, double>(nameof(ScrollDuration));
        public static readonly StyledProperty<double> DelayProperty = AvaloniaProperty.Register<LyricView, double>(nameof(Delay));
        public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty = AvaloniaProperty.Register<LyricView, IEnumerable?>(nameof(ItemsSource));
        public static readonly StyledProperty<string> DefClassesProperty = AvaloniaProperty.Register<LyricView, string>(nameof(DefClasses));
        public static readonly StyledProperty<string> SelectedClassesProperty = AvaloniaProperty.Register<LyricView, string>(nameof(SelectedClasses));

        /// <summary>
        /// 当前时间
        /// </summary>
        public double CurrentTime
        {
            get => GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        /// <summary>
        /// 第一次滚动延迟时间（毫秒）
        /// </summary>
        public double Delay
        {
            get => GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        /// <summary>
        /// 滚动速度
        /// </summary>
        public double ScrollDuration
        {
            get => GetValue(ScrollDurationProperty);
            set => SetValue(ScrollDurationProperty, value);
        }

        /// <summary>
        /// 对应ItemsControl中的ItemSource
        /// </summary>
        public IEnumerable? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string DefClasses
        {
            get => GetValue(DefClassesProperty);
            set => SetValue(DefClassesProperty, value);
        }

        public string SelectedClasses
        {
            get => GetValue(SelectedClassesProperty);
            set => SetValue(SelectedClassesProperty, value);
        }

        private const double _scrollDurationOffset = 100;
        private bool _attachedVisualTree = false;
        private DateTime _invisibleEndTime = DateTime.Now;
        private int _curIndex = -1;
        private ConcurrentStack<int> _selectedIndexs = new();
        private DispatcherTimer _timer = new(DispatcherPriority.ApplicationIdle);
        private DateTime _autoScrollEndTime = DateTime.Now;
        private int _scrollingIndex = -1;
        private bool _firstScrollDone = false;


        public LyricView()
        {
            InitializeComponent();

            // 订阅拖拽滚动条事件
            Thumb.DragStartedEvent.AddClassHandler<ScrollBar>(scrollBar_DragStarted, RoutingStrategies.Bubble);
            Thumb.DragDeltaEvent.AddClassHandler<ScrollBar>(scrollBar_OnThumbDrag, RoutingStrategies.Bubble);
            Thumb.DragCompletedEvent.AddClassHandler<ScrollBar>(scrollBar_DragCompleted, RoutingStrategies.Bubble);
            // 订阅鼠标滚轮事件
            PointerWheelChangedEvent.AddClassHandler<InputElement>(onPointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct);

            // timer
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += (s, e) => onTimerTick();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay((int)ScrollDuration);
                _attachedVisualTree = true;

                int index = getIndexOfCurTime(CurrentTime);
                _selectedIndexs.Push(index);
                onTimerTick();

                // 启动计时器
                _timer.Start();
            });
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _attachedVisualTree = false;
            _timer.Stop();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == ItemsSourceProperty)
            {
                itemsControl.ItemsSource = e.GetNewValue<IEnumerable?>();
            }
            else if (e.Property == ScrollDurationProperty)
            {
                if (_firstScrollDone)
                {
                    var transition = new VectorTransition();
                    transition.Property = ScrollViewer.OffsetProperty;
                    transition.Duration = TimeSpan.FromMilliseconds(e.GetNewValue<double>());
                    scrollViewer.Transitions![0] = transition;
                }
            }
            else if (e.Property == CurrentTimeProperty)
            {
                if (!_attachedVisualTree)
                {
                    return;
                }

                int newIndex = getIndexOfCurTime(e.GetNewValue<double>());
                if (DateTime.Now < _invisibleEndTime)
                {
                    _curIndex = newIndex;
                    return;
                }
                if (_curIndex == newIndex)
                {
                    return;
                }

                restoreToDefault(_curIndex);
                _selectedIndexs.Push(newIndex);
                onTimerTick();
            }
        }

        private void scrollBar_DragStarted(ScrollBar scrollBar, VectorEventArgs e)
        {
            timelineGrid.IsVisible = true;
            _invisibleEndTime = DateTime.MaxValue;
        }

        private void scrollBar_OnThumbDrag(ScrollBar scrollBar, VectorEventArgs e)
        {
            Dispatcher.UIThread.Post(showTimeline);
        }

        private void scrollBar_DragCompleted(ScrollBar scrollBar, VectorEventArgs e)
        {
            _invisibleEndTime = DateTime.Now.AddMilliseconds(Delay);
        }

        private void onPointerWheelChanged(InputElement? sender, PointerWheelEventArgs e)
        {
            if (e.Delta.NearlyEquals(Vector.Zero))
            {
                return;
            }

            timelineGrid.IsVisible = true;
            if (_invisibleEndTime != DateTime.MaxValue)
            {
                _invisibleEndTime = DateTime.Now.AddMilliseconds(Delay);
            }

            Dispatcher.UIThread.Post(showTimeline);
        }

        private void itemsControl_ContainerPrepared(object? sender, ContainerPreparedEventArgs e)
        {
            if (e.Index == _scrollingIndex)
            {
                setSelected(0, e.Container);
            }
            else
            {
                restoreToDefault(0, e.Container);
            }
        }

        private void playButton_Click(object? sender, RoutedEventArgs e)
        {
            timelineGrid.IsVisible = false;
            _invisibleEndTime = DateTime.Now;
            CurrentTime = getCurTimelineStamp();
        }

        private void onTimerTick()
        {
            if (timelineGrid.IsVisible)
            {
                if (DateTime.Now < _invisibleEndTime)
                {
                    return;
                }

                timelineGrid.IsVisible = false;
                _selectedIndexs.Push(_curIndex);
                if (_scrollingIndex != _curIndex)
                {
                    restoreToDefault(_scrollingIndex);
                }
                else
                {
                    _scrollingIndex = -1;
                }
            }

            if (!_selectedIndexs.IsEmpty)
            {
                if (DateTime.Now < _autoScrollEndTime || 
                    !_selectedIndexs.TryPeek(out var index))
                {
                    return;
                }

                ILogical? logical = itemsControl.ContainerFromIndex(index);
                if (logical is null)
                {
                    itemsControl.ScrollIntoView(index);
                    return;
                }

                if (index == _scrollingIndex)
                {
                    // 仍是之前的index，清空栈
                    _selectedIndexs.Clear();
                    _curIndex = _scrollingIndex;
                    if (!_firstScrollDone)
                    {
                        _firstScrollDone = true;
                        _initScrollViewerTransition();
                    }

                    // 确保选中
                    setSelected(_scrollingIndex);
                }
                else
                {
                    // 有新的index加入，需要继续滚动
                    scrollToCur(index);
                }
            }
        }

        private void restoreToDefault(int index, ILogical? logical = null)
        {
            if (logical is null)
            {
                logical = itemsControl.ContainerFromIndex(index);
            }
            var children = logical?.GetLogicalChildren();
            if (children is null || children.Count() <= 0)
            {
                return;
            }

            var textBlock = children.OfType<TextBlock>().First();
            if (textBlock != null)
            {
                textBlock.Classes.Remove(SelectedClasses);
                textBlock.Classes.Add(DefClasses);
            }
        }

        private void setSelected(int index, ILogical? logical = null)
        {
            if (logical is null)
            {
                logical = itemsControl.ContainerFromIndex(index);
            }
            var children = logical?.GetLogicalChildren();
            if (children is null || children.Count() <= 0)
            {
                return;
            }

            var textBlock = children.OfType<TextBlock>().First();
            if (textBlock != null)
            {
                textBlock.Classes.Remove(DefClasses);
                textBlock.Classes.Add(SelectedClasses);
            }
        }

        private void scrollCurToCenter(ILogical logical)
        {
            double remainHeight = scrollViewer.Extent.Height - scrollViewer.Bounds.Height - scrollViewer.Offset.Y;
            if (Math.Abs(remainHeight) < 1e-10)
            {
                return;
            }

            Visual? visual = logical as Visual;
            if (visual is null)
            {
                return;
            }

            var pos = visual.TranslatePoint(new Point(), scrollViewer);
            if (pos is not null)
            {
                double centerY = scrollViewer.Bounds.Height / 2;
                double centerItemY = pos.Value.Y + visual.Bounds.Height / 2;
                scrollViewer.Offset += new Vector(0, centerItemY - centerY);
            }
        }

        private void scrollToCur(int index = -1)
        {
            if (index == -1 && !_selectedIndexs.TryPeek(out index))
            {
                return;
            }

            if (_scrollingIndex != index && _scrollingIndex != -1)
            {
                restoreToDefault(_scrollingIndex);
            }

            ILogical? logical = itemsControl.ContainerFromIndex(index);
            if (logical is not null)
            {
                if (_scrollingIndex != index)
                {
                    setSelected(index);
                }
                _scrollingIndex = index;
                _autoScrollEndTime = DateTime.Now.AddMilliseconds(ScrollDuration + _scrollDurationOffset);
                scrollCurToCenter(logical);
            }
        }

        private int getIndexOfCurTime(double time)
        {
            for (int index = 0; index < itemsControl.Items.Count(); ++index)
            {
                var item = itemsControl.Items.ElementAt(index) as Lyric;
                if (item is null)
                {
                    continue;
                }

                if (time >= item.TimeStamp && (time < item.EndTimeStamp || item.EndTimeStamp < 0))
                {
                    return index;
                }
            }

            return -1;
        }

        private double getCurTimelineStamp()
        {
            foreach (var container in itemsControl.GetRealizedContainers())
            {
                var pos = container.TranslatePoint(new Point(), scrollViewer);
                if (pos is not null)
                {
                    double centerY = scrollViewer.Bounds.Height / 2;
                    if (centerY >= pos.Value.Y && centerY <= pos.Value.Y + container.Bounds.Height)
                    {
                        var lyric = container.DataContext as Lyric;
                        return lyric?.TimeStamp ?? 0;
                    }
                }
            }

            return -1;
        }

        private void showTimeline()
        {
            int sec = (int)getCurTimelineStamp();
            timeline.Text = string.Format("{0:D2}:{1:D2}", sec / 60, sec % 60);
        }

        private void _initScrollViewerTransition()
        {
            var transition = new VectorTransition();
            transition.Property = ScrollViewer.OffsetProperty;
            transition.Duration = TimeSpan.FromMilliseconds(ScrollDuration);
            if (scrollViewer.Transitions is null)
            {
                scrollViewer.Transitions = new() { transition };
            }
            else
            {
                scrollViewer.Transitions.Add(transition);
            }
        }
    }
}
