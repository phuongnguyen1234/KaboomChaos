using UnityEngine;
using Bombs.Data;
using Core.Miscellaneous;
using Core;
using Core.Interfaces;

namespace Bombs.Behaviors
{
    /// <summary>
    /// "Strategy" cho thủy lôi (Naval Mine).
    /// Được gắn vào một khối địa hình phá hủy được (DestructibleBlock) nằm ở bề mặt trên cùng:
    /// hiển thị hiệu ứng spawn tại khối, sau đó trôi nổi lên cho đến giới hạn dây xích rồi lơ lửng.
    /// Phát nổ khi va chạm với đối tượng thuộc lớp kích hoạt.
    /// </summary>
    public class NavalMineBehavior : IBombBehavior
    {
        private GameObject _anchorObject;
        private ChainController _chainInstance;
        private float _fixedChainLength;
        private Quaternion _initialRotation;

        private bool _isArmed = false;

        // Trạng thái trôi nổi tự do sau khi mất anchor: thả dây, tắt trọng lực để không rơi,
        // chỉ trôi lên nhẹ trong không trung thay vì rơi xuống như vật thể thường.
        private bool _isFreeFloating = false;

        // Trần tốc độ dao động ngang của thủy lôi khi dây căng (tránh tích lũy vận tốc văng vô hạn khiến vị trí tràn số).
        private const float MaxSwingSpeed = 2.0f;
        // Hệ số giảm dần vận tốc nằm ngang trong giai đoạn trôi lên để mìn đi thẳng lên.
        private const float HorizontalDamping = 2.0f;

        public void OnSetup(BombController controller)
        {
            // Bắt đầu với collider bị vô hiệu hóa và vật lý kinematic.
            // Vật lý và collider chỉ được bật khi đã xác định được điểm neo hợp lệ (trong OnActivate).
            controller.BombCollider.enabled = false;
            controller.BombRigidbody.isKinematic = true;

            // Reset trạng thái cờ để mìn dùng lại từ pool bắt đầu như mới (chưa mất anchor, chưa trôi nổi).
            _isArmed = false;
            _isFreeFloating = false;
            _anchorObject = null;
            if (_chainInstance != null)
            {
                _chainInstance.Detach();
                _chainInstance = null;
            }
        }

        public void OnActivate(BombController controller)
        {
            // Naval Mine yêu cầu một điểm neo là DestructibleBlock được truyền qua BehaviorData.
            if (controller.BehaviorData is not GameObject anchor)
            {
                Debug.LogError("NavalMineBehavior yêu cầu một GameObject làm điểm neo (anchor) trong BombController.BehaviorData. Hủy kích hoạt.", controller);
                Object.Destroy(controller.gameObject);
                return;
            }

            // Chỉ chấp nhận DestructibleBlock làm điểm neo (không còn dùng DestructiblePart).
            if (anchor.GetComponent<DestructibleBlock>() == null)
            {
                Debug.LogError("Điểm neo của Naval Mine không hợp lệ. Phải là DestructibleBlock nằm ở bề mặt trên cùng.", controller);
                Object.Destroy(controller.gameObject);
                return;
            }

            _anchorObject = anchor;
            // Lưu hướng ban đầu để giữ bom không xoay tự do (dây buộc vào nút thắt ở đỉnh).
            _initialRotation = controller.transform.rotation;

            if (controller.BombData is NavalMineData mineData)
            {
                // Giới hạn dây xích = chiều dài tối thiểu để thủy lôi chỉ trôi lên VỪA ĐỦ, không bay vọt quá cao.
                _fixedChainLength = mineData.chainMinLength;

                // Hiệu ứng spawn ngay tại khối điểm neo (nơi thủy lôi xuất hiện) trước khi trôi lên.
                SpawnSpawnVFX(controller, mineData);

                if (mineData.chainPrefab != null)
                {
                    // Đầu dây 'nút thắt' của bom: dây buộc vào điểm neo (anchor) ở một đầu, đầu còn lại buộc vào nút thắt trên quả bom.
                    Transform knot = ResolveKnotPoint(controller);

                    GameObject chainGO = Object.Instantiate(mineData.chainPrefab, controller.transform.position, Quaternion.identity);
                    _chainInstance = chainGO.GetComponent<ChainController>();
                    if (_chainInstance != null)
                    {
                        _chainInstance.SetEndpoints(_anchorObject.transform, knot);
                    }
                }
            }

            // Bóng bay: tắt trọng lực để mìn trôi lên như khí cầu, điều khiển vận tốc trực tiếp (không tích lũy lực vô hạn).
            controller.BombRigidbody.useGravity = false;
            controller.BombRigidbody.isKinematic = false;
            controller.BombRigidbody.linearVelocity = Vector3.zero; // Bắt đầu đứng yên, sau đó trôi lên theo OnFixedUpdate.
            controller.BombCollider.enabled = true;
            _isArmed = true;
        }

