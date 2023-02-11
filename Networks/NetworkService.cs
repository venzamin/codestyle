using System;

namespace Portfolio.Networks
{
    public abstract class NetworkService : IDisposable
    {
        private SessionPacket _sender;
        protected SessionPacket Sender => _sender;
        protected NetworkService(INetworkProvider components)
        {
            _sender = components.SessionPacket;
            components.SubscribeProcessPacket(ProcessPacket);
            components.SubscribeDisposable(this);
        }
        public abstract bool ProcessPacket(CSProtocolID protocolID, Packet packet);
        public abstract void Dispose();
    }
}
