using System;
using System.IO;
using System.Windows;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace KIOSK.Presentation.Controls
{
    public sealed class WebpPlayerControl : SKElement
    {
        public static readonly DependencyProperty SourcePathProperty =
            DependencyProperty.Register(
                nameof(SourcePath),
                typeof(string),
                typeof(WebpPlayerControl),
                new PropertyMetadata(null, OnSourcePathChanged));

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(
                nameof(HasError),
                typeof(bool),
                typeof(WebpPlayerControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage),
                typeof(string),
                typeof(WebpPlayerControl),
                new PropertyMetadata(string.Empty));

        private SKBitmap? _bitmap;
        private string? _pendingPath;

        public WebpPlayerControl()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            PaintSurface += OnPaintSurface;
        }

        public string? SourcePath
        {
            get => (string?)GetValue(SourcePathProperty);
            set => SetValue(SourcePathProperty, value);
        }

        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            private set => SetValue(HasErrorProperty, value);
        }

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            private set => SetValue(ErrorMessageProperty, value);
        }

        private static void OnSourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WebpPlayerControl)d;
            control.QueueLoad(e.NewValue as string);
        }

        private void QueueLoad(string? path)
        {
            _pendingPath = path;
            if (IsLoaded)
            {
                LoadImage(_pendingPath);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_bitmap is null && !string.IsNullOrWhiteSpace(_pendingPath))
            {
                LoadImage(_pendingPath);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _bitmap?.Dispose();
            _bitmap = null;
            InvalidateVisual();
        }

        private void LoadImage(string? path)
        {
            _bitmap?.Dispose();
            _bitmap = null;
            HasError = false;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                HasError = true;
                ErrorMessage = "File not found.";
                InvalidateVisual();
                return;
            }

            using var stream = File.OpenRead(path);
            _bitmap = SKBitmap.Decode(stream);
            if (_bitmap is null)
            {
                HasError = true;
                ErrorMessage = "Failed to load image.";
                InvalidateVisual();
                return;
            }

            InvalidateVisual();
        }

        private void OnPaintSurface(object? sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            if (_bitmap is null)
            {
                return;
            }

            var size = e.Info.Size;
            if (_bitmap.Width <= 0 || _bitmap.Height <= 0)
            {
                return;
            }

            var scale = Math.Min(size.Width / (float)_bitmap.Width, size.Height / (float)_bitmap.Height);
            var targetWidth = _bitmap.Width * scale;
            var targetHeight = _bitmap.Height * scale;
            var offsetX = (size.Width - targetWidth) / 2f;
            var offsetY = (size.Height - targetHeight) / 2f;

            var saveCount = canvas.Save();
            canvas.Translate(offsetX, offsetY);
            canvas.Scale(scale);
            canvas.DrawBitmap(_bitmap, 0, 0);
            canvas.RestoreToCount(saveCount);
        }
    }
}
