namespace CadRevealComposer.IdProviders
{
    using System;
    using System.Collections.Generic;

    // FIXME: placeholder implementation
    public class NodeIdProvider
    {
        private readonly Random _random = new();

        private readonly HashSet<ulong> _generatedIds = new();

        // TODO: this will generate or fetch Node ID based on project, hierarchy, name. The idea is to keep it deterministic if possible
        public ulong GetNodeId(CadRevealNode? cadNode)
        {
            ulong value;
            do
            {
                value = (uint)_random.Next(Int32.MinValue, Int32.MaxValue); // TODO: Expand to Javascript Safe Number range ((2^53)-1)
            } while (_generatedIds.Contains(value));

            _generatedIds.Add(value);
            return value;
        }
    }
}