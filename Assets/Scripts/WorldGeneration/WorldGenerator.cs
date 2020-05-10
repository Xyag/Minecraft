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
            dontReplace = BlockType.empty,
            replaceWith = BlockType.empty
        };
        var ironGenerator = new LerpBlockGenerationLayer
        {
            maxHeight = 70,
            minHeight = 20,
            tolerance = 1.2f,
            noise = new NoiseGroup(2, 0.1f, 3, 1, 0.15f, seed + 12), //I add random numbers to the seed to make sure that the generators are independent
            dontReplace = BlockType.empty,
            replaceWith = BlockType.iron_ore
        };
        var holeyHillsGenerator = new HeightmapGenerationLayer
        {
            heightNoise = new NoiseGroup(2, 0.01f, 5.0f, 16, 0.2f, seed + 55),
            topBlock = BlockType.grass,
            midBlock = BlockType.dirt,
            underGroundBlock = BlockType.stone,
            midDepth = 3,
            waterLevel = -100,
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
            waterLevel = 10,
        };
        var plainsTrees = new TreeGenerationLayer
        {
            treeDensity = 2,
            minHeight = 5,
            maxHeight = 12
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

        //generationLayers.Add(new PenishPicker());

        generationLayers.Add(holeyHillsGenerator);
        //generationLayers.Add(cactusGenerator);
        //generationLayers.Add(plainsTrees);
        //generationLayers.Add(ironGenerator);
        //generationLayers.Add(caveGenerator);
    }
    public static async Task<List<Chunk>> generateList(World world, List<Vector3Int> dests)
    {
        List<Task<Chunk>> genTasks = new List<Task<Chunk>>(dests.Count);
        List<Chunk> chunks = new List<Chunk>(dests.Count);
        List<Chunk> finishedChunks = new List<Chunk>(dests.Count);
        for (int i = 0; i < dests.Count; i++)
        {
            Chunk c = world.getChunk(dests[i]);
            if (c == null)
            {
                c = new Chunk(null, dests[i]);
                chunks.Add(c);
            }
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
                //process the chunks as the come in from the multiple threads.
                while (genTasks.Count > 0)
                {
                    var finishedTask = await Task.WhenAny(genTasks);
                    genTasks.Remove(finishedTask);
                    finishedChunks.Add(finishedTask.Result);
                }
            }
            
        }
        return finishedChunks;
    }
}