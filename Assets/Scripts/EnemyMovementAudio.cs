using UnityEngine;
using UnityEngine.AI;

namespace DarkMazeMinimal
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovementAudio : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource movementAudioSource;

        [Tooltip("通常不用手动填。脚本会自动找场景里的 AudioListener。")]
        [SerializeField] private Transform listenerOverride;

        [Header("Movement Audio")]
        [SerializeField] private AudioClip crawlLoopSFX;
        [SerializeField] private float minMoveSpeed = 0.1f;
        [SerializeField] private float volume = 0.25f;
        [SerializeField] private float pitch = 1.0f;

        [Header("3D Audio Settings")]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 6f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Hard Distance Limit")]
        [SerializeField] private bool useHardDistanceLimit = true;

        [Tooltip("超过这个距离，爬行声会被脚本强制停止。")]
        [SerializeField] private float hardMaxDistance = 7f;

        private NavMeshAgent agent;
        private Transform listenerTransform;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (movementAudioSource == null)
                movementAudioSource = GetComponent<AudioSource>();

            FindListener();
            SetupAudioSource();
        }

        private void Update()
        {
            if (agent == null || movementAudioSource == null || crawlLoopSFX == null)
                return;

            if (listenerTransform == null)
                FindListener();

            bool isMoving = agent.velocity.magnitude > minMoveSpeed && !agent.isStopped;
            bool isCloseEnough = IsCloseEnoughToListener();

            if (isMoving && isCloseEnough)
            {
                if (!movementAudioSource.isPlaying)
                    movementAudioSource.Play();
            }
            else
            {
                if (movementAudioSource.isPlaying)
                    movementAudioSource.Stop();
            }
        }

        private void SetupAudioSource()
        {
            if (movementAudioSource == null)
                return;

            movementAudioSource.playOnAwake = false;
            movementAudioSource.loop = true;
            movementAudioSource.clip = crawlLoopSFX;

            movementAudioSource.volume = volume;
            movementAudioSource.pitch = pitch;

            movementAudioSource.spatialBlend = spatialBlend;
            movementAudioSource.minDistance = minDistance;
            movementAudioSource.maxDistance = maxDistance;
            movementAudioSource.rolloffMode = rolloffMode;

            movementAudioSource.dopplerLevel = 0f;
        }

        private void FindListener()
        {
            if (listenerOverride != null)
            {
                listenerTransform = listenerOverride;
                return;
            }

#if UNITY_2023_1_OR_NEWER
            AudioListener listener = FindFirstObjectByType<AudioListener>();
#else
            AudioListener listener = FindObjectOfType<AudioListener>();
#endif

            if (listener != null)
                listenerTransform = listener.transform;
        }

        private bool IsCloseEnoughToListener()
        {
            if (!useHardDistanceLimit)
                return true;

            if (listenerTransform == null)
                return true;

            float distance = Vector3.Distance(transform.position, listenerTransform.position);
            return distance <= hardMaxDistance;
        }
    }
}