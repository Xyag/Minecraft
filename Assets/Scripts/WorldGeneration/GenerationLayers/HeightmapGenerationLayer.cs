using System.Collections.Generic;
using UnityEngine;

public class HeightmapGenerationLayer : IGenerationLayer
{
    //should be a factor of Chunk.CHUNK_SIZE
    public const int SAMPLE_INTERVAL = 8;
    public NoiseGroup heightNoise;
    public BlockType topBlock;
    public BlockType midBlock;
    public BlockType underGroundBlock;
    public int heightOffset = 0;
    public int midDepth = 3;
    public int waterLevel = 20;
    
    public bool isSingleThreaded()
    {
        return false;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        //samples heightnoise every SAMPLE_INTERVAL blocks and uses bilinear interpolation to fill in all the values
        Block[,,] blocks = chunk.blocks;
        float[,] heightSamples = new float[Chunk.CHUNK_SIZE / SAMPLE_INTERVAL+1, Chunk.CHUNK_SIZE / SAMPLE_INTERVAL+1]; //adding 1 to get one extra layer of samples right outside the chunk border
        for (int x = 0; x < heightSamples.GetLength(0); x++)
        {
            for (int y = 0; y < heightSamples.GetLength(1); y++)
            {
                //height function, multiplaying by sample interval so that changing that doens't change the overall shape of the terrain
                heightSamples[x, y] = heightOffset + heightNoise.sample(SAMPLE_INTERVAL * x + chunk.worldCoords.x, SAMPLE_INTERVAL * y + chunk.worldCoords.z,0);
            }
        }
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                //get height for this block by interpolating
                int height = (int)WorldGenerator.bilinearInterpolate(new Vector2((float)(x%SAMPLE_INTERVAL) / SAMPLE_INTERVAL, (float)(z%SAMPLE_INTERVAL)/SAMPLE_INTERVAL),
                    heightSamples[x / SAMPLE_INTERVAL, z / SAMPLE_INTERVAL], heightSamples[x / SAMPLE_INTERVAL, z / SAMPLE_INTERVAL+1], heightSamples[x / SAMPLE_INTERVAL+1, z / SAMPLE_INTERVAL+1], heightSamples[x / SAMPLE_INTERVAL+1, z / SAMPLE_INTERVAL]);
                //don't allocate the block array if no blocks will be generated
                if (blocks == null && (chunk.worldCoords.y <= height || chunk.worldCoords.y <= waterLevel))
                {
                    chunk.blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
                    blocks = chunk.blocks;
                }
                if (blocks != null)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        if (height <= waterLevel && y + chunk.worldCoords.y > height && y + chunk.worldCoords.y <= waterLevel)
                        {
                            blocks[x, y, z] = new Block(BlockType.water);
                        }
                        else if (y + chunk.worldCoords.y == height)
                        {
                            blocks[x, y, z] = new Block(topBlock);
                        }
                        else if (y + chunk.worldCoords.y < height && y + chunk.worldCoords.y >= height - midDepth)
                        {
                            blocks[x, y, z] = new Block(midBlock);
                        }
                        else if (y + chunk.worldCoords.y < height)
                        {
                            blocks[x, y, z] = new Block(underGroundBlock);
                        }
                        else
                        {
                            blocks[x, y, z] = new Block(BlockType.empty);
                        }
                    }
                }
            }
        }
        return chunk;
    }
}