        public void OnFixedUpdate(BombController controller)
        {
            if (controller.BombRigidbody == null) return;

            // Trạng thái trôi nổi tự do sau khi mất anchor: trôi lên nhẹ, không rơi xuống.
            if (_isFreeFloating)
            {
                if (controller.BombData is NavalMineData floatData)
                {
                    Vector3 floatVel = controller.BombRigidbody.linearVelocity;

                    // Trôi lên với tốc độ nhỏ hơn tốc độ trôi-khi-được-neo (nổi trên không, không rơi).
                    floatVel.y = Mathf.Lerp(floatVel.y, floatData.ascendSpeed * 0.5f, Time.fixedDeltaTime * 1.5f);

                    // Giảm dần vận tốc ngang để mìn trôi đều, không văng lệch tích lũy.
                    floatVel.x = Mathf.Lerp(floatVel.x, 0f, Time.fixedDeltaTime * HorizontalDamping);
                    floatVel.z = Mathf.Lerp(floatVel.z, 0f, Time.fixedDeltaTime * HorizontalDamping);

                    controller.BombRigidbody.linearVelocity = floatVel;
                }
                return;
            }

            if (!_isArmed) return;

            if (_anchorObject == null || !_anchorObject.activeInHierarchy)
            {
                BecomeFreeFloating(controller);
                return;
            }

            var mineData = (NavalMineData)controller.BombData;
            var rb = controller.BombRigidbody;

            Vector3 anchorPos = _anchorObject.transform.position;
            Vector3 pos = controller.transform.position;
            float dist = (anchorPos - pos).magnitude;

            Vector3 vel = rb.linearVelocity;

            if (dist >= _fixedChainLength)
            {
                // Dây đã căng: triệt tiêu thành phần vận tốc hướng RA XA anchor để không vượt quá chiều dài dây.
                Vector3 outward = pos - anchorPos;
                outward /= dist; // dist > 0 vì mìn và anchor ở hai vị trí khác nhau
                float outwardSpeed = Vector3.Dot(vel, outward);
                if (outwardSpeed > 0f)
                {
                    vel -= outward * outwardSpeed;
                }

                // Giới hạn tốc độ đu đưa ngang (giống bóng bay bị neo) để vận tốc luôn bị chặn, không văng vô hạn.
                vel = Vector3.ClampMagnitude(vel, MaxSwingSpeed);
            }
            else
            {
                // Trong phạm vi dây: trôi lên với tốc độ ổn định (ascendSpeed), không tích lũy gia tốc.
                vel.y = mineData.ascendSpeed;

                // Giảm dần vận tốc nằm ngang để mìn đi thẳng lên, không trôi lệch tích lũy.
                vel.x = Mathf.Lerp(vel.x, 0f, Time.fixedDeltaTime * HorizontalDamping);
                vel.z = Mathf.Lerp(vel.z, 0f, Time.fixedDeltaTime * HorizontalDamping);
            }

            rb.linearVelocity = vel;

            // Giữ bom không xoay tự do: giữ hướng thẳng đứng như một quả bom bị dây buộc vào nút thắt ở đỉnh.
            // Dùng Slerp mềm để bom vẫn đung đưa nhẹ theo chuyển động nhưng không quay lộn vô định.
            float rotLerp = Time.fixedDeltaTime * 6f;
            controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, _initialRotation, rotLerp);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
        }

