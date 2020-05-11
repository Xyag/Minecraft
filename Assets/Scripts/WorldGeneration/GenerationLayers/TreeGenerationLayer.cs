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
        return false;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        if (chunk.blocks == null)
            return chunk;
        Vector2Int treeLocation = new Vector2Int(5, 5);
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
                for (int i = high; i < Chunk.CHUNK_SIZE; i++)
                {
                    //chunk.blocks[treeLocation.x, i, treeLocation.y].type = BlockType.log;
                    world.setBlock(chunk.chunkCoords, new Vector3Int(treeLocation.x, i, treeLocation.y), BlockType.log, false);
                }
            }
        }
        return chunk;
        /*int treeCount = (int)(treeDensityConstant + treeDensity * Mathf.PerlinNoise((float)chunk.chunkCoords.x * 2.355f, (float)chunk.chunkCoords.z * 2.355f));
        if (chunk.worldCoords.y > -33)
        {
            int cacCount = (int)(treeDensityConstant + treeDensity * noise.GetSimplex(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z));
            for (int c = 0; c < cacCount; c++)
            {
                int start;
                bool set = false;
                Vector2Int cactusCoords = new Vector2Int((int)(Chunk.CHUNK_SIZE * (0.5f + 0.5f * noise.GetSimplex(chunk.worldCoords.x, chunk.worldCoords.y, chunk.worldCoords.z + c))), (int)(Chunk.CHUNK_SIZE * (0.5f + 0.5f * noise.GetSimplex(chunk.worldCoords.x + c + 123, chunk.worldCoords.y, chunk.worldCoords.z))));
                for (start = Chunk.CHUNK_SIZE - 1; start > 0; start--)
                {
                    var data = Block.blockTypes[(int)chunk.blocks[cactusCoords.x, start, cactusCoords.y].type];
                    if (data.fullCollision)
                    {
                        set = true;
                        break;
                    }
                }
                if (set)
                {
                    Vector3Int startWorldCoords = chunk.worldCoords + new Vector3Int(cactusCoords.x, start + 1, cactusCoords.y);
                    int height = (int)(Mathf.Lerp(minHeight, maxHeight, 0.5f + 0.5f * noise.GetSimplex(startWorldCoords.x, startWorldCoords.y)));
                    for (int i = startWorldCoords.y; i < startWorldCoords.y + height; i++)
                    {
                        world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z), BlockType.tnt);
                        if (startWorldCoords.y + height - i == 4)
                        {
                            bool zArms = 0.5f + 0.5f * noise.GetSimplex(startWorldCoords.x, 1, startWorldCoords.y) > 0.5f; //50% chance for arms to be orientated on x or z axis
                            if (zArms)
                            {
                                world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z + 1), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z + 2), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i + 1, startWorldCoords.z + 2), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z - 1), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z - 2), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i + 1, startWorldCoords.z - 2), BlockType.tnt);
                            }
                            else
                            {
                                world.setBlock(new Vector3Int(startWorldCoords.x + 1, i, startWorldCoords.z), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x + 2, i, startWorldCoords.z), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x + 2, i + 1, startWorldCoords.z), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x - 1, i, startWorldCoords.z), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x - 2, i, startWorldCoords.z), BlockType.tnt);
                                world.setBlock(new Vector3Int(startWorldCoords.x - 2, i + 1, startWorldCoords.z), BlockType.tnt);
                            }
                        }
                    }
                }
            }
        }

        return chunk;*/
    }
}