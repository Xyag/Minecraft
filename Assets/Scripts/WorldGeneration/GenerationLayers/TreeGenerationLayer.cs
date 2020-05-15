using System.Collections.Generic;
using UnityEngine;

public class TreeGenerationLayer : IGenerationLayer
{
    public int maxTreeHeight = 10;
    public int minTreeHeight = 3;
    public float leavesFraction = 0.25f; //fraction of the tree from the top that should be covered in leaves

    public float treeDensity = 4;
    public float treeDensityConstant = 0;
    private FastNoise noise = new FastNoise();
    public bool isSingleThreaded()
    {
        return true;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        try
        {
            if (chunk.blocks == null)
                return chunk;
            int treeCount = (int)(treeDensity * Mathf.PerlinNoise(2.1f * chunk.worldCoords.x, 2.1f * chunk.worldCoords.z) + treeDensityConstant);
            for (int i = 0; i < treeCount; i++)
            {
                generateTree(chunk, world, i);
            }
            return chunk;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
            return null;
        }
    }
    private void generateTree(Chunk chunk, World world, int iteration)
    {
        int treeCoord = (int)(Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Mathf.PerlinNoise(5.7f*iteration*chunk.worldCoords.x, 5.7f*iteration*chunk.worldCoords.z));
        Vector2Int treeLocation = new Vector2Int(treeCoord % Chunk.CHUNK_SIZE, treeCoord / Chunk.CHUNK_SIZE);
        bool set = false;
        if (chunk.worldCoords.y > -33)
        {
            int high;
            bool prevEmpty = true;
            for (high = Chunk.CHUNK_SIZE - 1; high >= 0; high--)
            {
                var data = Block.blockTypes[(int)chunk.blocks[treeLocation.x, high, treeLocation.y].type];
                if (data.opaque && prevEmpty)
                {
                    set = true;
                    break;
                }
                prevEmpty = data.type != BlockType.empty;
            }
            if (set)
            {
                int treeHeight = Mathf.RoundToInt(WorldGenerator.linearInterpolate(minTreeHeight, maxTreeHeight, Mathf.PerlinNoise(1.11f * iteration * chunk.worldCoords.x, 1.11f * iteration * chunk.worldCoords.z)));
                int leavesDepth = (int)(treeHeight * leavesFraction);
                //leaves/trunk generation.
                //makes a pyramid of leaves at the top of the tree. the topmost block of the trunk is a leaf
                for (int i = 0; i < treeHeight - leavesDepth; i++)
                {
                    //lower trunk
                    Vector3Int blockPos = new Vector3Int(treeLocation.x + chunk.worldCoords.x, chunk.worldCoords.y + high + i, treeLocation.y + chunk.worldCoords.z);
                    world.setBlock(blockPos, BlockType.log, forceLoadChunk: false);
                }
                for (int i = treeHeight - leavesDepth; i < treeHeight; i++)
                {
                    //uppter trunk w/ leaves
                    for (int x = -treeHeight + i; x <= treeHeight - i; x++)
                    {
                        for (int z = -treeHeight + i; z <= treeHeight - i; z++)
                        {
                            Vector3Int blockPos = new Vector3Int(treeLocation.x + x + chunk.worldCoords.x, chunk.worldCoords.y + high + i, treeLocation.y + z + chunk.worldCoords.z);
                            if (x == 0 && z == 0 && i != treeHeight - 1)
                            {
                                //center log
                                world.setBlock(blockPos, BlockType.log, forceLoadChunk: false);
                            }
                            else
                            {
                                world.setBlock(blockPos, BlockType.leaves, forceLoadChunk: false);
                            }
                        }
                    }
                    
                }
            }
        }
    }
}