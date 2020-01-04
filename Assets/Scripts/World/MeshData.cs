using UnityEngine;
using System.Collections.Generic;

public class MeshData
{
    public int faceCount;
    public Vector3Int worldPos;
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector3> normals;
    public List<Vector3> uvs;
}