using UnityEngine;
using System.Collections;
using Bombs.Data;
using Core.Miscellaneous;
using Core;
using Core.Interfaces;

namespace Bombs.Behaviors
{
    /// <summary>
    /// "Strategy" cho thủy lôi (Naval Mine).
    /// Trôi lên từ một điểm neo, sau đó lơ lửng bằng một sợi xích và phát nổ khi va chạm.
    /// </summary>
    public class NavalMineBehavior : IBombBehavior
    {
        private GameObject _anchorObject;
        private ChainController _chainInstance;
        private float _fixedChainLength;

        private bool _isArmed = false;
        private bool _isAscending = false;

        public void OnSetup(BombController controller)
        {
            // Bắt đầu với collider bị vô hiệu hóa và vật lý kinematic.
            // Chúng ta sẽ điều khiển chuyển động của nó theo cách thủ công trong giai đoạn trôi lên.
            controller.BombCollider.enabled = false;
            controller.BombRigidbody.isKinematic = true;
        }

        public void OnActivate(BombController controller)
        {
            // Naval Mine yêu cầu một điểm neo được truyền qua BehaviorData.
            if (controller.BehaviorData is not GameObject anchor)
            {
                Debug.LogError("NavalMineBehavior yêu cầu một GameObject làm điểm neo (anchor) trong BombController.BehaviorData. Hủy kích hoạt.", controller);
                Object.Destroy(controller.gameObject);
                return;
            }

            // Kiểm tra xem điểm neo có hợp lệ không (là block hoặc part còn nguyên vẹn)
            if (anchor.GetComponent<DestructibleBlock>() == null &&
                (!anchor.TryGetComponent<DestructiblePart>(out var part) || part.CurrentState != PartState.Intact))
            {
                Debug.LogError("Điểm neo của Naval Mine không hợp lệ. Phải là DestructibleBlock hoặc DestructiblePart còn nguyên vẹn (Intact).", controller);
                Object.Destroy(controller.gameObject);
                return;
            }

            _anchorObject = anchor;

            if (controller.BombData is NavalMineData mineData && mineData.chainPrefab != null)
            {
                GameObject chainGO = Object.Instantiate(mineData.chainPrefab, controller.transform.position, Quaternion.identity);
                _chainInstance = chainGO.GetComponent<ChainController>();
                if (_chainInstance != null)
                {
                    _chainInstance.SetEndpoints(_anchorObject.transform, controller.transform);
                }
            }

            controller.SetActiveCoroutine(controller.StartCoroutine(AscendRoutine(controller)));
        }

        private IEnumerator AscendRoutine(BombController controller)
        {
            _isAscending = true;
            var mineData = (NavalMineData)controller.BombData;

            // Bán kính an toàn tối thiểu phải đủ lớn để bao trùm cả phần thân collider của mìn,
            // tránh việc bật collider lên quá sớm khiến mìn bị kẹt trong các khối lân cận.
            // safeRadius có thể nhỏ hơn kích thước thật của collider, nên ta lấy giá trị lớn hơn.
            float clearanceRadius = Mathf.Max(mineData.safeRadius, GetColliderClearance(controller) + mineData.armClearanceBuffer);

            float currentLength = 0f;
            int consecutiveSafeFrames = 0;

            while (currentLength < mineData.chainMaxLength)
            {
                if (_anchorObject == null) // Nếu điểm neo bị phá hủy trong lúc đang trôi lên
                {
                    BecomeFreeFloating(controller);
                    yield break;
                }

                controller.transform.position += Vector3.up * mineData.ascendSpeed * Time.deltaTime;
                currentLength = Vector3.Distance(controller.transform.position, _anchorObject.transform.position);

                if (currentLength >= mineData.chainMinLength)
                {
                    bool isSafe = !Physics.CheckSphere(
                        controller.transform.position,
                        clearanceRadius,
                        mineData.AffectedLayers | mineData.TriggerLayers,
                        QueryTriggerInteraction.Ignore
                    );

                    // Yêu cầu một vài frame LIÊN TIẾP an toàn trước khi bật collider.
                    // Nếu chỉ cần một frame, một khối nằm sát rìa vùng an toàn có thể gây ra
                    // "kết quả dương tính giả" (clear rồi lại chạm ngay) khi mìn tiếp tục trôi lên.
                    if (isSafe)
                    {
                        consecutiveSafeFrames++;
                        if (consecutiveSafeFrames >= mineData.safeClearFrames)
                        {
                            break;
                        }
                    }
                    else
                    {
                        consecutiveSafeFrames = 0;
                    }
                }

                yield return null;
            }

            if (_anchorObject != null) // Chỉ kích hoạt nếu điểm neo vẫn còn
            {
                ArmMine(controller);
            }
            _isAscending = false;
        }

