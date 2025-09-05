using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class FlowManager : MonoBehaviour
    {
        [SerializeField] private float loadingDelay = 1f;
        public static FlowManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void LoadLoadingScene(string targetScene)
        {
            StartCoroutine(LoadSceneAsync(targetScene));
        }

        private System.Collections.IEnumerator LoadSceneAsync(string targetScene)
        {

            yield return new WaitForSeconds(loadingDelay);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
            while (!asyncLoad.isDone)
                yield return null;
        }
    }

}