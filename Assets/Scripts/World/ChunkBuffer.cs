using UnityEngine;
using System.Collections.Generic;

public class ChunkBuffer
{
    private List<Chunk> finishedMeshes;
    public ChunkBuffer(int initSize)
    {
        finishedMeshes = new List<Chunk>(initSize);
    }
    public void Push(Chunk data)
    {
        lock (finishedMeshes)
        {
            if (finishedMeshes.Count > 3000)
                Debug.Log(finishedMeshes.Count);
            finishedMeshes.Add(data);
        }
    }

    public Chunk Pop()
    {
        lock (finishedMeshes)
        {
            var top = finishedMeshes[finishedMeshes.Count - 1];
            finishedMeshes.RemoveAt(finishedMeshes.Count - 1);
            return top;
        }
    }

    public int Count()
    {
        lock (finishedMeshes)
        {
            return finishedMeshes.Count;
        }
    }

    public Chunk Get(int position)
    {
        lock (finishedMeshes)
        {
            return finishedMeshes[position];
        }
    }

    public bool Replace(Chunk toReplace)
    {
        lock (finishedMeshes)
        {
            int location = finishedMeshes.FindIndex(c => toReplace.chunkCoords == c.chunkCoords);
            if (location == -1)
            {
                return false;
            }
            else
            {
                finishedMeshes[location] = toReplace;
                return true;
            }
        }
    }
}