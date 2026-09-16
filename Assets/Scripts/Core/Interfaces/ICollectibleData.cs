using System.Collections.Generic;
using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho các biến thể của vật phẩm thu thập.
    /// </summary>
    public interface ICollectibleVariant
    {
        /// <summary>
        /// Dữ liệu của vật phẩm biến thể.
        /// </summary>
        ICollectibleData VariantCollectibleData { get; }

        /// <summary>
        /// Đường cong xác suất (0-100) mà biến thể này sẽ xuất hiện.
        /// </summary>
        AnimationCurve SpawnChanceByDifficulty { get; }
    }
    
    /// <summary>
    /// Interface for collectible ScriptableObject data.
    /// </summary>
    public interface ICollectibleData : ISpawnableData
    {
        string DisplayName { get; }
        AnimationCurve RarityByDifficulty { get; }
        float Lifespan { get; }
        AudioClip CollectionSFX { get; }

        /// <summary>
        /// Hieu ung hinh anh (VFX) khi vat pham duoc thu thap.
        /// </summary>
        GameObject CollectionVFX { get; }

        /// <summary>
        /// Danh sách các biến thể có thể thay thế cho vật phẩm này khi được sinh ra.
        /// Hệ thống sẽ duyệt qua danh sách này và chọn biến thể đầu tiên thỏa mãn xác suất.
        /// </summary>
        IReadOnlyList<ICollectibleVariant> PossibleVariants { get; }
    }
}
