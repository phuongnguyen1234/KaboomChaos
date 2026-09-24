using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Du lieu cua mot tab trong BaseTabUI.
    /// Gom nu tab (Button) va prefab noi dung (RectTransform) se duoc cai vao container khi tab duoc chon.
    /// </summary>
    [System.Serializable]
    public class TabItem
    {
        #region Fields
        [Tooltip("Nu dang de chon tab.")]
        [SerializeField] private Button _tabButton;

        [Tooltip("Prefab noi dung UI cua tab. Se duoc nhan ban (Instantiate) vao container khi tab duoc chon.")]
        [SerializeField] private RectTransform _contentPrefab;
        #endregion

        #region Properties
        /// <summary>
        /// Nu de chon tab.
        /// </summary>
        public Button TabButton => _tabButton;

        /// <summary>
        /// Prefab noi dung se duoc hien trong container khi tab duoc chon.
        /// </summary>
        public RectTransform ContentPrefab => _contentPrefab;
        #endregion
    }
}