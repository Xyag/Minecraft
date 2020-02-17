using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    private int loadingIndex = 0;
    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void loadScene(int sceneIndex)
    {
        var result = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
        loadingIndex = sceneIndex;
        result.completed += loading_completed;
    }

    private void loading_completed(AsyncOperation obj)
    {
        Scene currActive = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(loadingIndex));
        SceneManager.UnloadSceneAsync(currActive);
    }
}
