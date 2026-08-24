namespace Core.Interfaces
{
    /// <summary>
    /// Interface "Strategy" cho hành vi của một vật phẩm thu thập được.
    /// Nó định nghĩa hành động sẽ xảy ra khi vật phẩm được thu thập.
    /// </summary>
    public interface ICollectibleBehavior
    {
        /// <summary>
    /// Thực thi hành vi của vật phẩm khi được thu thập.
    /// </summary>
    /// <param name="controller">Controller của vật phẩm đang được thu thập.</param>
    /// <param name="player">Người chơi đã thu thập vật phẩm.</param>
    /// <returns>
    /// True nếu vật phẩm được TIÊU THỤ (play SFX + despawn).
    /// False nếu vật phẩm KHÔNG được dùng (vd: đã có Crystal Shield) → giữ nguyên để nhặt lại sau.
    /// </returns>
    bool Execute(ICollectibleController controller, IPlayer player);
    }
}