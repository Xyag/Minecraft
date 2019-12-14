using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;

public static class MeshGenerator
{
    public static GameObject emptyChunk;
    public static PhysicMaterial chunkPhysMaterial;
    //private static Texture blockAtlas;
    private static int texturesPerRow;
    private static Vector2 textureSize;
    private static Vector2 textureOffset;
    private static Vector2 paddingSize;

    //public static void setBlockAtlas(Texture blockAtlas)
    //{
    //    //block textures are 16x16 each, with 1px on padding on each side. making the actual image 14x14
    //    MeshGenerator.blockAtlas = blockAtlas;
    //    texturesPerRow = blockAtlas.width / Block.TEXTURE_SIZE;
    //    paddingSize = new Vector2((float)Block.TEXTURE_SIZE / (float)blockAtlas.width, (float)Block.TEXTURE_SIZE / (float)blockAtlas.height);
    //    textureSize = new Vector2((float)(Block.TEXTURE_SIZE - 2) / (float)blockAtlas.width, (float)(Block.TEXTURE_SIZE - 2) / (float)blockAtlas.height);
    //    textureOffset = new Vector2(1.0f / (float)blockAtlas.width, 1.0f / (float)blockAtlas.height);
    //}
    public static async Task spawnAll(IEnumerable<Chunk> collection, World world, int initSize = 1)
    {
        List<Task<Chunk>> meshTasks = new List<Task<Chunk>>(initSize);
        //start generating the meshes in parallel
        foreach (var item in collection)
        {
            meshTasks.Add(Task.Run<Chunk>(() => MeshGenerator.generateMesh(world, item)));
        }
        //spawn them as they come in
        while (meshTasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(meshTasks);
            spawnChunk(finishedTask.Result);
            meshTasks.Remove(finishedTask);
        }
    }
    public static async Task spawnAll(Chunk[,,] collection, World world, int initCount = 1) //multidimensional arrays implement GetIterator() but not GetIterator<T>(). weird
    {
        List<Task<Chunk>> meshTasks = new List<Task<Chunk>>(initCount);
        //start generating the meshes in parallel
        foreach (var item in collection)
        {
            meshTasks.Add(Task.Run<Chunk>(() => MeshGenerator.generateMesh(world, item)));
        }
        //spawn them as they come in
        while (meshTasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(meshTasks);
            spawnChunk(finishedTask.Result);
            meshTasks.Remove(finishedTask);
        }
    }
    public static async Task remeshAll(IEnumerable<Chunk> collection, World world, int initSize = 1)
    {
        List<Task<Chunk>> meshTasks = new List<Task<Chunk>>();
        foreach (var item in collection)
        {
            meshTasks.Add(Task.Run<Chunk>(() => MeshGenerator.generateMesh(world, item)));
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
        var data = chunk.renderData;
        var chunkObject = chunk.gameObject;
        if (chunkObject == null)
        {
            chunkObject = MonoBehaviour.Instantiate(emptyChunk);
            chunk.gameObject = chunkObject;
        }
        chunkObject.name = (data.worldPos / Chunk.CHUNK_SIZE).ToString();
        chunkObject.transform.position = data.worldPos;


        MeshFilter mf = chunkObject.GetComponent<MeshFilter>();
        mf.mesh.SetVertices(data.vertices);
        mf.mesh.SetTriangles(data.triangles, 0);
        mf.mesh.SetNormals(data.normals);
        mf.mesh.SetUVs(0, data.uvs);
        for (int i = 0; i < data.boxColliders.Count; i++)
        {
            BoxCollider box = chunkObject.AddComponent<BoxCollider>();
            box.material = chunkPhysMaterial;
            box.center = new Vector3((float)data.boxColliders[i].x - 0.5f + (float)data.boxColliders[i].dx * 0.5f,
              (float)data.boxColliders[i].y - 0.5f + (float)data.boxColliders[i].dy * 0.5f, (float)data.boxColliders[i].z - 0.5f + (float)data.boxColliders[i].dz * 0.5f);
            box.size = new Vector3(data.boxColliders[i].dx, data.boxColliders[i].dy, data.boxColliders[i].dz);
        }
    }
    public static void replaceChunkMesh(Chunk chunk, MeshData data, World world)
    {
        if (chunk != null && chunk.gameObject != null)
        {
            MeshFilter mf = chunk.gameObject.GetComponent<MeshFilter>();
            mf.mesh.Clear();
            mf.mesh.SetVertices(data.vertices);
            mf.mesh.SetTriangles(data.triangles, 0);
            mf.mesh.SetNormals(data.normals);
            mf.mesh.SetUVs(0, data.uvs);

            List<BoxCollider> colliders = new List<BoxCollider>();
            chunk.gameObject.GetComponents<BoxCollider>(colliders);
            int space = colliders.Count;

            if (space > data.boxColliders.Count)
            {
                for (int i = 0; i < data.boxColliders.Count; i++)
                {
                    var box = colliders[i];
                    box.enabled = true;
                    box.material = chunkPhysMaterial;
                    box.center = new Vector3((float)data.boxColliders[i].x - 0.5f + (float)data.boxColliders[i].dx * 0.5f,
                        (float)data.boxColliders[i].y - 0.5f + (float)data.boxColliders[i].dy * 0.5f, (float)data.boxColliders[i].z - 0.5f + (float)data.boxColliders[i].dz * 0.5f);
                    box.size = new Vector3(data.boxColliders[i].dx, data.boxColliders[i].dy, data.boxColliders[i].dz);
                }
                for (int i = data.boxColliders.Count; i < space; i++)
                {
                    colliders[i].enabled = false;
                }
            }
            else
            {
                for (int i = 0; i < space; i++)
                {
                    var box = colliders[i];
                    box.enabled = true;
                    box.material = chunkPhysMaterial;
                    box.center = new Vector3((float)data.boxColliders[i].x - 0.5f + (float)data.boxColliders[i].dx * 0.5f,
                        (float)data.boxColliders[i].y - 0.5f + (float)data.boxColliders[i].dy * 0.5f, (float)data.boxColliders[i].z - 0.5f + (float)data.boxColliders[i].dz * 0.5f);
                    box.size = new Vector3(data.boxColliders[i].dx, data.boxColliders[i].dy, data.boxColliders[i].dz); ;
                }
                for (int i = space; i < data.boxColliders.Count; i++)
                {
                    var box = chunk.gameObject.AddComponent<BoxCollider>();
                    box.enabled = true;
                    box.material = chunkPhysMaterial;
                    box.center = new Vector3((float)data.boxColliders[i].x - 0.5f + (float)data.boxColliders[i].dx * 0.5f,
                        (float)data.boxColliders[i].y - 0.5f + (float)data.boxColliders[i].dy * 0.5f, (float)data.boxColliders[i].z - 0.5f + (float)data.boxColliders[i].dz * 0.5f);
                    box.size = new Vector3(data.boxColliders[i].dx, data.boxColliders[i].dy, data.boxColliders[i].dz);
                }
            }
        }
    }
    public static void remeshChunk(World world, Chunk chunk, bool alertNeighbors = true)
    {
        if (chunk != null)
        {
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
                List<BoxCollider> colliders = new List<BoxCollider>();
                chunk.gameObject.GetComponents<BoxCollider>(colliders);
                int space = colliders.Count;
                if (space > data.boxColliders.Count)
                {
                    for (int i = 0; i < data.boxColliders.Count; i++)
                    {
                        var box = colliders[i];
                        box.enabled = true;
                        box.material = chunkPhysMaterial;
                        box.center = new Vector3((float)data.boxColliders[i].x - 0.5f + (float)data.boxColliders[i].dx * 0.5f,
              (float)data.boxColliders[i].y - 0.5f + (float)data.boxColliders[i].dy * 0.5f, (float)data.boxColliders[i].z - 0.5f + (float)data.boxColliders[i].dz * 0.5f);
                        box.size = new Vector3(data.boxColliders[i].dx, data.boxColliders[i].dy, data.boxColliders[i].dz);
                    }
                    for (int i = data.boxColliders.Count; i < space; i++)
                    {
                        colliders[i].enabled = false;
                    }
                }
                else
                {
                    for (int i = 0; i < space; i++)
                    {
                        var box = colliders[i];
                        box.enabled = true;
                        box.material = chunkPhysMaterial;
                        box.center = new Vector3((float)data.boxColliders[i].x - 0.5f + (float)data.boxColliders[i].dx * 0.5f,
              (float)data.boxColliders[i].y - 0.5f + (float)data.boxColliders[i].dy * 0.5f, (float)data.boxColliders[i].z - 0.5f + (float)data.boxColliders[i].dz * 0.5f);
                        box.size = new Vector3(data.boxColliders[i].dx, data.boxColliders[i].dy, data.boxColliders[i].dz); ;
                    }
                    for (int i = space; i < data.boxColliders.Count; i++)
                    {
                        var box = chunk.gameObject.AddComponent<BoxCollider>();
                        box.enabled = true;
                        box.material = chunkPhysMaterial;
                        box.center = new Vector3((float)data.boxColliders[i].x - 0.5f + (float)data.boxColliders[i].dx * 0.5f,
              (float)data.boxColliders[i].y - 0.5f + (float)data.boxColliders[i].dy * 0.5f, (float)data.boxColliders[i].z - 0.5f + (float)data.boxColliders[i].dz * 0.5f);
                        box.size = new Vector3(data.boxColliders[i].dx, data.boxColliders[i].dy, data.boxColliders[i].dz);
                    }
                }
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
        else
        {
            Debug.LogError("null chunk");
        }

    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void PosXFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x + 0.5f, blockPos.y + size.x + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x + 0.5f, blockPos.y + size.x + 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x + 0.5f, blockPos.y - 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x + 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));

        setUpTrisNormsUvs(faceIndex, triangles, normals, uvs, Block.blockTypes[(int)block].texture.PosX, new Vector2(size.y, size.x));
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void NegXFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + size.x + 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + size.x + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z + size.y + 0.5f));

        setUpTrisNormsUvs(faceIndex, triangles, normals, uvs, Block.blockTypes[(int)block].texture.NegX, size);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void PosYFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y + 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + 0.5f, blockPos.z + size.y + 0.5f));

        setUpTrisNormsUvs(faceIndex, triangles, normals, uvs, Block.blockTypes[(int)block].texture.PosY, new Vector2(size.y, size.x));
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void NegYFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y - 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z + size.y + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));

        setUpTrisNormsUvs(faceIndex, triangles, normals, uvs, Block.blockTypes[(int)block].texture.NegY, size);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void PosZFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y + size.y + 0.5f, blockPos.z + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + size.y + 0.5f, blockPos.z + 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z + 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y - 0.5f, blockPos.z + 0.5f));

        setUpTrisNormsUvs(faceIndex, triangles, normals, uvs, Block.blockTypes[(int)block].texture.PosZ, size);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void NegZFace(int faceIndex, Vector3 blockPos, Vector2 size, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, BlockType block)
    {
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y + size.y + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y + size.y + 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x + size.x + 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));
        vertices.Add(new Vector3(blockPos.x - 0.5f, blockPos.y - 0.5f, blockPos.z - 0.5f));

        setUpTrisNormsUvs(faceIndex, triangles, normals, uvs, Block.blockTypes[(int)block].texture.NegZ, size);
    }


    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void setUpTrisNormsUvs(int faceIndex, List<int> triangles, List<Vector3> normals, List<Vector3> uvs, int texId, Vector2 size)
    {
        int vertexStart = faceIndex * 4;
        triangles.Add(vertexStart);
        triangles.Add(vertexStart + 1);
        triangles.Add(vertexStart + 2);
        triangles.Add(vertexStart + 2);
        triangles.Add(vertexStart + 3);
        triangles.Add(vertexStart);

        normals.Add(new Vector3(1, 0, 0));
        normals.Add(new Vector3(1, 0, 0));
        normals.Add(new Vector3(1, 0, 0));
        normals.Add(new Vector3(1, 0, 0));

        uvs.Add(new Vector3(0, size.y + 1, texId));
        uvs.Add(new Vector3(size.x + 1, size.y + 1, texId));
        uvs.Add(new Vector3(size.x + 1, 0, texId));
        uvs.Add(new Vector3(0, 0, texId));
    }

    public static Chunk generateMesh(World world, Chunk chunk)
    {
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
        List<BoxInt> boxColliders;
        BlockData[] dataStore = new BlockData[6];
        bool[,,] meshed = new bool[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE]; //default value is false
        if (renderData == null) //means that the chunk hasn't been meshed before, we need to allocate for the mesh data.
        {
            //from testing, there's ususally < ~0.4x the block capacity in faces.
            //the initial allocations may be a little too big, but it's worth it to reduce resizes.
            chunk.renderData = new MeshData();
            renderData = chunk.renderData;
            int initFaceCount = (int)((float)(Chunk.BLOCK_COUNT) * 0.4f);
            renderData.vertices = new List<Vector3>(initFaceCount * 4);
            renderData.triangles = new List<int>(initFaceCount * 6);
            renderData.normals = new List<Vector3>(initFaceCount * 4);
            renderData.uvs = new List<Vector3>(initFaceCount * 4);
            renderData.boxColliders = new List<BoxInt>(initFaceCount / 25); //DIVING JUST CAUSE
        }
        else
        {
            renderData.vertices.Clear();
            renderData.triangles.Clear();
            renderData.normals.Clear();
            renderData.uvs.Clear();
            renderData.boxColliders.Clear();
        }
        vertices = renderData.vertices;
        triangles = renderData.triangles;
        normals = renderData.normals;
        uvs = renderData.uvs;
        boxColliders = renderData.boxColliders;
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
                        PosXFace(faceIndex, new Vector3(x, y, z), new Vector2(yExtent, zExtent), vertices, triangles, normals, uvs, type);
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
                        NegXFace(faceIndex, new Vector3(x, y, z), new Vector2(yExtent, zExtent), vertices, triangles, normals, uvs, type);
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
                        PosYFace(faceIndex, new Vector3(x, y, z), new Vector2(xExtent, zExtent), vertices, triangles, normals, uvs, type);
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
                        NegYFace(faceIndex, new Vector3(x, y, z), new Vector2(xExtent, zExtent), vertices, triangles, normals, uvs, type);
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
                        PosZFace(faceIndex, new Vector3(x, y, z), new Vector2(xExtent, yExtent), vertices, triangles, normals, uvs, type);
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
                        NegZFace(faceIndex, new Vector3(x, y, z), new Vector2(xExtent, yExtent), vertices, triangles, normals, uvs, type);
                        faceIndex++;
                    }
                }
            }
            set2dFalse(meshed);
        }

        //now we do collision
        //going y->x->z cause i think we will have bigger rectangles in the xz plane
        //TODO: fix this shit im bored of working on it for now
        /*for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
        {
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    bool collides = Block.blockTypes[(int)chunk.blocks[x, y, z].type].fullCollision;
                    if (!collides) { meshed[x, y, z] = true; continue; }
                    if (meshed[x, y, z]) { continue; }

                    //z is first direction here
                    int zExtent = 1;
                    while (z + zExtent < Chunk.CHUNK_SIZE && Block.blockTypes[(int)chunk.blocks[x, y, z + zExtent].type].fullCollision)
                    {
                        meshed[x, y, z + zExtent] = true;
                        zExtent++;
                    }
                    zExtent--; //always overcount
                    //next is x
                    int xExtent = 1;
                    while (x + xExtent < Chunk.CHUNK_SIZE)
                    {
                        for (int tz = z; tz <= z + zExtent; tz++)
                        {
                            if (meshed[x + xExtent, y, tz] || !Block.blockTypes[(int)chunk.blocks[x + xExtent, y, tz].type].fullCollision)
                                goto endXLoop;
                        }
                        for (int tz = z; tz <= z + zExtent; tz++)
                        {
                            meshed[x + xExtent, y, tz] = true;
                        }
                        xExtent++;
                    }
                endXLoop:
                    int yExtent = 1;
                    while (y + yExtent < Chunk.CHUNK_SIZE)
                    {
                        for (int tx = x; tx < x + xExtent; tx++)
                        {
                            for (int tz = z; tz <= z + zExtent; tz++)
                            {
                                if (meshed[tx, y + yExtent, tz] || !Block.blockTypes[(int)chunk.blocks[tx, y + yExtent, tz].type].fullCollision)
                                    goto endYLoop;
                            }
                        }
                        for (int tx = x; tx < x + xExtent; tx++)
                        {
                            for (int tz = z; tz <= z + zExtent; tz++)
                            {
                                meshed[tx, y + yExtent, tz] = true;
                            }
                        }
                        yExtent++;
                    }
                endYLoop:
                    boxColliders.Add(new BoxInt(x, y, z, xExtent + 1, yExtent + 1, zExtent + 1));
                }
            }
        }*/
        //-------------OLD COLLISION ALGORITHM-----------
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (chunk.blocks[x, y, z].type != BlockType.empty && chunk.blocks[x, y, z].type != BlockType.chunk_border)
                    {
                        world.getSurroundingBlocks(chunk.chunkCoords, new Vector3Int(x, y, z), dataStore);
                        if (!dataStore[(int)Direction.PosX].fullCollision || !dataStore[(int)Direction.PosY].fullCollision || !dataStore[(int)Direction.PosZ].fullCollision
                                || !dataStore[(int)Direction.NegX].fullCollision || !dataStore[(int)Direction.NegY].fullCollision || !dataStore[(int)Direction.NegZ].fullCollision)
                        {
                            boxColliders.Add(new BoxInt(x, y, z, 1, 1, 1));
                        }
                    }

                }
            }
        }
        renderData.faceCount = faceIndex;
        MeshData meshData = new MeshData
        {
            worldPos = chunk.worldCoords,
            vertices = vertices,
            triangles = triangles,
            normals = normals,
            uvs = uvs,
            boxColliders = boxColliders,
        };
        chunk.renderData = meshData;
        return chunk;
    }
}
