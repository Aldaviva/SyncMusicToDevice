using System;
using JetBrains.Annotations;

namespace SyncMusicToDevice.Injection
{
    /// <inheritdoc />
    /// <summary>
    /// Marker attribute for classes that should be automatically registered in the Dependency Injection container
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [MeansImplicitUse]
    public class ComponentAttribute : Attribute
    {
    }
}