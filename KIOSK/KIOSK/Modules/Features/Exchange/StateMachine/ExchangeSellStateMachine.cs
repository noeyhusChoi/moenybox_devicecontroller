using KIOSK.Services;
using KIOSK.Infrastructure.Logging;
using KIOSK.ViewModels;
using KIOSK.Shell.Sub.Menu.ViewModel;
using KIOSK.Shell.Sub.Gtf.ViewModel;
using KIOSK.Shell.Sub.Exchange.ViewModel;

using Stateless;
using System.Windows;
using System.Windows.Threading;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Infrastructure.UI.Navigation.State;

namespace KIOSK.FSM
{
    public enum ExchangeState
    {
        Start,
        Language,
        Currency,
        Terms,
        IDScan,
        IDScanning,
        IDScanningComplete,
        Deposit,
        ApiRequest,
        Withdrawal,
        Result,
        Complete,
        Error,
        Exit
    }

    public partial class ExchangeSellStateMachine
    {
        private readonly INavigationService _nav;
        private readonly ILoggingService _logging;
        private readonly StateMachine<ExchangeState, StateMachineTrigger> _fsm;
        private readonly Stack<ExchangeState> _history = new();
        private readonly SemaphoreSlim _fireLock = new(1, 1);

        public ExchangeSellStateMachine(INavigationService nav, ILoggingService logging)
        {
            _nav = nav;
            _logging = logging;
            _fsm = new StateMachine<ExchangeState, StateMachineTrigger>(ExchangeState.Start);

            // 전이 로깅 및 후처리
            _fsm.OnTransitioned(async trigger =>
            {
                _logging.Info($"{trigger.Source} -> {trigger.Destination} via {trigger.Trigger}");

                // Previous로 전이 완료되면 스택에서 제거
                if (trigger.Trigger.Equals(StateMachineTrigger.Previous) && _history.Count > 0)
                {
                    _history.Pop();
                }

                // Exit로 전이되면 히스토리 초기화
                if (trigger.Destination == ExchangeState.Exit)
                {
                    _history.Clear();
                }

                await Task.CompletedTask;
            });

            ConfigureStates();
        }

        #region Fire wrappers (스레드 안전)
        private async Task FireAsyncSafe(StateMachineTrigger trigger)
        {
            await _fireLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _fsm.FireAsync(trigger).ConfigureAwait(false);    //ConfigureAwait, UI와 관련될 경우 사용 권장
            }
            catch (InvalidOperationException ex)
            {
                _logging.Error(ex, $"invalid transition: {ex.Message}");
                //Trace.WriteLine($"[ExchangeSellStateMachine] invalid transition: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logging.Error(ex, $"fire error: {ex.Message}");
                //Trace.WriteLine($"[ExchangeSellStateMachine] fire error: {ex}");
            }
            finally
            {
                _fireLock.Release();
            }
        }

        public async Task NextAsync()
        {
            // Start(초기 진입)에서 자동으로 Next를 호출할 때는 Start를 히스토리에 쌓지 않음.
            if (_fsm.State != ExchangeState.Start)
            {
                _history.Push(_fsm.State);
            }
            await FireAsyncSafe(StateMachineTrigger.Next);
        }

        public async Task NextAsync(string? key)
        {
            if (_fsm.State != ExchangeState.Start)
            {
                _history.Push(_fsm.State);
            }

            await _fireLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _fsm.FireAsync(StateMachineTrigger.Next, key ?? string.Empty).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                _logging.Error(ex, $"invalid transition: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logging.Error(ex, $"fire error: {ex.Message}");
            }
            finally
            {
                _fireLock.Release();
            }
        }

        public Task PreviousAsync() => FireAsyncSafe(StateMachineTrigger.Previous);

        public Task ExitAsync() => FireAsyncSafe(StateMachineTrigger.Exit);

        public Task ErrorAsync() => FireAsyncSafe(StateMachineTrigger.Error);
        #endregion

