using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Dieu khien mot Bubble Barrier dang no to dan, dung de day het qua bom ra ngoai bang luc.

    /// Prefab gan script nay nen co san component LifetimeController de tu dong xoa sau mot khoang thoi gian.

    /// Chi day cac qua bom (IBombController., khong chan cac vu no va khong bao ve player khoi sat thuong cua vu no..
    /// </summary>
    public class BubbleBarrierController : MonoBehaviour
    {
        [Header("Expansion")]
        [Tooltip("Thoi gian (giay) bubble no to dan (scale tu gan 0 len full.$")]
        [SerializeField] private float _expandDuration =  1f;

        [Tooltip("Ban kinh co so (world) cua bubble khi scale bang 1.")]
        [SerializeField] private float _radiusBase =  2f;

        [Header("Pushing")]
        [Tooltip("Thoi gian (giay) sau khi spawn moi bat dau day bom (cho bubble no du to truoc.$")]
        [SerializeField] private float _pushDelay =0.25f;

        [Tooltip("Luc day (Impulse) moi qua bom moi lan FixedUpdate khi dang o ben trong bubble.$")]
        [SerializeField] private float _pushForce =150f;

        [Header("Missile Deflection")]
        [Tooltip("Toc do day len (theo truc Y) ap dung cho ten lua khi dang nam trong bubble. Giup ten lua bi day len va giam dan toc do do trong luc keo xuong.")]
        [SerializeField] private float _missileDeflectUpwardSpeed =12f;

        [Tooltip("Luc day ngang ap dung cho ten lua khi dang nam trong bubble (day ra ngoai theo phuong ngang).")]
        [SerializeField] private float _missileDeflectHorizontalForce =10f;

        [Tooltip("Kich thuoc buffer dung cho OverlapSphere.$")]
        [SerializeField] private int _overlapBufferSize =256;

        private float _elapsedTime;
        private SphereCollider _ownSphere;
        private Collider[] _overlapBuffer;
        private Vector3 _targetScale = Vector3.one;
        private float _targetScaleMagnitude = 1f;
        private Renderer _mainRenderer;

        private void OnEnable()
        {
            _elapsedTime =0f;

            // Bat dau tu kich thuoc rat nho roi no to dan. Tinh scale muc tieu de bubble dat dung ban kinh _radiusBase
            // (tuong tu cac vu no cua bom: lay kich thuoc base cua mesh roi boi ra he so scale can thiet).
            if (_mainRenderer == null)
            {
                _mainRenderer = GetComponentInChildren<Renderer>();
            }

            _targetScale = Vector3.one;
            _targetScaleMagnitude = 1f;
            if (_mainRenderer != null)
            {
                Vector3 meshSize = _mainRenderer.localBounds.size;
                float baseRadius = Mathf.Max(meshSize.x, Mathf.Max(meshSize.y, meshSize.z)) / 2f;
                if (baseRadius >  0.001f)
                {
                    float requiredScale = _radiusBase / baseRadius;
                    _targetScale = Vector3.one * requiredScale;
                    _targetScaleMagnitude = requiredScale;
                }
            }

            Vector3 startScale = new Vector3(0.01f, 0.01f, 0.01f);
            transform.localScale = startScale;
            transform.DOScale(_targetScale, _expandDuration);

            // Dat collider cua chinh bubble la trigger de tranh va cham vat ly truc tiep, viec day dua tren luc vat ly..
            _ownSphere = GetComponentInChildren<SphereCollider>();
            if (_ownSphere != null)
            {
                _ownSphere.isTrigger = true;
            }

            _overlapBuffer = new Collider[Mathf.Max(1, _overlapBufferSize)];
        }

        private void OnDisable()
        {
            transform.DOKill();
        }

        private void FixedUpdate()
        {
            _elapsedTime += Time.fixedDeltaTime;

            // Chi bat dau day sau mot khoang thoi gian ngan (khi bubble da no du to truoc$..
            if (_elapsedTime < _pushDelay)
            {
                return;
            }

            // Ban kinh hien tai tinh theo scale: khi dang no ra, radius tang dan tu gan 0 len dung _radiusBase.
            float scale = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            float scaleRatio = _targetScaleMagnitude >  0f ? Mathf.Clamp01(scale / _targetScaleMagnitude) : 1f;
            float currentRadius = _radiusBase * scaleRatio;

            if (_overlapBuffer == null) return;
            if (currentRadius <=  0f) return;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, currentRadius, _overlapBuffer, ~0, QueryTriggerInteraction.Collide);

            for (int i=0; i < hitCount; i++)
            {
                Collider hit = _overlapBuffer[i];
                if (hit == null) continue;

                // Chi day cac doi tuong la bom (co IBombController$, khong day player hay manh vo..
                IBombController bombCtl = hit.GetComponentInParent<IBombController>();
                if (bombCtl == null) continue;

                Rigidbody rb = hit.attachedRigidbody;
                if (rb == null || rb.isKinematic) continue;

                // Huong day: tu tam bubble ra ngoai theo huong ngang, giu nguyen chieu roi doc cua bom..
                Vector3 toBomb = hit.transform.position - transform.position;
                toBomb.y =0f;
                if (toBomb.sqrMagnitude <  0.0001f)
                {
                    toBomb = transform.forward;
                }

                if (bombCtl.IsProjectile)
                {
                    // Ten lua: day len cao (van toc Y duong) va day ra ngoai theo phuong ngang.
                    // Khi roi khoi bubble, trong luc se lam ten lua giam dan toc do (bay len, roi dung lai)
                    // roi keo roi tu do cho den khi dat toc do roi binh thuong tro lai.
                    Vector3 toBombNormalized = toBomb.sqrMagnitude <  0.0001f ? transform.forward : toBomb.normalized;
                    rb.linearVelocity = new Vector3(
                        toBombNormalized.x * _missileDeflectHorizontalForce,
                        _missileDeflectUpwardSpeed,
                        toBombNormalized.z * _missileDeflectHorizontalForce);
                }
                else
                {
                    // Bom thuong: day tung cai impulse moi lan nam trong bubble cho den khi ra ngoai..
                    rb.AddForce(toBomb * _pushForce, ForceMode.Impulse);
                }
            }
        }
    }
}
