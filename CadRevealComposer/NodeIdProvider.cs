namespace CadRevealComposer
{
    using System;
    using System.Collections.Generic;

    // FIXME: placeholder implementation
    public class NodeIdProvider
    {
        private readonly Random _random = new Random();

        private readonly HashSet<ulong> _generatedIds = new HashSet<ulong>();

        // TODO: this will generate or fetch Node ID based on project, hierarchy, name. The idea is to keep it deterministic if possible
        public ulong GetNodeId(CadRevealNode? cadNode)
        {
            ulong value;
            do
            {
                value = ((ulong)_random.Next(Int32.MinValue, Int32.MaxValue) << 32) &
                        (ulong)_random.Next(Int32.MinValue, Int32.MaxValue);
            } while (_generatedIds.Contains(value));

            _generatedIds.Add(value);
            return value;
        }
    }
}