using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Network
{
    public class NetworkCheckResult
    {
        /// <summary>로컬 머신에서 네트워크 어댑터가 연결된 상태인지 여부</summary>
        public bool IsNetworkAvailable { get; init; }

        /// <summary>DNS가 기본적으로 동작하는지 여부 (예: "www.google.com" 조회)</summary>
        public bool DnsOk { get; init; }

        /// <summary>특정 호스트(IP/도메인)에 Ping 성공 여부</summary>
        public bool HostReachable { get; init; }

        /// <summary>체크 중 에러 메시지 (필요 시 UI에 표시 가능)</summary>
        public string? ErrorMessage { get; init; }
    }

    public interface INetworkService
    {
        /// <summary>
        /// 네트워크 상태를 종합적으로 체크.
        /// hostToPing이 null이면 기본 게이트웨이/외부 사이트 대신 DNS만 확인.
        /// </summary>
        Task<NetworkCheckResult> CheckAsync(string? hostToPing = null, int timeoutMs = 2000);
    }

    public class NetworkService : INetworkService
    {
        public async Task<NetworkCheckResult> CheckAsync(string? hostToPing = null, int timeoutMs = 2000)
        {
            var result = new NetworkCheckResult
            {
                IsNetworkAvailable = NetworkInterface.GetIsNetworkAvailable()
            };

            bool dnsOk = false;
            bool hostOk = false;
            string? error = null;

            try
            {
                // 1) DNS 확인 (기본적으로 외부 도메인 하나 조회)
                dnsOk = await CheckDnsAsync().ConfigureAwait(false);

                // 2) 특정 호스트 핑 체크 (옵션)
                if (!string.IsNullOrWhiteSpace(hostToPing))
                {
                    hostOk = await PingHostAsync(hostToPing!, timeoutMs).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return new NetworkCheckResult
            {
                IsNetworkAvailable = result.IsNetworkAvailable,
                DnsOk = dnsOk,
                HostReachable = hostOk,
                ErrorMessage = error
            };
        }

        /// <summary>
        /// DNS가 기본적으로 동작하는지 확인 (구글 DNS를 예로 사용)
        /// </summary>
        private Task<bool> CheckDnsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    // 외부 도메인 하나만 조회해보는 수준이면 충분
                    var entry = Dns.GetHostEntry("www.google.com");
                    return entry.AddressList != null && entry.AddressList.Length > 0;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// ICMP Ping 으로 호스트 도달 가능 여부 확인
        /// </summary>
        private async Task<bool> PingHostAsync(string host, int timeoutMs)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, timeoutMs).ConfigureAwait(false);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        // 필요하다면 TCP 포트 체크도 추가 가능
        public async Task<bool> CheckTcpPortAsync(string host, int port, int timeoutMs = 2000)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(timeoutMs);

                var completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                if (completed == timeoutTask)
                    return false;

                return client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
