using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AutoDiscovery {
    /// <summary>
    /// Instances of this class can be autodiscovered on the local network through UDP broadcasts
    /// </summary>
    /// <remarks>
    /// The advertisement consists of the server's application type and a short server-specific string.
    /// </remarks>
    public class Server : IDisposable {
        internal const int DiscoveryPort = 35891;
        private readonly UdpClient _udp;

        public Server(string serverType, ushort advertisedPort) {
            ServerType = serverType;
            AdvertisedPort = advertisedPort;
            ServerData = "";

            _udp = new UdpClient();
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

            try {
                _udp.AllowNatTraversal(true);
            }
            catch (Exception ex) {
                Debug.WriteLine("Error switching on NAT traversal: " + ex.Message);
            }
        }

        public void Start() {
            Stopped = false;
            _udp.BeginReceive(ProbeReceived, null);
        }

        public void Stop() {
            Stopped = true;
        }

        private void ProbeReceived(IAsyncResult ar) {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            var bytes = _udp.EndReceive(ar, ref remote);

            // Compare server type to probe type
            var typeBytes = Encode(ServerType);
            if (HasPrefix(bytes, typeBytes)) {
                // If true, respond again with our type, port and payload
                var responseData = Encode(ServerType)
                    .Concat(BitConverter.GetBytes((ushort) IPAddress.HostToNetworkOrder((short) AdvertisedPort)))
                    .Concat(Encode(ServerData)).ToArray();
                _udp.Send(responseData, responseData.Length, remote);
            }

            if (!Stopped) _udp.BeginReceive(ProbeReceived, null);
        }

        internal static bool HasPrefix<T>(IEnumerable<T> haystack, IEnumerable<T> prefix) {
            return haystack.Count() >= prefix.Count() &&
                   haystack.Zip(prefix, (a, b) => a.Equals(b)).All(_ => _);
        }

        /// <summary>
        /// Convert a string to network bytes
        /// </summary>
        internal static IEnumerable<byte> Encode(string data) {
            var bytes = Encoding.UTF8.GetBytes(data);
            var len = IPAddress.HostToNetworkOrder((short) bytes.Length);

            return BitConverter.GetBytes(len).Concat(bytes);
        }

        /// <summary>
        /// Convert network bytes to a string
        /// </summary>
        internal static string Decode(IEnumerable<byte> data) {
            var listData = data as IList<byte> ?? data.ToList();

            var len = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(listData.Take(2).ToArray(), 0));
            if (listData.Count() < 2 + len) throw new ArgumentException("Too few bytes in packet");

            return Encoding.UTF8.GetString(listData.Skip(2).Take(len).ToArray());
        }

        /// <summary>
        /// Return the machine's hostname (usually nice to mention in the beacon text)
        /// </summary>
        public static string HostName {
            get { return Dns.GetHostName(); }
        }

        public string ServerType { get; private set; }
        public ushort AdvertisedPort { get; private set; }
        public bool Stopped { get; private set; }

        public string ServerData { get; set; }

        public void Dispose() {
            Stop();
        }
    }
}