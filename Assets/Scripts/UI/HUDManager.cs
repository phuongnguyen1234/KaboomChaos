using UnityEngine;
using UnityEngine.UI; // Cần cho Slider
using TMPro; // Cần cho TextMeshPro
using Core;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Quản lý các thành phần của Heads-Up Display (HUD) như thanh máu, năng lượng, v.v.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Health Components")]
        [Tooltip("Slider hiển thị thanh máu.")]
        [SerializeField] private Slider _healthSlider;
        [Tooltip("Text hiển thị số máu hiện tại.")]
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("Energy Components")]
        [Tooltip("Slider hiển thị thanh năng lượng.")]
        [SerializeField] private Slider _energySlider;

        #region Unity Lifecycle

        private void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerHealthChanged(IPlayer player, float currentHealth, float maxHealth)
        {
            // Trong một game nhiều người chơi, bạn có thể muốn kiểm tra xem 'player'
            // có phải là người chơi cục bộ (local player) hay không trước khi cập nhật HUD.
            // if (player.IsLocalPlayer) { ... }

            UpdateHealth(currentHealth, maxHealth);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cập nhật UI thanh máu.
        /// </summary>
        /// <param name="currentHealth">Máu hiện tại.</param>
        /// <param name="maxHealth">Máu tối đa.</param>
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (maxHealth <= 0) return;

            if (_healthSlider != null)
            {
                // Đảm bảo slider được cấu hình đúng với giá trị max, ví dụ 100.
                if (_healthSlider.maxValue != maxHealth) _healthSlider.maxValue = maxHealth;
                // Gán trực tiếp giá trị máu hiện tại, không phải dạng phần trăm (0-1).
                _healthSlider.value = currentHealth;
            }

            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.Clamp(currentHealth, 0, maxHealth)}";
            }
        }

        /// <summary>
        /// Cập nhật UI thanh năng lượng.
        /// </summary>
        /// <param name="currentEnergy">Năng lượng hiện tại.</param>
        /// <param name="maxEnergy">Năng lượng tối đa.</param>
        public void UpdateEnergy(float currentEnergy, float maxEnergy)
        {
            if (maxEnergy <= 0) return;

            if (_energySlider != null)
            {
                _energySlider.value = currentEnergy / maxEnergy;
            }
        }

        #endregion
    }
}