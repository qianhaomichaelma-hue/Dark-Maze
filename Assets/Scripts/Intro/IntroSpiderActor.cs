using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class IntroSpiderActor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;

        [Tooltip("Drag EnemyChaser or other AI scripts here. They will be disabled when spider dismisses.")]
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

        private bool isDismissing;

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
        }

        public void Dismiss(Transform exitPoint)
        {
            if (isDismissing) return;
            StartCoroutine(DismissRoutine(exitPoint));
        }

        private IEnumerator DismissRoutine(Transform exitPoint)
        {
            isDismissing = true;

            // 关闭普通追击 AI，避免它继续追玩家
            if (behavioursToDisableOnDismiss != null)
            {
                foreach (var behaviour in behavioursToDisableOnDismiss)
                {
                    if (behaviour != null && behaviour != this)
                        behaviour.enabled = false;
                }
            }

            // 关闭攻击判定，避免玩家已经进安全区还被咬
            if (collidersToDisableOnDismiss != null)
            {
                foreach (var col in collidersToDisableOnDismiss)
                {
                    if (col != null)
                        col.enabled = false;
                }
            }

            SetWalkingAnimation(true);

            float timer = 0f;

            if (agent != null && agent.enabled && agent.isOnNavMesh && exitPoint != null)
            {
                agent.isStopped = false;
                agent.ResetPath();
                agent.SetDestination(exitPoint.position);

                while (timer < walkAwayTime)
                {
                    timer += Time.deltaTime;

                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.15f)
                        break;

                    UpdateSpeedAnimation();
                    yield return null;
                }

                agent.isStopped = true;
                agent.ResetPath();
            }
            else
            {
                // 如果没有 NavMeshAgent，就手动朝 exitPoint 或自身反方向走
                Vector3 moveDir;

                if (exitPoint != null)
                {
                    moveDir = exitPoint.position - transform.position;
                    moveDir.y = 0f;
                    moveDir.Normalize();
                }
                else
                {
                    moveDir = -transform.forward;
                }

                while (timer < walkAwayTime)
                {
                    timer += Time.deltaTime;

                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
                        transform.position += moveDir * manualWalkSpeed * Time.deltaTime;
                    }

                    yield return null;
                }
            }

            SetWalkingAnimation(false);

            if (destroyDelay > 0f)
                yield return new WaitForSeconds(destroyDelay);

            if (destroyOnDismiss)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
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
    }
}