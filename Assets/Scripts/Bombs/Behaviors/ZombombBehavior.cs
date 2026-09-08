using System.Collections.Generic;
using Bombs.Data;
using Core.Interfaces;
using UnityEngine;

namespace Bombs.Behaviors
{
    /// <summary>
    /// Lop "strategy" hanh vi cho bom Zombomb. Ban chat la fuse bomb (hen gio),
    /// nhung khi duoc kich hoat thi lan theo nhung dot (dash) roi nghi de duoi theo
    /// nguoi choi gan nhat neu nguoi choi do nam trong ban kinh theo đuổi cua bom.
    /// </summary>
    public class ZombombBehavior : IBombBehavior
    {
        #region Nested Types

        /// <summary>
        /// Trang thai lan duoi theo hien tai cua Zombomb.
        /// </summary>
        private enum ZombombState
        {
            /// <summary>
            /// Dang nghi giua hai lan lao, bom tu tu dung lai.
            /// </summary>
            Rest,
            /// <summary>
            /// Dang lao toi (lan) theo huong nguoi choi muc tieu.
            /// </summary>
            Dash
        }

        #endregion

        #region Fields

        private Transform _target;
        private IPlayerManager _playerManager;
        private ZombombState _state = ZombombState.Rest;
        private float _stateTimer;

        #endregion

        #region IBombBehavior

        /// <summary>
        /// Thiet lap trang thai ban dau cua bom Zombomb va cache PlayerManager.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        public void OnSetup(BombController controller)
        {
            // Zombomb la collider vat ly ran (solid), khong phai trigger.
            controller.BombCollider.isTrigger = false;
            // Cache instance cua PlayerManager de tim muc tieu trong FixedUpdate.
            _playerManager = controller.PlayerManager;

            // Dat lai cay trang thai moi khi bom duoc thiet lap (hoac reset tu pool).
            _state = ZombombState.Rest;
            _stateTimer = 0f;
            _target = null;
        }

        /// <summary>
        /// Bat đau ngòi nổ (fuse) cho bom. Neu bom cau hinh kich hoat khi cham
        /// (isActivatedOnContact) thi cho va cham voi nguoi choi.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        public void OnActivate(BombController controller)
        {
            // Giong bom hẹn giờ tieu chuan: kích nổ ngay sau fuse time.
            if (controller.BombData is BombData fuseBombData && !fuseBombData.isActivatedOnContact)
            {
                controller.StartFuse();
            }
            // Neu isActivatedOnContact = true, bat đau ngo nao duoc xu ly
            // trong OnCollisionEnter / OnTriggerEnter (xem TryFuseOnContact).
        }

        /// <summary>
        /// Xu ly di chuyen moi FixedUpdate: tim muc tieu gan nhat trong ban kinh
        /// theo đuổi roi thuc hien chu ky Dash (lao) / Rest (nghi).
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        public void OnFixedUpdate(BombController controller)
        {
            if (!controller.IsActive) return;
            if (controller.BombData is not ZombombData data) return;

            // Tim nguoi choi gan nhat nam trong ban kinh theo đuổi.
            FindTarget(controller);

            // Khong co muc tieu hoac muc tieu ngoai pham vi -> dung yen.
            if (_target == null)
            {
                StopRolling(controller);
                return;
            }

            // Dat toi muc tieu (du gan) -> dung lao lai.
            float distanceToTarget = FlatDistance(controller.transform.position, _target.position);
            if (distanceToTarget <= data.stopRange)
            {
                StopRolling(controller);
                return;
            }

            // Chu ky lan tung dot: lao toi (Dash) roi nghi (Rest).
            switch (_state)
            {
                case ZombombState.Rest:
                    _stateTimer -= Time.fixedDeltaTime;
                    // Trong luc nghi, bom tu tu dung lai.
                    Decelerate(controller, data.restDeceleration);
                    if (_stateTimer <= 0f)
                    {
                        _state = ZombombState.Dash;
                        _stateTimer = data.dashDuration;
                    }
                    break;

                case ZombombState.Dash:
                    _stateTimer -= Time.fixedDeltaTime;
                    // Lao toi muc tieu theo huong ngang (mat phang XZ).
                    DashTowardTarget(controller, data);
                    if (_stateTimer <= 0f)
                    {
                        _state = ZombombState.Rest;
                        _stateTimer = data.restDuration;
                    }
                    break;
            }
        }