        /// <summary>
        /// Sinh hiệu ứng spawn ngay tại khối điểm neo và kích hoạt animation của nó (nếu prefab dùng ExplosionEffectController).
        /// </summary>
        /// <param name="controller">Bomb controller của thủy lôi.</param>
        /// <param name="mineData">Dữ liệu thủy lôi chứa prefab VFX và bán kính.</param>
        private void SpawnSpawnVFX(BombController controller, NavalMineData mineData)
        {
            if (mineData.spawnVFXPrefab == null) return;

            GameObject vfx = GameEvents.TriggerVFXSpawnRequest(mineData.spawnVFXPrefab, controller.transform.position, Quaternion.identity);
            if (vfx == null)
            {
                // Fallback: nếu VFXPoolManager chưa hoạt động, tự tạo instance để hiệu ứng vẫn hiển thị khi test.
                Debug.LogWarning("VFXPoolManager không hoạt động. Tự tạo spawn VFX của thủy lôi.", controller);
                vfx = Object.Instantiate(mineData.spawnVFXPrefab, controller.transform.position, Quaternion.identity);
            }

            if (vfx != null && vfx.TryGetComponent<ExplosionEffectController>(out var effectController))
            {
                effectController.Trigger(mineData.spawnVFXRadius);
            }
        }

        /// <summary>
        /// Xác định điểm 'nút thắt' trên quả bom để buộc dây xích. Ưu tiên transform được gán trong Inspector
        /// (TetherKnotPoint), sau đó tìm child có tên gợi ý, cuối cùng tự tạo một điểm nút ảo ở đỉnh bom (nút thắt).
        /// </summary>
        /// <param name="controller">Bomb controller của thủy lôi.</param>
        /// <returns>Transform điểm nút thắt trên quả bom.</returns>
        private Transform ResolveKnotPoint(BombController controller)
        {
            if (controller.TetherKnotPoint != null) return controller.TetherKnotPoint;

            // Tự tìm child có tên gợi ý 'nút thắt' (đã được tạo sẵn trong lần chạy trước hoặc có trên prefab).
            string[] hintNames = { "NavalMineKnot", "Knot", "TetherPoint", "AnchorPoint", "ThaDiem", "NutThat" };
            foreach (string name in hintNames)
            {
                Transform child = controller.transform.Find(name);
                if (child != null) return child;
            }

            // Không tìm thấy: tạo một điểm nút ảo ở đỉnh bom (dựa trên collider) để dây trông như buộc vào nút thắt.
            GameObject knotGO = new("NavalMineKnot");
            Transform knot = knotGO.transform;
            if (controller.BombCollider != null)
            {
                Vector3 top = controller.BombCollider.bounds.center + Vector3.up * controller.BombCollider.bounds.extents.y;
                knot.position = top;
            }
            else
            {
                knot.position = controller.transform.position + Vector3.up * 0.3f;
            }
            knot.SetParent(controller.transform, true); // Giữ nguyên vị trí world, đi theo bom.
            return knot;
        }

        private void BecomeFreeFloating(BombController controller)
        {
            // Chống gọi nhiều lần (ví dụ anchor chết + vụ nổ cùng lúc).
            if (_isFreeFloating) return;
            _isFreeFloating = true;

            // Mất anchor -> thả dây xích (dây không còn neo vào đâu nữa).
            if (_chainInstance != null)
            {
                _chainInstance.Detach();
                _chainInstance = null;
            }
            _anchorObject = null;

            // TRÔI NỔI TỰ DO TRONG KHÔNG TRUNG: KHÔNG dùng trọng lực để mìn không rơi xuống.
            // Nó chỉ trôi lên nhẹ (được duy trì trong OnFixedUpdate) như một quả mìn nổi trên không.
            controller.BombRigidbody.useGravity = false;

            if (controller.BombRigidbody.isKinematic)
            {
                controller.BombRigidbody.isKinematic = false;
            }
            if (!controller.BombCollider.enabled)
            {
                controller.BombCollider.enabled = true;
            }

            // Vẫn giữ _isArmed = true để mìn tiếp tục là mối đe dọa khi trôi: nổ khi chạm player.
        }

        public void OnCollisionEnter(BombController controller, Collision collision)
        {
            if (_isArmed) // Thủy lôi đã sẵn sàng sẽ phát nổ khi va chạm
            {
                TryFuseOnContact(controller, collision.gameObject);
            }
        }

        public void OnTriggerEnter(BombController controller, Collider other)
        {
            if (_isArmed) // Thủy lôi đã sẵn sàng sẽ phát nổ khi đi vào vùng trigger
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
            if (_isArmed)
            {
                BecomeFreeFloating(controller);
            }

            if (controller.BombRigidbody != null && !controller.BombRigidbody.isKinematic)
                controller.BombRigidbody.AddForceAtPosition(force, point, triggeringBombData.ForceMode);
        }
    }
}