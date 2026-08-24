namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho các đối tượng có thể được hồi máu hoặc tăng máu tối đa.
    /// </summary>
    public interface IHealable
    {
        /// <summary>
        /// Hồi một lượng máu cho đối tượng.
        /// </summary>
        /// <param name="amount">Lượng máu cần hồi.</param>
        /// <returns>Lượng máu thực tế đã được hồi.</returns>
        float Heal(float amount);

        /// <summary>
        /// Tăng máu tối đa của đối tượng.
        /// </summary>
        /// <param name="amount">Lượng máu tối đa cần tăng.</param>
        void IncreaseMaxHealth(float amount);
    }
}