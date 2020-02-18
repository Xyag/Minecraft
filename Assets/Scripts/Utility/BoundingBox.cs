using UnityEngine;
using System.Runtime.CompilerServices;
public struct BoundingBox
{
    public BoundingBox(Vector3 center, Vector3 extents)
    {
        this.center = center;
        this.extents = extents;
    }
    public Vector3 center;
    public Vector3 extents;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 top()
    {
        return new Vector3(center.x, center.y + extents.y, center.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 bottom()
    {
        return new Vector3(center.x, center.y - extents.y, center.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 left()
    {
        return new Vector3(center.x - extents.x, center.y, center.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 right()
    {
        return new Vector3(center.x + extents.x, center.y, center.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 forward()
    {
        return new Vector3(center.x, center.y, center.z + extents.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 backward()
    {
        return new Vector3(center.x, center.y, center.z - extents.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool intersectsBoundingBox(BoundingBox other)
    {
        bool y = center.y + extents.y >= other.center.y - other.extents.y && center.y - extents.y <= other.center.y + other.extents.y;
        bool x = center.x + extents.x >= other.center.x - other.extents.x && center.x - extents.x <= other.center.x + other.extents.x;
        bool z = center.z + extents.z >= other.center.z - other.extents.z && center.z - extents.z <= other.center.z + other.extents.z;

        return x && y && z;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool intersectsBlockCoords(Vector3Int coords)
    {
        bool x = Mathf.RoundToInt(center.x + extents.x) >= coords.x && Mathf.RoundToInt(center.x - extents.x) <= coords.x;
        bool y = Mathf.RoundToInt(center.y + extents.y) >= coords.y && Mathf.RoundToInt(center.y - extents.y) <= coords.y;
        bool z = Mathf.RoundToInt(center.z + extents.z) >= coords.z && Mathf.RoundToInt(center.z - extents.z) <= coords.z;

        return x && y && z;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool intersectsBlock(World world)
    {
        for (int x = Mathf.RoundToInt(center.x-extents.x); x <= Mathf.RoundToInt(center.x+extents.x); x++)
        {
            for (int y = Mathf.RoundToInt(center.y-extents.y); y <= Mathf.RoundToInt(center.y+extents.y); y++)
            {
                for (int z = Mathf.RoundToInt(center.z - extents.z); z <= Mathf.RoundToInt(center.z + extents.z); z++)
                {
                    var b = world.getBlock(new Vector3Int(x, y, z));
                    //Debug.Log("getting: " + new Vector3Int(x, y, z) + ". Type = " + b.type);
                    if (b.fullCollision)
                        return true;
                }
            }
        }
        return false;
    }
}
