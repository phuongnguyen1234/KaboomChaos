namespace Core
{
    public interface IGameloopManager
    {
        void InitializeGame();
        void StartGameLoop();
        void EndGameLoop();
        // Có thể thêm các phương thức khác liên quan đến quản lý gameplay chung ở đây
    }
}