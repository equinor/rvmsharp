namespace RvmSharp.Containers;

using Primitives;

/// <summary>
/// RvmGroup is the base class for everything in the RVM file format.
/// It's an abstract class, where you usually look for <see cref="RvmNode"/> or <see cref="RvmPrimitive"/>
/// </summary>
/// <param name="Version">RVM Format Version (Unused)</param>
public abstract record RvmGroup(uint Version);
