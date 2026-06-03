using System.Collections;
using UnityEngine;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class FinalEscortSlide : MonoBehaviour
    {
        [Header("Quest")]
        [SerializeField] private RescueQuestController quest;

        [Tooltip("滑梯结束后，NPC 会被放到这个位置，然后进入 WaitingFinalDialogue。")]
        [SerializeField] private Transform rescuedNpcStandPoint;

        [Header("Slide Path")]
        [Tooltip("滑梯路径点。按顺序从入口到出口摆放。")]
        [SerializeField] private Transform[] pathPoints;

        [Tooltip("进入滑梯时是否把玩家吸附到第一个路径点。")]
        [SerializeField] private bool snapPlayerToFirstPoint = true;

        [Tooltip("滑完后是否把玩家放到最后一个路径点。")]
        [SerializeField] private bool snapPlayerToLastPointAtEnd = true;

        [Header("Slide Movement")]
        [SerializeField] private float startSpeed = 2.5f;
        [SerializeField] private float acceleration = 4.5f;
        [SerializeField] private float maxSpeed = 9f;

        [Tooltip("玩家身体朝向滑行方向的旋转速度。")]
        [SerializeField] private float bodyRotationLerpSpeed = 10f;

        [Tooltip("到达路径点的距离阈值。")]
        [SerializeField] private float pointReachDistance = 0.08f;

        [Header("Camera Look During Slide")]
        [SerializeField] private bool allowFreeLookDuringSlide = true;

        [Tooltip("滑行开始时，是否锁定玩家身体朝向滑梯方向。相机仍然可以自由转。")]
        [SerializeField] private bool rotateBodyAlongSlide = true;

        [Tooltip("鼠标视角速度倍率。数值越大，滑行时转视角越快。")]
        [SerializeField] private float mouseLookSensitivity = 1f;

        [Tooltip("手柄视角速度倍率。")]
        [SerializeField] private float gamepadLookSensitivity = 1f;

        [Header("Control Lock")]
        [Tooltip("滑行结束后是否恢复玩家控制。通常设 true，因为之后玩家要和 NPC 最终对话。")]
        [SerializeField] private bool restoreControlAfterSlide = true;

        [Header("Audio")]
        [SerializeField] private AudioSource slideAudioSource;
        [SerializeField] private AudioClip slideStartSFX;
        [SerializeField] private AudioClip slideLoopSFX;
        [SerializeField] private AudioClip slideEndSFX;

        [Range(0f, 1f)]
        [SerializeField] private float slideStartVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float slideLoopVolume = 0.75f;

        [Range(0f, 1f)]
        [SerializeField] private float slideEndVolume = 1f;

        [Header("Camera Shake")]
        [SerializeField] private bool shakeCameraWhileSliding = true;
        [SerializeField] private float slideShakeInterval = 0.15f;
        [SerializeField] private float slideShakeDuration = 0.06f;
        [SerializeField] private float slideShakeStrength = 0.035f;

        [SerializeField] private bool shakeCameraOnEnd = true;
        [SerializeField] private float endShakeDuration = 0.22f;
        [SerializeField] private float endShakeStrength = 0.09f;

        [Header("Settings")]
        [Tooltip("滑梯只允许触发一次。推荐打开，因为滑完后会进入最终对话状态。")]
        [SerializeField] private bool onlyOnce = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;
        [SerializeField] private bool drawPathGizmos = true;

        private bool _used;
        private bool _sliding;
        private float _nextShakeTime;

        private StarterAssetsInputs _cachedInputs;
        private ThirdPersonController _cachedThirdPersonController;
        private CharacterController _cachedCharacterController;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _cachedPlayerInput;
#endif

        private bool _thirdPersonControllerWasEnabled;
        private bool _characterControllerWasEnabled;

        private GameObject _cameraTarget;
        private float _cameraYaw;
        private float _cameraPitch;

        private const float LookThreshold = 0.01f;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (quest == null)
                quest = FindFirstObjectByType<RescueQuestController>();

            SetupAudioSource();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_sliding)
                return;

            if (onlyOnce && _used)
                return;

            if (quest == null)
            {
                LogWarning("Quest reference is missing.");
                return;
            }

            if (!quest.IsEscorting)
            {
                Log("Slide ignored. Player is not escorting NPC.");
                return;
            }

            PlayerState playerState = other.GetComponentInParent<PlayerState>();
            if (playerState == null)
                return;

            if (playerState.IsDead)
                return;

            if (pathPoints == null || pathPoints.Length < 2)
            {
                LogWarning("Need at least 2 path points.");
                return;
            }

            StartCoroutine(SlideRoutine(playerState));
        }

        private IEnumerator SlideRoutine(PlayerState playerState)
        {
            _sliding = true;
            _used = true;
            _nextShakeTime = Time.time;

            Transform playerTransform = playerState.transform;

            CachePlayerControlComponents(playerTransform);
            InitializeSlideCamera();
            LockPlayerMovementButKeepLook();

            PlaySlideStartSFX();
            StartSlideLoopSFX();

            if (snapPlayerToFirstPoint && pathPoints[0] != null)
            {
                MovePlayerDirect(playerTransform, pathPoints[0].position);

                if (rotateBodyAlongSlide)
                    FaceBodyDirection(playerTransform, GetInitialDirection(playerTransform));
            }

            float speed = Mathf.Max(0.01f, startSpeed);
            int pointIndex = 1;

            while (pointIndex < pathPoints.Length)
            {
                if (playerState == null || playerState.IsDead)
                {
                    AbortSlideBecausePlayerDied();
                    yield break;
                }

                SuppressNonLookInputs();
                UpdateFreeLookCamera();

                Transform targetPoint = pathPoints[pointIndex];

                if (targetPoint == null)
                {
                    pointIndex++;
                    yield return null;
                    continue;
                }

                Vector3 currentPos = playerTransform.position;
                Vector3 targetPos = targetPoint.position;
                Vector3 toTarget = targetPos - currentPos;

                float distance = toTarget.magnitude;

                if (distance <= pointReachDistance)
                {
                    pointIndex++;
                    yield return null;
                    continue;
                }

                Vector3 direction = toTarget.normalized;

                speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);

                float moveDistance = Mathf.Min(speed * Time.deltaTime, distance);
                Vector3 motion = direction * moveDistance;

                MovePlayerByMotion(playerTransform, motion);

                if (rotateBodyAlongSlide)
                    FaceBodyDirection(playerTransform, direction);

                UpdateSlideCameraShake();

                yield return null;
            }

            if (snapPlayerToLastPointAtEnd && pathPoints[pathPoints.Length - 1] != null)
            {
                MovePlayerDirect(playerTransform, pathPoints[pathPoints.Length - 1].position);
            }

            StopSlideLoopSFX();
            PlaySlideEndSFX();
            PlayEndCameraShake();

            if (quest != null && quest.IsEscorting)
            {
                quest.ArriveAtEscortGoal(rescuedNpcStandPoint);
            }

            if (restoreControlAfterSlide && playerState != null && !playerState.IsDead)
            {
                RestorePlayerControl();
            }

            _sliding = false;

            Log("Slide finished. Quest moved to WaitingFinalDialogue.");
        }

        private void CachePlayerControlComponents(Transform playerTransform)
        {
            _cachedInputs = playerTransform.GetComponent<StarterAssetsInputs>();
            _cachedThirdPersonController = playerTransform.GetComponent<ThirdPersonController>();
            _cachedCharacterController = playerTransform.GetComponent<CharacterController>();

#if ENABLE_INPUT_SYSTEM
            _cachedPlayerInput = playerTransform.GetComponent<PlayerInput>();
#endif

            _thirdPersonControllerWasEnabled =
                _cachedThirdPersonController != null && _cachedThirdPersonController.enabled;

            _characterControllerWasEnabled =
                _cachedCharacterController != null && _cachedCharacterController.enabled;
        }

        private void InitializeSlideCamera()
        {
            _cameraTarget = null;

            if (_cachedThirdPersonController != null)
                _cameraTarget = _cachedThirdPersonController.CinemachineCameraTarget;

            if (_cameraTarget == null)
                return;

            Vector3 euler = _cameraTarget.transform.rotation.eulerAngles;

            _cameraYaw = euler.y;
            _cameraPitch = NormalizeAngle(euler.x);
        }

        private void LockPlayerMovementButKeepLook()
        {
            if (_cachedInputs != null)
            {
                SuppressNonLookInputs();

                // 不 disable StarterAssetsInputs。
                // 原因：要继续接收 look 输入，滑梯期间可以自由转视角。
                _cachedInputs.enabled = true;
            }

            if (_cachedThirdPersonController != null)
                _cachedThirdPersonController.enabled = false;

            // 不 disable PlayerInput。
            // 原因：如果关掉 PlayerInput，StarterAssetsInputs 就收不到鼠标/摇杆 look。
#if ENABLE_INPUT_SYSTEM
            if (_cachedPlayerInput != null)
                _cachedPlayerInput.enabled = true;
#endif

            if (_cachedCharacterController != null)
                _cachedCharacterController.enabled = _characterControllerWasEnabled;
        }

        private void RestorePlayerControl()
        {
            if (_cachedInputs != null)
            {
                SuppressNonLookInputs();
                _cachedInputs.enabled = true;
            }

            if (_cachedThirdPersonController != null)
                _cachedThirdPersonController.enabled = _thirdPersonControllerWasEnabled;

            if (_cachedCharacterController != null)
                _cachedCharacterController.enabled = _characterControllerWasEnabled;

#if ENABLE_INPUT_SYSTEM
            if (_cachedPlayerInput != null)
                _cachedPlayerInput.enabled = true;
#endif
        }

        private void SuppressNonLookInputs()
        {
            if (_cachedInputs == null)
                return;

            _cachedInputs.move = Vector2.zero;
            _cachedInputs.jump = false;
            _cachedInputs.sprint = false;

            _cachedInputs.interact = false;
            _cachedInputs.throwItem = false;
            _cachedInputs.switchEquipment = false;
            _cachedInputs.attack = false;
        }

        private void UpdateFreeLookCamera()
        {
            if (!allowFreeLookDuringSlide)
                return;

            if (_cachedInputs == null)
                return;

            if (_cameraTarget == null)
                return;

            Vector2 look = _cachedInputs.look;

            if (look.sqrMagnitude < LookThreshold)
                return;

            float deltaTimeMultiplier = Time.deltaTime;

#if ENABLE_INPUT_SYSTEM
            if (_cachedPlayerInput != null &&
                _cachedPlayerInput.currentControlScheme == "KeyboardMouse")
            {
                deltaTimeMultiplier = 1f;
            }
#endif

            float sensitivity =
#if ENABLE_INPUT_SYSTEM
                (_cachedPlayerInput != null && _cachedPlayerInput.currentControlScheme == "KeyboardMouse")
                    ? mouseLookSensitivity
                    : gamepadLookSensitivity;
#else
                gamepadLookSensitivity;
#endif

            _cameraYaw += look.x * deltaTimeMultiplier * sensitivity;
            _cameraPitch += look.y * deltaTimeMultiplier * sensitivity;

            float topClamp = _cachedThirdPersonController != null
                ? _cachedThirdPersonController.TopClamp
                : 70f;

            float bottomClamp = _cachedThirdPersonController != null
                ? _cachedThirdPersonController.BottomClamp
                : -30f;

            float cameraAngleOverride = _cachedThirdPersonController != null
                ? _cachedThirdPersonController.CameraAngleOverride
                : 0f;

            _cameraPitch = ClampAngle(_cameraPitch, bottomClamp, topClamp);

            _cameraTarget.transform.rotation = Quaternion.Euler(
                _cameraPitch + cameraAngleOverride,
                _cameraYaw,
                0f
            );
        }

        private void AbortSlideBecausePlayerDied()
        {
            StopSlideLoopSFX();

            _sliding = false;

            Log("Slide aborted because player died.");
        }

        private void MovePlayerByMotion(Transform playerTransform, Vector3 motion)
        {
            if (_cachedCharacterController != null && _cachedCharacterController.enabled)
            {
                _cachedCharacterController.Move(motion);
            }
            else
            {
                playerTransform.position += motion;
            }
        }

        private void MovePlayerDirect(Transform playerTransform, Vector3 position)
        {
            if (_cachedCharacterController != null)
            {
                bool wasEnabled = _cachedCharacterController.enabled;

                if (wasEnabled)
                    _cachedCharacterController.enabled = false;

                playerTransform.position = position;

                if (wasEnabled)
                    _cachedCharacterController.enabled = true;
            }
            else
            {
                playerTransform.position = position;
            }
        }

        private void FaceBodyDirection(Transform playerTransform, Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            playerTransform.rotation = Quaternion.Slerp(
                playerTransform.rotation,
                targetRotation,
                bodyRotationLerpSpeed * Time.deltaTime
            );
        }

        private Vector3 GetInitialDirection(Transform playerTransform)
        {
            if (pathPoints != null &&
                pathPoints.Length >= 2 &&
                pathPoints[0] != null &&
                pathPoints[1] != null)
            {
                Vector3 dir = pathPoints[1].position - pathPoints[0].position;
                if (dir.sqrMagnitude > 0.001f)
                    return dir.normalized;
            }

            return playerTransform.forward;
        }

        private void SetupAudioSource()
        {
            if (slideAudioSource == null)
                slideAudioSource = GetComponent<AudioSource>();

            if (slideAudioSource == null)
                slideAudioSource = gameObject.AddComponent<AudioSource>();

            slideAudioSource.playOnAwake = false;
            slideAudioSource.loop = false;
            slideAudioSource.spatialBlend = 0f;
        }

        private void PlaySlideStartSFX()
        {
            if (slideAudioSource == null || slideStartSFX == null)
                return;

            slideAudioSource.PlayOneShot(slideStartSFX, slideStartVolume);
        }

        private void StartSlideLoopSFX()
        {
            if (slideAudioSource == null || slideLoopSFX == null)
                return;

            slideAudioSource.clip = slideLoopSFX;
            slideAudioSource.loop = true;
            slideAudioSource.volume = slideLoopVolume;
            slideAudioSource.Play();
        }

        private void StopSlideLoopSFX()
        {
            if (slideAudioSource == null)
                return;

            if (slideAudioSource.isPlaying && slideAudioSource.clip == slideLoopSFX)
                slideAudioSource.Stop();

            slideAudioSource.loop = false;
            slideAudioSource.clip = null;
        }

        private void PlaySlideEndSFX()
        {
            if (slideAudioSource == null || slideEndSFX == null)
                return;

            slideAudioSource.PlayOneShot(slideEndSFX, slideEndVolume);
        }

        private void UpdateSlideCameraShake()
        {
            if (!shakeCameraWhileSliding)
                return;

            if (CameraShakeTarget.Instance == null)
                return;

            if (Time.time < _nextShakeTime)
                return;

            _nextShakeTime = Time.time + Mathf.Max(0.02f, slideShakeInterval);

            CameraShakeTarget.Instance.Shake(slideShakeDuration, slideShakeStrength);
        }

        private void PlayEndCameraShake()
        {
            if (!shakeCameraOnEnd)
                return;

            if (CameraShakeTarget.Instance == null)
                return;

            CameraShakeTarget.Instance.Shake(endShakeDuration, endShakeStrength);
        }

        private float NormalizeAngle(float angle)
        {
            if (angle > 180f)
                angle -= 360f;

            return angle;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f)
                angle += 360f;

            if (angle > 360f)
                angle -= 360f;

            return Mathf.Clamp(angle, min, max);
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[FinalEscortSlide] {message}", this);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[FinalEscortSlide] {message}", this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawPathGizmos)
                return;

            if (pathPoints == null || pathPoints.Length == 0)
                return;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (pathPoints[i] == null)
                    continue;

                Gizmos.DrawSphere(pathPoints[i].position, 0.18f);

                if (i < pathPoints.Length - 1 && pathPoints[i + 1] != null)
                    Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }
    }
}