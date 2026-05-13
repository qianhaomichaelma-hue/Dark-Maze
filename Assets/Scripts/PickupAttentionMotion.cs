using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class PickupAttentionMotion : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("要旋转和上下浮动的视觉物体。如果为空，就使用当前物体。")]
        [SerializeField] private Transform visualTarget;

        [Header("Rotation")]
        [SerializeField] private bool rotate = true;
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);

        [Header("Floating")]
        [SerializeField] private bool floatUpDown = true;
        [SerializeField] private float floatHeight = 0.2f;
        [SerializeField] private float floatSpeed = 2f;

        [Header("Optional Scale Pulse")]
        [SerializeField] private bool pulseScale = false;
        [SerializeField] private float pulseAmount = 0.05f;
        [SerializeField] private float pulseSpeed = 2f;

        [Header("Random Offset")]
        [Tooltip("开启后，多个拾取物不会完全同步上下浮动。")]
        [SerializeField] private bool useRandomPhase = true;

        private Vector3 _startLocalPosition;
        private Vector3 _startLocalScale;
        private float _phaseOffset;

        private void Awake()
        {
            if (visualTarget == null)
                visualTarget = transform;

            _startLocalPosition = visualTarget.localPosition;
            _startLocalScale = visualTarget.localScale;

            if (useRandomPhase)
                _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (visualTarget == null)
                return;

            if (rotate)
            {
                visualTarget.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
            }

            if (floatUpDown)
            {
                float yOffset = Mathf.Sin(Time.time * floatSpeed + _phaseOffset) * floatHeight;
                visualTarget.localPosition = _startLocalPosition + Vector3.up * yOffset;
            }

            if (pulseScale)
            {
                float scaleOffset = Mathf.Sin(Time.time * pulseSpeed + _phaseOffset) * pulseAmount;
                visualTarget.localScale = _startLocalScale * (1f + scaleOffset);
            }
        }

        private void OnDisable()
        {
            if (visualTarget == null)
                return;

            visualTarget.localPosition = _startLocalPosition;
            visualTarget.localScale = _startLocalScale;
        }
    }
}