        /// <summary>
        /// Xu ly khi bom doi dien vao mot collider vat ly.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="collision">Du lieu va cham.</param>
        public void OnCollisionEnter(BombController controller, Collision collision)
        {
            TryFuseOnContact(controller, collision.gameObject);
        }

        /// <summary>
        /// Zombomb khong co hanh vi dac biet khi tiep xuc lien tuc voi collider khac.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="collision">Du lieu va cham dang tiep dien.</param>
        public void OnCollisionStay(BombController controller, Collision collision)
        {
        }

        /// <summary>
        /// Xu ly khi mot trigger (nhu hurtbox cua player) di vao bom.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="other">Collider khac di vao.</param>
        public void OnTriggerEnter(BombController controller, Collider other)
        {
            TryFuseOnContact(controller, other.gameObject);
        }

        /// <summary>
        /// Xu ly khi bom bi tac dong boi mot vu no khac: chi bi day di.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="force">Vector luc tac dong.</param>
        /// <param name="point">Diem tac dong cua luc.</param>
        /// <param name="triggeringBombData">Du lieu cua qua bom gay ra vu no.</param>
        public void OnExplosionHit(BombController controller, Vector3 force, Vector3 point, IBaseBombData triggeringBombData)
        {
            // Zombomb chi bi day di boi cac vu no khac (nhu fuse bomb thuan).
            if (controller.BombRigidbody != null && !controller.BombRigidbody.isKinematic)
                controller.BombRigidbody.AddForceAtPosition(force, point, triggeringBombData.ForceMode);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Tim nguoi choi gan nhat dang tham gia round va con song.
        /// Chi giu muc tieu neu nguoi choi do nam trong ban kinh theo đuổi (chaseRadius).
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private void FindTarget(BombController controller)
        {
            if (_playerManager == null)
            {
                _target = null;
                return;
            }

            // Lay danh sach nguoi choi dang thuc su tham gia round va con hoat dong.
            List<IPlayer> validPlayers = _playerManager.GetPlayersInRound();
            if (validPlayers == null || validPlayers.Count == 0)
            {
                _target = null;
                return;
            }

            // Tim nguoi choi gan nhat theo khoang cach ngang (mat phang XZ).
            Vector3 bombPos = controller.transform.position;
            Transform nearest = null;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < validPlayers.Count; i++)
            {
                IPlayer player = validPlayers[i];
                if (player?.GameObject == null || !player.GameObject.activeInHierarchy) continue;

                float sqrDist = FlatSqrDistance(bombPos, player.GameObject.transform.position);
                if (sqrDist < nearestSqr)
                {
                    nearestSqr = sqrDist;
                    nearest = player.GameObject.transform;
                }
            }

            if (nearest == null)
            {
                _target = null;
                return;
            }

            // Chi theo đuổi neu nguoi choi gan nhat nam trong ban kinh chaseRadius.
            float chaseRadiusSqr = controller.BombData is ZombombData data
                ? data.chaseRadius * data.chaseRadius
                : float.MaxValue;
            _target = nearestSqr <= chaseRadiusSqr ? nearest : null;
        }

        /// <summary>
        /// Lao toi muc tieu theo huong ngang voi toc do dashSpeed;
        /// duy tri trong luc o truc Y de bom roi/lan tren mat dat.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="data">Du lieu cau hinh cua Zombomb.</param>
        private void DashTowardTarget(BombController controller, ZombombData data)
        {
            Rigidbody rb = controller.BombRigidbody;
            if (rb == null) return;

            // Huong ngang tu bom toi muc tieu.
            Vector3 dir = _target.position - controller.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            Vector3 vel = rb.linearVelocity;
            vel.x = dir.x * data.dashSpeed;
            vel.z = dir.z * data.dashSpeed;
            // Giu nguyen thanh phan Y de trong luc van tac dung lam bom roi xuong mat dat.
            rb.linearVelocity = vel;

            // Lan khong truot: toc do quay duoc suy ra tu toc do tinh tien va ban kinh sphere.
            ApplyRollingSpin(controller, vel, data.rollMultiplier);
        }

