using UnityEngine;
using DarkMazeItems;

namespace DarkMazePlayer
{
    [DisallowMultipleComponent]
    public class PlayerTorchVisual : MonoBehaviour
    {
        [Header("Requirement")]
        [SerializeField] private ItemData torchItem;

        [Header("References")]
        [SerializeField] private PlayerEquipment equipment;

        [Tooltip("The root object of the visible torch model. Usually TorchSwingPivot or Torch_Model.")]
        [SerializeField] private GameObject torchVisualRoot;

        [Tooltip("The pivot that rotates when attacking. Usually parent of the torch model.")]
        [SerializeField] private Transform torchSwingPivot;

        [Header("Swing Timing")]
        [Tooltip("Total visual swing duration.")]
        [SerializeField] private float swingDuration = 0.32f;

        [Tooltip("Small backward preparation before the strike.")]
        [Range(0.05f, 0.4f)]
        [SerializeField] private float windupRatio = 0.22f;

        [Tooltip("Fast forward strike section. The hit delay should usually land near the end of this section.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float strikeRatio = 0.28f;

        [Tooltip("Short impact hold / overshoot after strike.")]
        [Range(0.01f, 0.25f)]
        [SerializeField] private float impactRatio = 0.10f;

        [Header("Swing Rotation")]
        [Tooltip("The torch moves slightly backward first.")]
        [SerializeField] private Vector3 windupRotation = new Vector3(-22f, 0f, 18f);

        [Tooltip("Main forward swing pose.")]
        [SerializeField] private Vector3 strikeRotation = new Vector3(65f, 0f, -38f);

        [Tooltip("Small extra overshoot after impact. Makes the swing feel heavier.")]
        [SerializeField] private Vector3 overshootRotation = new Vector3(78f, 0f, -48f);

        [Header("Swing Position")]
        [Tooltip("Small local movement during strike. Helps sell the torch moving forward.")]
        [SerializeField] private Vector3 strikeLocalOffset = new Vector3(0.08f, -0.03f, 0.10f);

        [Tooltip("Small local movement during overshoot.")]
        [SerializeField] private Vector3 overshootLocalOffset = new Vector3(0.10f, -0.04f, 0.13f);

        [Header("Curves")]
        [SerializeField] private AnimationCurve windupCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve strikeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Optional Trail")]
        [SerializeField] private TrailRenderer[] swingTrails;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private Quaternion _defaultLocalRotation;
        private Vector3 _defaultLocalPosition;
        private bool _hasDefaultTransform;

        private float _swingTimer;
        private bool _isSwinging;

        private void Awake()
        {
            if (equipment == null)
                equipment = GetComponent<PlayerEquipment>();

            CacheDefaultTransform();

            if (torchVisualRoot != null)
                torchVisualRoot.SetActive(false);

            SetTrails(false);
        }

        private void Update()
        {
            UpdateTorchVisibility();
            UpdateSwing();
        }

        private void UpdateTorchVisibility()
        {
            if (torchVisualRoot == null || equipment == null || torchItem == null)
                return;

            bool shouldShow = equipment.IsHolding(torchItem);

            if (torchVisualRoot.activeSelf != shouldShow)
                torchVisualRoot.SetActive(shouldShow);

            if (!shouldShow)
                StopSwingInstant();
        }

        public void PlaySwing()
        {
            if (torchSwingPivot == null)
                return;

            CacheDefaultTransform();

            _swingTimer = 0f;
            _isSwinging = true;

            SetTrails(true);

            Log("Swing started.");
        }

        private void UpdateSwing()
        {
            if (!_isSwinging || torchSwingPivot == null)
                return;

            _swingTimer += Time.deltaTime;

            float duration = Mathf.Max(0.01f, swingDuration);
            float t = Mathf.Clamp01(_swingTimer / duration);

            ApplySwingPose(t);

            if (t >= 1f)
                StopSwingInstant();
        }

        private void ApplySwingPose(float t)
        {
            NormalizeRatios(
                out float windupEnd,
                out float strikeEnd,
                out float impactEnd
            );

            Quaternion targetRot;
            Vector3 targetPos;

            if (t <= windupEnd)
            {
                float localT = windupEnd > 0f ? t / windupEnd : 1f;
                localT = windupCurve.Evaluate(localT);

                targetRot = _defaultLocalRotation * Quaternion.Euler(
                    Vector3.LerpUnclamped(Vector3.zero, windupRotation, localT)
                );

                targetPos = Vector3.LerpUnclamped(
                    _defaultLocalPosition,
                    _defaultLocalPosition - strikeLocalOffset * 0.35f,
                    localT
                );
            }
            else if (t <= strikeEnd)
            {
                float localT = Mathf.InverseLerp(windupEnd, strikeEnd, t);
                localT = strikeCurve.Evaluate(localT);

                Quaternion fromRot = _defaultLocalRotation * Quaternion.Euler(windupRotation);
                Quaternion toRot = _defaultLocalRotation * Quaternion.Euler(strikeRotation);

                targetRot = Quaternion.SlerpUnclamped(fromRot, toRot, localT);

                Vector3 fromPos = _defaultLocalPosition - strikeLocalOffset * 0.35f;
                Vector3 toPos = _defaultLocalPosition + strikeLocalOffset;

                targetPos = Vector3.LerpUnclamped(fromPos, toPos, localT);
            }
            else if (t <= impactEnd)
            {
                float localT = Mathf.InverseLerp(strikeEnd, impactEnd, t);

                Quaternion fromRot = _defaultLocalRotation * Quaternion.Euler(strikeRotation);
                Quaternion toRot = _defaultLocalRotation * Quaternion.Euler(overshootRotation);

                targetRot = Quaternion.SlerpUnclamped(fromRot, toRot, localT);

                Vector3 fromPos = _defaultLocalPosition + strikeLocalOffset;
                Vector3 toPos = _defaultLocalPosition + overshootLocalOffset;

                targetPos = Vector3.LerpUnclamped(fromPos, toPos, localT);
            }
            else
            {
                float localT = Mathf.InverseLerp(impactEnd, 1f, t);
                localT = returnCurve.Evaluate(localT);

                Quaternion fromRot = _defaultLocalRotation * Quaternion.Euler(overshootRotation);
                Quaternion toRot = _defaultLocalRotation;

                targetRot = Quaternion.SlerpUnclamped(fromRot, toRot, localT);

                Vector3 fromPos = _defaultLocalPosition + overshootLocalOffset;
                Vector3 toPos = _defaultLocalPosition;

                targetPos = Vector3.LerpUnclamped(fromPos, toPos, localT);
            }

            torchSwingPivot.localRotation = targetRot;
            torchSwingPivot.localPosition = targetPos;
        }

        private void StopSwingInstant()
        {
            _isSwinging = false;
            _swingTimer = 0f;

            if (torchSwingPivot != null && _hasDefaultTransform)
            {
                torchSwingPivot.localRotation = _defaultLocalRotation;
                torchSwingPivot.localPosition = _defaultLocalPosition;
            }

            SetTrails(false);
        }

        private void CacheDefaultTransform()
        {
            if (torchSwingPivot == null)
                return;

            if (_hasDefaultTransform)
                return;

            _defaultLocalRotation = torchSwingPivot.localRotation;
            _defaultLocalPosition = torchSwingPivot.localPosition;
            _hasDefaultTransform = true;
        }

        private void NormalizeRatios(out float windupEnd, out float strikeEnd, out float impactEnd)
        {
            float w = Mathf.Max(0.01f, windupRatio);
            float s = Mathf.Max(0.01f, strikeRatio);
            float i = Mathf.Max(0.01f, impactRatio);

            float total = w + s + i;

            if (total >= 0.95f)
            {
                float scale = 0.95f / total;
                w *= scale;
                s *= scale;
                i *= scale;
            }

            windupEnd = w;
            strikeEnd = windupEnd + s;
            impactEnd = strikeEnd + i;
        }

        private void SetTrails(bool visible)
        {
            if (swingTrails == null)
                return;

            for (int i = 0; i < swingTrails.Length; i++)
            {
                if (swingTrails[i] == null)
                    continue;

                swingTrails[i].emitting = visible;

                if (!visible)
                    swingTrails[i].Clear();
            }
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[PlayerTorchVisual] {message}", this);
        }
    }
}