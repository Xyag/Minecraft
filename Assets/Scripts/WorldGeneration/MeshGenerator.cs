using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Diagnostics;

public static class MeshGenerator
{
    public static ConcurrentQueue<Chunk> finishedMeshes = new ConcurrentQueue<Chunk>();
    private static ConcurrentQueue<Chunk> frameBuffer = new ConcurrentQueue<Chunk>();
    public static ConcurrentQueue<Chunk> remeshQueue = new ConcurrentQueue<Chunk>();
    public static Pool<GameObject> chunkPool;
    private static readonly Stopwatch stopwatch = new Stopwatch();

    public static void emptyFrameBuffer(World world)
    {
        spawnAll(frameBuffer, world);
    }
    public static void spawnFromQueue(long maxTimeMS, int minSpawns)
    {
        stopwatch.Restart();
        int chunksRemaining = finishedMeshes.Count;
        int spawns = 0;
        while (chunksRemaining > 0 && (spawns < minSpawns || stopwatch.ElapsedMilliseconds < maxTimeMS))
        {
            if (finishedMeshes.TryDequeue(out Chunk data))
            {
                spawnChunk(data);
                chunksRemaining--;
                spawns++;
            }
        }
    }
    public static void generateAndQueue(World world, Chunk chunk)
    {
        if (chunk == null)
            return;
        generateMesh(world, chunk);
        finishedMeshes.Enqueue(chunk);
    }
    public static void spawnAll(IEnumerable<Chunk> collection, World world)
    {
        foreach (var item in collection)
        {
            Task.Run(() => generateAndQueue(world, item));
        }
    }
    public static void spawnChunk(Chunk chunk)
    {
        if (chunk == null || chunk.renderData == null)
            return;
        var data = chunk.renderData;
        var chunkObject = chunk.gameObject;
        if (chunkObject == null)
        {
            chunkObject = chunkPool.get();
            chunk.gameObject = chunkObject;
        }
        chunkObject.name = (data.worldPos / Chunk.CHUNK_SIZE).ToString();
        chunkObject.transform.position = data.worldPos;

        replaceChunkMesh(chunk, data);
    }
    public static void replaceChunkMesh(Chunk chunk, MeshData data)
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
        chunk.changed = false;
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
        BlockType[,,] blocks = new BlockType[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    blocks[x, y, z] = chunk.blocks[x, y, z].type;
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
                    BlockType type = blocks[x, y, z];

                    if (x == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x + 1, chunk.chunkCoords.y, chunk.chunkCoords.z), new Vector3Int(0, y, z)).opaque)
                        {
                            meshed[0, y, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x + 1, y, z]].opaque) { meshed[0, y, z] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, y, z])
                    {
                        //first we expand in y-direction
                        meshed[0, y, z] = true;
                        int yExtent = 1;
                        while (y + yExtent < Chunk.CHUNK_SIZE && !meshed[0, y + yExtent, z] && blocks[x, y + yExtent, z] == type)
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
                                if (blocks[x, i, z + zExtent] != type || meshed[0, i, z + zExtent])
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
                    BlockType type = blocks[x, y, z];

                    if (x == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x - 1, chunk.chunkCoords.y, chunk.chunkCoords.z), new Vector3Int(Chunk.CHUNK_SIZE - 1, y, z)).opaque)
                        {
                            meshed[0, y, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x - 1, y, z]].opaque) { meshed[0, y, z] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, y, z])
                    {
                        //first we expand in y-direction
                        meshed[0, y, z] = true;
                        int yExtent = 1;
                        while (y + yExtent < Chunk.CHUNK_SIZE && !meshed[0, y + yExtent, z] && blocks[x, y + yExtent, z] == type)
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
                                if (blocks[x, i, z + zExtent] != type || meshed[0, i, z + zExtent])
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
                    BlockType type = blocks[x, y, z];

                    if (y == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y + 1, chunk.chunkCoords.z), new Vector3Int(x, 0, z)).opaque)
                        {
                            meshed[0, x, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x, y + 1, z]].opaque) { meshed[0, x, z] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, x, z])
                    {
                        //first we expand in x-direction
                        meshed[0, x, z] = true;
                        int xExtent = 1;
                        while (x + xExtent < Chunk.CHUNK_SIZE && !meshed[0, x + xExtent, z] && blocks[x + xExtent, y, z] == type)
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
                                if (blocks[i, y, z + zExtent] != type || meshed[0, i, z + zExtent])
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
                    BlockType type = blocks[x, y, z];

                    if (y == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y - 1, chunk.chunkCoords.z), new Vector3Int(x, Chunk.CHUNK_SIZE - 1, z)).opaque)
                        {
                            meshed[0, x, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x, y - 1, z]].opaque) { meshed[0, x, z] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, x, z])
                    {
                        //first we expand in x-direction
                        meshed[0, x, z] = true;
                        int xExtent = 1;
                        while (x + xExtent < Chunk.CHUNK_SIZE && !meshed[0, x + xExtent, z] && blocks[x + xExtent, y, z] == type)
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
                                if (blocks[i, y, z + zExtent] != type || meshed[0, i, z + zExtent])
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
                    BlockType type = blocks[x, y, z];

                    if (z == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z + 1), new Vector3Int(x, y, 0)).opaque)
                        {
                            meshed[0, x, y] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x, y, z + 1]].opaque) { meshed[0, x, y] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, x, y])
                    {
                        //first we expand in x-direction
                        meshed[0, x, y] = true;
                        int xExtent = 1;
                        while (x + xExtent < Chunk.CHUNK_SIZE && !meshed[0, x + xExtent, y] && blocks[x + xExtent, y, z] == type)
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
                                if (blocks[i, y + yExtent, z] != type || meshed[0, i, y + yExtent])
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
                    BlockType type = blocks[x, y, z];

                    if (z == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z - 1), new Vector3Int(x, y, Chunk.CHUNK_SIZE - 1)).opaque)
                        {
                            meshed[0, x, y] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x, y, z - 1]].opaque) { meshed[0, x, y] = true; continue; }
                    if (type != BlockType.empty && type != BlockType.chunk_border && !meshed[0, x, y])
                    {
                        //first we expand in x-direction
                        meshed[0, x, y] = true;
                        int xExtent = 1;
                        while (x + xExtent < Chunk.CHUNK_SIZE && !meshed[0, x + xExtent, y] && blocks[x + xExtent, y, z] == type)
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
                                if (blocks[i, y + yExtent, z] != type || meshed[0, i, y + yExtent])
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
