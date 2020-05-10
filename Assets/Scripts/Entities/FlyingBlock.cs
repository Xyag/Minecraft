using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingBlock : Entity
{
    public float lifetime = 10;
    private float timer;
    private BlockType blockType;
    private List<Vector3> uvs;
    private MeshFilter mf;

    public override void Awake()
    {
        base.Awake();
        mf = GetComponent<MeshFilter>();
        //mesh is just a cube
        var verts = new List<Vector3>(24);
        var tris = new List<int>(36);
        uvs = new List<Vector3>(24);
        var norms = new List<Vector3>(24);
        blockType = BlockType.stone;
        MeshGenerator.posXFace(0, Vector3.zero, Vector2.zero, verts, tris, norms, uvs, blockType);
        MeshGenerator.posYFace(1, Vector3.zero, Vector2.zero, verts, tris, norms, uvs, blockType);
        MeshGenerator.posZFace(2, Vector3.zero, Vector2.zero, verts, tris, norms, uvs, blockType);
        MeshGenerator.negXFace(3, Vector3.zero, Vector2.zero, verts, tris, norms, uvs, blockType);
        MeshGenerator.negYFace(4, Vector3.zero, Vector2.zero, verts, tris, norms, uvs, blockType);
        MeshGenerator.negZFace(5, Vector3.zero, Vector2.zero, verts, tris, norms, uvs, blockType);
        mf.mesh.Clear();
        mf.mesh.SetVertices(verts);
        mf.mesh.SetTriangles(tris, 0);
        mf.mesh.SetNormals(norms);
        mf.mesh.SetUVs(0, uvs);
    }
    public override void Update()
    {
        base.Update();
        timer += Time.deltaTime;
        if (timer > lifetime)
        {
            place();
        }
    }
    protected override void onCollision(Vector3 oldV)
    {
        place();
    }
    public void place()
    {
        world.setBlock(new Vector3Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), Mathf.RoundToInt(transform.position.z)), blockType);
        Disable();
    }
    public void setType(BlockType type)
    {
        uvs.Clear();
        blockType = type;
        BlockTexture tex= Block.blockTypes[(int)type].texture;
        int texId = tex.PosX;
        uvs.Add(new Vector3(0, 1, texId));
        uvs.Add(new Vector3(1, 1, texId));
        uvs.Add(new Vector3(1, 0, texId));
        uvs.Add(new Vector3(0, 0, texId));
        texId = tex.PosY;
        uvs.Add(new Vector3(0, 1, texId));
        uvs.Add(new Vector3(1, 1, texId));
        uvs.Add(new Vector3(1, 0, texId));
        uvs.Add(new Vector3(0, 0, texId));
        texId = tex.PosZ;
        uvs.Add(new Vector3(0, 1, texId));
        uvs.Add(new Vector3(1, 1, texId));
        uvs.Add(new Vector3(1, 0, texId));
        uvs.Add(new Vector3(0, 0, texId));
        texId = tex.NegX;
        uvs.Add(new Vector3(0, 1, texId));
        uvs.Add(new Vector3(1, 1, texId));
        uvs.Add(new Vector3(1, 0, texId));
        uvs.Add(new Vector3(0, 0, texId));
        texId = tex.NegY;
        uvs.Add(new Vector3(0, 1, texId));
        uvs.Add(new Vector3(1, 1, texId));
        uvs.Add(new Vector3(1, 0, texId));
        uvs.Add(new Vector3(0, 0, texId));
        texId = tex.NegZ;
        uvs.Add(new Vector3(0, 1, texId));
        uvs.Add(new Vector3(1, 1, texId));
        uvs.Add(new Vector3(1, 0, texId));
        uvs.Add(new Vector3(0, 0, texId));
        mf.mesh.SetUVs(0, uvs);
    }
    public override void initialize(World world)
    {
        base.initialize(world);
        timer = 0;
    }
}
