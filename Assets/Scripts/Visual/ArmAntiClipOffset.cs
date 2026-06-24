using UnityEngine;

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public class ArmAntiClipOffset : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;

        [Header("Only Apply While Moving")]
        [SerializeField] private bool onlyWhileMoving = true;
        [SerializeField] private float moveSpeedThreshold = 0.1f;

        [Header("Upper Arm Rotation Offset")]
        [Tooltip("Try small values first. If the arm moves inward, reverse the sign.")]
        [SerializeField] private Vector3 leftUpperArmOffset = new Vector3(0f, 0f, 8f);

        [Tooltip("Try small values first. If the arm moves inward, reverse the sign.")]
        [SerializeField] private Vector3 rightUpperArmOffset = new Vector3(0f, 0f, -8f);

        [Header("Optional Lower Arm Offset")]
        [SerializeField] private Vector3 leftLowerArmOffset = Vector3.zero;
        [SerializeField] private Vector3 rightLowerArmOffset = Vector3.zero;

        [Header("Strength")]
        [Range(0f, 1f)]
        [SerializeField] private float strength = 1f;

        private Transform leftUpperArm;
        private Transform rightUpperArm;
        private Transform leftLowerArm;
        private Transform rightLowerArm;

        private void Reset()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (animator != null)
            {
                leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            }
        }

        private void LateUpdate()
        {
            if (animator == null)
                return;

            if (onlyWhileMoving && characterController != null)
            {
                Vector3 horizontalVelocity = characterController.velocity;
                horizontalVelocity.y = 0f;

                if (horizontalVelocity.magnitude < moveSpeedThreshold)
                    return;
            }

            ApplyOffset(leftUpperArm, leftUpperArmOffset);
            ApplyOffset(rightUpperArm, rightUpperArmOffset);
            ApplyOffset(leftLowerArm, leftLowerArmOffset);
            ApplyOffset(rightLowerArm, rightLowerArmOffset);
        }

        private void ApplyOffset(Transform bone, Vector3 eulerOffset)
        {
            if (bone == null)
                return;

            Quaternion offset = Quaternion.Euler(eulerOffset * strength);
            bone.localRotation = bone.localRotation * offset;
        }
    }
}