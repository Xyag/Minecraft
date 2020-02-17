using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldEntry : MonoBehaviour
{
    public string worldName;
    public Text WorldNameText;
    public Text WorldDateText;
    public Button PlayButton;
    public Button DeleteButton;
    public void SetValues(string worldName, string dispName, string date, System.Action<WorldEntry> playClicked, System.Action<WorldEntry> deleteClicked)
    {
        this.worldName = worldName;
        WorldNameText.text = dispName;
        WorldDateText.text = date;
        PlayButton.onClick.AddListener(() => playClicked(this));
        DeleteButton.onClick.AddListener(() => deleteClicked(this));
    }
}
