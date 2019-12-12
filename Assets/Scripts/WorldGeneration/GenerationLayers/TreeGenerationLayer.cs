using System.Collections.Generic;
using UnityEngine;

public class TreeGenerationLayer : IGenerationLayer
{
    public int maxHeight = 10;
    public int minHeight = 3;
    public float treeDensity = 4;
    public float treeDensityConstant = 0;
    private FastNoise noise = new FastNoise();
    public bool isSingleThreaded()
    {
        return true;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        int treeCount = (int)(treeDensityConstant + treeDensity * Mathf.PerlinNoise((float)chunk.chunkCoords.x * 2.355f, (float)chunk.chunkCoords.z * 2.355f));
        if (chunk.worldCoords.y > 0)
        {
            for (int t = 0; t < treeCount; t++)
            {
                Vector2Int treeCoords = new Vector2Int((int)((float)(Chunk.CHUNK_SIZE - 1) * Mathf.Clamp01(Mathf.PerlinNoise((float)(145 * t + chunk.chunkCoords.x) * 13.5f, (float)(1763 * t + chunk.chunkCoords.z) * 13.5f)))
                  , (int)((float)(Chunk.CHUNK_SIZE - 1) * Mathf.Clamp01(Mathf.PerlinNoise((float)(t * 1230 + chunk.chunkCoords.x) * 0.265f, (float)(t * 3333 + chunk.chunkCoords.z * 0.265f)))));
                int high = Chunk.CHUNK_SIZE - 1;
                int treeHeight = (int)Mathf.Clamp((float)maxHeight * Mathf.PerlinNoise(546.888f * (float)(222 * t + chunk.worldCoords.x + 1474 * treeCoords.x), 781.18f * (float)(919191 * t + chunk.worldCoords.z + 12499 * treeCoords.y)), minHeight, Chunk.CHUNK_SIZE - 1);
                treeHeight = treeHeight + 1 - (treeHeight % 2); //forces it to be odd so the leaves line up
                if (chunk.blocks[treeCoords.x, high, treeCoords.y].type == BlockType.empty)
                {
                    for (high = Chunk.CHUNK_SIZE - 1; high > 0 && chunk.blocks[treeCoords.x, high, treeCoords.y].type == BlockType.empty; high--)
                    {
                        //finding highest nonempty block in column
                    }
                    //makes sure the tree will be above the max height and not floating
                    if (high < Chunk.CHUNK_SIZE - minHeight && chunk.blocks[treeCoords.x, high, treeCoords.y].type != BlockType.empty)
                    {
                        //generates tree if there's enough vertical room
                        for (int i = high + 1; i < treeHeight; i++)
                        {
                            chunk.blocks[treeCoords.x, i, treeCoords.y].type = BlockType.log;
                        }

                        Vector3Int leafStart = new Vector3Int(treeCoords.x + chunk.worldCoords.x, treeHeight + chunk.worldCoords.y, treeCoords.y + chunk.worldCoords.z);
                        for (int y = 0; y < treeHeight; y += 2)
                        {
                            for (int x = 0; x < treeHeight - y; x++)
                            {
                                for (int z = 0; z < treeHeight - y; z++)
                                {
                                    world.setBlock(leafStart + new Vector3Int(x + (y - treeHeight) / 2, y / 2, z + (y - treeHeight) / 2), BlockType.leaves, true);
                                }
                            }
                        }
                    }
                }
            }
        }

        return chunk;
    }
}