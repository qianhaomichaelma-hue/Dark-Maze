using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DarkMazeMinimal
{
    [DisallowMultipleComponent]
    public class MainMenuActions : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string gameSceneName = "MainGame_RefinedV03";

        [Header("Click Delay")]
        [Tooltip("Small delay so the template button click sound can be heard before loading/quitting.")]
        [SerializeField] private float clickDelay = 0.15f;

        private bool _busy;

        public void PlayStory()
        {
            if (_busy) return;
            StartCoroutine(PlayStoryRoutine());
        }

        public void ExitGame()
        {
            if (_busy) return;
            StartCoroutine(ExitGameRoutine());
        }

        private IEnumerator PlayStoryRoutine()
        {
            _busy = true;

            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (clickDelay > 0f)
                yield return new WaitForSecondsRealtime(clickDelay);

            SceneManager.LoadScene(gameSceneName);
        }

        private IEnumerator ExitGameRoutine()
        {
            _busy = true;

            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (clickDelay > 0f)
                yield return new WaitForSecondsRealtime(clickDelay);

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}