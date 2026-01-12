using KIOSK.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services
{
    public class BackgroundTaskDescriptor
    {
        public string Name { get; }
        public Func<IServiceProvider, CancellationToken, Task> Action { get; }
        public TimeSpan Interval { get; } // Task 개별 주기

        public BackgroundTaskDescriptor(string name, Func<IServiceProvider, CancellationToken, Task> action, TimeSpan interval)
        {
            Name = name;
            Action = action;
            Interval = interval;
        }
    }

    // 작업 주기 = Fixed-Delay(작업이 끝난 시점으로 딜레이)
    public class BackgroundTaskService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILoggingService _logging;
        private readonly IEnumerable<BackgroundTaskDescriptor> _tasks;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastRun = new();

        // 실행중인 job Task 추적용 (job.Name -> Task)
        private readonly ConcurrentDictionary<string, Task> _runningTasks = new();

        public BackgroundTaskService(IEnumerable<BackgroundTaskDescriptor> tasks, IServiceProvider sp, ILoggingService logging)
        {
            _tasks = tasks;
            _sp = sp;
            _logging = logging;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logging.Info("BackgroundTaskService starting.");

            //// (선택) 서비스 시작 시 "한번만" 모든 job을 즉시 실행하고 싶다면 아래처럼 호출
            //// 주의: 오래걸리는 초기화는 Host start를 지연시키므로 짧게 유지하거나 비동기로 처리하세요.
            //try
            //{
            //    foreach (var job in _tasks)
            //    {
            //        // RunJobSafe를 기다리지 않고 병렬으로 시작하려면 주석 제거 후 Task로 처리 가능.
            //        // 여기서는 각 job을 순차적으로(또는 필요시 병렬) 트리거하는 예시입니다.
            //        var t = RunJobSafe(job, cancellationToken);
            //        // 추적: 만약 시작 시 실행한 작업들을 StopAsync에서 기다리려면 _runningTasks에 추가
            //        _runningTasks.TryAdd(job.Name, t);
            //        // remove when finished
            //        _ = t.ContinueWith(_ => _runningTasks.TryRemove(job.Name, out _), TaskScheduler.Default);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logging.Error(ex, "BackgroundTaskService start-phase jobs failed.");
            //}

            // ExecuteAsync가 동작하도록 base.StartAsync 호출
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logging.Info("BackgroundTaskService ExecuteAsync loop started.");

            var baseInterval = TimeSpan.FromSeconds(1);
            using var timer = new PeriodicTimer(baseInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    foreach (var task in _tasks)
                    {
                        var last = _lastRun.GetOrAdd(task.Name, DateTime.MinValue);
                        
                        
                        if (DateTime.UtcNow - last < task.Interval) continue;

                        var backgroundTask = RunJobSafe(task, stoppingToken);
                        _runningTasks.TryAdd(task.Name, backgroundTask);
                        _ = backgroundTask.ContinueWith(_ => _runningTasks.TryRemove(task.Name, out _), TaskScheduler.Default);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 정상적인 취소 흐름
                _logging.Info("BackgroundTaskService ExecuteAsync canceled.");
            }
            catch (Exception ex)
            {
                _logging.Error(ex, "BackgroundTaskService ExecuteAsync unexpected error.");
            }
            finally
            {
                _logging.Info("BackgroundTaskService ExecuteAsync loop ended.");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logging.Info("BackgroundTaskService stopping...");

            // 1) 기본 Stop 작업 (이 시점에 stoppingToken이 ExecuteAsync로 전달되어 루프가 중단됨)
            // 2) 현재 실행중인 job들을 일정 시간(타임아웃)만큼 기다리기
            TimeSpan waitTimeout = TimeSpan.FromSeconds(10);

            Task[] running;
            running = _runningTasks.Values.ToArray();

            if (running.Length > 0)
            {
                _logging.Info($"Waiting for {running.Length} running background job(s) to finish (timeout {waitTimeout}).");
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(waitTimeout);
                    await Task.WhenAll(running).WaitAsync(cts.Token); // .NET 7+의 WaitAsync 사용 가능, 없으면 아래 대체 사용
                }
                catch (OperationCanceledException)
                {
                    _logging.Warn($"Timeout while waiting for background jobs to finish.");
                }
                catch (Exception ex)
                {
                    _logging.Error(ex, "Error while awaiting running background jobs.");
                }
            }

            await base.StopAsync(cancellationToken);

            _logging.Info("BackgroundTaskService stopped.");
        }

        private async Task RunJobSafe(BackgroundTaskDescriptor job, CancellationToken ct)
        {
            var sem = _locks.GetOrAdd(job.Name, _ => new SemaphoreSlim(1, 1));
            if (!await sem.WaitAsync(0, ct))
            {
                _logging.Debug($"Skipped {job.Name} because previous run is still executing.");
                return;
            }

            try
            {
                using var scope = _sp.CreateScope(); // scoped dependencies OK
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);

                // 필요시 job별 timeout 적용 가능
                // linked.CancelAfter(jobTimeout);

                //_logging.Info($"Starting job: {job.Name}");
                await job.Action(scope.ServiceProvider, linked.Token);
                _lastRun[job.Name] = DateTime.UtcNow;
                _logging.Info($"Background Task Excute  >> [{job.Name}]");
            }
            catch (OperationCanceledException)
            {
                _logging.Warn($"Background Task TimeOut >> [{job.Name}]");
            }
            catch (Exception ex)
            {
                _logging.Error(ex, $"Background Task Failed >> [{job.Name}]");
            }
            finally
            {
                sem.Release();
            }
        }
    }
}
