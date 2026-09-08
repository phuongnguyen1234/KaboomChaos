using UnityEngine;

namespace Core.Utilities
{
    /// <summary>
    /// Script hỗ trợ xoay đối tượng quanh các trục được chỉ định theo thời gian.
    /// Chỉ xoay Transform, không dùng Rigidbody.
    /// </summary>
    public class ObjectRotator : MonoBehaviour
    {
        #region Fields
        [Tooltip("Tốc độ xoay trên từng trục (độ/giây).")]
        [SerializeField] private Vector3 _rotationSpeed = new Vector3(0f, 90f, 0f);

        [Tooltip("Không gian xoay (World hoặc Self/Local).")]
        [SerializeField] private Space _rotationSpace = Space.World;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (_rotationSpeed != Vector3.zero)
            {
                transform.Rotate(_rotationSpeed * Time.deltaTime, _rotationSpace);
            }
        }
        #endregion
    }
}

