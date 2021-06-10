namespace CadRevealComposer.IdProviders
{
    using System;

    public class TreeIndexGenerator
    {
        public const ulong MaxSafeInteger = (1L << 53) - 1;
        
        // ReSharper disable once RedundantDefaultMemberInitializer
        private ulong _internalIdCounter = 0;

        public ulong GetNextId()
        {
            var candidate = _internalIdCounter++;
            if (candidate > MaxSafeInteger)
                throw new Exception("Too many ids generated");
            return candidate;
        }

        public ulong CurrentMaxGeneratedIndex => _internalIdCounter;
    }
}