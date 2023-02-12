using System;
namespace Portfolio.Processors
{
    public abstract class Processor : IDisposable
    {
        public abstract void Dispose();
    }
}
