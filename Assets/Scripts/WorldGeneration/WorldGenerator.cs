using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;

public static class WorldGenerator
{
    public static int seed = 42000;
    public static bool currentlyLoading = false;
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
        };
        var plainsTrees = new TreeGenerationLayer
        {
            treeDensity = 2,
            minHeight = 5,
            maxHeight = 12
        };

        generationLayers.Add(desertGenerator);
        //generationLayers.Add(plainsTrees);
        generationLayers.Add(ironGenerator);
        generationLayers.Add(caveGenerator);
    }
    public static async Task generateRegion(World world, Vector3Int startChunk, int xSize, int ySize, int zSize)
    {
        currentlyLoading = true;
        //generates the block data asynchronosly using multiThreadPasses, then synchronously using singleThreadPasses, then loads the chunks synchronously, then generates the meshes asyncronously.
        List<Task<Chunk>> genTasks = new List<Task<Chunk>>(xSize * ySize * zSize);
        Chunk[,,] chunks = new Chunk[xSize, ySize, zSize];

        //initialize chunks
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    chunks[x, y, z] = world.getChunk(new Vector3Int(x + startChunk.x, y + startChunk.y, z + startChunk.z));
                }
            }
        }
        //generating the world layer by layer
        for (int i = 0; i < generationLayers.Count; i++)
        {
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        if (generationLayers[i].isSingleThreaded())
                        {
                            generationLayers[i].generateChunk(chunks[x, y, z], world);
                        }
                        else
                        {
                            Chunk chunk = chunks[x, y, z];
                            genTasks.Add(Task.Run(() => generationLayers[i].generateChunk(chunk, world)));
                        }
                    }
                }
            }
            if (!generationLayers[i].isSingleThreaded())
            {
                //process the chunks as the come in from the multiple threads.
                while (genTasks.Count > 0)
                {
                    var finishedTask = await Task.WhenAny(genTasks);
                    genTasks.Remove(finishedTask);
                    Vector3Int listCoords = finishedTask.Result.chunkCoords - startChunk;
                    chunks[listCoords.x, listCoords.y, listCoords.z] = finishedTask.Result;
                }
            }
        }
        //we send all the finished chunks to the world to be loaded
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    world.loadChunk(chunks[x, y, z]);
                }
            }
        }
        currentlyLoading = false;
    }
    public static async Task<List<Chunk>> generateList(World world, List<Vector3Int> dests)
    {
        currentlyLoading = true;
        float timestamp = Time.realtimeSinceStartup;
        List<Task<(int, Chunk)>> genTasks = new List<Task<(int, Chunk)>>(dests.Count);
        List<Chunk> chunks = new List<Chunk>(dests.Count);
        for (int i = 0; i < dests.Count; i++)
        {
            chunks.Add(world.getChunk(dests[i]));
        }
        for (int i = 0; i < generationLayers.Count; i++)
        {

            if (generationLayers[i].isSingleThreaded())
            {
                for (int n = 0; n < chunks.Count; n++)
                {
                    generationLayers[i].generateChunk(chunks[n], world);
                }
            }
            else
            {
                for (int n = 0; n < chunks.Count; n++)
                {
                    Chunk chunk = chunks[n];
                    int index = n;
                    genTasks.Add(Task.Run(() => (index, generationLayers[i].generateChunk(chunk, world))));
                }
            }
            if (!generationLayers[i].isSingleThreaded())
            {
                //process the chunks as the come in from the multiple threads.
                while (genTasks.Count > 0)
                {
                    var finishedTask = await Task.WhenAny(genTasks);
                    genTasks.Remove(finishedTask);
                    chunks[finishedTask.Result.Item1] = finishedTask.Result.Item2;
                }
            }
        }
        foreach (var chunk in chunks)
        {
            world.loadChunk(chunk);
        }
        currentlyLoading = false;
        return chunks;
    }
    public static async Task generateAndMeshList(World world, List<Vector3Int> dests)
    {
        currentlyLoading = true;
        var chunks = await generateList(world, dests);

        await MeshGenerator.spawnAll(chunks, world, chunks.Count);
        currentlyLoading = false;
    }
}