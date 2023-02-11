using System;

namespace Portfolio.Networks
{
    public interface INetworkProvider
    {
        void SubscribeProcessPacket(Func<CSProtocolID, Packet, bool> subscriber);
        void SubscribeDisposable(IDisposable disposable);
        SessionPacket SessionPacket { get; }
    }
}
