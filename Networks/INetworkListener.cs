using System;

namespace R.Client.Services
{
    public interface INetworkListener
    {
        ClientSocket Socket { get; }
        ClientSocket CreateSocket(string host, int port, Action<Packet> processor);
        void ReceivedPacket(Packet pakcet);
        void Update();
    }
}
