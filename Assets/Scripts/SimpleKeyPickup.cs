using UnityEngine;

namespace DarkMazeMinimal
{
    public class SimpleKeyPickup : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip pickupSFX;
        [SerializeField] private float pickupVolume = 1.0f;

        [Header("3D Audio Settings")]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Debug")]
        [SerializeField] private bool logPickup = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;

            SimpleKeyHolder keyHolder = other.GetComponentInParent<SimpleKeyHolder>();
            if (keyHolder == null) return;

            if (keyHolder.HasKey) return;

            keyHolder.GiveKey();

            PlayPickupSFX();

            if (logPickup)
                Debug.Log($"[SimpleKeyPickup] Picked up by {other.name}", this);

            Destroy(gameObject);
        }

        private void PlayPickupSFX()
        {
            if (pickupSFX == null)
                return;

            GameObject audioObject = new GameObject("KeyPickupSFX");
            audioObject.transform.position = transform.position;

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = pickupSFX;
            source.volume = pickupVolume;
            source.playOnAwake = false;
            source.loop = false;

            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = rolloffMode;
            source.dopplerLevel = 0f;

            source.Play();

            Destroy(audioObject, pickupSFX.length + 0.1f);
        }
    }
}