        /// <summary>
        /// Ham van toc ngang (XZ) cho den khi dung lai muot khi dang nghi hoac khong co muc tieu.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="deceleration">Gia toc cham (don vi/giay binh phuong).</param>
        private void Decelerate(BombController controller, float deceleration)
        {
            Rigidbody rb = controller.BombRigidbody;
            if (rb == null) return;

            Vector3 vel = rb.linearVelocity;
            vel.x = Mathf.MoveTowards(vel.x, 0f, deceleration * Time.fixedDeltaTime);
            vel.z = Mathf.MoveTowards(vel.z, 0f, deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = vel;

            // Trong luc nghi, toc do quay cung giam dan theo van toc thuc te (lan khong truot).
            ApplyRollingSpin(controller, vel, 1f);
        }

        /// <summary>
        /// Lan khong truot tren mat phang ngang: dat van toc goc omega = tocDo / banKinhSphere,
        /// quanh truc vuong goc voi ca huong di chuyen va mat dat (Cross(up, huong) ).
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="horizontalVelocity">Van toc tinh tien hien tai (de suy ra toc do lan).</param>
        /// <param name="multiplier">He so nhan toc do quay (1 = lan chuan khong truot).</param>
        private static void ApplyRollingSpin(BombController controller, Vector3 horizontalVelocity, float multiplier)
        {
            Rigidbody rb = controller.BombRigidbody;
            if (rb == null) return;

            Vector3 horiz = horizontalVelocity;
            horiz.y = 0f;
            float speed = horiz.magnitude;

            // Van toc gan bang 0 hoac khong mong muon xoay -> dung quay.
            if (speed < 0.01f || multiplier <= 0f)
            {
                rb.angularVelocity = Vector3.zero;
                return;
            }

            float worldRadius = GetWorldRadius(controller);
            if (worldRadius < 0.0001f)
            {
                rb.angularVelocity = Vector3.zero;
                return;
            }

            // Omega (rad/s) = v / r, quay quanh truc Cross(up, huong di chuyen).
            Vector3 dir = horiz / speed;
            Vector3 rollAxis = Vector3.Cross(Vector3.up, dir).normalized;
            rb.angularVelocity = rollAxis * ((speed / worldRadius) * multiplier);
        }

        /// <summary>
        /// Ban kinh the gioi (world) cua collider hinh cau cua bom, tinh theo ca radius va scale.
        /// Dung de suy ra toc do quay lan tu toc do tinh tien (lan khong truot).
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private static float GetWorldRadius(BombController controller)
        {
            // Zombomb dung SphereCollider nen tinh ban kinh world = radius * scale (gia dinh scale dong nhat).
            if (controller.BombCollider is SphereCollider sphere)
            {
                float scale = Mathf.Max(controller.transform.lossyScale.x, 0.0001f);
                return sphere.radius * scale;
            }
            // Khong phai sphere collider: khong the tinh lan chuan, tra ve 0.
            return 0f;
        }

        /// <summary>
        /// Dung lai hoan toan khi khong con muc tieu hoac da dat muc tieu:
        /// ve khong van toc ngang va dung xoay.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private void StopRolling(BombController controller)
        {
            Rigidbody rb = controller.BombRigidbody;
            if (rb == null) return;

            Vector3 vel = rb.linearVelocity;
            vel.x = 0f;
            vel.z = 0f;
            rb.linearVelocity = vel;
            rb.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Kiem tra va kích hoạt ngòi nổ cho bom landmine khi co va cham.
        /// Duoc goi tu ca OnCollisionEnter va OnTriggerEnter.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="contactObject">GameObject da va cham.</param>
        private void TryFuseOnContact(BombController controller, GameObject contactObject)
        {
            if (contactObject == null) return;

            // Chi xu ly cho bom kich hoat khi cham (isActivatedOnContact) va chua kich hoat.
            if (controller.BombData is BombData fuseBombData && fuseBombData.isActivatedOnContact && !controller.IsActive)
            {
                // Uu tien TriggerLayers, neu khong thi dung AffectedLayers.
                LayerMask layersToTest = controller.BombData.TriggerLayers.value == 0
                    ? controller.BombData.AffectedLayers
                    : controller.BombData.TriggerLayers;

                // Chi kích hoạt neu layer cua doi tuong va cham nam trong danh sach duoc phep.
                if ((layersToTest.value & (1 << contactObject.layer)) != 0)
                {
                    controller.StartFuse();
                }
            }
        }

        /// <summary>
        /// Khoang cach ngang (XZ) giua hai vi tri the gioi.
        /// </summary>
        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Binh phuong khoang cach ngang (XZ) giua hai vi tri the gioi, tranh phep can bac hai.
        /// </summary>
        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        #endregion
    }
}