using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Diagnostics;

public static class MeshGenerator
{
    public static PhysicMaterial chunkPhysMaterial;
    public static ChunkBuffer finishedMeshes = new ChunkBuffer(3000); //3000 cause that's probably more than we need.
    public static List<Vector3Int> currentlyMeshing = new List<Vector3Int>();
    public static Pool<GameObject> chunkPool;
    private static readonly Stopwatch stopwatch = new Stopwatch();

    public static void spawnFromQueue(long maxTimeMS, int minSpawns)
    {
        lock(finishedMeshes)
        {
            stopwatch.Restart();
            int chunksRemaining = finishedMeshes.Count();
            int spawns = 0;
            if(chunksRemaining != 0)
            while (chunksRemaining > 0 && (spawns < minSpawns || stopwatch.ElapsedMilliseconds < maxTimeMS))
            {
                Chunk data = finishedMeshes.Pop();
                spawnChunk(data);
                chunksRemaining--;
                spawns++;
            }
        }
    }
    public static void spawnAll(IEnumerable<Chunk> collection, World world)
    {
        foreach (var item in collection)
        {
            Task.Run(() => generateAndQueue(world, item));
        }
    }
    public static void spawnAll(Chunk[,,] collection, World world) //multidimensional arrays implement GetIterator() but not GetIterator<T>(). weird
    {
        foreach (var item in collection)
        {
            Task.Run(() => generateAndQueue(world, item));
        }
    }
    public static void meshChunkBlockChanged(Chunk chunk, Vector3Int blockCoords, World world)
    {
        if (chunk == null)
            return;
        Task.Run(() =>
        {
            Vector3Int chunkCoords = chunk.chunkCoords;
            generateAndQueue(world, chunk);
            lock (world.loadedChunks)
            {
                //check surrounding chunks
                if (blockCoords.x == Chunk.CHUNK_SIZE - 1 && world.loadedChunks.TryGetValue(chunkCoords + new Vector3Int(1, 0, 0), out chunk))
                {
                    generateAndQueue(world, chunk);
                }
                if (blockCoords.y == Chunk.CHUNK_SIZE - 1 && world.loadedChunks.TryGetValue(chunkCoords + new Vector3Int(0, 1, 0), out chunk))
                {
                    generateAndQueue(world, chunk);
                }
                if (blockCoords.z == Chunk.CHUNK_SIZE - 1 && world.loadedChunks.TryGetValue(chunkCoords + new Vector3Int(0, 0, 1), out chunk))
                {
                    generateAndQueue(world, chunk);
                }
                if (blockCoords.x == 0 && world.loadedChunks.TryGetValue(chunkCoords + new Vector3Int(-1, 0, 0), out chunk))
                {
                    generateAndQueue(world, chunk);
                }
                if (blockCoords.y == 0 && world.loadedChunks.TryGetValue(chunkCoords + new Vector3Int(0, -1, 0), out chunk))
                {
                    generateAndQueue(world, chunk);
                }
                if (blockCoords.z == 0 && world.loadedChunks.TryGetValue(chunkCoords + new Vector3Int(0, 0, -1), out chunk))
                {
                    generateAndQueue(world, chunk);
                }
            }
        });
    }
    public static async Task remeshAll(IEnumerable<Chunk> collection, World world, int initSize = 1)
    {
        List<Task<Chunk>> meshTasks = new List<Task<Chunk>>();
        foreach (var item in collection)
        {
            meshTasks.Add(Task.Run(() => generateMesh(world, item)));
        }
        while (meshTasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(meshTasks);
            replaceChunkMesh(finishedTask.Result, finishedTask.Result.renderData, world);
            meshTasks.Remove(finishedTask);
        }
    }
    public static void spawnChunk(Chunk chunk)
    {
        if (chunk == null)
            return;
        var data = chunk.renderData;
        var chunkObject = chunk.gameObject;
        if (chunkObject == null)
        {
            chunkObject = chunkPool.get();
            chunk.gameObject = chunkObject;
        }
        if (data == null)
        {
            return;
        }
        chunkObject.name = (data.worldPos / Chunk.CHUNK_SIZE).ToString();
        chunkObject.transform.position = data.worldPos;


        MeshFilter mf = chunkObject.GetComponent<MeshFilter>();
        mf.mesh.Clear();
        mf.mesh.SetVertices(data.vertices);
        mf.mesh.SetTriangles(data.triangles, 0);
        mf.mesh.SetNormals(data.normals);
        mf.mesh.SetUVs(0, data.uvs);
    }
    public static void replaceChunkMesh(Chunk chunk, MeshData data, World world)
    {
        if (chunk != null && chunk.gameObject != null)
        {
            MeshFilter mf = chunk.gameObject.GetComponent<MeshFilter>();
            
            mf.mesh.Clear();
            if (data != null)
            {
                mf.mesh.SetVertices(data.vertices);
                mf.mesh.SetTriangles(data.triangles, 0);
                mf.mesh.SetNormals(data.normals);
                mf.mesh.SetUVs(0, data.uvs);
            }
        }
    }
    public static void remeshChunk(World world, Chunk chunk, bool alertNeighbors = true)
    {
        if (chunk == null)
            return;
        if (chunk.blocks == null)
        {
            return;
        }
        if (chunk.gameObject == null)
        {
            spawnChunk(generateMesh(world, chunk));
        }
        else
        {
            MeshFilter mf = chunk.gameObject.GetComponent<MeshFilter>();
            MeshData data = generateMesh(world, chunk).renderData;
            mf.mesh.Clear();
            mf.mesh.SetVertices(data.vertices);
            mf.mesh.SetTriangles(data.triangles, 0);
            mf.mesh.SetNormals(data.normals);
            mf.mesh.SetUVs(0, data.uvs);

        }
        if (alertNeighbors)
        {
            var neighbors = world.getNeighboringChunks(chunk.chunkCoords);
            foreach (var c in neighbors)
            {
                remeshChunk(world, c, false);
            }
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void posXFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x + 0.5f, blockPos.y + size.x + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x + 0.5f, blockPos.y + size.x + 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x + 0.5f, blockPos.y - 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x + 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));

