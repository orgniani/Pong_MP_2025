using System;
using System.Collections;
using Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class FlowManager : MonoBehaviour
    {
        [Header("Loading")]
        [SerializeField] private float loadingDelay = 1f;
        [SerializeField] private GameObject loadingScreenRoot;

        public static FlowManager Instance { get; private set; }

        public bool IsLoading { get; private set; }

        public event Action OnLoadingScreenHidden;

        private Coroutine _hideLoadingScreenCoroutine;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            if (!ReferenceValidator.Validate(loadingScreenRoot, nameof(loadingScreenRoot), this)) return;

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void LoadLoadingScene(string targetScene)
        {
            StartCoroutine(LoadSceneAsync(targetScene));
        }

        private IEnumerator LoadSceneAsync(string targetScene)
        {
            yield return new WaitForSeconds(loadingDelay);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
            while (!asyncLoad.isDone)
                yield return null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsLoading = true;
            loadingScreenRoot.SetActive(true);

            if (_hideLoadingScreenCoroutine != null)
                StopCoroutine(_hideLoadingScreenCoroutine);

            _hideLoadingScreenCoroutine = StartCoroutine(HideLoadingScreenAfterDelay());
        }

        private IEnumerator HideLoadingScreenAfterDelay()
        {
            yield return new WaitForSeconds(loadingDelay);

            loadingScreenRoot.SetActive(false);
            IsLoading = false;
            _hideLoadingScreenCoroutine = null;

            OnLoadingScreenHidden?.Invoke();
        }
    }
}