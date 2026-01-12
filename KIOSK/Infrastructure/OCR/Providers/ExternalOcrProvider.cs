using KIOSK.Infrastructure.OCR.Models;
using Newtonsoft.Json;
using Pr22.Imaging;
using Pr22.Processing;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace KIOSK.Infrastructure.OCR.Providers
{
    public sealed class ExternalOcrProvider : IOcrProvider
    {
        private readonly OcrOptions _opt;
        public ExternalOcrProvider(OcrOptions opt)
        {
            _opt = opt;
            Directory.CreateDirectory(_opt.InputDir);
            Directory.CreateDirectory(_opt.ResultDir);
            Directory.CreateDirectory(_opt.ResultTypeDir);
        }

        public async Task<OcrOutcome> RunAsync(Page page, CancellationToken ct)
        {
            var sid = EnsureFourDigits("1");
            var job = BuildJob(sid);
            var sw = Stopwatch.StartNew();
            //test
            try
            {
                // 0. 같은 거래 번호 파일 삭제
                var deleted = await DeleteResultsAsync(job, TimeSpan.FromSeconds(5), ct);

                // 1. 이미지 저장
                var infra = page.Select(Light.Infra).DocView().GetImage();
                await Task.Run(() => infra.Save(RawImage.FileFormat.Jpeg).Save(job.InfraImagePath), ct);
               
                // 2. 트리거 파일 생성
                using (File.Create(job.TriggerPath)) { }

                // 3. 결과 대기 (Watcher + 폴링 혼합)
                var (typeJson, resultJson) = await WaitForResultsAsync(job, _opt.ResultTimeout, _opt.PollInterval, ct);

                if (typeJson == null || resultJson == null)
                    return new OcrOutcome { Success = false, Source = "External", Error = "Timed out waiting for OCR results." };

                // 4) 파싱
                var typeObj = JsonConvert.DeserializeObject<ExternalTypeJson>(typeJson);
                var resObj = JsonConvert.DeserializeObject<ExternalResultJson>(resultJson);

                var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (resObj != null)
                {
                    void add(string key, string value)
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                            fields[key] = value;

                    }
                    add("type", resObj.type);
                    add("id", resObj.id);
                    add("name", resObj.name);
                    add("address", resObj.address);
                    add("nation", resObj.nation);
                    add("comment", resObj.comment);
                    add("rotate", resObj.rotate_image.ToString());

                    add("NO", resObj.id);
                    add("NAME", resObj.name);
                    add("NATIONALITY", resObj.nation);
                }

                return new OcrOutcome
                {
                    Success = true,
                    Source = "External",
                    DocumentType = typeObj?.type,
                    Fields = fields,
                    RawTypeJson = typeJson,
                    RawResultJson = resultJson
                };
            }
            catch (Exception ex)
            {
                return new OcrOutcome { Success = false, Source = "External", Error = ex.Message };
            }
            finally
            {
                sw.Stop();
                Trace.WriteLine($"[OCR] OCR Success : Elapsed [{sw.Elapsed}]");
            }
        }

        private ExternalOcrFilePath BuildJob(string sid)
        {
            var img = Path.Combine(_opt.InputDir, $"{sid}_Infra.jpg");
            var white = Path.Combine(_opt.InputDir, $"{sid}_White.jpg");
            var trig = Path.Combine(_opt.InputDir, $"{sid}_Infra.ocr");
            var type = Path.Combine(_opt.ResultTypeDir, $"{sid}_Infra.json");
            var res = Path.Combine(_opt.ResultDir, $"{sid}_Infra.json");
            return new ExternalOcrFilePath
            {
                SessionId = sid,
                InfraImagePath = img,
                WhiteImagePath = white,
                TriggerPath = trig,
                TypeJsonPath = type,
                ResultJsonPath = res
            };
        }

        private static string EnsureFourDigits(string s)
        {
            if (int.TryParse(s, out var n))
                return n.ToString("0000");

            // 숫자가 아니면 해시/랜덤으로 4자리 리턴
            return Math.Abs(s.GetHashCode() % 10000).ToString("0000");
        }

        private static async Task<(string? typeJson, string? resultJson)> WaitForResultsAsync(
            ExternalOcrFilePath job, TimeSpan timeout, TimeSpan poll, CancellationToken ct)
        {
            var tcsType = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcsRes = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var watcherType = new FileSystemWatcher(Path.GetDirectoryName(job.TypeJsonPath)!)
            {
                Filter = Path.GetFileName(job.TypeJsonPath),
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            using var watcherRes = new FileSystemWatcher(Path.GetDirectoryName(job.ResultJsonPath)!)
            {
                Filter = Path.GetFileName(job.ResultJsonPath),
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            FileSystemEventHandler onType = (_, __) => { if (File.Exists(job.TypeJsonPath)) tcsType.TrySetResult(true); };
            FileSystemEventHandler onRes = (_, __) => { if (File.Exists(job.ResultJsonPath)) tcsRes.TrySetResult(true); };

            watcherType.Created += onType; watcherType.Changed += onType;
            watcherRes.Created += onRes; watcherRes.Changed += onRes;

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
            {
                if (File.Exists(job.TypeJsonPath)) tcsType.TrySetResult(true);
                if (File.Exists(job.ResultJsonPath)) tcsRes.TrySetResult(true);

                if (tcsType.Task.IsCompleted && tcsRes.Task.IsCompleted) break;
                await Task.Delay(poll, ct);
            }

            watcherType.Created -= onType; watcherType.Changed -= onType;
            watcherRes.Created -= onRes; watcherRes.Changed -= onRes;

            // === 여기서 '안정화 후 읽기' 적용 ===
            string? typeJson = null, resultJson = null;

            if (File.Exists(job.TypeJsonPath))
                typeJson = await ReadAllTextWhenReadyAsync(job.TypeJsonPath, timeout - sw.Elapsed, ct);

            if (File.Exists(job.ResultJsonPath))
                resultJson = await ReadAllTextWhenReadyAsync(job.ResultJsonPath, timeout - sw.Elapsed, ct);

            return (typeJson, resultJson);
        }


        private static async Task<string?> ReadAllTextWhenReadyAsync(
        string path,
        TimeSpan timeout,
        CancellationToken ct,
        TimeSpan? stableWindow = null,
        TimeSpan? checkInterval = null)
        {
            // 파일 크기/최종수정시각이 일정 시간(stableWindow) 동안 변하지 않고,
            // 공유 잠금이 풀릴 때까지 대기했다가 읽습니다.
            stableWindow ??= TimeSpan.FromMilliseconds(250);
            checkInterval ??= TimeSpan.FromMilliseconds(100);

            var sw = Stopwatch.StartNew();
            long? lastLen = null;
            DateTime? lastWrite = null;
            DateTime stableSince = DateTime.UtcNow;

            while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
            {
                try
                {
                    var fi = new FileInfo(path);
                    if (!fi.Exists)
                    {
                        await Task.Delay(checkInterval.Value, ct);
                        continue;
                    }

                    var len = fi.Length;
                    var w = fi.LastWriteTimeUtc;

                    // 크기/시간이 유지되는지 감시
                    if (lastLen.HasValue && lastLen == len && lastWrite == w)
                    {
                        // 안정화 구간이 충분히 지났다면 시도
                        if ((DateTime.UtcNow - stableSince) >= stableWindow.Value)
                        {
                            // 여기서는 공유 읽기만 허용(다른 프로세스가 Read로는 여전히 열 수 있도록)
                            using var fs = new FileStream(
                                path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                            var text = await sr.ReadToEndAsync();
                            return text;
                        }
                    }
                    else
                    {
                        // 변동이 확인되면 안정화 타이머 리셋
                        stableSince = DateTime.UtcNow;
                        lastLen = len;
                        lastWrite = w;
                    }
                }
                catch (IOException)
                {
                    // 공유 위반(다른 프로세스가 FileShare.None 등으로 쥔 상태) -> 잠시 대기 후 재시도
                }
                catch (UnauthorizedAccessException)
                {
                    // 잠금/권한 문제 → 잠시 대기 후 재시도
                }

                await Task.Delay(checkInterval.Value, ct);
            }

            return null;
        }


        public static async Task<(bool typeDeleted, bool resultDeleted)> DeleteResultsAsync(
        ExternalOcrFilePath job, TimeSpan timeout, CancellationToken ct)
        {
            // 남은 시간 균등 분배(대략) — 상황에 맞게 조절 가능
            var half = TimeSpan.FromMilliseconds(Math.Max(100, timeout.TotalMilliseconds / 2));

            var typeDeletedTask = DeleteFileWhenReadyAsync(job.TypeJsonPath, half, ct);
            var resultDeletedTask = DeleteFileWhenReadyAsync(job.ResultJsonPath, half, ct);

            await Task.WhenAll(typeDeletedTask, resultDeletedTask);
            return (typeDeletedTask.Result, resultDeletedTask.Result);
        }

        public static async Task<bool> DeleteFileWhenReadyAsync(
        string path,
        TimeSpan timeout,
        CancellationToken ct,
        TimeSpan? stableWindow = null,
        TimeSpan? checkInterval = null,
        bool ignoreMissing = true)
        {
            stableWindow ??= TimeSpan.FromMilliseconds(250);
            checkInterval ??= TimeSpan.FromMilliseconds(100);

            var sw = Stopwatch.StartNew();
            long? lastLen = null;
            DateTime? lastWrite = null;
            DateTime stableSince = DateTime.UtcNow;

            while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
            {
                try
                {
                    var fi = new FileInfo(path);
                    if (!fi.Exists)
                    {
                        // 이미 없어졌다면 성공으로 간주할지 여부
                        return ignoreMissing;
                    }

                    // 크기/최종수정시각이 stableWindow 동안 변하지 않는지 관찰
                    var len = fi.Length;
                    var w = fi.LastWriteTimeUtc;

                    if (lastLen.HasValue && lastLen == len && lastWrite == w)
                    {
                        if ((DateTime.UtcNow - stableSince) >= stableWindow.Value)
                        {
                            // 잠금 테스트: 단독으로 열어보기 (공유 금지)
                            // 열리면 다른 프로세스가 사용 중이 아님
                            using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                            {
                                // 읽기/쓰기 핸들이 잡힌 상태에서 바로 닫고 삭제
                            }

                            // 삭제 플래그 방지: ReadOnly 제거
                            File.SetAttributes(path, FileAttributes.Normal);

                            File.Delete(path);
                            return true;
                        }
                    }
                    else
                    {
                        stableSince = DateTime.UtcNow;
                        lastLen = len;
                        lastWrite = w;
                    }
                }
                catch (FileNotFoundException)
                {
                    return ignoreMissing;
                }
                catch (DirectoryNotFoundException)
                {
                    return ignoreMissing;
                }
                catch (IOException)
                {
                    // 공유 위반/사용 중 → 잠시 대기 후 재시도
                }
                catch (UnauthorizedAccessException)
                {
                    // 권한/잠금 → 잠시 대기 후 재시도 (또는 ReadOnly 클리어)
                    try { File.SetAttributes(path, FileAttributes.Normal); } catch { /* ignore */ }
                }

                await Task.Delay(checkInterval.Value, ct);
            }

            return false; // 타임아웃
        }
    }
}
