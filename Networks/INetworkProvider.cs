using System;

namespace R.Client.Services
{
    public interface INetworkProvider
    {
        void SubscribeProcessPacket(Func<CSProtocolID, Packet, bool> subscriber);
        void SubscribeDisposable(IDisposable disposable);
        SessionPacket SessionPacket { get; }
    }
}
