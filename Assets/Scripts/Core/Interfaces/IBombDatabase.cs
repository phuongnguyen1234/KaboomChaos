using System.Collections.Generic;
using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for mapping bomb data to its prefab.
    /// </summary>
    public interface IBombMapping : ISpawnableMapping<IBaseBombData>
    {
    }
    
    /// <summary>
    /// Interface cho database chứa tất cả các loại bom có trong game.
    /// </summary>
    public interface IBombDatabase : ISpawnableDatabase<IBombMapping, IBaseBombData>
    {
    }
}