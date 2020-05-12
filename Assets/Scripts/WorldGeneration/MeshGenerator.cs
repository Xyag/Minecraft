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
    private static ConcurrentDictionary<Vector3Int, Chunk> frameBuffer = new ConcurrentDictionary<Vector3Int, Chunk>();
    private static ConcurrentDictionary<Vector3Int, Chunk> meshing = new ConcurrentDictionary<Vector3Int, Chunk>();
    public static Pool<GameObject> solidChunkPool;
    public static Pool<GameObject> transparentChunkPool;
    private static readonly Stopwatch stopwatch = new Stopwatch();
    private static World world;

    //we generate all queue all requests for meshing from the main thread at the beginning of each frame
    public static void emptyFrameBuffer(World world)
    {
        foreach (var item in frameBuffer.Values)
        {
            if (item == null)
            {
                continue;
            }
            if (!meshing.ContainsKey(item.chunkCoords))
            {
                meshing.TryAdd(item.chunkCoords, item);
                Task.Run(() => generateAndQueue(world, item));
            }
        }
        frameBuffer.Clear();
    }
    public static void queueChunk(Chunk chunk)
    {
        if (chunk == null)
        {
            return;
        }
        if (!frameBuffer.ContainsKey(chunk.chunkCoords))
            frameBuffer.TryAdd(chunk.chunkCoords, chunk);
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
    private static void generateAndQueue(World world, Chunk chunk)
    {
        MeshGenerator.world = world;
        if (chunk == null)
            return;
        generateMesh(world, chunk);
        finishedMeshes.Enqueue(chunk);
    }
    private static void spawnChunk(Chunk chunk)
    {
        if (chunk == null)
            return;
        if (!meshing.TryRemove(chunk.chunkCoords, out Chunk _))
        {
            UnityEngine.Debug.LogError("spawned chunk not present in framebuffer");
        }
        if (chunk.solidRenderData == null || chunk.transparentRenderData == null)
            return;
        //create solid obj if not already extant
        var solidObj = chunk.solidObject;
        if (solidObj == null)
        {
            solidObj = solidChunkPool.get();
            chunk.solidObject = solidObj;
        }
        solidObj.name = (chunk.solidRenderData.worldPos / Chunk.CHUNK_SIZE).ToString();
        solidObj.transform.position = chunk.solidRenderData.worldPos;

       // UnityEngine.Debug.Log(world.loadedChunks.TryGetValue(chunk.chunkCoords, out Chunk test) + ": " + (test == null ? "false" : test.solidObject.name));

        //create transparent obj if not already extant
        var transparentObj = chunk.transparentObject;
        if (transparentObj == null)
        {
            transparentObj = transparentChunkPool.get();
            chunk.transparentObject = transparentObj;
        }
        transparentObj.name = (chunk.transparentRenderData.worldPos / Chunk.CHUNK_SIZE).ToString();
        transparentObj.transform.position = chunk.transparentRenderData.worldPos;

        replaceChunkMesh(chunk, chunk.solidRenderData, chunk.transparentRenderData);
    }
    private static void replaceChunkMesh(Chunk chunk, MeshData solid, MeshData transparent)
    {
        MeshFilter mf = chunk.solidObject.GetComponent<MeshFilter>();

        mf.mesh.Clear();
        if (solid != null)
        {
            mf.mesh.SetVertices(solid.vertices);
            mf.mesh.SetTriangles(solid.triangles, 0);
            mf.mesh.SetNormals(solid.normals);
            mf.mesh.SetUVs(0, solid.uvs);
        }

        mf = chunk.transparentObject.GetComponent<MeshFilter>();

        mf.mesh.Clear();
        if (solid != null)
        {
            mf.mesh.SetVertices(transparent.vertices);
            mf.mesh.SetTriangles(transparent.triangles, 0);
            mf.mesh.SetNormals(transparent.normals);
            mf.mesh.SetUVs(0, transparent.uvs);
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

    private static Chunk generateMesh(World world, Chunk chunk)
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
        MeshData solidData = chunk.solidRenderData;
        List<Vector3> solidVertices;
        List<int> solidTris;
        List<Vector3> solidNorms;
        List<Vector3> solidUVs;
        MeshData transparentData = chunk.transparentRenderData;
        List<Vector3> transparentVertices;
        List<int> transparentTris;
        List<Vector3> transparentNorms;
        List<Vector3> transparentUVs;
        bool[,,] meshed = new bool[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE]; //default value is false
        if (solidData == null) //means that the chunk hasn't been meshed before, we need to allocate for the mesh data.
        {
            chunk.solidRenderData = new MeshData();
            solidData = chunk.solidRenderData;
            solidData.vertices = new List<Vector3>();
            solidData.triangles = new List<int>();
            solidData.normals = new List<Vector3>();
            solidData.uvs = new List<Vector3>();
        }
        else
        {
            solidData.vertices.Clear();
            solidData.triangles.Clear();
            solidData.normals.Clear();
            solidData.uvs.Clear();
        }
        if (transparentData == null) //means that the chunk hasn't been meshed before, we need to allocate for the mesh data.
        {
            chunk.transparentRenderData = new MeshData();
            transparentData = chunk.transparentRenderData;
            transparentData.vertices = new List<Vector3>();
            transparentData.triangles = new List<int>();
            transparentData.normals = new List<Vector3>();
            transparentData.uvs = new List<Vector3>();
        }
        else
        {
            transparentData.vertices.Clear();
            transparentData.triangles.Clear();
            transparentData.normals.Clear();
            transparentData.uvs.Clear();
        }
        solidVertices = solidData.vertices;
        solidTris = solidData.triangles;
        solidNorms = solidData.normals;
        solidUVs = solidData.uvs;
        int solidFaceIndex = 0;
        transparentVertices = transparentData.vertices;
        transparentTris = transparentData.triangles;
        transparentNorms = transparentData.normals;
        transparentUVs = transparentData.uvs;
        int transparentFaceIndex = 0;
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
                    var blockData = Block.blockTypes[(int)type];
                    if (x == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x + 1, chunk.chunkCoords.y, chunk.chunkCoords.z), new Vector3Int(0, y, z)).opaque
                            || (!blockData.opaque && (type == world.getBlock(new Vector3Int(chunk.chunkCoords.x + 1, chunk.chunkCoords.y, chunk.chunkCoords.z), new Vector3Int(0, y, z)).type)))
                        {
                            meshed[0, y, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x + 1, y, z]].opaque || (!blockData.opaque && (type == blocks[x+1,y,z]))) { meshed[0, y, z] = true; continue; }
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
                        if (Block.blockTypes[(int)type].opaque)
                        {
                            posXFace(solidFaceIndex, new Vector3(x, y, z), new Vector2(yExtent, zExtent), solidVertices, solidTris, solidNorms, solidUVs, type);
                            solidFaceIndex++;
                        }
                        else
                        {
                            posXFace(transparentFaceIndex, new Vector3(x, y, z), new Vector2(yExtent, zExtent), transparentVertices, transparentTris, transparentNorms, transparentUVs, type);
                            transparentFaceIndex++;
                        }
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
                    var blockData = Block.blockTypes[(int)type];
                    if (x == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x - 1, chunk.chunkCoords.y, chunk.chunkCoords.z), new Vector3Int(Chunk.CHUNK_SIZE - 1, y, z)).opaque
                            || (!blockData.opaque && (type == world.getBlock(new Vector3Int(chunk.chunkCoords.x - 1, chunk.chunkCoords.y, chunk.chunkCoords.z), new Vector3Int(0, y, z)).type)))
                        {
                            meshed[0, y, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x - 1, y, z]].opaque || (!blockData.opaque && (type == blocks[x - 1, y, z]))) { meshed[0, y, z] = true; continue; }
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
                        if (Block.blockTypes[(int)type].opaque)
                        {
                            negXFace(solidFaceIndex, new Vector3(x, y, z), new Vector2(yExtent, zExtent), solidVertices, solidTris, solidNorms, solidUVs, type);
                            solidFaceIndex++;
                        }
                        else
                        {
                            negXFace(transparentFaceIndex, new Vector3(x, y, z), new Vector2(yExtent, zExtent), transparentVertices, transparentTris, transparentNorms, transparentUVs, type);
                            transparentFaceIndex++;
                        }
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
                    var blockData = Block.blockTypes[(int)type];
                    if (y == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y + 1, chunk.chunkCoords.z), new Vector3Int(x, 0, z)).opaque
                            || (!blockData.opaque && (type == world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y+1, chunk.chunkCoords.z), new Vector3Int(x, 0, z)).type)))
                        {
                            meshed[0, x, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x, y + 1, z]].opaque || (!blockData.opaque && (type == blocks[x, y+1, z]))) { meshed[0, x, z] = true; continue; }
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
                        if (Block.blockTypes[(int)type].opaque)
                        {
                            posYFace(solidFaceIndex, new Vector3(x, y, z), new Vector2(xExtent, zExtent), solidVertices, solidTris, solidNorms, solidUVs, type);
                            solidFaceIndex++;
                        }
                        else
                        {
                            posYFace(transparentFaceIndex, new Vector3(x, y, z), new Vector2(xExtent, zExtent), transparentVertices, transparentTris, transparentNorms, transparentUVs, type);
                            transparentFaceIndex++;
                        }
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
                    var blockData = Block.blockTypes[(int)type];
                    if (y == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y - 1, chunk.chunkCoords.z), new Vector3Int(x, Chunk.CHUNK_SIZE - 1, z)).opaque
                            || (!blockData.opaque && (type == world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y -1, chunk.chunkCoords.z), new Vector3Int(x, 0, z)).type)))
                        {
                            meshed[0, x, z] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x, y - 1, z]].opaque || (!blockData.opaque && (type == blocks[x, y-1, z]))) { meshed[0, x, z] = true; continue; }
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
                        if (Block.blockTypes[(int)type].opaque)
                        {
                            negYFace(solidFaceIndex, new Vector3(x, y, z), new Vector2(xExtent, zExtent), solidVertices, solidTris, solidNorms, solidUVs, type);
                            solidFaceIndex++;
                        }
                        else
                        {
                            negYFace(transparentFaceIndex, new Vector3(x, y, z), new Vector2(xExtent, zExtent), transparentVertices, transparentTris, transparentNorms, transparentUVs, type);
                            transparentFaceIndex++;
                        }
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
                    var blockData = Block.blockTypes[(int)type];
                    if (z == Chunk.CHUNK_SIZE - 1)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z + 1), new Vector3Int(x, y, 0)).opaque
                            || (!blockData.opaque && (type == world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z + 1), new Vector3Int(x, y, 0)).type)))
                        {
                            meshed[0, x, y] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x, y, z + 1]].opaque || (!blockData.opaque && (type == blocks[x, y, z+1]))) { meshed[0, x, y] = true; continue; }
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
                        if (Block.blockTypes[(int)type].opaque)
                        {
                            posZFace(solidFaceIndex, new Vector3(x, y, z), new Vector2(xExtent, yExtent), solidVertices, solidTris, solidNorms, solidUVs, type);
                            solidFaceIndex++;
                        }
                        else
                        {
                            posZFace(transparentFaceIndex, new Vector3(x, y, z), new Vector2(xExtent, yExtent), transparentVertices, transparentTris, transparentNorms, transparentUVs, type);
                            transparentFaceIndex++;
                        }
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
                    var blockData = Block.blockTypes[(int)type];
                    if (z == 0)
                    {
                        if (world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z - 1), new Vector3Int(x, y, Chunk.CHUNK_SIZE - 1)).opaque
                            || (!blockData.opaque && (type == world.getBlock(new Vector3Int(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z - 1), new Vector3Int(x, y, 0)).type)))
                        {
                            meshed[0, x, y] = true;
                            continue;
                        }
                    }
                    else if (Block.blockTypes[(int)blocks[x, y, z - 1]].opaque || (!blockData.opaque && (type == blocks[x, y, z-1]))) { meshed[0, x, y] = true; continue; }
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
                        if (Block.blockTypes[(int)type].opaque)
                        {
                            negZFace(solidFaceIndex, new Vector3(x, y, z), new Vector2(xExtent, yExtent), solidVertices, solidTris, solidNorms, solidUVs, type);
                            solidFaceIndex++;
                        }
                        else
                        {
                            negZFace(transparentFaceIndex, new Vector3(x, y, z), new Vector2(xExtent, yExtent), transparentVertices, transparentTris, transparentNorms, transparentUVs, type);
                            transparentFaceIndex++;
                        }
                    }
                }
            }
            set2dFalse(meshed);
        }

        MeshData solidMeshData = new MeshData
        {
            worldPos = chunk.worldCoords,
            vertices = solidVertices,
            triangles = solidTris,
            normals = solidNorms,
            uvs = solidUVs,
        };
        chunk.solidRenderData = solidMeshData;
        MeshData transparentMeshData = new MeshData
        {
            worldPos = chunk.worldCoords,
            vertices = transparentVertices,
            triangles = transparentTris,
            normals = transparentNorms,
            uvs = transparentUVs,
        };
        chunk.transparentRenderData = transparentMeshData;
        return chunk;
    }
    
}
