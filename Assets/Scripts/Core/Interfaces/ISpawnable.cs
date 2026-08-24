// Tạo file tại: Assets/Scripts/Core/Interfaces/ISpawnable.cs
using UnityEngine;
using System.Collections.Generic;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cơ sở cho mọi loại dữ liệu có thể được sinh ra.
    /// </summary>
    public interface ISpawnableData
    {
        string Id { get; }
    }

    /// <summary>
    /// Interface generic cho controller của một đối tượng có thể sinh ra.
    /// </summary>
    /// <typeparam name="TData">Loại dữ liệu mà controller này được khởi tạo.</typeparam>
    public interface ISpawnableController<in TData> where TData : ISpawnableData
    {
    }

    /// <summary>
    /// Interface generic để ánh xạ dữ liệu với prefab của nó.
    /// </summary>
    public interface ISpawnableMapping<out TData> where TData : ISpawnableData
    {
        TData Data { get; }
        GameObject Prefab { get; }
    }

    /// <summary>
    /// Interface generic cho một database chứa các đối tượng có thể sinh ra.
    /// </summary>
    public interface ISpawnableDatabase<out TMapping, out TData>
        where TMapping : ISpawnableMapping<TData>
        where TData : ISpawnableData
    {
        IReadOnlyList<TMapping> Mappings { get; }
    }
}
