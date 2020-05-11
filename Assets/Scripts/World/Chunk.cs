using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public const int CHUNK_SIZE = 16;
    public const int BLOCK_COUNT = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;

    public Block[,,] blocks;
    public Dictionary<Vector3Int, BlockInstance> instanceData;
    public Vector3Int worldCoords;
    public Vector3Int chunkCoords;
    public GameObject gameObject;
    public MeshData renderData;
    public bool changed = true;

    public Chunk(Block[,,] blocks, Vector3Int chunkCoords)
    {
        if (blocks != null && (blocks.GetLength(0) != CHUNK_SIZE || blocks.GetLength(1) != CHUNK_SIZE || blocks.GetLength(2) != CHUNK_SIZE))
            throw new System.Exception("Invalid chunk dimensions: " + chunkCoords);

        this.worldCoords = CHUNK_SIZE * chunkCoords;
        this.chunkCoords = chunkCoords;
        this.blocks = blocks;
        gameObject = null;
        renderData = null;
        changed = true;
    }
}
