using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for collectible MonoBehaviour controllers.
    /// </summary>
    public interface ICollectibleController : ISpawnableController<ICollectibleData>
    {
        /// <summary>
        /// Initializes the controller with its defining data.
        /// </summary>
        void Initialize(ICollectibleData data);

        /// <summary>
        /// Dữ liệu ScriptableObject định nghĩa vật phẩm này.
        /// </summary>
        ICollectibleData Data { get; }

        GameObject GameObject { get; }
        void OnCollect(IPlayer player);
        void ResetState();
    }
}