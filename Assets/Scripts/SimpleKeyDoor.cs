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

        [Header("Audio - Door")]
        [SerializeField] private AudioClip lockedSFX;
        [SerializeField] private AudioClip openSFX;

        [SerializeField] private float lockedVolume = 0.8f;
        [SerializeField] private float openVolume = 1.0f;

        [Tooltip("防止玩家反复进出 Trigger 时锁门音效过度重复。")]
        [SerializeField] private float lockedSFXCooldown = 0.5f;

        [Header("3D Audio Settings")]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 15f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Debug")]
        [SerializeField] private bool logDoor = true;

        private bool _isOpen = false;
        private float _lastLockedSFXTime = -999f;

        private void OnTriggerEnter(Collider other)
        {
            if (_isOpen) return;
            if (other == null) return;

            SimpleKeyHolder keyHolder = other.GetComponentInParent<SimpleKeyHolder>();
            if (keyHolder == null) return;

            if (!keyHolder.HasKey)
            {
                PlayLockedSFX();

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

            PlayOpenSFX();

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

        private void PlayLockedSFX()
        {
            if (lockedSFX == null)
                return;

            if (Time.time - _lastLockedSFXTime < lockedSFXCooldown)
                return;

            _lastLockedSFXTime = Time.time;

            PlayClipAtDoorPosition(lockedSFX, lockedVolume, "LockedDoorSFX");
        }

        private void PlayOpenSFX()
        {
            if (openSFX == null)
                return;

            PlayClipAtDoorPosition(openSFX, openVolume, "OpenDoorSFX");
        }

        private void PlayClipAtDoorPosition(AudioClip clip, float volume, string objectName)
        {
            if (clip == null)
                return;

            Vector3 soundPosition = doorVisual != null
                ? doorVisual.position
                : transform.position;

            GameObject audioObject = new GameObject(objectName);
            audioObject.transform.position = soundPosition;

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.playOnAwake = false;
            source.loop = false;

            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = rolloffMode;
            source.dopplerLevel = 0f;

            source.Play();

            Destroy(audioObject, clip.length + 0.1f);
        }
    }
}