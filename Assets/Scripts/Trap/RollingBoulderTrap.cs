using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class RollingBoulderTrap : MonoBehaviour
    {
        [Header("Scene Boulder")]
        [Tooltip("The boulder/cup already placed in the scene.")]
        [SerializeField] private RollingBoulder boulder;

        [Tooltip("Where the boulder is held before being released. If empty, the boulder's initial scene position is used.")]
        [SerializeField] private Transform heldPoint;

        [Header("Release")]
        [SerializeField] private bool addInitialImpulse = false;
        [SerializeField] private Transform initialDirectionReference;
        [SerializeField] private float initialImpulse = 2f;

        [Header("Rules")]
        [Tooltip("If true, the trap becomes permanently disabled after the boulder reaches the end zone.")]
        [SerializeField] private bool spendTrapWhenBoulderFinishes = true;

        [Tooltip("If true, the boulder stops and becomes non-lethal after reaching the end zone.")]
        [SerializeField] private bool stopBoulderAtEndZone = true;

        [Tooltip("If true, the boulder stays visible after the trap is spent.")]
        [SerializeField] private bool keepBoulderVisibleAfterSpent = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private bool _isActive;
        private bool _isSpent;

        private Vector3 _heldPosition;
        private Quaternion _heldRotation;

        public bool IsActive => _isActive;
        public bool IsSpent => _isSpent;

        private void Awake()
        {
            if (boulder == null)
                boulder = GetComponentInChildren<RollingBoulder>(true);

            if (boulder == null)
            {
                Debug.LogWarning("[RollingBoulderTrap] No scene boulder assigned.", this);
                return;
            }

            if (heldPoint != null)
            {
                _heldPosition = heldPoint.position;
                _heldRotation = heldPoint.rotation;
            }
            else
            {
                _heldPosition = boulder.transform.position;
                _heldRotation = boulder.transform.rotation;
            }

            boulder.Initialize(this);
            ResetBoulderToHeldState();

            Log("Trap initialized. Boulder is held in scene.");
        }

        public void TriggerTrap()
        {
            if (_isSpent)
            {
                Log("Trigger ignored. Trap is spent.");
                return;
            }

            if (_isActive)
            {
                Log("Trigger ignored. Boulder is already active.");
                return;
            }

            if (boulder == null)
            {
                Debug.LogWarning("[RollingBoulderTrap] Boulder reference missing.", this);
                return;
            }

            _isActive = true;

            boulder.Release();

            if (addInitialImpulse && initialImpulse > 0f)
            {
                Vector3 dir = initialDirectionReference != null
                    ? initialDirectionReference.forward
                    : transform.forward;

                boulder.ApplyInitialImpulse(dir, initialImpulse);
            }

            Log("Boulder released.");
        }

        public void NotifyPlayerKilledByBoulder(RollingBoulder sourceBoulder)
        {
            if (sourceBoulder == null)
                return;

            if (sourceBoulder != boulder)
            {
                Log("Ignored kill event from another boulder.");
                return;
            }

            Log("Player killed by boulder. Trap resets for retry.");

            _isActive = false;
            _isSpent = false;

            ResetBoulderToHeldState();
        }

        public void NotifyBoulderFinished(RollingBoulder sourceBoulder)
        {
            if (sourceBoulder == null)
                return;

            if (sourceBoulder != boulder)
            {
                Log("Ignored finish event from another boulder.");
                return;
            }

            Log("Boulder reached end zone. Player survived. Trap is now spent.");

            _isActive = false;

            if (spendTrapWhenBoulderFinishes)
                _isSpent = true;

            if (stopBoulderAtEndZone)
                boulder.StopAndDisableDamage();

            if (!keepBoulderVisibleAfterSpent)
                boulder.gameObject.SetActive(false);
        }

        public void ResetBoulderToHeldState()
        {
            if (boulder == null)
                return;

            if (heldPoint != null)
            {
                _heldPosition = heldPoint.position;
                _heldRotation = heldPoint.rotation;
            }

            boulder.ResetToHeldState(_heldPosition, _heldRotation);
        }

        [ContextMenu("Debug / Release Boulder")]
        private void DebugReleaseBoulder()
        {
            TriggerTrap();
        }

        [ContextMenu("Debug / Reset Trap For Retry")]
        public void ResetTrapForRetry()
        {
            _isActive = false;
            _isSpent = false;

            if (boulder != null)
                boulder.gameObject.SetActive(true);

            ResetBoulderToHeldState();

            Log("Trap manually reset.");
        }

        [ContextMenu("Debug / Spend Trap")]
        public void SpendTrap()
        {
            _isActive = false;
            _isSpent = true;

            if (boulder != null)
                boulder.StopAndDisableDamage();

            Log("Trap manually spent.");
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[RollingBoulderTrap] {message}", this);
        }
    }
}