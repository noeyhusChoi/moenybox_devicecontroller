using NAudio.Wave;
using KIOSK.Infrastructure.Logging;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Media
{
    public interface IAudioPlayService : IDisposable
    {
        /// <summary>파일 경로로 재생 (동기). 캐시가 없으면 로드 후 재생.</summary>
        void Play(string filePath);

        /// <summary>비동기 재생: 캐시가 없으면 먼저 Preload 후 재생.</summary>
        Task PlayAsync(string filePath);

        /// <summary>모든 재생 중지 및 내부 오브젝트 재초기화</summary>
        void StopAll();

        /// <summary>사전 로드 (비동기). 지정 파일을 백그라운드에서 캐싱.</summary>
        Task PreloadAsync(string filePath);

        /// <summary>여러 파일 사전 로드</summary>
        Task PreloadAllAsync(IEnumerable<string> filePaths);

        /// <summary>캐시에서 특정 항목 제거</summary>
        bool RemoveFromCache(string filePath);

        /// <summary>전체 캐시 삭제</summary>
        void ClearCache();

        /// <summary>0.0 ~ 1.0 전역 볼륨</summary>
        float Volume { get; set; }
    }

    public class AudioPlayService : IAudioPlayService
    {
        private readonly ILoggingService _logging;

        // 캐시: path -> CachedSound
        private readonly ConcurrentDictionary<string, CachedSound> _cache = new();

        // 캐시 순서 추적 (FIFO 제거 전략)
        private readonly ConcurrentQueue<string> _cacheOrder = new();

        // 캐시 총 바이트
        private long _cacheTotalBytes = 0;

        // 캐시 바이트 상한 (기본 200MB)
        private readonly long _maxCacheBytes;

        // 오디오 엔진
        private readonly object _lock = new();
        private WaveOutEvent? _outputDevice;
        private MixingSampleProvider _mixer;
        private VolumeSampleProvider _masterVolume;
        private float _volume = 1.0f;

        // 현재 믹서에 추가된(재생 중인) 입력
        private ISampleProvider? _currentInput;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0f, 1f);
                lock (_lock)
                {
                    if (_masterVolume != null) _masterVolume.Volume = _volume;
                }
            }
        }

        /// <summary>
        /// AudioService 생성자
        /// </summary>
        /// <param name="logging">로깅 서비스</param>
        /// <param name="sampleRate">샘플레이트 (기본 44100)</param>
        /// <param name="channels">채널 (기본 2)</param>
        /// <param name="maxCacheBytes">캐시 최대 바이트 (기본 200MB)</param>
        public AudioPlayService(ILoggingService logging, int sampleRate = 44100, int channels = 2, long maxCacheBytes = 200L * 1024 * 1024)
        {
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _maxCacheBytes = Math.Max(1, maxCacheBytes);

            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            _mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
            _masterVolume = new VolumeSampleProvider(_mixer) { Volume = _volume };

            _outputDevice = new WaveOutEvent();
            _outputDevice.Init(_masterVolume);
            _outputDevice.Play();
        }

        #region Cache helpers

        private CachedSound GetOrLoadCachedSound(string filePath)
        {
            return _cache.GetOrAdd(filePath, path =>
            {
                try
                {
                    var target = _mixer.WaveFormat;
                    var cs = new CachedSound(path, target);

                    // 캐시 바이트 계산 (float 배열 * 4)
                    var bytes = (long)cs.AudioData.Length * sizeof(float);
                    Interlocked.Add(ref _cacheTotalBytes, bytes);
                    _cacheOrder.Enqueue(path);

                    EnsureCacheUnderLimit();

                    _logging.Info($"Cached audio loaded: {Path.GetFileName(path)} ({bytes} bytes). Total cache bytes: {Interlocked.Read(ref _cacheTotalBytes)}");
                    return cs;
                }
                catch (Exception ex)
                {
                    _logging.Error(ex, $"CachedSound Create Exception for {path}: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// 캐시 사이즈 확인하고 초과 시 제거 (FIFO)
        /// </summary>
        private void EnsureCacheUnderLimit()
        {
            try
            {
                while (Interlocked.Read(ref _cacheTotalBytes) > _maxCacheBytes && _cacheOrder.TryDequeue(out var oldKey))
                {
                    if (_cache.TryRemove(oldKey, out var removed))
                    {
                        var bytes = (long)removed.AudioData.Length * sizeof(float);
                        Interlocked.Add(ref _cacheTotalBytes, -bytes);
                        _logging.Info($"Evicted audio cache: {Path.GetFileName(oldKey)} ({bytes} bytes). New cache total: {Interlocked.Read(ref _cacheTotalBytes)}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logging.Error(ex, "EnsureCacheUnderLimit Exception");
            }
        }

        #endregion

        #region Play

        /// <summary>
        /// 동기 재생. 캐시가 없으면 동기적으로 로드(블로킹) 후 재생.
        /// </summary>
        public void Play(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            CachedSound cs;
            try
            {
                cs = GetOrLoadCachedSound(filePath);
            }
            catch
            {
                return;
            }

            var provider = new CachedSoundSampleProvider(cs);

            lock (_lock)
            {
                TryRemoveCurrentInputOrReset();

                _mixer.AddMixerInput(provider);
                _currentInput = provider;
            }

            _logging.Info($"Play Audio (sync): {Path.GetFileName(filePath)}");
        }

        /// <summary>
        /// 비동기 재생: 미리 캐시가 없으면 PreloadAsync로 먼저 로드하고 재생.
        /// </summary>
        public async Task PlayAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            // 비동기 프리로드 보장
            if (!_cache.ContainsKey(filePath))
            {
                await PreloadAsync(filePath).ConfigureAwait(false);
            }

            // 재생은 UI/호출 스레드에서 호출할 수 있도록 동기 Play 호출
            Play(filePath);
        }

        /// <summary>모든 재생 즉시 중지. mixer/output 재생성.</summary>
        public void StopAll()
        {
            lock (_lock)
            {
                try
                {
                    _outputDevice?.Stop();
                    _outputDevice?.Dispose();

                    var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_mixer.WaveFormat.SampleRate, _mixer.WaveFormat.Channels);
                    _mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
                    _masterVolume = new VolumeSampleProvider(_mixer) { Volume = _volume };

                    _outputDevice = new WaveOutEvent();
                    _outputDevice.Init(_masterVolume);
                    _outputDevice.Play();

                    _currentInput = null;
                    _logging.Info("Stop All Audio");
                }
                catch (Exception ex)
                {
                    _logging.Error(ex, "StopAll Exception");
                }
            }
        }
        
        private void TryRemoveCurrentInputOrReset()
        {
            if (_currentInput == null) return;

            try
            {
                _mixer.RemoveMixerInput(_currentInput);
                _currentInput = null;
                return;
            }
            catch (Exception ex)
            {
                _logging.Error(ex, $"RemoveMixerInput Exception: {ex.Message}");
            }

            // 실패 시 안전하게 mixer 재생성 (stop all)
            StopAll();
        }

        #endregion

        #region Preload / Cache control

        /// <summary>단일 파일 비동기 Preload (백그라운드에서 캐시 생성)</summary>
        public Task PreloadAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return Task.CompletedTask;
            if (_cache.ContainsKey(filePath)) return Task.CompletedTask;

            return Task.Run(() =>
            {
                try
                {
                    // 강제로 GetOrLoadCachedSound 호출 (동기 내부)
                    GetOrLoadCachedSound(filePath);
                }
                catch (Exception ex)
                {
                    _logging.Error(ex, $"Preload failed for {filePath}: {ex.Message}");
                }
            });
        }

        /// <summary>여러 파일 병렬 Preload</summary>
        public Task PreloadAllAsync(IEnumerable<string> filePaths)
        {
            if (filePaths == null) return Task.CompletedTask;
            var distinct = filePaths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToArray();
            var tasks = distinct.Select(p => PreloadAsync(p));
            return Task.WhenAll(tasks);
        }

        /// <summary>캐시에서 특정 키 제거</summary>
        public bool RemoveFromCache(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            if (_cache.TryRemove(filePath, out var removed))
            {
                var bytes = (long)removed.AudioData.Length * sizeof(float);
                Interlocked.Add(ref _cacheTotalBytes, -bytes);
                _logging.Info($"Removed from cache: {Path.GetFileName(filePath)} ({bytes} bytes)");
                return true;
            }
            return false;
        }

        /// <summary>전체 캐시 제거</summary>
        public void ClearCache()
        {
            _cache.Clear();
            while (_cacheOrder.TryDequeue(out _)) { }
            Interlocked.Exchange(ref _cacheTotalBytes, 0);
            _logging.Info("Audio cache cleared");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            lock (_lock)
            {
                try
                {
                    _outputDevice?.Stop();
                    _outputDevice?.Dispose();
                    _outputDevice = null;
                }
                catch { /* ignore */ }
            }

            ClearCache();
        }

        #endregion
    }

    #region CachedSound + Provider

    internal class CachedSound
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }

        public CachedSound(string filePath, WaveFormat targetFormat)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

            using var reader = new AudioFileReader(filePath);
            ISampleProvider provider = reader.ToSampleProvider();

            // 샘플레이트 다르면 리샘플링
            if (provider.WaveFormat.SampleRate != targetFormat.SampleRate)
            {
                provider = new WdlResamplingSampleProvider(provider, targetFormat.SampleRate);
            }

            // 채널 다르면 변환 (mono<->stereo)
            if (provider.WaveFormat.Channels != targetFormat.Channels)
            {
                if (provider.WaveFormat.Channels == 1 && targetFormat.Channels == 2)
                {
                    provider = new MonoToStereoSampleProvider(provider);
                }
                else if (provider.WaveFormat.Channels == 2 && targetFormat.Channels == 1)
                {
                    provider = new StereoToMonoSampleProvider(provider);
                }
                else
                {
                    throw new NotSupportedException("지원하지 않는 채널 구성입니다.");
                }
            }

            WaveFormat = targetFormat;

            var whole = new List<float>();
            var buffer = new float[targetFormat.SampleRate * targetFormat.Channels];
            int read;
            while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++) whole.Add(buffer[i]);
            }

            AudioData = whole.ToArray();
        }
    }

    internal class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound cached;
        private long position;

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            cached = cachedSound ?? throw new ArgumentNullException(nameof(cachedSound));
            position = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var available = cached.AudioData.Length - position;
            if (available <= 0) return 0;
            var toCopy = (int)Math.Min(available, count);
            Array.Copy(cached.AudioData, position, buffer, offset, toCopy);
            position += toCopy;
            return toCopy;
        }

        public WaveFormat WaveFormat => cached.WaveFormat;
    }

    #endregion
}
