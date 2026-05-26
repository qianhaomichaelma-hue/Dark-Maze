using System.Collections;
using UnityEngine;
using StarterAssets;

namespace DarkMazeMinimal
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerState player;

        [Header("Respawn")]
        [SerializeField] private Transform currentRespawnPoint;

        [Tooltip("画面完全变黑后，等待多久再复活。")]
        [SerializeField] private float respawnDelay = 0.6f;

        [Header("Death Transition")]
        [SerializeField] private DeathPostProcessFader deathPostProcessFader;
        [SerializeField] private bool useDeathTransition = true;
        [SerializeField] private bool fadeBackAfterRespawn = true;

        private bool _respawning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void RegisterPlayer(PlayerState p)
        {
            player = p;
        }

        public PlayerState GetPlayerState()
        {
            return player;
        }

        public void SetRespawnPoint(Transform point)
        {
            if (point == null) return;

            currentRespawnPoint = point;
        }

        public void RequestRespawn()
        {
            if (_respawning) return;
            if (player == null) return;

            if (currentRespawnPoint == null)
            {
                Debug.LogWarning("[GameManager] No respawn point set yet.");
                return;
            }

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            _respawning = true;

            // 死亡后再次强制锁住输入，并清空当前动作输入
            LockAndStopPlayerControl();

            // Post Processing 渐变到黑
            if (useDeathTransition && deathPostProcessFader != null)
            {
                yield return StartCoroutine(deathPostProcessFader.FadeToBlack());
            }

            // 黑屏后等待一小段时间
            if (respawnDelay > 0f)
                yield return new WaitForSeconds(respawnDelay);

            // 关键：
            // 玩家死亡只重置 intro 蜘蛛生成器，不触发蜘蛛撤退动画。
            // 这样玩家复活后再次进入 Spawn Volume 时，教学蜘蛛会重新生成。
            IntroSpiderSpawner.ResetAllForNewLife();

            // 复活
            if (player != null && currentRespawnPoint != null)
            {
                player.ReviveAt(currentRespawnPoint);
            }

            // PlayerState.ReviveAt 会恢复输入，所以这里如果要等画面淡回来，再重新锁一次
            if (fadeBackAfterRespawn && useDeathTransition && deathPostProcessFader != null)
            {
                LockAndStopPlayerControl();

                yield return StartCoroutine(deathPostProcessFader.FadeFromBlack());

                UnlockPlayerControl();
            }

            _respawning = false;
        }

        private void LockAndStopPlayerControl()
        {
            if (player == null) return;

            StarterAssetsInputs inputs = player.GetComponent<StarterAssetsInputs>();
            ThirdPersonController controller = player.GetComponent<ThirdPersonController>();

            if (inputs != null)
            {
                inputs.move = Vector2.zero;
                inputs.look = Vector2.zero;
                inputs.jump = false;
                inputs.sprint = false;

                // 你项目里扩展过的输入
                inputs.interact = false;
                inputs.throwItem = false;
                inputs.switchEquipment = false;
                inputs.attack = false;

                inputs.enabled = false;
            }

            if (controller != null)
                controller.enabled = false;
        }

        private void UnlockPlayerControl()
        {
            if (player == null) return;

            StarterAssetsInputs inputs = player.GetComponent<StarterAssetsInputs>();
            ThirdPersonController controller = player.GetComponent<ThirdPersonController>();

            if (inputs != null)
                inputs.enabled = true;

            if (controller != null)
                controller.enabled = true;
        }
    }
}