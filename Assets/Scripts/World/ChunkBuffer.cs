using UnityEngine;
using System.Collections.Generic;

public class ChunkBuffer
{
    private List<Chunk> buffer;
    public ChunkBuffer(int initSize)
    {
        buffer = new List<Chunk>(initSize);
    }
    public void Push(Chunk data)
    {
        lock (buffer)
        {
            if (buffer.Count > 3000)
                Debug.Log(buffer.Count);
            buffer.Add(data);
        }
    }

    public Chunk Pop()
    {
        lock (buffer)
        {
            var top = buffer[buffer.Count - 1];
            buffer.RemoveAt(buffer.Count - 1);
            return top;
        }
    }

    public int Count()
    {
        return buffer.Count;
    }

    public Chunk Get(int position)
    {
        return buffer[position];
    }
    public bool Contains(Chunk chunk)
    {
        for (int i = 0; i < buffer.Count; i++)
        {
            if (buffer[i] == chunk)
            {
                return true;
            }
        }
        return false;
    }

    public bool Replace(Chunk toReplace)
    {
        lock (buffer)
        {
            int location = buffer.FindIndex(c => toReplace.chunkCoords == c.chunkCoords);
            if (location == -1)
            {
                return false;
            }
            else
            {
                buffer[location] = toReplace;
                return true;
            }
        }
    }
}