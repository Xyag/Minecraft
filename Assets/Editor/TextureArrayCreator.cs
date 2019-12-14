using UnityEngine;
using UnityEditor;

public class TextureArrayCreator : EditorWindow
{
    private string texName = "TextureArray";
    private int texDimension = 16;
    public Texture[] Textures = new Texture[1];
    private SerializedObject thisSerialized;

    [MenuItem("Window/TextureArrayCreator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TextureArrayCreator));

    }

    void OnGUI()
    {
        GUILayout.Label("Texture Array", EditorStyles.boldLabel);
        texName = EditorGUILayout.TextField("Texture Name", texName);
        texDimension = EditorGUILayout.IntField("Texture Width/Height", texDimension);
        // "target" can be any class derrived from ScriptableObject 
        // (could be EditorWindow, MonoBehaviour, etc)
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty texturesProperty = so.FindProperty("Textures");

        EditorGUILayout.PropertyField(texturesProperty, true); // True means show children
        so.ApplyModifiedProperties(); // Remember to apply modified properties

        if (GUILayout.Button("Create Array"))
        {
            Texture2DArray texArray = new Texture2DArray(texDimension, texDimension, Textures.Length, Textures[0].graphicsFormat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            for (int i = 0; i < Textures.Length; i++)
            {
                Graphics.CopyTexture(Textures[i], 0, texArray, i);
            }
            texArray.filterMode = FilterMode.Point;
            texArray.wrapMode = TextureWrapMode.Repeat;
            AssetDatabase.CreateAsset(texArray, "Assets/" + texName + ".asset");
        }
    }
}