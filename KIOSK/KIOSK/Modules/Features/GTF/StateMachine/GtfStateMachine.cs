using KIOSK.Modules.GTF;
using KIOSK.Infrastructure.Logging;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Infrastructure.UI.Navigation.State;
using KIOSK.Services;
using KIOSK.ViewModels;
using KIOSK.Shell.Sub.Menu.ViewModel;
using KIOSK.Shell.Sub.Gtf.ViewModel;

using KIOSK.Modules.GTF.ViewModels;
using Stateless;

namespace KIOSK.FSM
{
    public enum GtfState
    {
        Start,
        Language,
        IdScanConsent,
        IdScanGuide,
        IdScanProcess,
        IdScanComplete,
        RefundMethodSelect,
        RefundMethodGuide,
        AlipayGuide,
        CreditGuide,
        WeChatGuide,
        AlipayRegister,
        CreditRegister,
        WeChatRegister,
        AlipayAccountSelect,
        WeChatRegisterGuide,
        Info,
        RegisterQR,
        Sign,
        RefundVoucherRegister,
        RefundComplete,
        Exit,
        Error
    }

    public class GtfStateMachine
    {
        private readonly INavigationService _nav;
        private readonly ILoggingService _logging;
        private readonly IInactivityService _idle;

        // FSM 관련 필드
        private readonly StateMachine<GtfState, StateMachineTrigger> _fsm;  // 상태 머신 인스턴스
        private readonly Stack<GtfState> _history = new();                  // 이전 상태 추적용 스택
        private readonly SemaphoreSlim _fireLock = new(1, 1);               // 상태 전이 동기화용 락
        private StateMachine<GtfState, StateMachineTrigger>.TriggerWithParameters<string> _nextTrigger;  // Next 트리거 (문자열 매개변수 포함)

