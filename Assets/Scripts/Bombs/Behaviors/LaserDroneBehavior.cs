using System.Collections;
using System.Collections.Generic;
using Bombs.Data;
using Core;
using Core.Interfaces;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bombs.Behaviors
{
    /// <summary>
    /// Lop "strategy" hanh vi cho Laser Drone - mot doi tuong bay giong bom nhung khong no tai cho.
    /// Trinh tu: ha xuong -Y -> nghi 3s -> xoay Head nhin mot player ngau nhien (Head facing Z+) ->
    /// ngam 3s (hien duong ngam tu Eye + audio) -> co dinh duong ban, nghi 0.5s -> ban chum vụ nổ
    /// chay tu Eye den muc tieu -> reset Head -> bay len -> despawn.
    /// </summary>
    public class LaserDroneBehavior : IBombBehavior
    {
        #region Fields

        // Cac transform con tren prefab: Head (cinh theo truc Z+) va Eye (diem xuat phat tia/ban).
        private Transform _head;
        private Transform _eye;
        private Quaternion _headDefaultLocalRotation;

        // Truc quay trang tri "Copter": xoay lien tuc quanh truc Y khi drone hoat dong.
        private Transform _copter;
        private Quaternion _copterDefaultLocalRotation;
        private float _copterSpinSpeed;

        // Trang thai duong ngam (sight line).
        private GameObject _sightLineInstance;
        private LineRenderer _sightLineRenderer;
        private bool _sightLineHasRenderer;
        private float _sightLineWidth = 0.15f;

        // Muc do xuat hien dan (fade in) cua duong ngam trong 3 giay ngam: 0 = chua hien, 1 = hien day du.
        private float _sightLineFade;

        // VFX tai CUOI duong ngam (diem muc tieu).
        private GameObject _sightEndVFXInstance;

        // Handle loop audio beep.
        private ISfxLoopHandle _beepLoopHandle;

        // Vi tri muc tieu da lock (dung de co dinh duong ban sau khi ngam xong).
        private Vector3 _aimTargetPosition;
        private Coroutine _activeCoroutine;

        #endregion

        #region IBombBehavior

        /// <summary>
        /// Thiet lap trang thai ban dau: drone kinematic, collider tat, cache Head/Eye va audio.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        public void OnSetup(BombController controller)
        {
            // Drone bay tren khong: kinematic, khong trong luc, khong dung collider de va cham.
            controller.BombCollider.enabled = false;
            if (controller.BombRigidbody != null)
            {
                if (!controller.BombRigidbody.isKinematic)
                {
                    controller.BombRigidbody.linearVelocity = Vector3.zero;
                    controller.BombRigidbody.angularVelocity = Vector3.zero;
                }
                controller.BombRigidbody.isKinematic = true;
                controller.BombRigidbody.useGravity = false;
            }

            // Tim Head (mat nhin truoc la Z+) va Eye (diem ban). Neu khong tim thay thi dung chinh transform.
            _head = FindInChildren(controller.transform, "Head");
            if (_head == null) _head = FindInChildren(controller.transform, "LaserHead");
            if (_head == null) _head = controller.transform;

            _eye = FindInChildren(controller.transform, "Eye");
            if (_eye == null) _eye = FindInChildren(controller.transform, "Muzzle");
            if (_eye == null) _eye = FindInChildren(controller.transform, "LaserOrigin");
            if (_eye == null) _eye = _head;

            _headDefaultLocalRotation = _head.localRotation;

            // Tim truc quay trang tri "Copter" (neu co). Khong bat buoc phai co.
            _copter = FindInChildren(controller.transform, "Copter");
            if (_copter == null) _copter = FindInChildren(controller.transform, "Rotor");
            if (_copter != null)
            {
                _copterDefaultLocalRotation = _copter.localRotation;
            }
            else
            {
                _copterDefaultLocalRotation = Quaternion.identity;
            }

            // Lay toc do quay Copter tu data (mac dinh 0 neu chua duoc cau hinh, se duoc gan trong OnActivate).
            _copterSpinSpeed = 0f;

            // Reset trang thai va don dep sach VFX/Audio moi khi duoc thiet lap tu pool hoac khi despawn.
            controller.transform.DOKill();
            CleanupSightAndAudio(controller);
            _aimTargetPosition = controller.transform.position;
            _activeCoroutine = null;
            ResetCopterRotation();
            if (_head != null) _head.localRotation = _headDefaultLocalRotation;
        }

        /// <summary>
        /// Kich hoat drone: bat dau coroutine trinh tu duoi khi bay, ngam va ban.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        public void OnActivate(BombController controller)
        {
            controller.SetActivationState(true);
            if (controller.BombData is LaserDroneData data)
            {
                // Lay toc do quay Copter tu Data.
                _copterSpinSpeed = data.copterSpinSpeed;
                _activeCoroutine = controller.StartCoroutine(DroneRoutine(controller, data));
                controller.SetActiveCoroutine(_activeCoroutine);
            }
        }

        /// <summary>
        /// Quay truc trang tri "Copter" quanh truc Y lien tuc khi drone hoat dong.
        /// Di chuyen bay/ngam duoc xu ly trong coroutine (OnActivate), con viec quay
        /// truc su dung FixedUpdate de chay muot va doc lap voi trinh tu.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        public void OnFixedUpdate(BombController controller)
        {
            if (!controller.IsActive) return;
            if (_copter == null || _copterSpinSpeed <= 0f) return;

            _copter.Rotate(Vector3.up, _copterSpinSpeed * Time.fixedDeltaTime, Space.Self);
        }

        /// <summary>
        /// Collider cua drone bi tat nen khong co va cham vat ly.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="collision">Du lieu va cham.</param>
        public void OnCollisionEnter(BombController controller, Collision collision)
        {
        }

        /// <summary>
        /// Khong co hanh vi khi tiep xuc lien tuc.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="collision">Du lieu va cham.</param>
        public void OnCollisionStay(BombController controller, Collision collision)
        {
        }

        /// <summary>
        /// Khong co hanh vi khi di vao trigger.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="other">Collider khac di vao.</param>
        public void OnTriggerEnter(BombController controller, Collider other)
        {
        }

        /// <summary>
        /// Drone kinematic va mien nhiem voi luc tu cac vụ nổ khac.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="force">Vector luc tac dong.</param>
        /// <param name="point">Diem tac dong cua luc.</param>
        /// <param name="triggeringBombData">Du lieu cua qua bom gay ra vụ nổ.</param>
        public void OnExplosionHit(BombController controller, Vector3 force, Vector3 point, IBaseBombData triggeringBombData)
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine chính dieu khien toan bo trinh tu cua Laser Drone
        /// tu khi ha xuong, ngam, ban cho den khi bay len va despawn.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="data">Du lieu cau hinh cua Laser Drone.</param>
        private IEnumerator DroneRoutine(BombController controller, LaserDroneData data)
        {
            // Buoc 1: Bay xuong -Y mot doan ngau nhien trong [min, max], theo ease out back
            // (ha nhanh, cham dan va co mot do "nảy nhẹ" dac trung cua easeOutBack khi gan cuoi).
            float descend = Random.Range(data.descentDistanceMin, data.descentDistanceMax);
            Vector3 descendFrom = controller.transform.position;
            Vector3 descendTo = descendFrom + Vector3.down * descend;
            float descendDuration = descend / Mathf.Max(data.descentSpeed, 0.001f);
            yield return MoveEased(controller, descendTo, descendDuration, DG.Tweening.Ease.OutBack);

            // Buoc 1.1: Bay len xuong nhe quanh diem ha canh (hover).
            Tween hoverTween = null;
            if (data.hoverAmplitude > 0f && data.hoverDuration > 0f)
            {
                hoverTween = controller.transform.DOMoveY(descendTo.y + data.hoverAmplitude, data.hoverDuration * 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            // Buoc 2: Nghi truoc khi quay Head nhin muc tieu.
            yield return new WaitForSeconds(data.preAimRestDuration);

            // Buoc 3: Chon mot player ngau nhien dang tham gia va con song, lock muc tieu.
            IPlayer targetPlayer = PickRandomTargetPlayer(controller);
            if (targetPlayer == null)
            {
                // Khong con player nao hop le -> khong ban, bo qua giai doan ngam.
                Debug.Log("[LaserDrone] Khong tim thay player nao hop le, drone bay len va despawn.", controller);
            }
            else
            {
                Transform target = targetPlayer.GameObject.transform;
                // Xoay Head (mat nhin Z+) ve phia player va co dinh muc tieu.
                RotateHeadToward(controller, target.position);
                _aimTargetPosition = target.position;

                // Buoc 4: Hien duong ngam + VFX cuoi, va ngam trong 3 giay.
                SpawnSightLine(controller, data);
                PlayAimAudio(controller, data);

                _sightLineFade = 0f;
                float aimElapsed = 0f;
                bool hasValidTarget = true;

                while (aimElapsed < data.aimDuration)
                {
                    // Kiem tra neu player muc tieu hien tai bi chet hoac khong con hop le trong round
                    if (!IsTargetValid(targetPlayer, controller.PlayerManager))
                    {
                        // Chuyen sang muc tieu khac con song trong round
                        IPlayer newTarget = PickRandomTargetPlayer(controller, targetPlayer);
                        if (newTarget != null)
                        {
                            targetPlayer = newTarget;
                            target = targetPlayer.GameObject.transform;
                            _aimTargetPosition = target.position;
                            RotateHeadToward(controller, _aimTargetPosition);
                        }
                        else
                        {
                            // Khong con player nao khac con song trong round
                            hasValidTarget = false;
                            break;
                        }
                    }
                    else
                    {
                        _aimTargetPosition = target.position;
                        RotateHeadToward(controller, _aimTargetPosition);
                    }

                    // Tien trinh ngam (0->1).
                    float t = Mathf.Clamp01(data.aimDuration <= 0f ? 1f : aimElapsed / data.aimDuration);

                    // Duong ngam xuat hien DAN (fade in) theo t.
                    _sightLineFade = t;
                    UpdateSightLine(controller, _aimTargetPosition);

                    // VFX cuoi duong ngam di chuyen theo player khi ngam.
                    UpdateSightEndVFX(controller, _aimTargetPosition);

                    aimElapsed += Time.deltaTime;
                    yield return null;
                }

                if (hasValidTarget)
                {
                    // Audio beep rieng: play tai thoi diem ket thuc aim, loop trong 1 thoi gian ngan (beepLoopDuration).
                    PlayBeepAudio(controller, data);

                    // Luu lai diem xuat phat cua chum ban (Eye).
                    Vector3 fireStart = _eye.position;

                    // Buoc 5: Dung ngam follow player, co dinh duong ban, nghi 0.5s.
                    // Duong ngam va VFX cuoi van hien thi o vi tri co dinh trong luc nay.
                    // Beep chi loop trong beepLoopDuration (ngan), trong luc van cho du postAimDelay.
                    float beepTime = Mathf.Min(data.postAimDelay, data.beepLoopDuration);
                    yield return new WaitForSeconds(beepTime);
                    StopBeepAudio(controller);
                    if (data.postAimDelay > beepTime)
                    {
                        yield return new WaitForSeconds(data.postAimDelay - beepTime);
                    }

                    // Khi khai hoa: AN duong ngam (line renderer) nhung GIU LAI VFX cuoi.
                    HideSightLine(controller);

                    // Buoc 6: Ban chum vu no chay tu Eye den muc tieu.
                    yield return FireBeam(controller, fireStart, _aimTargetPosition, data);

                    // VFX cuoi duong ngam bien mat SAU vu no cuoi cung cua chum ban.
                    DespawnSightEndVFX(controller);
                }
                else
                {
                    // Don dep duong ngam va VFX vi khong con player nao de ban
                    CleanupSightAndAudio(controller);
                }
            }

            // Buoc 7: Dung hover, bay len theo ease in back roi despawn (giu nguyen goc quay Head sau khi ban).
            hoverTween?.Kill();
            ResetCopterRotation();
            CleanupSightAndAudio(controller);

            Vector3 ascendFrom = controller.transform.position;
            Vector3 ascendTo = ascendFrom + Vector3.up * data.ascentDistance;
            float ascendDuration = data.ascentDistance / Mathf.Max(data.ascentSpeed, 0.001f);
            yield return MoveEased(controller, ascendTo, ascendDuration, Ease.InBack);

            controller.SetActivationState(false);
            GameEvents.TriggerBombDespawnRequest(controller.gameObject);
        }

        /// <summary>
        /// Di chuyen drone tu vi tri hien tai den 'to' trong mot khoang thoi gian bang DOTween.
        /// Dung cho ca hanh vi bay xuong (Ease.OutBack) va bay len (Ease.InQuad) voi easing co san.
        /// Tween duoc auto-kill khi hoan tat.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="to">Vi tri the gioi ket thuc.</param>
        /// <param name="duration">Thoi gian di chuyen (giay).</param>
        /// <param name="ease">Kieu easing cua DOTween ap dung.</param>
        private IEnumerator MoveEased(BombController controller, Vector3 to, float duration, Ease ease)
        {
            if (duration <= 0f)
            {
                controller.transform.position = to;
                yield break;
            }

            // DOMove di chuyen tu vi tri hien tai den 'to' voi easing cho truoc.
            Tween tween = controller.transform.DOMove(to, duration).SetEase(ease);
            yield return tween.WaitForCompletion();
        }

        /// <summary>
        /// Chon ngau nhien mot player dang tham gia round va con song.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="exclude">Player muon loai tru khoi danh sach lua chon (vi du: player vua chet).</param>
        /// <returns>IPlayer duoc chon, hoac null neu khong co ai hop le.</returns>
        private IPlayer PickRandomTargetPlayer(BombController controller, IPlayer exclude = null)
        {
            IPlayerManager playerManager = controller.PlayerManager;
            if (playerManager == null) return null;

            var inRound = playerManager.GetPlayersInRound();
            if (inRound == null || inRound.Count == 0) return null;

            List<IPlayer> valid = new();
            for (int i = 0; i < inRound.Count; i++)
            {
                IPlayer p = inRound[i];
                if (p != null && p != exclude && IsTargetValid(p, playerManager))
                {
                    valid.Add(p);
                }
            }

            if (valid.Count == 0) return null;
            return valid[Random.Range(0, valid.Count)];
        }

        /// <summary>
        /// Kiem tra xem mot player co con hop le va con song trong round hay khong.
        /// </summary>
        /// <param name="player">Player can kiem tra.</param>
        /// <param name="playerManager">PlayerManager de kiem tra danh sach trong round.</param>
        /// <returns>True neu player hop le, con song va dang trong round.</returns>
        private static bool IsTargetValid(IPlayer player, IPlayerManager playerManager)
        {
            if (player == null || player.GameObject == null || !player.GameObject.activeInHierarchy)
            {
                return false;
            }

            if (playerManager != null)
            {
                var inRound = playerManager.GetPlayersInRound();
                if (inRound == null || !inRound.Contains(player))
                {
                    return false;
                }
            }

            if (player.GameObject.TryGetComponent<IDamageable>(out var damageable) && !damageable.IsAlive)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Xoay Head (mat nhin Z+) de nhin ve phia diem the gioi tren ca 3 truc
        /// (yaw + pitch, va roll duoc chuan hoa theo vector up). Cho phep Head "facing"
        /// mục tieu truc tiep, bao gồm ca len xuong.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="worldPoint">Diem the gioi (vi tri player) can nhin toi.</param>
        private void RotateHeadToward(BombController controller, Vector3 worldPoint)
        {
            if (_head == null) return;

            Vector3 dir = worldPoint - _head.position; // Hướng day du 3 chieu (khong bỏ truc Y).
            if (dir.sqrMagnitude < 0.0001f) return;    // Target trùng vị trí Head -> bo qua.
            dir.Normalize();

            // LookRotation lam truc forward (Z+) cua Head huong ve muc tieu tren ca 3 truc.
            // Chon up reference on dinh de tranh gimbal lock khi dir song song voi truc Y
            // (target thang tu tren xuong hoac tu duoi len).
            Vector3 referenceUp = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.999f)
            {
                referenceUp = _head.up; // Truong hop thang dung, giu up hien tai de kiem soat roll.
            }
            _head.rotation = Quaternion.LookRotation(dir, referenceUp);
        }

        /// <summary>
        /// Khoi phuc rotation ban dau cua Head (nhin thang truoc, mat nhin Z+) sau khi khai hoa.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private void ResetHead(BombController controller)
        {
            if (_head != null) _head.localRotation = _headDefaultLocalRotation;
        }

        /// <summary>
        /// Khoi phuc rotation ban dau cua truc Copter (dung quay tich luy sai lech qua cac lan dung tu pool).
        /// </summary>
        private void ResetCopterRotation()
        {
            if (_copter != null) _copter.localRotation = _copterDefaultLocalRotation;
        }

        /// <summary>
        /// Ban chum vụ nổ chay tu Eye den muc tieu theo duong da co dinh.
        /// Moi vụ nổ cach nhau explosionInterval (giay); khoang cach = beamTravelSpeed * explosionInterval,
        /// do do muc tieu cang xa thi cang nhieu vụ nổ.
        /// ĐAM BAO: luon co vụ nổ XAC tai diem cuoi (muc tieu).
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="start">Diem bat dau (Eye).</param>
        /// <param name="end">Diem ket thuc (muc tieu da lock).</param>
        /// <param name="data">Du lieu cau hinh cua Laser Drone.</param>
        private IEnumerator FireBeam(BombController controller, Vector3 start, Vector3 end, LaserDroneData data)
        {
            Vector3 dir = end - start;
            float totalDistance = dir.magnitude;

            // Muc tieu ngay tai Eye (trùng): nổ deu chinh tai diem cuoi.
            if (totalDistance < 0.001f)
            {
                controller.TriggerSingleExplosion(end, false);
                yield break;
            }
            dir.Normalize();

            float spacing = data.beamTravelSpeed * data.explosionInterval;
            if (spacing < 0.001f) spacing = 0.001f;

            // Tru vụ nổ dau tien khong nam ngay tai Eye (tranh no ngay tren drone).
            // Loop cuoc tinh diem dat < total distance; vụ nổ cuoi co dinh o cap du ma.
            float travelled = spacing;
            while (travelled < totalDistance)
            {
                Vector3 center = start + dir * travelled;
                controller.TriggerSingleExplosion(center, false);
                travelled += spacing;
                yield return new WaitForSeconds(data.explosionInterval);
            }

            // ĐAM BAO: fire vụ nổ cuoi XAC tai diem muc tieu. Luon co vụ nổ tai cap du.
            if (travelled - spacing < totalDistance)
            {
                yield return new WaitForSeconds(data.explosionInterval);
            }
            controller.TriggerSingleExplosion(end, false);
        }

        /// <summary>
        /// Tao duong ngam (sight line) tai Eye. Neu prefab co LineRenderer thi behavior se
        /// dieu khien 2 diem; nguoc lai dat giua va scale theo chieu dai truc Z+ (1 don vi = 1 world unit).
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="data">Du lieu cau hinh cua Laser Drone.</param>
        private void SpawnSightLine(BombController controller, LaserDroneData data)
        {
            if (data.sightLineVFX == null) return;

            // Dan don duong ngam cu (neu co).
            HideSightLine(controller);

            _sightLineInstance = GameEvents.TriggerVFXSpawnRequest(data.sightLineVFX, _eye.position, Quaternion.identity);
            _sightLineWidth = data.sightLineWidth;
            _sightLineHasRenderer = _sightLineInstance != null &&
                                    _sightLineInstance.TryGetComponent(out _sightLineRenderer);

            // Tao VFX tai cuoi duong ngam (diem muc tieu) neu duoc cau hinh.
            DespawnSightEndVFX(controller);
            if (data.sightEndVFX != null)
            {
                _sightEndVFXInstance = GameEvents.TriggerVFXSpawnRequest(data.sightEndVFX, _aimTargetPosition, Quaternion.identity);
            }
        }

        /// <summary>
        /// Cap nhat duong ngam moi frame de noi Eye den vi tri muc tieu.
        /// Muc do xuat hien (fade in) duoc ap dung tu _sightLineFade (0->1) de duong ngam
        /// hien ra dan trong 3 giay ngam thay vi xuat hien ngay lap tuc.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="targetPosition">Vi tri muc tieu (player) hien tai.</param>
        private void UpdateSightLine(BombController controller, Vector3 targetPosition)
        {
            if (_sightLineInstance == null) return;

            float fade = Mathf.Clamp01(_sightLineFade);

            // Neu prefab dung LineRenderer, cap nhat 2 diem dau-cuoi va do day theo fade.
            if (_sightLineHasRenderer && _sightLineRenderer != null)
            {
                _sightLineRenderer.SetPosition(0, _eye.position);
                _sightLineRenderer.SetPosition(1, targetPosition);

                // Xuat hien dan: do day tang tu 0 len gay day du theo fade.
                float w = (_sightLineWidth > 0f ? _sightLineWidth : 0.1f) * fade;
                _sightLineRenderer.startWidth = w;
                _sightLineRenderer.endWidth = w;
                return;
            }

            // Fallback: dat object o giua Eye va muc tieu, xoay theo huong va scale theo khoang cach.
            Vector3 eye = _eye.position;
            Vector3 dir = targetPosition - eye;
            float dist = dir.magnitude;

            _sightLineInstance.transform.position = (eye + targetPosition) * 0.5f;
            if (dist > 0.0001f)
            {
                _sightLineInstance.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }

            // Xuat hien dan: ca ca do day lan chieu dai deu tang theo fade.
            float thickness = (_sightLineWidth > 0f ? _sightLineWidth : 0.1f) * fade;
            float lengthZ = (dist > 0.0001f ? dist : 0.001f) * fade;
            _sightLineInstance.transform.localScale = new Vector3(thickness, thickness, lengthZ);
        }

        /// <summary>
        /// Cap nhat vi tri cua VFX cuoi duong ngam (theo muc tieu trong luc ngam).
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="targetPosition">Vi tri muc tieu (player) hien tai.</param>
        private void UpdateSightEndVFX(BombController controller, Vector3 targetPosition)
        {
            if (_sightEndVFXInstance != null)
            {
                _sightEndVFXInstance.transform.position = targetPosition;
            }
        }

        /// <summary>
        /// An DUONG NGAM (line renderer) ve pool, nhung GIU LAI VFX cuoi duong ngam.
        /// Dung khi khai hoa: an line, VFX cuoi van con hien cho den khi vụ nổ cuoi ket thuc.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private void HideSightLine(BombController controller)
        {
            if (_sightLineInstance != null)
            {
                GameEvents.TriggerVFXDespawnRequest(_sightLineInstance);
                _sightLineInstance = null;
                _sightLineRenderer = null;
                _sightLineHasRenderer = false;
            }
        }

        /// <summary>
        /// An VFX cuoi duong ngam ve pool. Duoc goi sau vụ nổ cuoi cung cua chum ban.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private void DespawnSightEndVFX(BombController controller)
        {
            if (_sightEndVFXInstance != null)
            {
                GameEvents.TriggerVFXDespawnRequest(_sightEndVFXInstance);
                _sightEndVFXInstance = null;
            }
        }

        /// <summary>
        /// Don dep sach duong ngam, VFX cuoi duong ngam va cac audio loop khi drone despawn hoac duoc reset tu pool.
        /// </summary>
        private void CleanupSightAndAudio(BombController controller)
        {
            HideSightLine(controller);
            DespawnSightEndVFX(controller);
            StopBeepAudio(controller);
            _sightLineFade = 0f;
        }

        /// <summary>
        /// Phat audio ngam (one-shot) khi bat dau ngam.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="data">Du lieu cau hinh cua Laser Drone.</param>
        private void PlayAimAudio(BombController controller, LaserDroneData data)
        {
            if (data.aimAudioClip == null || SfxService.Instance == null) return;

            SfxService.Instance.PlaySfx(data.aimAudioClip, controller.transform.position);
        }

        /// <summary>
        /// Phat audio beep (loop) tai thoi diem ket thuc aim. Day la audio RIENG, tach khoi audio aim.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="data">Du lieu cau hinh cua Laser Drone.</param>
        private void PlayBeepAudio(BombController controller, LaserDroneData data)
        {
            if (data.beepAudioClip == null || SfxService.Instance == null) return;

            _beepLoopHandle = SfxService.Instance.PlaySfxLoop(data.beepAudioClip, controller.transform, 1f, data.beepAudioPitch, false);
        }

        /// <summary>
        /// Dung phat audio beep sau khi het thoi gian loop ngau.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private void StopBeepAudio(BombController controller)
        {
            if (_beepLoopHandle != null)
            {
                _beepLoopHandle.Stop();
                _beepLoopHandle = null;
            }
        }

        /// <summary>
        /// Tim de quy mot child transform theo ten (ho tro child long nhau).
        /// </summary>
        /// <param name="root">Transform goc de bat dau tim.</param>
        /// <param name="name">Ten child can tim.</param>
        /// <returns>Transform tim thay, hoac null.</returns>
        private static Transform FindInChildren(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindInChildren(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        #endregion
    }
}

