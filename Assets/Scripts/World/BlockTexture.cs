using UnityEngine;

public struct BlockTexture
{
    public int PosX;
    public int PosY;
    public int PosZ;
    public int NegX;
    public int NegY;
    public int NegZ;

    public BlockTexture(int posx, int posy, int posz, int negx, int negy, int negz)
    {
        PosX = posx;
        PosY = posy;
        PosZ = posz;
        NegX = negx;
        NegY = negy;
        NegZ = negz;
    }
    public BlockTexture(int pos)
    {
        PosX = pos;
        PosY = pos;
        PosZ = pos;
        NegX = pos;
        NegY = pos;
        NegZ = pos;
    }
}