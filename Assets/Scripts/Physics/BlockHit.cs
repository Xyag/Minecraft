using UnityEngine;

public struct BlockHit
{
    public BlockData blockHit;
    public Vector3 hitOffset;
    public Vector3Int coords;
    public bool hit;

    public BlockHit(BlockData blockHit, Vector3Int coords, Vector3 offset, bool hit = true)
    {
        this.blockHit = blockHit;
        this.coords = coords;
        this.hit = hit;
        this.hitOffset = offset;
    }
}
