namespace CadRevealComposer.IdProviders;

using System;
using System.Collections.Concurrent;

// FIXME: placeholder implementation

// TODO: NodeId is currently unused, we always use TreeIndex
public class NodeIdProvider
{
    // Using ConcurrentDict as a substitute for the non-existent ConcurrentHashSet.
    private readonly ConcurrentDictionary<ulong, byte> _generatedIds = new ConcurrentDictionary<ulong, byte>();

    // TODO: this will generate or fetch Node ID based on project, hierarchy, name. The idea is to keep it deterministic if possible
    public ulong GetNodeId(CadRevealNode? cadNode)
    {
        ulong value;
        do
        {
            value = (uint)Random.Shared.Next(0,
                Int32.MaxValue); // TODO: Expand to Javascript Safe Number range ((2^53)-1)
        } while (!_generatedIds.TryAdd(value, 0));

        return value;
    }
}