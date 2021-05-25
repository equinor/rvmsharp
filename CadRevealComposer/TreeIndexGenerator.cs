namespace CadRevealComposer
{
    using System;

    public class TreeIndexGenerator
    {
        private const ulong MaxSafeInteger = (1L << 53) - 1;
        
        private ulong _internalIdCounter = 0;

        public ulong GetNextId()
        {
            var candidate = _internalIdCounter++;
            if (candidate > MaxSafeInteger)
                throw new Exception("Too many ids generated");
            return candidate;
        }
    }
}