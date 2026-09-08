using UnityEngine;
using TMPro;
using Core;

namespace UI
{
    /// <summary>
    /// Hieu thi text "AFK" tren dau Player khi nguoi choi bat che do AFK (tu Option Menu).
    /// Dat component nay len prefab Player (o assembly UI de dung duoc TextMeshPro world-space),
    /// va gan vao mot child TextMeshPro dat phia tren dau player.
    /// - Lang nghe su kien trang thai AFK thay doi de hien/an text.
    /// - Khoi phuc trang thai da luu tru khi vao game.
    /// - Billboard (facing camera): moi frame xoay text huong ve Camera chinh de luon thay duoc
    ///   tuc ke ca khi player/camera xoay lung tung trong goc nhin thu ba.
    /// </summary>
    public class PlayerAfkIndicator : MonoBehaviour
    {
        #region Fields

        [Tooltip("TextMeshPro (world-space) dat phia tren dau player de hien chu 'AFK'. Hien khi AFK bat, an khi tat.")]
        [SerializeField] private TextMeshPro _afkLabel;

        [Tooltip("(Tuy chon) Truc xoay (pivot) cua text de billboard. Neu bo trong se xoay truc tiep _afkLabel.")]
        [SerializeField] private Transform _billboardPivot;

        // Trang thai AFK hien tai de billboard chi chay khi text dang hien.
        private bool _afkOn;

        // Camera chinh de billboard (cache lay 1 lan, tuan thu). null neu chua co.
        private Camera _mainCamera;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            GameEvents.OnAfkStateChanged += HandleAfkStateChanged;

            // Dong bo trang thai AFK ngay khi component bat (vao game / respawn).
            RefreshAfkIndicator();
        }

        private void OnDisable()
        {
            GameEvents.OnAfkStateChanged -= HandleAfkStateChanged;
        }

        private void LateUpdate()
        {
            // Chi billboard khi text dang hien (AFK dai). Khi khong AFK bo qua de tiet kiem.
            if (!_afkOn) return;

            BillboardTowardCamera();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Phan hoi khi trang thai AFK thay doi: hien/an text AFK tren dau player.
        /// </summary>
        /// <param name="enabled">True neu AFK dang bat.</param>
        private void HandleAfkStateChanged(bool enabled)
        {
            SetAfkLabelActive(enabled);
        }

        /// <summary>
        /// Dong bo text AFK theo trang thai luu tru hien tai.
        /// Neu _afkLabel chua duoc gan tren Inspector, tu dong tim child ten "AFKLabel" (de phong).
        /// </summary>
        private void RefreshAfkIndicator()
        {
            EnsureLabelResolved();

            bool enabled = GameEvents.TriggerRequestAfkEnabled();
            SetAfkLabelActive(enabled);
        }

        /// <summary>
        /// Bat/tat text AFK tren dau player (neu da gan).
        /// </summary>
        /// <param name="active">True de hien text, false de an.</param>
        private void SetAfkLabelActive(bool active)
        {
            EnsureLabelResolved();

            if (_afkLabel == null) return;

            _afkOn = active;

            if (_afkLabel.gameObject.activeSelf != active)
            {
                _afkLabel.gameObject.SetActive(active);
            }

            if (active && string.IsNullOrEmpty(_afkLabel.text))
            {
                _afkLabel.text = "AFK";
            }

            // Ngay khi hien thi, xoay text ve phia camera mot lan (billboard tuc thi).
            if (active)
            {
                BillboardTowardCamera();
            }
        }

        /// <summary>
        /// Xoay pivot/text huong ve Camera chinh moi frame (billboard),
        /// dam bao chu "AFK" luon nhin thay khi player quay lung lai camera.
        /// </summary>
        private void BillboardTowardCamera()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return; // Chua co camera (luc chua vao game).
            }

            Transform target = _billboardPivot != null ? _billboardPivot : (_afkLabel != null ? _afkLabel.transform : null);
            if (target == null) return;

            target.rotation = _mainCamera.transform.rotation;
        }

        /// <summary>
        /// Tu dong tim thanh phan _afkLabel neu chua duoc gan (child ten "AFKLabel" hoac
        /// component TextMeshPro dau tien tren child). Giup text hoat dong ke ca khi quen gan.
        /// </summary>
        private void EnsureLabelResolved()
        {
            if (_afkLabel != null) return;

            _afkLabel = GetComponentInChildren<TextMeshPro>();
            if (_afkLabel != null)
            {
                Debug.LogWarning("[PlayerAfkIndicator] _afkLabel chua duoc gan tren Inspector - tu dong dung TextMeshPro dau tien tim thay trong child.", this);
            }
        }

        #endregion
    }
}