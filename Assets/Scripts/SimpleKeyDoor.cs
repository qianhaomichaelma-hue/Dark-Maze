using UnityEngine;

namespace DarkMazeMinimal
{
    public class SimpleKeyDoor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform doorVisual;
        [SerializeField] private DoorWorldText doorText;

        [Header("Door Open")]
        [SerializeField] private bool destroyDoorWhenOpen = false;
        [SerializeField] private Vector3 openedLocalEulerAngles = new Vector3(0f, 90f, 0f);

        [Header("Text")]
        [SerializeField] private string lockedMessage = "Need a Key";
        [SerializeField] private float textDuration = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool logDoor = true;

        private bool _isOpen = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_isOpen) return;
            if (other == null) return;

            SimpleKeyHolder keyHolder = other.GetComponentInParent<SimpleKeyHolder>();
            if (keyHolder == null) return;

            if (!keyHolder.HasKey)
            {
                if (doorText != null)
                    doorText.Show(lockedMessage, textDuration);

                if (logDoor)
                    Debug.Log($"[SimpleKeyDoor] Locked - player has no key.", this);

                return;
            }

            OpenDoor();
        }

        private void OpenDoor()
        {
            if (_isOpen) return;
            _isOpen = true;

            if (doorText != null)
                doorText.HideNow();

            if (destroyDoorWhenOpen)
            {
                if (doorVisual != null)
                    Destroy(doorVisual.gameObject);
                else
                    Destroy(gameObject);

                if (logDoor)
                    Debug.Log("[SimpleKeyDoor] Door opened by destroy.", this);

                return;
            }

            if (doorVisual != null)
                doorVisual.localEulerAngles = openedLocalEulerAngles;

            if (logDoor)
                Debug.Log("[SimpleKeyDoor] Door opened.", this);
        }
    }
}
