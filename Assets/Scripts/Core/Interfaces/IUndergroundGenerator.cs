using System.Collections;
using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho hệ thống tạo thế giới ngầm.
    /// </summary>
    public interface IUndergroundGenerator
    {
        /// <summary>
        /// Chiều cao (tọa độ Y) của điểm cao nhất của thế giới ngầm được tạo ra lần cuối.
        /// </summary>
        float LastGeneratedHeight { get; }

        IEnumerator BuildAsync(UndergroundData profile, Transform container, int blocksPerFrame = 50);
    }
}