        normals.Add(new Vector3(1, 0, 0));
        normals.Add(new Vector3(1, 0, 0));
        normals.Add(new Vector3(1, 0, 0));
        normals.Add(new Vector3(1, 0, 0));

        setUpTrisUVs(faceIndex, triangles, uvs, Block.blockTypes[(int)block].texture.PosX, new Vector2(size.y, size.x));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void negXFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + size.x + 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + size.x + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z + size.y + 0.5f));

        normals.Add(new Vector3(-1, 0, 0));
        normals.Add(new Vector3(-1, 0, 0));
        normals.Add(new Vector3(-1, 0, 0));
        normals.Add(new Vector3(-1, 0, 0));

        setUpTrisUVs(faceIndex, triangles, uvs, Block.blockTypes[(int)block].texture.NegX, new Vector2(size.y, size.x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void posYFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y + 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + 0.5f, blockPos.z + size.y + 0.5f));

        normals.Add(new Vector3(0, 1, 0));
        normals.Add(new Vector3(0, 1, 0));
        normals.Add(new Vector3(0, 1, 0));
        normals.Add(new Vector3(0, 1, 0));

        setUpTrisUVs(faceIndex, triangles, uvs, Block.blockTypes[(int)block].texture.PosY, new Vector2(size.y, size.x));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void negYFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y - 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));

        normals.Add(new Vector3(0, -1, 0));
        normals.Add(new Vector3(0, -1, 0));
        normals.Add(new Vector3(0, -1, 0));
        normals.Add(new Vector3(0, -1, 0));

        setUpTrisUVs(faceIndex, triangles, uvs, Block.blockTypes[(int)block].texture.NegY, new Vector2(size.y, size.x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void posZFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y + size.y + 0.5f, blockPos.z + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + size.y + 0.5f, blockPos.z + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z + 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y - 0.5f, blockPos.z + 0.5f));

        normals.Add(new Vector3(0, 0, 1));
        normals.Add(new Vector3(0, 0, 1));
        normals.Add(new Vector3(0, 0, 1));
        normals.Add(new Vector3(0, 0, 1));

        setUpTrisUVs(faceIndex, triangles, uvs, Block.blockTypes[(int)block].texture.PosZ, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void negZFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + size.y + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y + size.y + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));

        normals.Add(new Vector3(0, 0, -1));
        normals.Add(new Vector3(0, 0, -1));
        normals.Add(new Vector3(0, 0, -1));
        normals.Add(new Vector3(0, 0, -1));

        setUpTrisUVs(faceIndex, triangles, uvs, Block.blockTypes[(int)block].texture.NegZ, size);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void setUpTrisUVs(int faceIndex, List<int> triangles, List<Vector3> uvs, int texId, Vector2 size)
    {
        int vertexStart = faceIndex * 4;
        triangles.Add(vertexStart);
        triangles.Add(vertexStart + 1);
        triangles.Add(vertexStart + 2);
        triangles.Add(vertexStart + 2);
        triangles.Add(vertexStart + 3);
        triangles.Add(vertexStart);



        uvs.Add(new Vector3(0, size.y + 1, texId));
        uvs.Add(new Vector3(size.x + 1, size.y + 1, texId));
        uvs.Add(new Vector3(size.x + 1, 0, texId));
        uvs.Add(new Vector3(0, 0, texId));
    }

    public static void generateAndQueue(World world, Chunk chunk)
    {
        if (chunk == null)
            return;
        lock (currentlyMeshing)
        {
            if (currentlyMeshing.Contains(chunk.chunkCoords))
            {
                return;
            }
            currentlyMeshing.Add(chunk.chunkCoords);
        }
        generateMesh(world, chunk);
        if (chunk.renderData != null)
        {
            if (!finishedMeshes.Replace(chunk))
            {
                finishedMeshes.Push(chunk);
            }
        }
        lock (currentlyMeshing)
        {
            currentlyMeshing.Remove(chunk.chunkCoords);
        }
    }
    public static Chunk generateMesh(World world, Chunk chunk)
    {
        if (chunk == null || chunk.blocks == null)
        {
            return chunk;
        }
        void set2dFalse(bool[,,] array)
        {
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    array[0, x, y] = false;
                }
            }
        }
        MeshData renderData = chunk.renderData;
        List<Vector3> vertices;
        List<int> triangles;
        List<Vector3> normals;
        List<Vector3> uvs;
        bool[,,] meshed = new bool[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE]; //default value is false
        if (renderData == null) //means that the chunk hasn't been meshed before, we need to allocate for the mesh data.
        {
            chunk.renderData = new MeshData();
            renderData = chunk.renderData;
            renderData.vertices = new List<Vector3>();
            renderData.triangles = new List<int>();
            renderData.normals = new List<Vector3>();
            renderData.uvs = new List<Vector3>();
        }
        else
        {
            renderData.vertices.Clear();
            renderData.triangles.Clear();
            renderData.normals.Clear();
            renderData.uvs.Clear();
        }
        vertices = renderData.vertices;
        triangles = renderData.triangles;
        normals = renderData.normals;
        uvs = renderData.uvs;
        int faceIndex = 0;
        //we take by layers. first we do +x, then -x, then y's, then z's
        //we're also doing greedy meshing: basically we find the biggest rectangle that can fit on the block we loop on and go with that
        //good animation here https://www.gedge.ca/dev/2014/08/17/greedy-voxel-meshing

        //+x faces
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    BlockType type = chunk.blocks[x, y, z].type;

                    if (x == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x + 1, chunk.chunkCoords.y, chunk.chunkCoords.z), new Vector3Int(0, y, z)).opaque)
                        {
                            meshed[0, y, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)chunk.blocks[x + 1, y, z].type].opaque) { meshed[0, y, z] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, y, z])
                    {
                        //first we expand in y-direction
                        meshed[0, y, z] = true;
                        int yExtent = 1;
                        while (y + yExtent < Chunk.CHUNK_SIZE && !meshed[0, y + yExtent, z] && chunk.blocks[x, y + yExtent, z].type == type)
                        {
                            meshed[0, y + yExtent, z] = true;
                            yExtent++;
                        }
                        yExtent--; //we will always overcount by 1.
                                   //now we expand in z-direction
                        int zExtent = 1; //we already checked the initial column
                        while (z + zExtent < Chunk.CHUNK_SIZE)
                        {
                            //we have to check all the blocks on the edge of the rectangle now
                            for (int i = y; i <= y + yExtent; i++)
                            {
                                if (chunk.blocks[x, i, z + zExtent].type != type || meshed[0, i, z + zExtent])
                                {
                                    goto endLoop;
                                }
                            }
                            for (int i = y; i <= y + yExtent; i++)
                            {
                                meshed[0, i, z + zExtent] = true;
                            }
                            zExtent++;
                        }
                    endLoop:
                        zExtent--; //we always overcount by 1.
                        posXFace(faceIndex, new Vector3(x, y, z), new Vector2(yExtent, zExtent), vertices, triangles, normals, uvs, type);
                        faceIndex++;
                    }
                }
            }
            set2dFalse(meshed);
        }

        //-x faces
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    BlockType type = chunk.blocks[x, y, z].type;

                    if (x == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x - 1, chunk.chunkCoords.y, chunk.chunkCoords.z), new Vector3Int(Chunk.CHUNK_SIZE - 1, y, z)).opaque)
                        {
                            meshed[0, y, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)chunk.blocks[x - 1, y, z].type].opaque) { meshed[0, y, z] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, y, z])
                    {
                        //first we expand in y-direction
                        meshed[0, y, z] = true;
                        int yExtent = 1;
                        while (y + yExtent < Chunk.CHUNK_SIZE && !meshed[0, y + yExtent, z] && chunk.blocks[x, y + yExtent, z].type == type)
                        {
                            meshed[0, y + yExtent, z] = true;
                            yExtent++;
                        }
                        yExtent--; //we will always overcount by 1.
                                   //now we expand in z-direction
                        int zExtent = 1; //we already checked the initial column
                        while (z + zExtent < Chunk.CHUNK_SIZE)
                        {
                            //we have to check all the blocks on the edge of the rectangle now
                            for (int i = y; i <= y + yExtent; i++)
                            {
                                if (chunk.blocks[x, i, z + zExtent].type != type || meshed[0, i, z + zExtent])
                                {
                                    goto endLoop;
                                }
                            }
                            for (int i = y; i <= y + yExtent; i++)
                            {
                                meshed[0, i, z + zExtent] = true;
                            }
                            zExtent++;
                        }
                    endLoop:
                        zExtent--; //we always overcount by 1.
                        negXFace(faceIndex, new Vector3(x, y, z), new Vector2(yExtent, zExtent), vertices, triangles, normals, uvs, type);
                        faceIndex++;
                    }
                }
            }
            set2dFalse(meshed);
        }

        //+y faces
        for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
        {
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    BlockType type = chunk.blocks[x, y, z].type;

                    if (y == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y + 1, chunk.chunkCoords.z), new Vector3Int(x, 0, z)).opaque)
                        {
                            meshed[0, x, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)chunk.blocks[x, y + 1, z].type].opaque) { meshed[0, x, z] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, x, z])
                    {
                        //first we expand in x-direction
                        meshed[0, x, z] = true;
                        int xExtent = 1;
                        while (x + xExtent < Chunk.CHUNK_SIZE && !meshed[0, x + xExtent, z] && chunk.blocks[x + xExtent, y, z].type == type)
                        {
                            meshed[0, x + xExtent, z] = true;
                            xExtent++;
                        }
                        xExtent--; //we will always overcount by 1.
                                   //now we expand in z-direction
                        int zExtent = 1; //we already checked the initial column
                        while (z + zExtent < Chunk.CHUNK_SIZE)
                        {
                            //we have to check all the blocks on the edge of the rectangle now
                            for (int i = x; i <= x + xExtent; i++)
                            {
                                if (chunk.blocks[i, y, z + zExtent].type != type || meshed[0, i, z + zExtent])
                                {
                                    goto endLoop;
                                }
                            }
                            for (int i = x; i <= x + xExtent; i++)
                            {
                                meshed[0, i, z + zExtent] = true;
                            }
                            zExtent++;
                        }
                    endLoop:
                        zExtent--; //we always overcount by 1.
                        posYFace(faceIndex, new Vector3(x, y, z), new Vector2(xExtent, zExtent), vertices, triangles, normals, uvs, type);
                        faceIndex++;
                    }
                }
            }
            set2dFalse(meshed);
        }

        //-y faces
        for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
        {
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    BlockType type = chunk.blocks[x, y, z].type;

                    if (y == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y - 1, chunk.chunkCoords.z), new Vector3Int(x, Chunk.CHUNK_SIZE - 1, z)).opaque)
                        {
                            meshed[0, x, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)chunk.blocks[x, y - 1, z].type].opaque) { meshed[0, x, z] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, x, z])
                    {
                        //first we expand in x-direction
                        meshed[0, x, z] = true;
                        int xExtent = 1;
                        while (x + xExtent < Chunk.CHUNK_SIZE && !meshed[0, x + xExtent, z] && chunk.blocks[x + xExtent, y, z].type == type)
                        {
                            meshed[0, x + xExtent, z] = true;
                            xExtent++;
                        }
                        xExtent--; //we will always overcount by 1.
                                   //now we expand in z-direction
                        int zExtent = 1; //we already checked the initial column
                        while (z + zExtent < Chunk.CHUNK_SIZE)
                        {
                            //we have to check all the blocks on the edge of the rectangle now
                            for (int i = x; i <= x + xExtent; i++)
                            {
                                if (chunk.blocks[i, y, z + zExtent].type != type || meshed[0, i, z + zExtent])
                                {
                                    goto endLoop;
                                }
                            }
                            for (int i = x; i <= x + xExtent; i++)
                            {
                                meshed[0, i, z + zExtent] = true;
                            }
                            zExtent++;
                        }
                    endLoop:
                        zExtent--; //we always overcount by 1.
                        negYFace(faceIndex, new Vector3(x, y, z), new Vector2(xExtent, zExtent), vertices, triangles, normals, uvs, type);
                        faceIndex++;
                    }
                }
            }
            set2dFalse(meshed);
        }

        //+z faces
        for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                {
                    BlockType type = chunk.blocks[x, y, z].type;

                    if (z == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z + 1), new Vector3Int(x, y, 0)).opaque)
                        {
                            meshed[0, x, y] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)chunk.blocks[x, y, z + 1].type].opaque) { meshed[0, x, y] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, x, y])
                    {
                        //first we expand in x-direction
                        meshed[0, x, y] = true;
                        int xExtent = 1;
                        while (x + xExtent < Chunk.CHUNK_SIZE && !meshed[0, x + xExtent, y] && chunk.blocks[x + xExtent, y, z].type == type)
                        {
                            meshed[0, x + xExtent, y] = true;
                            xExtent++;
                        }
                        xExtent--; //we will always overcount by 1.
                                   //now we expand in z-direction
                        int yExtent = 1; //we already checked the initial column
                        while (y + yExtent < Chunk.CHUNK_SIZE)
                        {
                            //we have to check all the blocks on the edge of the rectangle now
                            for (int i = x; i <= x + xExtent; i++)
                            {
                                if (chunk.blocks[i, y + yExtent, z].type != type || meshed[0, i, y + yExtent])
                                {
                                    goto endLoop;
                                }
                            }
                            for (int i = x; i <= x + xExtent; i++)
                            {
                                meshed[0, i, y + yExtent] = true;
                            }
                            yExtent++;
                        }
                    endLoop:
                        yExtent--; //we always overcount by 1.
                        posZFace(faceIndex, new Vector3(x, y, z), new Vector2(xExtent, yExtent), vertices, triangles, normals, uvs, type);
                        faceIndex++;
                    }
                }
            }
            set2dFalse(meshed);
        }

        //-z faces
        for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
        {
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    BlockType type = chunk.blocks[x, y, z].type;

                    if (z == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z - 1), new Vector3Int(x, y, Chunk.CHUNK_SIZE - 1)).opaque)
                        {
                            meshed[0, x, y] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)chunk.blocks[x, y, z - 1].type].opaque) { meshed[0, x, y] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, x, y])
                    {
                        //first we expand in x-direction
                        meshed[0, x, y] = true;
                        int xExtent = 1;
                        while (x + xExtent < Chunk.CHUNK_SIZE && !meshed[0, x + xExtent, y] && chunk.blocks[x + xExtent, y, z].type == type)
                        {
                            meshed[0, x + xExtent, y] = true;
                            xExtent++;
                        }
                        xExtent--; //we will always overcount by 1.
                                   //now we expand in y-direction
                        int yExtent = 1; //we already checked the initial column
                        while (y + yExtent < Chunk.CHUNK_SIZE)
                        {
                            //we have to check all the blocks on the edge of the rectangle now
                            for (int i = x; i <= x + xExtent; i++)
                            {
                                if (chunk.blocks[i, y + yExtent, z].type != type || meshed[0, i, y + yExtent])
                                {
                                    goto endLoop; //I think this is the one time in my life that a goto is the best solution
                                }
                            }
                            for (int i = x; i <= x + xExtent; i++)
                            {
                                meshed[0, i, y + yExtent] = true;
                            }
                            yExtent++;
                        }
                    endLoop:
                        yExtent--; //we always overcount by 1.
                        negZFace(faceIndex, new Vector3(x, y, z), new Vector2(xExtent, yExtent), vertices, triangles, normals, uvs, type);
                        faceIndex++;
                    }
                }
            }
            set2dFalse(meshed);
        }

        MeshData meshData = new MeshData
        {
            worldPos = chunk.worldCoords,
            vertices = vertices,
            triangles = triangles,
            normals = normals,
            uvs = uvs,
        };
        chunk.renderData = meshData;
        return chunk;
    }
    
}
