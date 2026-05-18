using TMPro;
using UnityEngine;
using DarkMazeMinimal;

namespace DarkMazeUI
{
    public class ChaseWarningUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerChaseTracker chaseTracker;
        [SerializeField] private TMP_Text warningText;

        [Header("Text")]
        [SerializeField] private string warningMessage = "BEING CHASED";

        [Header("Options")]
        [SerializeField] private bool hideWhenSafe = true;

        private void Start()
        {
            if (chaseTracker == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    chaseTracker = player.GetComponent<PlayerChaseTracker>();
            }

            if (warningText != null)
                warningText.text = warningMessage;

            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (warningText == null)
                return;

            bool isBeingChased = chaseTracker != null && chaseTracker.IsBeingChased;

            if (hideWhenSafe)
            {
                warningText.gameObject.SetActive(isBeingChased);
            }
            else
            {
                warningText.gameObject.SetActive(true);
                warningText.text = isBeingChased ? warningMessage : string.Empty;
            }
        }
    }
}
