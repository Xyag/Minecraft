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
    public const int SAMPLE_INTERVAL = 8;
    public bool isSingleThreaded()
    {
        return false;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        if (chunk == null || chunk.blocks == null)
            return chunk;
        float[,,] noiseSamples = new float[Chunk.CHUNK_SIZE / SAMPLE_INTERVAL + 1, Chunk.CHUNK_SIZE / SAMPLE_INTERVAL + 1, Chunk.CHUNK_SIZE / SAMPLE_INTERVAL + 1]; //adding 1 to get one extra layer of samples right outside the chunk border
        for (int x = 0; x < noiseSamples.GetLength(0); x++)
        {
            for (int y = 0; y < noiseSamples.GetLength(1); y++)
            {
                for (int z = 0; z < noiseSamples.GetLength(2); z++)
                {
                    noiseSamples[x, y, z] = noise.sample(SAMPLE_INTERVAL * x + chunk.worldCoords.x, SAMPLE_INTERVAL * y + chunk.worldCoords.y, SAMPLE_INTERVAL * z + chunk.worldCoords.z);
                }
            }
        }
        float slope = 1.0f / (maxHeight - minHeight);
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    Vector3 blockCoords = new Vector3(chunk.worldCoords.x + x, chunk.worldCoords.y + y, chunk.worldCoords.z + z);
                    float caveNoise = WorldGenerator.trilinearInterpolate(
                        new Vector3((float)(x % SAMPLE_INTERVAL) / SAMPLE_INTERVAL, (float)(y % SAMPLE_INTERVAL) / SAMPLE_INTERVAL, (float)(z % SAMPLE_INTERVAL) / SAMPLE_INTERVAL),
                        noiseSamples[x / SAMPLE_INTERVAL, y / SAMPLE_INTERVAL, z / SAMPLE_INTERVAL],
                        noiseSamples[x / SAMPLE_INTERVAL + 1, y / SAMPLE_INTERVAL, z / SAMPLE_INTERVAL],
                        noiseSamples[x / SAMPLE_INTERVAL, y / SAMPLE_INTERVAL + 1, z / SAMPLE_INTERVAL],
                        noiseSamples[x / SAMPLE_INTERVAL, y / SAMPLE_INTERVAL, z / SAMPLE_INTERVAL + 1],
                        noiseSamples[x / SAMPLE_INTERVAL + 1, y / SAMPLE_INTERVAL + 1, z / SAMPLE_INTERVAL],
                        noiseSamples[x / SAMPLE_INTERVAL, y / SAMPLE_INTERVAL + 1, z / SAMPLE_INTERVAL + 1],
                        noiseSamples[x / SAMPLE_INTERVAL + 1, y / SAMPLE_INTERVAL, z / SAMPLE_INTERVAL + 1],
                        noiseSamples[x / SAMPLE_INTERVAL + 1, y / SAMPLE_INTERVAL + 1, z / SAMPLE_INTERVAL + 1]);

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