        private void ConfigureStates()
        {
            // Start -> Language (Next)
            _fsm.Configure(ExchangeState.Start)
                .OnEntryAsync(async () => await NextAsync())
                .Permit(StateMachineTrigger.Next, ExchangeState.Language);

            // Language 화면
            _fsm.Configure(ExchangeState.Language)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<ExchangeLanguageViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            //Trace.WriteLine(ex, "[ExchangeSellStateMachine] 언어 선택 중 오류 발생");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, ExchangeState.Currency)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                // Previous 는 히스토리 기반으로 동작: PermitDynamic으로 모든 State에서 처리
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // Currency 화면
            _fsm.Configure(ExchangeState.Currency)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<ExchangeCurrencyViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            //Trace.WriteLine(ex, "[ExchangeSellStateMachine] 통화 선택 중 오류 발생");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, ExchangeState.Terms)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // Terms 화면
            _fsm.Configure(ExchangeState.Terms)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<ExchangeIDScanConsentViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            //Trace.WriteLine(ex, "[ExchangeSellStateMachine] 약관 동의 중 오류 발생");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, ExchangeState.IDScan)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // IDScan (준비 화면)
            _fsm.Configure(ExchangeState.IDScan)
                .OnEntryAsync(async () =>
                {
                    var op = Application.Current.Dispatcher.InvokeAsync(async () =>
                    await _nav.NavigateTo<ExchangeIDScanGuideViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            //Trace.WriteLine(ex, "[ExchangeSellStateMachine] 신분증 스캔 준비 오류 발생");
                            await ErrorAsync();
                        };
                    }), DispatcherPriority.ApplicationIdle);

                })
                .Permit(StateMachineTrigger.Next, ExchangeState.IDScanning)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // IDScanning (스캔 중)
            _fsm.Configure(ExchangeState.IDScanning)
                .OnEntryAsync(async () =>
                {
                    var op = Application.Current.Dispatcher.InvokeAsync(async () =>
                    await _nav.NavigateTo<ExchangeIDScanProcessViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            //Trace.WriteLine(ex, "[ExchangeSellStateMachine] 신분증 스캔 중 오류 발생");
                            await ErrorAsync();
                        };
                    }), DispatcherPriority.ApplicationIdle);
                })
                .Permit(StateMachineTrigger.Next, ExchangeState.IDScanningComplete)   // 수정 필요
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // IDScanningComplete (검증/확인 후 입금 단계로)
            _fsm.Configure(ExchangeState.IDScanningComplete)
                .OnEntryAsync(async () =>
                {
                    // 예: 검증 결과 표시 후 다음(입금)으로 이동
                    var op = Application.Current.Dispatcher.InvokeAsync(async () =>
                    await _nav.NavigateTo<ExchangeIDScanCompleteViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    }), DispatcherPriority.ApplicationIdle);
                })
                .Permit(StateMachineTrigger.Next, ExchangeState.Deposit)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // Deposit (입금)
            _fsm.Configure(ExchangeState.Deposit)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<ExchangeDepositViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? res) =>
                        {
                            await NextAsync();
                        };
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, ExchangeState.Withdrawal)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // 출금
            _fsm.Configure(ExchangeState.Withdrawal)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<ExchangeWithdrawalViewModel>(vm =>
                    {
                        vm.OnStepNext = async (string? res) =>
                        {
                            await NextAsync();
                        };
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, ExchangeState.Result)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // 출금 결과 화면
            _fsm.Configure(ExchangeState.Result)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<ExchangeResultViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? res) =>
                        {
                            // 입금이 완료되면 서버 요청 단계로 진행
                            await NextAsync();
                        };
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, ExchangeState.Complete)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            // Complete 화면
            _fsm.Configure(ExchangeState.Complete)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<ExchangeCompleteViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error);

            // Error 화면
            _fsm.Configure(ExchangeState.Error)
                .OnEntryAsync(async () =>
                {
                    _logging.Info($"Error Occured, Fire Exit");
                    await ExitAsync();
                })
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit);

            // Exit (복귀 처리)
            _fsm.Configure(ExchangeState.Exit)
                .OnEntryAsync(async () =>
                {
                    _history.Clear();
                    await _nav.SwitchSubShell<MenuSubShellViewModel>();//<>(vm => { /* 초기화 작업 필요 시 추가 */ });
                });
        }


        // 외부에서 호출 가능한 안전 래퍼들
        public Task StartAsync() => NextAsync(); // Start에서 Next로 이동
        public Task FireNextAsync() => NextAsync();
        public Task FirePreviousAsync() => PreviousAsync();
        public Task FireMainAsync() => ExitAsync();
        public Task FireErrorAsync() => ErrorAsync();

        // 테스트용
        public ExchangeState CurrentState => _fsm.State;
    }
}
