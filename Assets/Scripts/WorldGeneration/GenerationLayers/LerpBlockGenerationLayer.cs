using System.Collections.Generic;
using UnityEngine;

public class LerpBlockGenerationLayer : IGenerationLayer
{
    public int maxHeight;
    public int minHeight;
    public float tolerance;
    public NoiseGroup noise;
    public BlockType dontReplace;
    public BlockType replaceWith;
    public bool isSingleThreaded()
    {
        return false;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        float slope = 1.0f / (maxHeight - minHeight);
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    Vector3 blockCoords = new Vector3(chunk.worldCoords.x + x, chunk.worldCoords.y + y, chunk.worldCoords.z + z);
                    float caveNoise = noise.sample(blockCoords.x, blockCoords.y, blockCoords.z);

                    float scaledCaveNoise = (slope * (-blockCoords.y) + slope * maxHeight) * caveNoise;

                    if (scaledCaveNoise > tolerance && chunk.blocks[x, y, z].type != dontReplace)
                    {
                        chunk.blocks[x, y, z].type = replaceWith;
                    }
                }
            }
        }

        return chunk;
    }
}