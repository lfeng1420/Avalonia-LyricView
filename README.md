## 效果
https://github.com/lfeng1420/Avalonia-LyricView/assets/7549203/9ea0a788-8307-4818-86be-9232def9aff7

## 存在的问题
* 目前`ItemsControl`没有使用虚拟化Panel，如果启用，有概率出现滚动时出现item重影的情况（一般在各行歌词高度差异较大时），见[issue-15194](https://github.com/AvaloniaUI/Avalonia/issues/15194)
* `ScrollViewer`滚动已达边界（例如最上方）时无法再继续滚动，因为`Offset`的X/Y最小值已被限制为0，见[ScrollViewer.cs#L694](https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Controls/ScrollViewer.cs#L694)，临时解决办法是自行在头部和末尾添加一些空数据
