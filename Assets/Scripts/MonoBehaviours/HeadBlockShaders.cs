using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[RequireComponent(typeof(Camera))]
public class HeadBlockShaders : MonoBehaviour
{
    public PlayerManager PlayerManager;

    public BlockType[] Blocks;
    public Material[] Shaders;
    private Material currMaterial;
    // Start is called before the first frame update
    void Start()
    {
        PlayerManager.OnHeadEnterBlock += onEnterHeadBlock;
    }
    private void onEnterHeadBlock(object sender, HeadEnterBlockEventArgs args)
    {
        for(int i = 0; i < Blocks.Length; i++)
        {
            if (args.block.type == Blocks[i])
            {
                currMaterial = Shaders[i];
                return;
            }
        }
        currMaterial = null;
        return;
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (currMaterial != null)
        {
            Graphics.Blit(source, destination, currMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

}
