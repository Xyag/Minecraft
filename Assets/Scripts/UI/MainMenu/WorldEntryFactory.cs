using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Events;

public class WorldEntryFactory : MonoBehaviour
{
    
    public Transform ListPanel;
    public GameObject EntryPrefab;
    public float EntrySpacing = 1;
    public int MaxWorldNameLength = 20;
    public UnityEvent playEvent;
    public Text ConfirmDeleteWorldNameText;
    public UnityEvent ConfirmDeleteEvent;

    private int index = 0;
    private List<WorldEntry> entries;
    private string location;
    private string worldDeleting;

    public void Start()
    {
        location = Application.persistentDataPath + "/";
        entries = new List<WorldEntry>();
        GenerateAll(location);
    }
    public void GenerateAll(string path)
    {
        string[] worlds = Directory.GetDirectories(path);
        foreach (var w in worlds)
        {
            Generate(w.Substring(w.LastIndexOf('/')+1));
        }
    }
    public void Generate(string worldName)
    {
        if (!Directory.Exists(location + worldName))
        {
            Debug.LogError("world not found: " + worldName);
            return;
        }
        GameObject entry = Instantiate(EntryPrefab, ListPanel, false);
        entry.transform.position = new Vector3(ListPanel.position.x, ListPanel.position.y - EntrySpacing * index, ListPanel.position.z);
        string dispName = worldName;
        if (worldName.Length > MaxWorldNameLength)
        {
            dispName = worldName.Substring(0, MaxWorldNameLength-3) + "...";
            Debug.Log(dispName);
        }
        var scriptEntiry = entry.GetComponent<WorldEntry>();
        scriptEntiry.SetValues(worldName, dispName, System.DateTime.Now.ToString(), g => { SceneData.targetWorld = g.worldName; playEvent.Invoke(); }, g => ConfirmDelete(g.worldName));
        entries.Add(scriptEntiry);
        index++;
    }
    public void Reset()
    {
        index = 0;
        foreach(var g in entries)
        {
            Destroy(g.gameObject);
        }
        GenerateAll(location);
    }
    public void ConfirmDelete(string worldName)
    {
        ConfirmDeleteWorldNameText.text = worldName;
        worldDeleting = worldName;
        ConfirmDeleteEvent.Invoke();
    }
    public void DeleteWorld()
    {
        if (!Directory.Exists(location + worldDeleting))
        {
            Debug.LogError("world not found: " + worldDeleting);
            return;
        }
        try
        {
            Directory.Delete(location + worldDeleting, true);
            Debug.Log("Deleted: " + location + worldDeleting);
        }
        catch
        {
            Debug.Log("error deleting world");
        }
        Reset();
    }
}
