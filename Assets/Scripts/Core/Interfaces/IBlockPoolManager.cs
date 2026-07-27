using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho trình quản lý pooling các khối có thể phá hủy.
    /// Kế thừa từ IGameObjectPoolManager để có các phương thức pooling tiêu chuẩn.
    /// </summary>
    public interface IBlockPoolManager : IGameObjectPoolManager
    {
    }
}