using System;
using System.Net;

namespace AutoDiscovery {
    /// <summary>
    /// Class that represents a discovered server
    /// </summary>
    public class ServerData {
        public ServerData(IPEndPoint address, string data, DateTime lastAdvertised) {
            Address = address;
            Data = data;
            LastAdvertised = lastAdvertised;
        }

        public IPEndPoint Address { get; private set; }
        public string Data { get; private set; }
        public DateTime LastAdvertised { get; private set; }

        public override string ToString() {
            return Data;
        }

        protected bool Equals(ServerData other) {
            return Equals(Address, other.Address);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ServerData) obj);
        }

        public override int GetHashCode() {
            return Address != null ? Address.GetHashCode() : 0;
        }
    }
}