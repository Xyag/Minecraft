using System.Collections.Generic;
using UnityEngine;



public class MountainGenerationLayer : IGenerationLayer
{
    public NoiseGroup heightNoise;
    public NoiseGroup snowHeightNoise;
    public NoiseGroup snowLevelNoise;
    public BlockType snowBlock;
    public BlockType surfaceBlock;
    public BlockType underGroundBlock;
    public int heightOffset = 0;
    public int surfaceDepth = 3;
    public int snowHeight;
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
                Vector3Int worldCoords = new Vector3Int(x + chunk.worldCoords.x, 0, z + chunk.worldCoords.z);
                int localSnowHeight = snowHeight + (int)snowLevelNoise.sample(worldCoords.x, worldCoords.z, 0);
                int snowDepth = 1 + (int)snowHeightNoise.sample(worldCoords.x, 0, worldCoords.z);
                int height = heightOffset + (int)heightNoise.sample(worldCoords.x, worldCoords.z, 0);
                if (height + snowDepth >= localSnowHeight)
                    height += snowDepth;
                if (blocks == null && chunk.worldCoords.y <= height)
                {
                    chunk.blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
                    blocks = chunk.blocks;
                }

                if (blocks != null)
                {

                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        if (y + chunk.worldCoords.y > height)
                        {
                            blocks[x, y, z] = new Block(BlockType.empty);
                        }
                        else if (y + chunk.worldCoords.y + snowDepth >= localSnowHeight && y + chunk.worldCoords.y >= height - snowDepth)
                        {
                            blocks[x, y, z] = new Block(snowBlock);
                        }
                        else if (y + chunk.worldCoords.y < height && y + chunk.worldCoords.y >= height - surfaceDepth)
                        {
                            blocks[x, y, z] = new Block(surfaceBlock);
                        }
                        else if (y + chunk.worldCoords.y < height)
                        {
                            blocks[x, y, z] = new Block(underGroundBlock);
                        }
                    }
                }
            }
        }
        return chunk;
    }
}
