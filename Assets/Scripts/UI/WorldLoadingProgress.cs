using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldLoadingProgress : MonoBehaviour
{
    public GameObject PanelObject;
    public PlayerManager playerManager;
    public Slider SliderUI;
    public Text LoadingText;
    public WorldLoader WorldLoad;
    private float progress;

    private int initialChunks = 0;
    private bool running;

    private void Start()
    {
        running = false;
        Time.timeScale = 0;
    }

    void Update()
    {
        if (initialChunks == 0)
        {
            initialChunks = WorldLoad.toLoad;
            SliderUI.value = 0;
            LoadingText.text = "0%";
        }
        else
        {
            int meshCount = MeshGenerator.finishedMeshes.Count();
            if (!running && meshCount > 0)
            {
                running = true;
            }
            if (running)
            {
                progress = 1 - ((float)meshCount / (float)initialChunks);
                SliderUI.value = progress;
                LoadingText.text = string.Format("{0:n0}%", progress * 100.0f);
                if (progress >= 1.0f)
                {
                    Time.timeScale = 1;
                    running = false;
                    PanelObject.SetActive(false);
                    playerManager.finishedLoading();
                }
            }
        }
    }
}
