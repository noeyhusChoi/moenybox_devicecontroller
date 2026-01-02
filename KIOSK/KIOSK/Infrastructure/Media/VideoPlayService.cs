using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace KIOSK.Infrastructure.Media
{
    public interface IVideoPlayService
    {
        /// <summary>WPF에서 배경으로 쓸 Brush (VideoDrawing 래핑)</summary>
        Brush BackgroundBrush { get; }

        void SetSource(Uri source, bool loop = true, bool mute = true, bool autoPlay = true);
        void Play();
        void Pause();
        void Stop();
        void Dispose();
    }
    public class VideoPlayService : IVideoPlayService, IDisposable
    {
        private readonly MediaPlayer _player;
        private readonly DrawingBrush _brush;
        private bool _loop;

        public Brush BackgroundBrush => _brush;

        public VideoPlayService()
        {
            _player = new MediaPlayer();

            var videoDrawing = new VideoDrawing
            {
                Player = _player,
                Rect = new Rect(0, 0, 1, 1) // 전체 채우기
            };

            _brush = new DrawingBrush(videoDrawing)
            {
                Stretch = Stretch.Fill
            };

            _player.MediaEnded += OnMediaEnded;
        }

        public void SetSource(Uri source, bool loop = true, bool mute = true, bool autoPlay = true)
        {
            RunOnUi(() =>
            {
                _loop = loop;
                _player.IsMuted = mute;
                
                _player.Open(source);

                if (autoPlay)
                {
                    _player.Position = TimeSpan.Zero;
                    _player.Play();
                }
            });
        }

        public void Play()
        {
            RunOnUi(() =>
            {
                _player.Play();
            });
        }

        public void Pause()
        {
            RunOnUi(() =>
            {
                _player.Pause();
            });
        }

        public void Stop()
        {
            RunOnUi(() =>
            {
                _player.Stop();
                _player.Position = TimeSpan.Zero;
            });
        }

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            if (!_loop) return;

            RunOnUi(() =>
            {
                _player.Position = TimeSpan.Zero;
                _player.Play();
            });
        }

        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                action();
                return;
            }

            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        public void Dispose()
        {
            _player.Stop();
            _player.MediaEnded -= OnMediaEnded;
            _player.Close();
        }
    }
}
