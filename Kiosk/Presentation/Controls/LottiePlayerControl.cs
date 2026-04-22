using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Skottie;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace Kiosk.Presentation.Controls
{
    public sealed class LottiePlayerControl : SKElement
    {
        public static readonly DependencyProperty SourcePathProperty =
            DependencyProperty.Register(
                nameof(SourcePath),
                typeof(string),
                typeof(LottiePlayerControl),
                new PropertyMetadata(null, OnSourcePathChanged));

        public static readonly DependencyProperty AutoPlayProperty =
            DependencyProperty.Register(
                nameof(AutoPlay),
                typeof(bool),
                typeof(LottiePlayerControl),
                new PropertyMetadata(true, OnAutoPlayChanged));

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(
                nameof(HasError),
                typeof(bool),
                typeof(LottiePlayerControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage),
                typeof(string),
                typeof(LottiePlayerControl),
                new PropertyMetadata(string.Empty));

        private Animation? _animation;
        private readonly Stopwatch _clock = new();
        private SKRect _targetRect = SKRect.Empty;
        private bool _isLoaded;

        public LottiePlayerControl()
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

        public bool AutoPlay
        {
            get => (bool)GetValue(AutoPlayProperty);
            set => SetValue(AutoPlayProperty, value);
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
            var control = (LottiePlayerControl)d;
            control.LoadAnimation(e.NewValue as string);
        }

        private static void OnAutoPlayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LottiePlayerControl)d;
            if (control._animation is null)
            {
                return;
            }

            if (control.AutoPlay)
            {
                if (!control._clock.IsRunning)
                {
                    control._clock.Restart();
                }
            }
            else
            {
                control._clock.Reset();
                control.InvalidateVisual();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Loaded");
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;
            CompositionTarget.Rendering += OnRendering;

            if (AutoPlay && _animation is not null && !_clock.IsRunning)
            {
                _clock.Restart();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("UnLoaded");

            if (!_isLoaded)
            {
                return;
            }

            _isLoaded = false;
            CompositionTarget.Rendering -= OnRendering;
            _clock.Stop();
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_animation is null || !AutoPlay)
            {
                return;
            }

            InvalidateVisual();
        }

        private void LoadAnimation(string? path)
        {
            _animation?.Dispose();
            _animation = null;
            _targetRect = SKRect.Empty;
            _clock.Reset();
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
            _animation = Animation.Create(stream);
            if (_animation is null)
            {
                HasError = true;
                ErrorMessage = "Failed to load animation.";
                InvalidateVisual();
                return;
            }

            _targetRect = SKRect.Create(0, 0, _animation.Size.Width, _animation.Size.Height);

            if (AutoPlay)
            {
                _clock.Restart();
            }

            InvalidateVisual();
        }

        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            if (_animation is null)
            {
                return;
            }

            var size = e.Info.Size;
            var bounds = _animation.Size;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var scale = Math.Min(size.Width / bounds.Width, size.Height / bounds.Height);
            var targetWidth = bounds.Width * scale;
            var targetHeight = bounds.Height * scale;
            var offsetX = (size.Width - targetWidth) / 2f;
            var offsetY = (size.Height - targetHeight) / 2f;

            var saveCount = canvas.Save();
            canvas.Translate(offsetX, offsetY);
            canvas.Scale(scale);

            var elapsedSeconds = AutoPlay ? (float)_clock.Elapsed.TotalSeconds : 0f;
            var durationSeconds = _animation.Duration.TotalSeconds;
            var seconds = durationSeconds > 0 ? elapsedSeconds % (float)durationSeconds : 0f;

            _animation.SeekFrameTime(seconds);
            _animation.Render(canvas, _targetRect);
            canvas.RestoreToCount(saveCount);
        }
    }
}
