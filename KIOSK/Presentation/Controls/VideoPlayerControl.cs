using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KIOSK.Presentation.Controls
{
    public sealed class VideoPlayerControl : Grid
    {
        public static readonly DependencyProperty SourcePathProperty =
            DependencyProperty.Register(
                nameof(SourcePath),
                typeof(string),
                typeof(VideoPlayerControl),
                new PropertyMetadata(string.Empty, OnSourcePathChanged));

        public static readonly DependencyProperty AutoPlayProperty =
            DependencyProperty.Register(
                nameof(AutoPlay),
                typeof(bool),
                typeof(VideoPlayerControl),
                new PropertyMetadata(true, OnAutoPlayChanged));

        public static readonly DependencyProperty LoopProperty =
            DependencyProperty.Register(
                nameof(Loop),
                typeof(bool),
                typeof(VideoPlayerControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(
                nameof(HasError),
                typeof(bool),
                typeof(VideoPlayerControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage),
                typeof(string),
                typeof(VideoPlayerControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsReadyProperty =
            DependencyProperty.Register(
                nameof(IsReady),
                typeof(bool),
                typeof(VideoPlayerControl),
                new PropertyMetadata(false));

        private readonly MediaElement _mediaElement;

        public VideoPlayerControl()
        {
            _mediaElement = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                ScrubbingEnabled = true,
                Stretch = Stretch.Uniform,
                Opacity = 0
            };

            Children.Add(_mediaElement);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _mediaElement.MediaOpened += OnMediaOpened;
            _mediaElement.MediaEnded += OnMediaEnded;
            _mediaElement.MediaFailed += OnMediaFailed;
        }

        public string SourcePath
        {
            get => (string)GetValue(SourcePathProperty);
            set => SetValue(SourcePathProperty, value);
        }

        public bool AutoPlay
        {
            get => (bool)GetValue(AutoPlayProperty);
            set => SetValue(AutoPlayProperty, value);
        }

        public bool Loop
        {
            get => (bool)GetValue(LoopProperty);
            set => SetValue(LoopProperty, value);
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

        public bool IsReady
        {
            get => (bool)GetValue(IsReadyProperty);
            set => SetValue(IsReadyProperty, value);
        }

        private static void OnSourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (VideoPlayerControl)d;
            control.LoadVideo(e.NewValue as string);
        }

        private static void OnAutoPlayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (VideoPlayerControl)d;
            if (control._mediaElement.Source is null)
            {
                return;
            }

            if (control.AutoPlay)
            {
                control._mediaElement.Play();
            }
            else
            {
                control._mediaElement.Pause();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Load Video");

            if (_mediaElement.Source is null && !string.IsNullOrWhiteSpace(SourcePath))
            {
                LoadVideo(SourcePath);
                return;
            }

            if (AutoPlay && _mediaElement.Source is not null)
            {
                _mediaElement.Play();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Unload Video");

            _mediaElement.Stop();
            _mediaElement.Source = null;
            _mediaElement.Opacity = 0;
            IsReady = false;
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            IsReady = true;
            _mediaElement.Opacity = 1;

            if (AutoPlay)
            {
                _mediaElement.Play();
            }
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            if (!Loop)
            {
                return;
            }

            _mediaElement.Position = TimeSpan.Zero;
            _mediaElement.Play();
        }

        private void OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            HasError = true;
            ErrorMessage = "Failed to load video.";
            IsReady = false;
            _mediaElement.Opacity = 0;
        }

        private void LoadVideo(string? path)
        {
            HasError = false;
            ErrorMessage = string.Empty;
            IsReady = false;
            _mediaElement.Stop();
            _mediaElement.Source = null;
            _mediaElement.Opacity = 0;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                HasError = true;
                ErrorMessage = "Video file not found.";
                return;
            }

            _mediaElement.Source = new Uri(path, UriKind.Absolute);
            if (AutoPlay && IsLoaded)
            {
                _mediaElement.Play();
            }
        }
    }
}
