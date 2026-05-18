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

        [Header("Swing")]
        [SerializeField] private float swingDuration = 0.22f;
        [SerializeField] private Vector3 swingRotation = new Vector3(55f, 0f, -25f);
        [SerializeField] private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Quaternion _defaultLocalRotation;
        private bool _hasDefaultRotation;

        private float _swingTimer;
        private bool _isSwinging;

        private void Awake()
        {
            if (equipment == null)
                equipment = GetComponent<PlayerEquipment>();

            if (torchSwingPivot != null)
            {
                _defaultLocalRotation = torchSwingPivot.localRotation;
                _hasDefaultRotation = true;
            }

            if (torchVisualRoot != null)
                torchVisualRoot.SetActive(false);
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

            if (!_hasDefaultRotation)
            {
                _defaultLocalRotation = torchSwingPivot.localRotation;
                _hasDefaultRotation = true;
            }

            _swingTimer = 0f;
            _isSwinging = true;
        }

        private void UpdateSwing()
        {
            if (!_isSwinging || torchSwingPivot == null)
                return;

            _swingTimer += Time.deltaTime;

            float duration = Mathf.Max(0.01f, swingDuration);
            float t = Mathf.Clamp01(_swingTimer / duration);

            // 0 → 1 → 0，前半段挥出去，后半段收回来
            float swing01;

            if (t <= 0.5f)
                swing01 = swingCurve.Evaluate(t / 0.5f);
            else
                swing01 = swingCurve.Evaluate(1f - ((t - 0.5f) / 0.5f));

            Quaternion offset = Quaternion.Euler(swingRotation);

            torchSwingPivot.localRotation =
                _defaultLocalRotation * Quaternion.Slerp(Quaternion.identity, offset, swing01);

            if (t >= 1f)
                StopSwingInstant();
        }

        private void StopSwingInstant()
        {
            _isSwinging = false;
            _swingTimer = 0f;

            if (torchSwingPivot != null && _hasDefaultRotation)
                torchSwingPivot.localRotation = _defaultLocalRotation;
        }
    }
}