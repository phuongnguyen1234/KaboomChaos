namespace Bombs.Explosions
{
    /// <summary>
    /// Interface cho các "strategy" xử lý logic khi một quả bom nổ.
    /// Điều này cho phép tách biệt các hành vi nổ phức tạp (chùm, nhiều lần, Matryoshka)
    /// ra khỏi BombController.
    /// </summary>
    public interface IExplosionStrategy
    {
        /// <summary>
        /// Thực thi logic của vụ nổ.
        /// </summary>
        /// <param name="controller">BombController đang thực hiện vụ nổ.</param>
        void Execute(BombController controller);
    }
}