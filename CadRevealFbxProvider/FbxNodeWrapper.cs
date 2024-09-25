﻿namespace CadRevealFbxProvider;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

public record FbxNode(IntPtr NodeAddress, FbxNode? Parent, int Depth)
{
    /// <summary>
    /// Gets the LocalSpace Transform of the node in the FBX file.
    ///
    /// Wrapper for <see cref="FbxNodeWrapper.GetLocalTransform"/>
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global -- May be useful for debugging
    public Matrix4x4 LocalTransform => this.GetLocalTransform();

    /// <summary>
    /// Gets the WorldSpace Transform of the node in the FBX file.
    ///
    /// Wrapper for <see cref="FbxNodeWrapper.GetLocalTransform"/> for Self and all parents.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global -- May be useful for debugging
    public Matrix4x4 WorldTransform => LocalTransform * (Parent?.WorldTransform ?? Matrix4x4.Identity);

    /// <summary>
    /// The local geometric transform of the node in the FBX file.
    /// This is the transform that should be applied to the geometry (mesh) of the node, BUT does not apply to the nodes children (if any).
    /// </summary>
    // ReSharper disable once UnusedMember.Global -- May be useful for debugging
    public Matrix4x4 LocalGeometricTransform => this.GetGeometricTransform();

    /// <summary>
    /// The WorldSpace Geometric Transform of the node in the FBX file.
    /// This is the transform that should be applied to the geometry (mesh) of the node, BUT does not apply to the nodes children (if any).
    /// </summary>
    public Matrix4x4 WorldGeometricTransform => this.GetGeometricTransform() * this.WorldTransform;
};

public static class FbxNodeWrapper
{
    private const string Library = FbxSdkWrapper.FbxLibraryName;

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_name")]
    private static extern void node_get_name(IntPtr node, StringBuilder nameOut, int bufferSize);

    public static string GetNodeName(this FbxNode node)
    {
        StringBuilder sb = new(512);
        node_get_name(node.NodeAddress, sb, 512);
        return sb.ToString();
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_child_count")]
    private static extern int node_get_child_count(IntPtr node);

    public static int GetChildCount(this FbxNode node)
    {
        return node_get_child_count(node.NodeAddress);
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_child")]
    private static extern IntPtr node_get_child(IntPtr node, int index);

    public static FbxNode GetChild(this FbxNode node, int index)
    {
        return new FbxNode(node_get_child(node.NodeAddress, index), node, node.Depth + 1);
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_transform")]
    private static extern FbxTransform node_get_transform(IntPtr node);

    public static Matrix4x4 GetLocalTransform(this FbxNode node)
    {
        var transform = node_get_transform(node.NodeAddress);
        return FbxTransformConverter.ToMatrix4x4(transform);
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_geometric_transform")]
    private static extern FbxTransform node_get_geometric_transform(IntPtr node);

    public static Matrix4x4 GetGeometricTransform(this FbxNode node)
    {
        var transform = node_get_geometric_transform(node.NodeAddress);
        return FbxTransformConverter.ToMatrix4x4(transform);
    }
}
