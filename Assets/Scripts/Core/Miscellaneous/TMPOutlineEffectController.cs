using UnityEngine;
using TMPro;

namespace Core
{
    /// <summary>
    /// Quan ly viec ghi de mau sac vien (outline) cua TextMeshPro (ca uGUI va World Space).
    /// Tu tao instance material rieng cho doi tuong nay de tranh ghi de len fontSharedMaterial dung chung.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    [SelectionBase]
    public class TMPOutlineEffectController : MonoBehaviour
    {
        #region Fields
        [Header("Outline Settings")]
        [Tooltip("Bat de script nay ghi de cac thuoc tinh vien goc cua TextMeshPro.")]
        [SerializeField] private bool _applyOutlineOverrides = true;

        [SerializeField, ColorUsage(true, true)]
        [Tooltip("Mau vien mac dinh.")]
        private Color _customOutlineColor = Color.black;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Do day vien mac dinh.")]
        private float _customOutlineWidth = 0.2f;

        private TMP_Text _textComponent;
        private Material _localMaterialInstance;
        private Material _originalSharedMaterial;
        
        private const string OUTLINE_COLOR_PROPERTY = "_OutlineColor";
        private const string OUTLINE_WIDTH_PROPERTY = "_OutlineWidth";
        #endregion

        #region Properties
        /// <summary>
        /// Cho phep truy cap va thay doi mau vien tuy chinh tu cac script khac.
        /// </summary>
        public Color CustomOutlineColor
        {
            get => _customOutlineColor;
            set
            {
                _customOutlineColor = value;
                UpdateOutline();
            }
        }

        /// <summary>
        /// Cho phep truy cap va thay doi do day vien tuy chinh tu cac script khac.
        /// </summary>
        public float CustomOutlineWidth
        {
            get => _customOutlineWidth;
            set
            {
                _customOutlineWidth = Mathf.Clamp01(value);
                UpdateOutline();
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            UpdateOutline();
        }

        private void OnDisable()
        {
            RevertSharedMaterial();
        }

        private void OnDestroy()
        {
            DestroyMaterialInstance();
        }

        private void OnValidate()
        {
            if (enabled)
            {
                UpdateOutline();
            }
        }
        #endregion

        #region Private Methods
        private void UpdateOutline()
        {
            if (_textComponent == null) _textComponent = GetComponent<TMP_Text>();
            if (_textComponent == null) return;

            if (!_applyOutlineOverrides)
            {
                RevertSharedMaterial();
                return;
            }

            EnsureLocalMaterialInstance();

            if (_localMaterialInstance != null)
            {
                _localMaterialInstance.SetColor(OUTLINE_COLOR_PROPERTY, _customOutlineColor);
                _localMaterialInstance.SetFloat(OUTLINE_WIDTH_PROPERTY, _customOutlineWidth);

                _textComponent.UpdateMeshPadding();
            }
        }

        /// <summary>
        /// Dam bao tao mot instance material rieng cho doi tuong nay de khong anh huong den cac text khac.
        /// </summary>
        private void EnsureLocalMaterialInstance()
        {
            if (_textComponent == null) return;

            Material currentShared = _textComponent.fontSharedMaterial;
            if (currentShared == null) return;

            // Neu chua co instance rieng hoac nguoi dung da doi font asset goc
            if (_localMaterialInstance == null || (_originalSharedMaterial != null && currentShared != _originalSharedMaterial && currentShared != _localMaterialInstance))
            {
                DestroyMaterialInstance();
                _originalSharedMaterial = currentShared;
                _localMaterialInstance = new Material(_originalSharedMaterial)
                {
                    name = $"{_originalSharedMaterial.name} (TMPOutline Instance)",
                    hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                };
                _textComponent.fontMaterial = _localMaterialInstance;
            }
        }

        /// <summary>
        /// Khoi phuc lai shared material ban dau va dond dep instance rieng.
        /// </summary>
        private void RevertSharedMaterial()
        {
            if (_textComponent != null && _originalSharedMaterial != null)
            {
                _textComponent.fontSharedMaterial = _originalSharedMaterial;
            }
            DestroyMaterialInstance();
        }

        /// <summary>
        /// Huy instance material rieng an toan trong ca Editor Edit Mode va Play Mode.
        /// </summary>
        private void DestroyMaterialInstance()
        {
            if (_localMaterialInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_localMaterialInstance);
                }
                else
                {
                    DestroyImmediate(_localMaterialInstance);
                }
                _localMaterialInstance = null;
            }
        }
        #endregion
    }
}
