using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;

public static class WorldGenerator
{
    public static int seed = 42000;
    static List<IGenerationLayer> generationLayers = new List<IGenerationLayer>();
    static NoiseGroup caveNoiseGenerator;
    static NoiseGroup ironNoise;
    static WorldGenerator()
    {
        var caveGenerator = new LerpBlockGenerationLayer
        {
            maxHeight = 20,
            minHeight = -20,
            tolerance = 0.5f,
            noise = new NoiseGroup(2, 0.05f, 2.0f, 2, 0.5f, seed),
            dontReplace = BlockType.water,
            replaceWith = BlockType.empty
        };
        var ironGenerator = new LerpBlockGenerationLayer
        {
            maxHeight = 70,
            minHeight = 20,
            tolerance = 1.2f,
            noise = new NoiseGroup(2, 0.1f, 3, 1, 0.15f, seed + 12), //I add random numbers to the seed to make sure that the generators are independent
            dontReplace = BlockType.empty,
            replaceWith = BlockType.ironOre
        };
        var holeyHillsGenerator = new HeightmapGenerationLayer
        {
            heightNoise = new NoiseGroup(2, 0.01f, 5.0f, 16, 0.2f, seed + 55),
            topBlock = BlockType.grass,
            midBlock = BlockType.dirt,
            underGroundBlock = BlockType.stone,
            midDepth = 3,
            seaLevel = -100,
        };

        var plainsGenerator = new HeightmapGenerationLayer
        {
            heightNoise = new NoiseGroup(2, 0.0025f, 8.0f, 8, 0.3f, seed - 1230),
            heightOffset = 16,
            topBlock = BlockType.grass,
            midBlock = BlockType.dirt,
            underGroundBlock = BlockType.stone,
            midDepth = 3,
        };

        var desertGenerator = new HeightmapGenerationLayer
        {
            heightNoise = new NoiseGroup(2, 0.005f, 4.0f, 16, 0.8f, seed - 1230),
            heightOffset = 16,
            topBlock = BlockType.sand,
            midBlock = BlockType.sand,
            underGroundBlock = BlockType.stone,
            midDepth = 10,
            seaLevel = 10,
        };
        var plainsTrees = new TreeGenerationLayer
        {
            treeDensity = 2,
            minTreeHeight = 5,
            maxTreeHeight = 12
        };
        var mountainsGenerator = new MountainGenerationLayer
        {
            heightNoise = new NoiseGroup(3, 0.0002f, 5.0f, 1024, 0.25f, seed - 1230),
            snowHeightNoise = new NoiseGroup(1, 0.3f, 0, 2, 0, seed - 17),
            snowLevelNoise = new NoiseGroup(2, 0.01f, 5, 25, 0.2f, seed - 8888),
            heightOffset = 16,
            snowBlock = BlockType.snow,
            surfaceBlock = BlockType.dirt,
            underGroundBlock = BlockType.stone,
            surfaceDepth = 2,
            snowHeight = 100,
        };
        var cactusGenerator = new CactusGenerationLayer
        {
            cactusDensity = 1,
            cactusDensityConstant = 1,
        };
        var crazyGenerator = new HeightmapGenerationLayer
        {
            heightNoise = new NoiseGroup(3, 0.005f, 5.0f, 64, 0.6f),
            heightOffset = 16,
            topBlock = BlockType.grass,
            midBlock = BlockType.dirt,
            underGroundBlock = BlockType.stone,
            midDepth = 10,
            seaLevel = 10,
        };
        var crazyTrees = new TreeGenerationLayer
        {
            maxTreeHeight = 30,
            minTreeHeight = 5,
            treeDensity = 2,
            treeDensityConstant = 1
        };



        //generationLayers.Add(new PenishPicker());

        generationLayers.Add(crazyGenerator);
        generationLayers.Add(crazyTrees);
        //generationLayers.Add(cactusGenerator);
        //generationLayers.Add(plainsTrees);
        //generationLayers.Add(ironGenerator);
        //generationLayers.Add(caveGenerator);
    }
    public static async Task generateList(World world, List<Chunk> chunks)
    {
        List<Task<Chunk>> genTasks = new List<Task<Chunk>>(chunks.Count);
        foreach (var chunk in chunks)
        {
            chunk.valid = false;
        }
        for (int i = 0; i < generationLayers.Count; i++)
        {
            if (generationLayers[i].isSingleThreaded())
            {
                foreach (var chunk in chunks)
                {
                    generationLayers[i].generateChunk(chunk, world);
                }
            }
            else
            {
                genTasks.Clear();
                foreach (var chunk in chunks)
                {
                    genTasks.Add(Task.Run(() => generationLayers[i].generateChunk(chunk, world)));
                }
                //wait for all chunks to be generated.
                await Task.WhenAll(genTasks);
            }
        }
        foreach (var chunk in chunks)
        {
            chunk.valid = true;
        }
    }
    //returns the value of f(x) where f is a line that has points f(0) = val1 and f(1) = val2.
    public static float linearInterpolate(float val1, float val2, float x)
    {
        return val1 * (1 - x) + val2 * x;
    }
    //interoplates pos in the unit square where the corners have the values provided by the arguments
    public static float bilinearInterpolate(Vector2 pos, float botLeft, float topLeft, float topRight, float botRight)
    {
        return botLeft * (1 - pos.x) * (1 - pos.y) + botRight * pos.x * (1 - pos.y) + topLeft * (1 - pos.x) * pos.y + topRight * pos.x * pos.y;
    }
    //unit cube. p<letter> indicates that you have moved on that axis. ex: px is (1,0,0), pyz is (0,1,1), etc.
    public static float trilinearInterpolate(Vector3 pos, float origin, float px, float py, float pz, float pxy, float pyz, float pxz, float pxyz)
    {
        return origin * (1 - pos.x) * (1 - pos.y) * (1 - pos.z)
            + px * pos.x * (1 - pos.y) * (1 - pos.z)
            + py * (1 - pos.x) * pos.y * (1 - pos.z)
            + pz * (1 - pos.x) * (1 - pos.y) * pos.z
            + pxy * pos.x * pos.y * (1 - pos.z)
            + pxz * pos.x * (1 - pos.y) * pos.z
            + pyz * (1 - pos.x) * pos.y * pos.z
            + pxyz * pos.x * pos.y * pos.z;
    }
}