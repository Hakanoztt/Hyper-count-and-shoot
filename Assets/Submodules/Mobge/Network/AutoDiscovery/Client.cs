using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ThreadState = System.Threading.ThreadState;

namespace AutoDiscovery {
    /// <summary>
    /// Counterpart of the server, searches for auto discovery server
    /// </summary>
    /// <remarks>
    /// The server list event will not be raised on your main thread!
    /// </remarks>
    public class Client : IDisposable {
        /// <summary>
        /// Remove servers older than this
        /// </summary>
        private static readonly TimeSpan ServerTimeout = new TimeSpan(0, 0, 0, 5); // seconds

        public event Action<IEnumerable<ServerData>> ServersUpdated;

        private Thread _thread;
        private readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly UdpClient _udp = new UdpClient();
        private IEnumerable<ServerData> _currentServers = Enumerable.Empty<ServerData>();

        private bool _running;

        public Client(string serverType) {
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            ServerType = serverType;
            _thread = new Thread(BackgroundLoop) {IsBackground = true};

            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            try {
                _udp.AllowNatTraversal(true);
            }
            catch (Exception ex) {
                Debug.WriteLine("Error switching on NAT traversal: " + ex.Message);
            }

            _udp.BeginReceive(ResponseReceived, null);
        }

        public void Start() {
            if (_thread.ThreadState != (ThreadState.Unstarted | ThreadState.Background)) {
                _thread = new Thread(BackgroundLoop) {IsBackground = true};
            }
            _running = true;
            _thread.Start();
        }

        private void ResponseReceived(IAsyncResult ar) {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            var bytes = _udp.EndReceive(ar, ref remote);

            var typeBytes = Server.Encode(ServerType).ToList();
            Debug.WriteLine(string.Join(", ", typeBytes.Select(_ => (char) _)));
            if (Server.HasPrefix(bytes, typeBytes)) {
                try {
                    var portBytes = bytes.Skip(typeBytes.Count()).Take(2).ToArray();
                    var port = (ushort) IPAddress.NetworkToHostOrder((short) BitConverter.ToUInt16(portBytes, 0));
                    var payload = Server.Decode(bytes.Skip(typeBytes.Count() + 2));
                    NewServer(new ServerData(new IPEndPoint(remote.Address, port), payload, DateTime.Now));
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex);
                }
            }

            _udp.BeginReceive(ResponseReceived, null);
        }

        public string ServerType { get; private set; }

        private void BackgroundLoop() {
            while (_running) {
                try {
                    BroadcastPairInfo();
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex);
                }

                _waitHandle.WaitOne(2000);
                RefreshServers();
            }
        }

        private void BroadcastPairInfo() {
            var probe = Server.Encode(ServerType).ToArray();
            _udp.Send(probe, probe.Length, new IPEndPoint(IPAddress.Broadcast, Server.DiscoveryPort));
        }

        private void RefreshServers() {
            var cutOff = DateTime.Now - ServerTimeout;
            var oldServers = _currentServers.ToList();
            var newServers = oldServers.Where(_ => _.LastAdvertised >= cutOff).ToList();
            if (EnumsEqual(oldServers, newServers)) return;

            var u = ServersUpdated;
            u?.Invoke(newServers);
            _currentServers = newServers;
        }

        private void NewServer(ServerData newServer) {
            var newServers = _currentServers
                .Where(_ => !_.Equals(newServer))
                .Concat(new[] {newServer})
                .OrderBy(_ => _.Data)
                .ThenBy(_ => _.Address, IPEndPointComparer.Instance)
                .ToList();
            var u = ServersUpdated;
            u?.Invoke(newServers);
            _currentServers = newServers;
        }

        private static bool EnumsEqual<T>(IEnumerable<T> xs, IEnumerable<T> ys) {
            return xs.Zip(ys, (x, y) => x.Equals(y)).Count() == xs.Count();
        }

        public void Stop() {
            _running = false;
            _waitHandle.Set();
            _thread.Join();
        }

        public void Dispose() {
            try {
                Stop();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
            }
        }
    }
}