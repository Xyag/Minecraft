using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ChatPanelUI : MonoBehaviour
{
    public InputField textBox;
    public WorldManager worldManager;
    public GameObject chatTextPrefab;
    public Transform chatTextScrollView;
    public float chatTextSpacing = 40;
    public Vector2 chatTextOffset = new Vector2(0,15);
    public float chatLeftPaddingPx = 5;
    public int maxChatLines = 50;
    public int maxChatLength = 54;
    public float openSpeed = 5;

    private Pool<GameObject> chatLinePool;
    private bool opening = false;

    public void open()
    {
        opening = true;
    }
    public void close()
    {
        opening = false;
    }
    public void Awake()
    {
        chatLinePool = new Pool<GameObject>(() => Instantiate<GameObject>(chatTextPrefab), g => g.activeInHierarchy, g => g.SetActive(true), maxChatLines);
        transform.localScale = new Vector3(1, 0, 1);
        close();
    }
    public void Update()
    {
        if (opening)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 1, 1), Time.fixedDeltaTime * openSpeed);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 0, 1), Time.fixedDeltaTime * openSpeed);
        }
        if (!opening && Input.GetKeyDown(KeyCode.Return))
        {
            open();
        }
        if (opening && Input.GetKeyDown(KeyCode.Escape))
        {
            close();
        }
    }
    public void onSend()
    {
        string text = textBox.text;
        if (worldManager.runGameCommand(text))
        {
            return;
        }
        //need to display the chat message
        foreach (var obj in chatLinePool.objects)
        {
            if (obj.activeInHierarchy == true)
            {
                obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y + chatTextSpacing, obj.transform.position.z);
                if (obj.transform.position.y > chatTextOffset.y + (float)(maxChatLines-1)*chatTextSpacing)
                {
                    obj.SetActive(false);
                }
            }
        }
        GameObject message = chatLinePool.get();
        RectTransform rectTransform = message.GetComponent<RectTransform>();
        rectTransform.SetParent(chatTextScrollView);
        rectTransform.position = chatTextOffset;
        rectTransform.offsetMin = new Vector2(5, 0);
        rectTransform.offsetMax = new Vector2(0, 30);
        
        

        text = "Player: " + text;
        if (text.Length > maxChatLength)
        {
            text = text.Substring(0, maxChatLength);
        }
        message.GetComponent<Text>().text = text;
    }
}
