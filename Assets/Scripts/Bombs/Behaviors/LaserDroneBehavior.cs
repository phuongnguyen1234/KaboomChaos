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
                // QUAN TRONG: Phai xoa van toc TRUOC khi bat kinematic, vi Unity khong cho phep
                // gan linear/angularVelocity len mot rigidbody dang kinematic (se in warning
                // "Setting ... velocity of a kinematic body is not supported").
                controller.BombRigidbody.linearVelocity = Vector3.zero;
                controller.BombRigidbody.angularVelocity = Vector3.zero;
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

            // Reset trang thai moi khi duoc thiet lap tu pool.
            _sightLineInstance = null;
            _sightLineRenderer = null;
            _sightLineHasRenderer = false;
            _sightLineFade = 0f;
            _sightEndVFXInstance = null;
            _aimTargetPosition = controller.transform.position;
            _activeCoroutine = null;
            ResetCopterRotation();
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

            // Buoc 2: Nghi truoc khi quay Head nhin muc tieu.
            yield return new WaitForSeconds(data.preAimRestDuration);

            // Buoc 3: Chon mot player ngau nhien dang tham gia va con song, lock muc tieu.
            Transform target = PickRandomTarget(controller);
            if (target == null)
            {
                // Khong con player nao -> khong ban, bo qua giai doan ngam.
                Debug.Log("[LaserDrone] Khong tim thay player nao, drone bay len va despawn.", controller);
            }
            else
            {
                // Xoay Head (mat nhin Z+) ve phia player va co dinh muc tieu.
                RotateHeadToward(controller, target.position);
                _aimTargetPosition = target.position;

                // Buoc 4: Hien duong ngam + VFX cuoi, va ngam trong 3 giay.
                SpawnSightLine(controller, data);
                PlayAimAudio(controller, data);

                _sightLineFade = 0f;
                float aimElapsed = 0f;
                while (aimElapsed < data.aimDuration)
                {
                    // Tien trinh ngam (0->1).
                    float t = Mathf.Clamp01(data.aimDuration <= 0f ? 1f : aimElapsed / data.aimDuration);

                    // Neu player van con hoat dong thi cap nhat vi tri de theo dau;
                    // nguoc lai giu vi tri cuoi da lock.
                    if (target != null && target.gameObject != null && target.gameObject.activeInHierarchy)
                    {
                        _aimTargetPosition = target.position;
                    }

                    // Pitch audio tang dan theo t va co dinh o cuoi (t=1 -> pitch = aimAudioPitchEnd).
                    SetAimAudioPitch(controller, data, t);

                    // Duong ngam xuat hien DAN (fade in) theo t.
                    _sightLineFade = t;
                    UpdateSightLine(controller, _aimTargetPosition);

                    // VFX cuoi duong ngam di chuyen theo player khi ngam.
                    UpdateSightEndVFX(controller, _aimTargetPosition);

                    aimElapsed += Time.deltaTime;
                    yield return null;
                }
                // Pitch co dinh o gia tri ket thuc ngam.
                SetAimAudioPitch(controller, data, 1f);
                StopAimAudio(controller);

                // Audio beep rieng: play tai thoi diem ket thuc aim, loop trong 1 thoi gian ngau (beepLoopDuration).
                PlayBeepAudio(controller, data);

                // Luu lai diem xuat phat cua chum ban (Eye).
                Vector3 fireStart = _eye.position;

                // Buoc 5: Dung ngam follow player, co dinh duong ban, nghi 0.5s.
                // Duong ngam va VFX cuoi van hien thi o vi tri co dinh trong lúc nay.
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

                // Buoc 6: Ban chum vụ nổ chay tu Eye den muc tieu.
                yield return FireBeam(controller, fireStart, _aimTargetPosition, data);

                // VFX cuoi duong ngam bien mat SAU vụ nổ cuoi cung cua chum ban.
                DespawnSightEndVFX(controller);
            }

            // Buoc 7: Reset Head nhin thang truoc, bay len NHANH DAN (ease in) roi despawn.
            ResetHead(controller);
            ResetCopterRotation();
            HideSightLine(controller);        // An toan neu chua an.
            DespawnSightEndVFX(controller);    // An toan neu chua tao.
            StopAimAudio(controller);          // An toan neu chua phat.

            Vector3 ascendFrom = controller.transform.position;
            Vector3 ascendTo = ascendFrom + Vector3.up * data.ascentDistance;
            float ascendDuration = data.ascentDistance / Mathf.Max(data.ascentSpeed, 0.001f);
            yield return MoveEased(controller, ascendTo, ascendDuration, Ease.InQuad);

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
        /// Chon ngau nhien mot player dang tham gia round va con hoat dong.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <returns>Transform cua player duoc chon, hoac null neu khong co ai.</returns>
        private Transform PickRandomTarget(BombController controller)
        {
            IPlayerManager playerManager = controller.PlayerManager;
            if (playerManager == null) return null;

            List<IPlayer> valid = new();
            var inRound = playerManager.GetPlayersInRound();
            if (inRound == null) return null;

            for (int i = 0; i < inRound.Count; i++)
            {
                IPlayer player = inRound[i];
                if (player?.GameObject != null && player.GameObject.activeInHierarchy)
                {
                    valid.Add(player);
                }
            }

            if (valid.Count == 0) return null;
            return valid[Random.Range(0, valid.Count)].GameObject.transform;
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
        /// Phat audio ngam (loop) trong suot thoi gian ngam. Pitch ban dau tai gia tri aimAudioPitchStart.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="data">Du lieu cau hinh cua Laser Drone.</param>
        private void PlayAimAudio(BombController controller, LaserDroneData data)
        {
            AudioSource source = controller.BombAudioSource;
            if (source == null || data.aimAudioClip == null) return;

            source.clip = data.aimAudioClip;
            source.pitch = data.aimAudioPitchStart;
            source.loop = true;
            source.Play();
        }

        /// <summary>
        /// Cap nhat pitch cua audio ngam theo tien trinh ngam t (0->1).
        /// Pitch tang dan tu aimAudioPitchStart den aimAudioPitchEnd va co dinh o cuoi.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="data">Du lieu cau hinh cua Laser Drone.</param>
        /// <param name="t">Tien trinh ngam tu 0 (bat dau) den 1 (ket thuc).</param>
        private void SetAimAudioPitch(BombController controller, LaserDroneData data, float t)
        {
            AudioSource source = controller.BombAudioSource;
            if (source == null || !source.isPlaying) return;

            source.pitch = Mathf.Lerp(data.aimAudioPitchStart, data.aimAudioPitchEnd, Mathf.Clamp01(t));
        }

        /// <summary>
        /// Dung phat audio ngam.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private void StopAimAudio(BombController controller)
        {
            AudioSource source = controller.BombAudioSource;
            if (source == null) return;
            if (source.isPlaying) source.Stop();
        }

        /// <summary>
        /// Phat audio beep (loop) tai thoi diem ket thuc aim. Day la audio RIENG, tach khoi audio aim.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        /// <param name="data">Du lieu cau hinh cua Laser Drone.</param>
        private void PlayBeepAudio(BombController controller, LaserDroneData data)
        {
            AudioSource source = controller.BombAudioSource;
            if (source == null || data.beepAudioClip == null) return;

            source.clip = data.beepAudioClip;
            source.pitch = data.beepAudioPitch;
            source.loop = true;
            source.Play();
        }

        /// <summary>
        /// Dung phat audio beep sau khi het thoi gian loop ngau.
        /// </summary>
        /// <param name="controller">Bomb controller ma hanh vi nay duoc gan vao.</param>
        private void StopBeepAudio(BombController controller)
        {
            AudioSource source = controller.BombAudioSource;
            if (source == null) return;
            if (source.isPlaying) source.Stop();
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