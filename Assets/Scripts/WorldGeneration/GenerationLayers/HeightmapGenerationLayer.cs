using System.Collections.Generic;
using UnityEngine;

public class HeightmapGenerationLayer : IGenerationLayer
{
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
        Block[,,] blocks = chunk.blocks;
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                int height = heightOffset + (int)heightNoise.sample(x + chunk.worldCoords.x, z + chunk.worldCoords.z, 0);
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