using System.Collections.Generic;
using UnityEngine;

public class TreeGenerationLayer : IGenerationLayer
{
    public int maxTreeHeight = 10;
    public int minTreeHeight = 3;

    public float treeDensity = 4;
    public float treeDensityConstant = 0;
    private FastNoise noise = new FastNoise();
    public bool isSingleThreaded()
    {
        return true;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        if (chunk.blocks == null)
            return chunk;
        int treeCount = (int)(treeDensity * Mathf.PerlinNoise(2.1f * chunk.worldCoords.x, 2.1f * chunk.worldCoords.z)+treeDensityConstant);
        for (int i = 0; i < treeCount; i++)
        {
            Debug.Log(treeCount);
            generateTree(chunk, world, i);
        }
        return chunk;
    }
    private void generateTree(Chunk chunk, World world, int iteration)
    {
        int treeCoord = (int)(Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Mathf.PerlinNoise(5.7f*iteration*chunk.worldCoords.x, 5.7f*iteration*chunk.worldCoords.y));
        Vector2Int treeLocation = new Vector2Int(treeCoord % Chunk.CHUNK_SIZE, treeCoord / Chunk.CHUNK_SIZE);
        bool set = false;
        if (chunk.worldCoords.y > -33)
        {
            int high;
            for (high = Chunk.CHUNK_SIZE - 1; high >= 0; high--)
            {
                var data = Block.blockTypes[(int)chunk.blocks[treeLocation.x, high, treeLocation.y].type];
                if (data.opaque)
                {
                    set = true;
                    break;
                }
            }
            if (set)
            {
                for (int i = high; i < maxTreeHeight; i++)
                {
                    if (i + high < Chunk.CHUNK_SIZE)
                    {
                        chunk.blocks[treeLocation.x, i, treeLocation.y].type = BlockType.glass;
                    }
                    else
                    {
                        world.setBlock(chunk.worldCoords + new Vector3Int(treeLocation.x, i, treeLocation.y), BlockType.glass, forceLoadChunk: true);
                    }
                }
            }
        }
    }
}