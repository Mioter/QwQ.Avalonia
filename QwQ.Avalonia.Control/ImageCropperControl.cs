using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace QwQ.Avalonia.Control;

/// <summary>
/// 图片裁剪控件，支持自由比例和固定比例裁剪，提供直观的交互体验
/// </summary>
public class ImageCropperControl : global::Avalonia.Controls.Control
{
    #region 常量定义

    /// <summary>
    /// 最小缩放比例
    /// </summary>
    private const double MIN_SCALE = 0.1;

    /// <summary>
    /// 最大缩放比例
    /// </summary>
    private const double MAX_SCALE = 10.0;

    /// <summary>
    /// 缩放因子
    /// </summary>
    private const double SCALE_FACTOR = 0.1;

    /// <summary>
    /// 调整手柄大小
    /// </summary>
    private const double RESIZE_HANDLE_SIZE = 10;

    /// <summary>
    /// 安全边距，确保手柄可见
    /// </summary>
    private const double SAFE_MARGIN = RESIZE_HANDLE_SIZE * 2;

    /// <summary>
    /// 最小裁剪框尺寸
    /// </summary>
    private const double MIN_CROP_SIZE = 50;

    #endregion

    #region 私有字段

    private Bitmap? _sourceImage;
    private Rect _cropRect;
    private Point _dragStart;
    private bool _isDragging;
    private double _aspectRatio = 1.0;
    private double _imageScale = 1.0;
    private Point _imageOffset;
    private ResizeHandle _activeHandle = ResizeHandle.None;
    private bool _isResizing;

    #endregion

    #region 依赖属性

    /// <summary>
    /// 源图片
    /// </summary>
    public static readonly StyledProperty<Bitmap?> SourceImageProperty = AvaloniaProperty.Register<
        ImageCropperControl,
        Bitmap?
    >(nameof(SourceImage));

    /// <summary>
    /// 裁剪框宽高比，0表示自由比例
    /// </summary>
    public static readonly StyledProperty<double> AspectRatioProperty = AvaloniaProperty.Register<
        ImageCropperControl,
        double
    >(nameof(AspectRatio), defaultValue: 1.0);

    #endregion

    #region 属性访问器

    /// <summary>
    /// 获取或设置源图片
    /// </summary>
    public Bitmap? SourceImage
    {
        get => GetValue(SourceImageProperty);
        set => SetValue(SourceImageProperty, value);
    }