        public GtfStateMachine(INavigationService nav, ILoggingService logging, IInactivityService idle)
        {
            _nav = nav;
            _logging = logging;
            _idle = idle;

            _fsm = new StateMachine<GtfState, StateMachineTrigger>(GtfState.Start);
            _nextTrigger = _fsm.SetTriggerParameters<string>(StateMachineTrigger.Next);

            // 전이 로깅 및 후처리
            _fsm.OnTransitioned(async trigger =>
            {
                _logging.Info($"{trigger.Source} -> {trigger.Destination} via {trigger.Trigger}");

                // Previous로 전이 완료되면 스택에서 제거
                if (trigger.Trigger.Equals(StateMachineTrigger.Previous) && _history.Count > 0)
                {
                    _history.Pop();
                }

                // Start
                if (trigger.Destination == GtfState.Language)
                    _idle.Start(TimeSpan.FromMinutes(1), async () => await ExitAsync());

                // Exit
                if (trigger.Destination == GtfState.Exit)
                {
                    _idle.Stop();
                    _history.Clear();
                }

                // 그 외
                if (trigger.Destination is not GtfState.Language or GtfState.Exit)
                {
                    _idle.Reset();
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

        public async Task NextAsync()
        {
            // Start(초기 진입)에서 자동으로 Next를 호출할 때는 Start를 히스토리에 쌓지 않음.
            if (_fsm.State != GtfState.Start)
            {
                _history.Push(_fsm.State);
            }
            await FireAsyncSafe(StateMachineTrigger.Next);
        }

        public async Task NextAsync(string? key)
        {
            if (_fsm.State != GtfState.Start)
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
            _fsm.Configure(GtfState.Start)
                .OnEntryAsync(async () => await NextAsync())
                .Permit(StateMachineTrigger.Next, GtfState.Language);

            // Language 화면
            _fsm.Configure(GtfState.Language)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfLanguageSelectViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.IdScanConsent)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 신분증 스캔 Consent 화면
            _fsm.Configure(GtfState.IdScanConsent)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfIdScanConsentViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.IdScanGuide)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 신분증 스캔 Guide 화면
            _fsm.Configure(GtfState.IdScanGuide)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfIdScanGuideViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.IdScanProcess)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 신분증 스캔 Process 화면
            _fsm.Configure(GtfState.IdScanProcess)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfIdScanProcessViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.RefundMethodSelect)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 환급 수단 선택 화면
            _fsm.Configure(GtfState.RefundMethodSelect)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfRefundMethodSelectViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? param) => await NextAsync(param);
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .PermitDynamic(_nextTrigger, method =>
                {
                    var methodLower = method?.ToLowerInvariant();

                    return methodLower switch
                    {
                        "credit" => GtfState.CreditGuide,
                        "alipay" => GtfState.AlipayGuide,
                        "wechat" => GtfState.WeChatGuide,
                        _ => GtfState.Error
                    };
                })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 환급 수단 안내 화면 Credit
            _fsm.Configure(GtfState.CreditGuide)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfCreditGuideViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 환급 수단 안내 화면 Alipay
            _fsm.Configure(GtfState.AlipayGuide)
                .OnEntryAsync(async () =>
            {
                await _nav.NavigateTo<GtfAlipayGuideViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await ExitAsync();
                    vm.OnStepPrevious = async () => await PreviousAsync();
                    vm.OnStepNext = async (string? pass) => await NextAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await ErrorAsync();
                    };
                });
            })
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 환급 수단 안내 화면 WeChat
            _fsm.Configure(GtfState.WeChatGuide)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfWeChatGuideViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 환급 영수증 스캔 화면
            _fsm.Configure(GtfState.RefundVoucherRegister)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfRefundVoucherRegisterViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? param) => await NextAsync(param);
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .PermitDynamic(_nextTrigger, method =>
                {
                    var methodLower = method?.ToLowerInvariant();

                    return methodLower switch
                    {
                        "sign" => GtfState.Sign,
                        "credit" => GtfState.CreditRegister,
                        "alipay" => GtfState.AlipayRegister,
                        "wechat" => GtfState.WeChatRegisterGuide,
                        _ => GtfState.Error
                    };
                })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 환급 사인 화면 (숙박,용역 존재 시)
            _fsm.Configure(GtfState.Sign)
                .OnEntryAsync(async () =>
               {
                   await _nav.NavigateTo<GtfRefundSignatureViewModel>(vm =>
                   {
                       vm.OnStepMain = async () => await ExitAsync();
                       vm.OnStepPrevious = async () => await PreviousAsync();
                       vm.OnStepNext = async (string? param) => await NextAsync(param);
                       vm.OnStepError = async ex =>
                       {
                           _logging.Error(ex, $"OnStepError, {ex.Message}");
                           await ErrorAsync();
                       };
                   });
               })
                 .PermitDynamic(_nextTrigger, method =>
                 {
                     var methodLower = method?.ToLowerInvariant();

                     return methodLower switch
                     {
                         "credit" => GtfState.CreditRegister,
                         "alipay" => GtfState.AlipayRegister,
                         "wechat" => GtfState.WeChatRegisterGuide,
                         _ => GtfState.Error
                     };
                 })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 환급 수단 등록 Credit
            _fsm.Configure(GtfState.CreditRegister)
               .OnEntryAsync(async () =>
               {
                   await _nav.NavigateTo<GtfCreditRegisterViewModel>(vm =>
                   {
                       vm.OnStepMain = async () => await ExitAsync();
                       vm.OnStepPrevious = async () => await PreviousAsync();
                       vm.OnStepNext = async (string? param) => await NextAsync();
                       vm.OnStepError = async ex =>
                       {
                           _logging.Error(ex, $"OnStepError, {ex.Message}");
                           await ErrorAsync();
                       };
                   });
               })
               .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
               .Permit(StateMachineTrigger.Exit, GtfState.Exit)
               .Permit(StateMachineTrigger.Error, GtfState.Error)
               .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);
            
            // 환급 수단 등록 Alipay
            _fsm.Configure(GtfState.AlipayRegister)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfAlipayRegisterViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.AlipayAccountSelect)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);
            
            // 환급 수단 안내
            _fsm.Configure(GtfState.WeChatRegisterGuide)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfWeChatRegisterGuideViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.WeChatRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 알리페이 계좌 선택
            _fsm.Configure(GtfState.AlipayAccountSelect)
              .OnEntryAsync(async () =>
              {
                  await _nav.NavigateTo<GtfAlipayAccountSelectViewModel>(vm =>
                  {
                      vm.OnStepMain = async () => await ExitAsync();
                      vm.OnStepPrevious = async () => await PreviousAsync();
                      vm.OnStepNext = async (string? param) => await NextAsync();
                      vm.OnStepError = async ex =>
                      {
                          _logging.Error(ex, $"OnStepError, {ex.Message}");
                          await ErrorAsync();
                      };
                  });
              })
              .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
              .Permit(StateMachineTrigger.Exit, GtfState.Exit)
              .Permit(StateMachineTrigger.Error, GtfState.Error)
              .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            // 환급 수단 등록 WeChat
            _fsm.Configure(GtfState.WeChatRegister)
                .OnEntryAsync(async () =>
                {
                    await _nav.NavigateTo<GtfWeChatRegisterViewModel>(vm =>
                    {
                        vm.OnStepMain = async () => await ExitAsync();
                        vm.OnStepPrevious = async () => await PreviousAsync();
                        vm.OnStepNext = async (string? pass) => await NextAsync();
                        vm.OnStepError = async ex =>
                        {
                            _logging.Error(ex, $"OnStepError, {ex.Message}");
                            await ErrorAsync();
                        };
                    });
                })
                .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.RefundComplete)
             .OnEntryAsync(async () =>
             {
                 await _nav.NavigateTo<GtfRefundCompleteViewModel>(vm =>
                 {
                     vm.OnStepMain = async () => await ExitAsync();
                     vm.OnStepError = async ex =>
                     {
                         _logging.Error(ex, $"OnStepError, {ex.Message}");
                         await ErrorAsync();
                     };
                 });
             })
             .Permit(StateMachineTrigger.Exit, GtfState.Exit)
             .Permit(StateMachineTrigger.Error, GtfState.Error);

            // Exit (복귀 처리)
            _fsm.Configure(GtfState.Exit)
                .OnEntryAsync(async () =>
                {
                    _history.Clear();
                    await _nav.SwitchSubShell<MenuSubShellViewModel>();
                });

            // Error (복귀 처리)
            _fsm.Configure(GtfState.Error)
                .OnEntryAsync(async () => await PreviousAsync())
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);
        }

        // 외부에서 호출 가능한 안전 래퍼들
        public Task StartAsync() => NextAsync(); // Start에서 Next로 이동
        public Task FireNextAsync() => NextAsync();
        public Task FirePreviousAsync() => PreviousAsync();
        public Task FireMainAsync() => ExitAsync();
        public Task FireErrorAsync() => ErrorAsync();

        // 테스트용
        public GtfState CurrentState => _fsm.State;
    }
}
