// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;


// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]

    // Why is this added to RvmSharp?
    // C# 9 added support for the init and record keywords. When using C# 9
    // with target frameworks <= .NET 5.0, using these new features is not
    // possible because the compiler is missing the IsExternalInit class,
    // hence making the features unavailable for any target framework prior to .NET 5.
    //
    // Luckily, this problem can be solved by re-declaring the IsExternalInit
    // class as an internal class in your own project. The compiler will use
    // this custom class definition and thus allow you to use both the init
    // keywords and records in any project.

    // ReSharper disable once UnusedType.Global
    internal static class IsExternalInit
    {
    }
}