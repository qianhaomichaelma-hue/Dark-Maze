using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class IntroSpiderActor : MonoBehaviour
    {
        [Header("Destroy Target")]
        [Tooltip("The whole spider root object that should disappear. If empty, the script will try to use the NavMeshAgent object.")]
        [SerializeField] private GameObject objectToRemove;

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;

        [Tooltip("Drag EnemyChaser or other AI scripts here. They will be disabled when the spider dismisses.")]
        [SerializeField] private MonoBehaviour[] behavioursToDisableOnDismiss;

        [Tooltip("Optional. Drag attack / body colliders here if you want to disable damage when dismissing.")]
        [SerializeField] private Collider[] collidersToDisableOnDismiss;

        [Header("Dismiss")]
        [SerializeField] private float walkAwayTime = 2.5f;
        [SerializeField] private float destroyDelay = 0.5f;
        [SerializeField] private float manualWalkSpeed = 2.0f;
        [SerializeField] private bool destroyOnDismiss = true;

        [Header("Animator Parameters")]
        [SerializeField] private string speedFloatName = "Speed";
        [SerializeField] private string walkingBoolName = "IsWalking";

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private bool isDismissing;

        public bool IsDismissing => isDismissing;

        public event Action<IntroSpiderActor> OnRemoved;

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();

            if (agent == null)
                agent = GetComponentInParent<NavMeshAgent>();

            animator = GetComponentInChildren<Animator>();

            if (objectToRemove == null)
            {
                if (agent != null)
                    objectToRemove = agent.gameObject;
                else
                    objectToRemove = gameObject;
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void SetDestroyOnDismiss(bool destroy)
        {
            destroyOnDismiss = destroy;
        }

        public void ResetForReuse()
        {
            StopAllCoroutines();

            isDismissing = false;

            ResolveReferences();

            EnableGameplayBehaviours();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }

            SetWalkingAnimation(false);

            Log("ResetForReuse.");
        }

        public void Dismiss(Transform exitPoint)
        {
            if (isDismissing)
                return;

            StartCoroutine(DismissRoutine(exitPoint));
        }

        public void ForceRemoveImmediately()
        {
            StopAllCoroutines();

            DisableGameplayBehaviours();
            StopAgent();

            RemoveNow();

            Log("ForceRemoveImmediately.");
        }

        private IEnumerator DismissRoutine(Transform exitPoint)
        {
            isDismissing = true;

            // 撤退一旦开始，就彻底关闭正式 AI 和攻击判定。
            // 这样玩家离开安全区后，蜘蛛不会折返追玩家。
            DisableGameplayBehaviours();

            SetWalkingAnimation(true);

            float timer = 0f;

            bool canUseAgent =
                agent != null &&
                agent.enabled &&
                agent.isOnNavMesh &&
                exitPoint != null;

            if (canUseAgent)
            {
                agent.isStopped = false;
                agent.ResetPath();
                agent.SetDestination(exitPoint.position);

                while (timer < walkAwayTime)
                {
                    timer += Time.deltaTime;

                    UpdateSpeedAnimation();

                    yield return null;
                }

                StopAgent();
            }
            else
            {
                Transform moveTransform = objectToRemove != null ? objectToRemove.transform : transform;

                Vector3 moveDir;

                if (exitPoint != null)
                {
                    moveDir = exitPoint.position - moveTransform.position;
                    moveDir.y = 0f;
                    moveDir.Normalize();
                }
                else
                {
                    moveDir = -moveTransform.forward;
                }

                while (timer < walkAwayTime)
                {
                    timer += Time.deltaTime;

                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                        moveTransform.rotation = Quaternion.Slerp(moveTransform.rotation, targetRot, Time.deltaTime * 8f);
                        moveTransform.position += moveDir * manualWalkSpeed * Time.deltaTime;
                    }

                    yield return null;
                }
            }

            SetWalkingAnimation(false);

            if (destroyDelay > 0f)
                yield return new WaitForSeconds(destroyDelay);

            RemoveNow();
        }

        private void ResolveReferences()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();

                if (agent == null)
                    agent = GetComponentInParent<NavMeshAgent>();
            }

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (objectToRemove == null)
            {
                if (agent != null)
                    objectToRemove = agent.gameObject;
                else
                    objectToRemove = gameObject;
            }
        }

        private void DisableGameplayBehaviours()
        {
            if (behavioursToDisableOnDismiss != null)
            {
                foreach (var behaviour in behavioursToDisableOnDismiss)
                {
                    if (behaviour != null && behaviour != this)
                        behaviour.enabled = false;
                }
            }

            if (collidersToDisableOnDismiss != null)
            {
                foreach (var col in collidersToDisableOnDismiss)
                {
                    if (col != null)
                        col.enabled = false;
                }
            }
        }

        private void EnableGameplayBehaviours()
        {
            if (behavioursToDisableOnDismiss != null)
            {
                foreach (var behaviour in behavioursToDisableOnDismiss)
                {
                    if (behaviour != null && behaviour != this)
                        behaviour.enabled = true;
                }
            }

            if (collidersToDisableOnDismiss != null)
            {
                foreach (var col in collidersToDisableOnDismiss)
                {
                    if (col != null)
                        col.enabled = true;
                }
            }
        }

        private void StopAgent()
        {
            if (agent == null) return;
            if (!agent.enabled) return;
            if (!agent.isOnNavMesh) return;

            agent.isStopped = true;
            agent.ResetPath();
        }

        private void RemoveNow()
        {
            GameObject target = objectToRemove != null ? objectToRemove : gameObject;

            OnRemoved?.Invoke(this);

            if (destroyOnDismiss)
            {
                Destroy(target);
            }
            else
            {
                target.SetActive(false);
            }

            Log($"Removed spider target = {target.name}");
        }

        private void SetWalkingAnimation(bool walking)
        {
            if (animator == null) return;

            if (HasParameter(walkingBoolName, AnimatorControllerParameterType.Bool))
                animator.SetBool(walkingBoolName, walking);

            if (HasParameter(speedFloatName, AnimatorControllerParameterType.Float))
                animator.SetFloat(speedFloatName, walking ? 1f : 0f);
        }

        private void UpdateSpeedAnimation()
        {
            if (animator == null || agent == null) return;

            if (HasParameter(speedFloatName, AnimatorControllerParameterType.Float))
                animator.SetFloat(speedFloatName, agent.velocity.magnitude);
        }

        private bool HasParameter(string paramName, AnimatorControllerParameterType type)
        {
            if (animator == null || string.IsNullOrEmpty(paramName))
                return false;

            foreach (var p in animator.parameters)
            {
                if (p.name == paramName && p.type == type)
                    return true;
            }

            return false;
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log($"[IntroSpiderActor] {message}", this);
        }
    }
}