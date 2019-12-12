using System.Collections.Generic;
using UnityEngine;

public class HillsGenerationLayer : IGenerationLayer
{
    public bool isSingleThreaded()
    {
        return false;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        int dirtDepth = 3;
        Block[,,] blocks = chunk.blocks;
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                int height = (int)(32 * Mathf.PerlinNoise((float)(x + chunk.worldCoords.x) * 0.123456765432f, (float)(z + chunk.worldCoords.z) * 0.123456765432f));
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    if (y + chunk.worldCoords.y == height)
                    {
                        blocks[x, y, z] = new Block(BlockType.grass);
                    }
                    else if (y + chunk.worldCoords.y < height && y + chunk.worldCoords.y >= height - dirtDepth)
                    {
                        blocks[x, y, z] = new Block(BlockType.dirt);
                    }
                    else if (y + chunk.worldCoords.y < height)
                    {
                        blocks[x, y, z] = new Block(BlockType.stone);
                    }
                    else
                    {
                        blocks[x, y, z] = new Block(BlockType.empty);
                    }
                }
            }
        }

        return chunk;
    }
}