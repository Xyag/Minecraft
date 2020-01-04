using UnityEngine;

public struct BlockHit
{
    public BlockData blockHit;
    public Vector3Int coords;
    public bool hit;

    public BlockHit(BlockData blockHit, Vector3Int coords, bool hit = true)
    {
        this.blockHit = blockHit;
        this.coords = coords;
        this.hit = hit;
    }
}