    /// <summary>
    /// 获取或设置裁剪框宽高比，0表示自由比例
    /// </summary>
    public double AspectRatio
    {
        get => GetValue(AspectRatioProperty);
        set => SetValue(AspectRatioProperty, value);
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化图片裁剪控件
    /// </summary>
    public ImageCropperControl()
    {
        ClipToBounds = true;
        _cropRect = new Rect(0, 0, MIN_CROP_SIZE, MIN_CROP_SIZE);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取调整手柄类型
    /// </summary>
    private ResizeHandle GetResizeHandle(Point point)
    {
        var topLeft = new Rect(
            _cropRect.Left - RESIZE_HANDLE_SIZE,
            _cropRect.Top - RESIZE_HANDLE_SIZE,
            RESIZE_HANDLE_SIZE * 2,
            RESIZE_HANDLE_SIZE * 2
        );
        var topRight = new Rect(
            _cropRect.Right - RESIZE_HANDLE_SIZE,
            _cropRect.Top - RESIZE_HANDLE_SIZE,
            RESIZE_HANDLE_SIZE * 2,
            RESIZE_HANDLE_SIZE * 2
        );
        var bottomLeft = new Rect(
            _cropRect.Left - RESIZE_HANDLE_SIZE,
            _cropRect.Bottom - RESIZE_HANDLE_SIZE,
            RESIZE_HANDLE_SIZE * 2,
            RESIZE_HANDLE_SIZE * 2
        );
        var bottomRight = new Rect(
            _cropRect.Right - RESIZE_HANDLE_SIZE,
            _cropRect.Bottom - RESIZE_HANDLE_SIZE,
            RESIZE_HANDLE_SIZE * 2,
            RESIZE_HANDLE_SIZE * 2
        );

        if (topLeft.Contains(point))
            return ResizeHandle.TopLeft;
        if (topRight.Contains(point))
            return ResizeHandle.TopRight;
        if (bottomLeft.Contains(point))
            return ResizeHandle.BottomLeft;
        return bottomRight.Contains(point) ? ResizeHandle.BottomRight : ResizeHandle.None;
    }

    /// <summary>
    /// 计算最小缩放比例，确保图片至少能覆盖裁剪框
    /// </summary>
    private double CalculateMinScale()
    {
        if (_sourceImage == null)
            return MIN_SCALE;

        double scaleX = _cropRect.Width / _sourceImage.PixelSize.Width;
        double scaleY = _cropRect.Height / _sourceImage.PixelSize.Height;
        return Math.Max(MIN_SCALE, Math.Max(scaleX, scaleY));
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取裁剪后的图片
    /// </summary>
    /// <returns>裁剪后的图片，如果源图片为空则返回null</returns>
    public Bitmap? GetCroppedImage()
    {
        if (_sourceImage == null)
            return null;

        var cropRect = new PixelRect(
            (int)((_cropRect.X - _imageOffset.X) / _imageScale),
            (int)((_cropRect.Y - _imageOffset.Y) / _imageScale),
            (int)(_cropRect.Width / _imageScale),
            (int)(_cropRect.Height / _imageScale)
        );

        var croppedBitmap = new WriteableBitmap(
            cropRect.Size,
            _sourceImage.Dpi,
            _sourceImage.Format,
            _sourceImage.AlphaFormat
        );

        using var fb = croppedBitmap.Lock();
        _sourceImage.CopyPixels(cropRect, fb.Address, fb.RowBytes * cropRect.Height, fb.RowBytes);

        return croppedBitmap;
    }

    #endregion

    #region 重写方法

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_sourceImage == null)
            return;

        // 计算图片的实际大小和位置
        double imageWidth = _sourceImage.PixelSize.Width * _imageScale;
        double imageHeight = _sourceImage.PixelSize.Height * _imageScale;
        var imageRect = new Rect(_imageOffset.X, _imageOffset.Y, imageWidth, imageHeight);

        // 绘制源图片
        context.DrawImage(_sourceImage, imageRect);

        // 绘制半透明遮罩
        var overlayBrush = new SolidColorBrush(Colors.Black, 0.5);
        var leftRect = new Rect(0, 0, _cropRect.X, Bounds.Height);
        var rightRect = new Rect(_cropRect.Right, 0, Bounds.Width - _cropRect.Right, Bounds.Height);
        var topRect = new Rect(_cropRect.X, 0, _cropRect.Width, _cropRect.Y);
        var bottomRect = new Rect(
            _cropRect.X,
            _cropRect.Bottom,
            _cropRect.Width,
            Bounds.Height - _cropRect.Bottom
        );

        context.FillRectangle(overlayBrush, leftRect);
        context.FillRectangle(overlayBrush, rightRect);
        context.FillRectangle(overlayBrush, topRect);
        context.FillRectangle(overlayBrush, bottomRect);

        // 绘制裁剪框
        var cropPen = new Pen(new SolidColorBrush(Colors.White));
        context.DrawRectangle(cropPen, _cropRect);

        // 绘制调整大小的手柄
        if (_aspectRatio == 0) // 只在自由比例模式下显示调整手柄
        {
            var handleBrush = new SolidColorBrush(Colors.White);

            // 左上角
            var handleRect = new Rect(
                _cropRect.Left - RESIZE_HANDLE_SIZE / 2,
                _cropRect.Top - RESIZE_HANDLE_SIZE / 2,
                RESIZE_HANDLE_SIZE,
                RESIZE_HANDLE_SIZE
            );
            context.FillRectangle(handleBrush, handleRect);

            // 右上角
            handleRect = new Rect(
                _cropRect.Right - RESIZE_HANDLE_SIZE / 2,
                _cropRect.Top - RESIZE_HANDLE_SIZE / 2,
                RESIZE_HANDLE_SIZE,
                RESIZE_HANDLE_SIZE
            );
            context.FillRectangle(handleBrush, handleRect);

            // 左下角
            handleRect = new Rect(
                _cropRect.Left - RESIZE_HANDLE_SIZE / 2,
                _cropRect.Bottom - RESIZE_HANDLE_SIZE / 2,
                RESIZE_HANDLE_SIZE,
                RESIZE_HANDLE_SIZE
            );
            context.FillRectangle(handleBrush, handleRect);

            // 右下角
            handleRect = new Rect(
                _cropRect.Right - RESIZE_HANDLE_SIZE / 2,
                _cropRect.Bottom - RESIZE_HANDLE_SIZE / 2,
                RESIZE_HANDLE_SIZE,
                RESIZE_HANDLE_SIZE
            );
            context.FillRectangle(handleBrush, handleRect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_sourceImage == null)
            return;

        var point = e.GetPosition(this);
        _activeHandle = GetResizeHandle(point);

        if (_activeHandle != ResizeHandle.None)
        {
            _isResizing = true;
        }
        else
        {
            _isDragging = true;
        }

        _dragStart = point;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_sourceImage == null)
            return;

        var point = e.GetPosition(this);
        var delta = point - _dragStart;
        _dragStart = point;

        if (_isResizing && _aspectRatio == 0)
        {
            // 调整裁剪框大小
            double newWidth = _cropRect.Width;
            double newHeight = _cropRect.Height;
            double newX = _cropRect.X;
            double newY = _cropRect.Y;

            // 计算图片的实际大小
            double imageWidth = _sourceImage.PixelSize.Width * _imageScale;
            double imageHeight = _sourceImage.PixelSize.Height * _imageScale;

            // 计算图片在控件中的实际位置
            double imageLeft = _imageOffset.X;
            double imageTop = _imageOffset.Y;
            double imageRight = imageLeft + imageWidth;
            double imageBottom = imageTop + imageHeight;

            switch (_activeHandle)
            {
                case ResizeHandle.TopLeft:
                    newWidth = _cropRect.Right - point.X;
                    newHeight = _cropRect.Bottom - point.Y;
                    newX = point.X;
                    newY = point.Y;
                    break;
                case ResizeHandle.TopRight:
                    newWidth = point.X - _cropRect.Left;
                    newHeight = _cropRect.Bottom - point.Y;
                    newY = point.Y;
                    break;
                case ResizeHandle.BottomLeft:
                    newWidth = _cropRect.Right - point.X;
                    newHeight = point.Y - _cropRect.Top;
                    newX = point.X;
                    break;
                case ResizeHandle.BottomRight:
                    newWidth = point.X - _cropRect.Left;
                    newHeight = point.Y - _cropRect.Top;
                    break;
            }

            // 确保最小尺寸
            if (newWidth >= MIN_CROP_SIZE && newHeight >= MIN_CROP_SIZE)
            {
                // 限制裁剪框在图片范围内
                if (newX < imageLeft)
                {
                    newWidth += newX - imageLeft;
                    newX = imageLeft;
                }
                if (newY < imageTop)
                {
                    newHeight += newY - imageTop;
                    newY = imageTop;
                }
                if (newX + newWidth > imageRight)
                {
                    newWidth = imageRight - newX;
                }
                if (newY + newHeight > imageBottom)
                {
                    newHeight = imageBottom - newY;
                }

                // 限制裁剪框在控件安全范围内
                if (newX < SAFE_MARGIN)
                {
                    newWidth += newX - SAFE_MARGIN;
                    newX = SAFE_MARGIN;
                }
                if (newY < SAFE_MARGIN)
                {
                    newHeight += newY - SAFE_MARGIN;
                    newY = SAFE_MARGIN;
                }
                if (newX + newWidth > Bounds.Width - SAFE_MARGIN)
                {
                    newWidth = Bounds.Width - SAFE_MARGIN - newX;
                }
                if (newY + newHeight > Bounds.Height - SAFE_MARGIN)
                {
                    newHeight = Bounds.Height - SAFE_MARGIN - newY;
                }

                // 再次检查最小尺寸
                if (newWidth >= MIN_CROP_SIZE && newHeight >= MIN_CROP_SIZE)
                {
                    _cropRect = new Rect(newX, newY, newWidth, newHeight);
                }
            }
        }
        else if (_isDragging)
        {
            // 计算图片的实际大小
            double imageWidth = _sourceImage.PixelSize.Width * _imageScale;
            double imageHeight = _sourceImage.PixelSize.Height * _imageScale;

            // 计算新的偏移量
            double newOffsetX = _imageOffset.X + delta.X;
            double newOffsetY = _imageOffset.Y + delta.Y;

            // 计算裁剪框在图片坐标系中的位置
            double cropLeft = _cropRect.Left - newOffsetX;
            double cropTop = _cropRect.Top - newOffsetY;
            double cropRight = _cropRect.Right - newOffsetX;
            double cropBottom = _cropRect.Bottom - newOffsetY;

            // 限制偏移量，确保裁剪框不会超出图片范围
            if (cropLeft < 0)
                newOffsetX = _cropRect.Left;
            else if (cropRight > imageWidth)
                newOffsetX = _cropRect.Right - imageWidth;

            if (cropTop < 0)
                newOffsetY = _cropRect.Top;
            else if (cropBottom > imageHeight)
                newOffsetY = _cropRect.Bottom - imageHeight;

            _imageOffset = new Point(newOffsetX, newOffsetY);
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging || _isResizing)
        {
            _isDragging = false;
            _isResizing = false;
            _activeHandle = ResizeHandle.None;
            e.Pointer.Capture(null);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_sourceImage == null)
            return;

        var point = e.GetPosition(this);
        double delta = e.Delta.Y;

        // 计算缩放中心点相对于图片的偏移
        double relativeX =
            (point.X - _imageOffset.X) / (_sourceImage.PixelSize.Width * _imageScale);
        double relativeY =
            (point.Y - _imageOffset.Y) / (_sourceImage.PixelSize.Height * _imageScale);

        // 计算新的缩放比例
        double newScale = _imageScale * (1 + delta * SCALE_FACTOR);

        // 限制缩放范围
        double minScale = CalculateMinScale();
        newScale = Math.Max(minScale, Math.Min(MAX_SCALE, newScale));

        // 如果缩放比例没有变化，直接返回
        if (Math.Abs(newScale - _imageScale) < 0.001)
            return;

        // 计算新的图片偏移，保持鼠标指向的点不变
        double newWidth = _sourceImage.PixelSize.Width * newScale;
        double newHeight = _sourceImage.PixelSize.Height * newScale;
        double newOffsetX = point.X - relativeX * newWidth;
        double newOffsetY = point.Y - relativeY * newHeight;

        // 计算裁剪框在图片坐标系中的位置
        double cropLeft = _cropRect.Left - newOffsetX;
        double cropTop = _cropRect.Top - newOffsetY;
        double cropRight = _cropRect.Right - newOffsetX;
        double cropBottom = _cropRect.Bottom - newOffsetY;

        // 限制偏移量，确保裁剪框不会超出图片范围
        if (cropLeft < 0)
            newOffsetX = _cropRect.Left;
        else if (cropRight > newWidth)
            newOffsetX = _cropRect.Right - newWidth;

        if (cropTop < 0)
            newOffsetY = _cropRect.Top;
        else if (cropBottom > newHeight)
            newOffsetY = _cropRect.Bottom - newHeight;

        _imageScale = newScale;
        _imageOffset = new Point(newOffsetX, newOffsetY);

        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceImageProperty)
        {
            _sourceImage = change.GetNewValue<Bitmap?>();
            if (_sourceImage != null)
            {
                // 初始化裁剪框大小和位置，考虑安全边距
                double size =
                    Math.Min(Bounds.Width - SAFE_MARGIN * 2, Bounds.Height - SAFE_MARGIN * 2) * 0.8;
                double x = (Bounds.Width - size) / 2;
                double y = (Bounds.Height - size) / 2;
                _cropRect = new Rect(x, y, size, size / AspectRatio);

                // 初始化图片位置和缩放
                _imageScale = CalculateMinScale();

                // 计算初始偏移量，确保图片居中且裁剪框在图片范围内
                double imageWidth = _sourceImage.PixelSize.Width * _imageScale;
                double imageHeight = _sourceImage.PixelSize.Height * _imageScale;

                _imageOffset = new Point(
                    (Bounds.Width - imageWidth) / 2,
                    (Bounds.Height - imageHeight) / 2
                );
            }
            InvalidateVisual();
        }
        else if (change.Property == AspectRatioProperty)
        {
            _aspectRatio = change.GetNewValue<double>();

            if (_sourceImage == null)
                return;

            if (_aspectRatio != 0) // 切换到固定比例
            {
                // 计算可用的最大尺寸（考虑安全边距）
                double maxWidth = Bounds.Width - SAFE_MARGIN * 2;
                double maxHeight = Bounds.Height - SAFE_MARGIN * 2;

                // 计算图片的实际大小
                double imageWidth = _sourceImage.PixelSize.Width * _imageScale;
                double imageHeight = _sourceImage.PixelSize.Height * _imageScale;

                // 计算裁剪框的新尺寸
                double newWidth,
                    newHeight;
                if (_aspectRatio > 1) // 宽大于高
                {
                    newWidth = Math.Min(maxWidth, imageWidth);
                    newHeight = newWidth / _aspectRatio;
                    if (newHeight > maxHeight)
                    {
                        newHeight = maxHeight;
                        newWidth = newHeight * _aspectRatio;
                    }
                }
                else // 高大于宽
                {
                    newHeight = Math.Min(maxHeight, imageHeight);
                    newWidth = newHeight * _aspectRatio;
                    if (newWidth > maxWidth)
                    {
                        newWidth = maxWidth;
                        newHeight = newWidth / _aspectRatio;
                    }
                }

                // 确保最小尺寸
                if (newWidth < MIN_CROP_SIZE)
                {
                    newWidth = MIN_CROP_SIZE;
                    newHeight = newWidth / _aspectRatio;
                }
                if (newHeight < MIN_CROP_SIZE)
                {
                    newHeight = MIN_CROP_SIZE;
                    newWidth = newHeight * _aspectRatio;
                }

                // 计算新的位置（居中）
                double newX = (Bounds.Width - newWidth) / 2;
                double newY = (Bounds.Height - newHeight) / 2;

                // 确保裁剪框在图片范围内
                double cropLeft = newX - _imageOffset.X;
                double cropTop = newY - _imageOffset.Y;
                double cropRight = cropLeft + newWidth;
                double cropBottom = cropTop + newHeight;

                // 如果超出图片范围，调整位置
                if (cropLeft < 0)
                {
                    newX = _imageOffset.X;
                }
                else if (cropRight > imageWidth)
                {
                    newX = _imageOffset.X + imageWidth - newWidth;
                }

                if (cropTop < 0)
                {
                    newY = _imageOffset.Y;
                }
                else if (cropBottom > imageHeight)
                {
                    newY = _imageOffset.Y + imageHeight - newHeight;
                }

                // 确保在安全边距内
                if (newX < SAFE_MARGIN)
                {
                    newX = SAFE_MARGIN;
                }
                else if (newX + newWidth > Bounds.Width - SAFE_MARGIN)
                {
                    newX = Bounds.Width - SAFE_MARGIN - newWidth;
                }

                if (newY < SAFE_MARGIN)
                {
                    newY = SAFE_MARGIN;
                }
                else if (newY + newHeight > Bounds.Height - SAFE_MARGIN)
                {
                    newY = Bounds.Height - SAFE_MARGIN - newHeight;
                }

                _cropRect = new Rect(newX, newY, newWidth, newHeight);
            }
            InvalidateVisual();
        }
    }

    #endregion

    #region 内部类型

    /// <summary>
    /// 调整手柄类型
    /// </summary>
    private enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    #endregion
}
