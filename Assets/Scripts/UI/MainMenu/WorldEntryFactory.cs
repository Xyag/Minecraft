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
        List<WorldInfo> infos = new List<WorldInfo>(worlds.Length);
        foreach (var w in worlds)
        {
            WorldInfo info = new WorldInfo();
            if (File.Exists(w+"/worldInfo.dat"))
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(w + "/worldInfo.dat")))
                {
                    info.lastPlayed = System.DateTime.FromFileTimeUtc(br.ReadInt64()).ToLocalTime();
                }
            }
            else
            {
                info.lastPlayed = System.DateTime.Now;
            }
            
            info.fileName = w.Substring(w.LastIndexOf('/')+1);
            infos.Add(info);
        }
        infos.Sort((a, b) => b.lastPlayed.CompareTo(a.lastPlayed)); //sorts by most recently played
        foreach(var wi in infos)
        {
            Generate(wi);
        }
    }
    public void Generate(WorldInfo info)
    {
        if (!Directory.Exists(location + info.fileName))
        {
            Debug.LogError("world not found: " + info.fileName);
            return;
        }
        GameObject entry = Instantiate(EntryPrefab, ListPanel, false);
        entry.transform.position = new Vector3(ListPanel.position.x, ListPanel.position.y - EntrySpacing * index, ListPanel.position.z);
        string dispName = info.fileName;
        if (info.fileName.Length > MaxWorldNameLength)
        {
            dispName = info.fileName.Substring(0, MaxWorldNameLength-3) + "...";
            Debug.Log(dispName);
        }
        var scriptEntiry = entry.GetComponent<WorldEntry>();
        scriptEntiry.SetValues(info.fileName, dispName, info.lastPlayed.ToString(), g => { SceneData.targetWorld = g.worldName; playEvent.Invoke(); }, g => ConfirmDelete(g.worldName));
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
