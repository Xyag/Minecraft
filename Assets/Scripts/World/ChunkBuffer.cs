using UnityEngine;
using System.Collections.Generic;

public class ChunkBuffer
{
    private Queue<Chunk> finishedMeshes;
    public ChunkBuffer(int initSize)
    {
        finishedMeshes = new Queue<Chunk>(initSize);
    }
    public void Enqueue(Chunk data)
    {
        lock (finishedMeshes)
        {
            finishedMeshes.Enqueue(data);
        }
    }

    public Chunk Dequeue()
    {
        lock (finishedMeshes)
        {
            return finishedMeshes.Dequeue();
        }
    }

    public int Count()
    {
        lock (finishedMeshes)
        {
            return finishedMeshes.Count;
        }
    }
}