        /// <summary>
        /// Tính "bán kính thân" của mìn dựa trên extents (nửa kích thước) lớn nhất của collider hiện tại.
        /// Dùng để đảm bảo khoảng cách an toàn khi bật collider đủ lớn để mìn không chui vào khối khác.
        /// </summary>
        /// <param name="controller">Bomb controller chứa collider của mìn.</param>
        /// <returns>Bán kính ước lượng của collider (0 nếu không có collider).</returns>
        private float GetColliderClearance(BombController controller)
        {
            if (controller.BombCollider == null) return 0f;
            Vector3 extents = controller.BombCollider.bounds.extents;
            return Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
        }

        private void ArmMine(BombController controller)
        {
            _isArmed = true;
            _fixedChainLength = Vector3.Distance(controller.transform.position, _anchorObject.transform.position);

            controller.BombCollider.enabled = true;
            controller.BombRigidbody.isKinematic = false;
        }

        public void OnFixedUpdate(BombController controller)
        {
            if (_isAscending) return;

            if (_isArmed)
            {
                if (_anchorObject == null || !_anchorObject.activeInHierarchy ||
                    (_anchorObject.TryGetComponent<DestructiblePart>(out var part) && part.CurrentState != PartState.Intact))
                {
                    BecomeFreeFloating(controller);
                }
                else
                {
                    var mineData = (NavalMineData)controller.BombData;
                    var rb = controller.BombRigidbody;

                    rb.AddForce(Vector3.up * mineData.buoyancyForce, ForceMode.Force);

                    float currentDistance = Vector3.Distance(controller.transform.position, _anchorObject.transform.position);
                    if (currentDistance > _fixedChainLength)
                    {
                        Vector3 directionToAnchor = (_anchorObject.transform.position - controller.transform.position).normalized;
                        float overshoot = currentDistance - _fixedChainLength;
                        rb.AddForce(directionToAnchor * mineData.chainTensionForce * overshoot, ForceMode.Force);
                    }
                    return;
                }
            }

        }

        private void BecomeFreeFloating(BombController controller)
        {
            if (!_isArmed && !_isAscending) return;

            _isArmed = false;
            _isAscending = false;
            _anchorObject = null;
            if (_chainInstance != null)
            {
                _chainInstance.Detach();
                _chainInstance = null;
            }

            if (controller.BombRigidbody.isKinematic)
            {
                controller.BombRigidbody.isKinematic = false;
            }
            if (!controller.BombCollider.enabled)
            {
                controller.BombCollider.enabled = true;
            }
        }

        public void OnCollisionEnter(BombController controller, Collision collision)
        {
            if (_isArmed || (!_isArmed && !_isAscending)) // Kích hoạt nếu được trang bị HOẶC đang trôi tự do
            {
                TryFuseOnContact(controller, collision.gameObject);
            }
        }

        public void OnTriggerEnter(BombController controller, Collider other)
        {
            if (_isArmed || (!_isArmed && !_isAscending)) // Kích hoạt nếu được trang bị HOẶC đang trôi tự do
            {
                TryFuseOnContact(controller, other.gameObject);
            }
        }

        private void TryFuseOnContact(BombController controller, GameObject contactObject)
        {
            if (controller.BombData is BombData fuseBombData && !controller.IsActive)
            {
                LayerMask layersToTest = fuseBombData.TriggerLayers.value == 0 ? fuseBombData.AffectedLayers : fuseBombData.TriggerLayers;

                if ((layersToTest.value & (1 << contactObject.layer)) != 0)
                {
                    controller.StartFuse();
                }
            }
        }

        public void OnCollisionStay(BombController controller, Collision collision) { }

        public void OnExplosionHit(BombController controller, Vector3 force, Vector3 point, IBaseBombData triggeringBombData)
        {
            if (_isArmed || _isAscending)
            {
                BecomeFreeFloating(controller);
            }

            if (controller.BombRigidbody != null && !controller.BombRigidbody.isKinematic)
                controller.BombRigidbody.AddForceAtPosition(force, point, triggeringBombData.ForceMode);
        }
    }
}