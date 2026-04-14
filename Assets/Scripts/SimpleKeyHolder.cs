using UnityEngine;

namespace DarkMazeMinimal
{
    public class SimpleKeyHolder : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool hasKey = false;

        public bool HasKey => hasKey;

        public void GiveKey()
        {
            hasKey = true;
            Debug.Log("[SimpleKeyHolder] Key acquired.", this);
        }
    }
}
