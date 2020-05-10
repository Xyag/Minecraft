using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.IO;
public class WorldCreatorUI : MonoBehaviour
{
    public InputField worldNameInput;
    public Button createButton;
    public GameObject WorldExistsLabel;
    public UnityEvent playEvent;
    private string path;

    public void Start()
    {
        worldNameInput.onValueChanged.AddListener(fieldValueChanged); //for some reason this throws a null reference exception even though its assigned in the editor.
        //then the function runs again (due to the error im assuming) idk why. no idea why there's an error either.
        path = Application.persistentDataPath + "/";
        createButton.onClick.AddListener(() =>
        {
            SceneData.targetWorld = worldNameInput.text;
            playEvent.Invoke();
        });
    }
    public void fieldValueChanged(string val)
    {
        if (val != null && Directory.Exists(path + val))
        {
            WorldExistsLabel.SetActive(true);
            createButton.interactable = false;
        }
        else
        {
            WorldExistsLabel.SetActive(false);
            createButton.interactable = true;
        }
    }
}
