using System.Collections.Concurrent;
using UnityEngine;
public static class ExtensionMethods
{
    public static bool Contains<T>(this ConcurrentQueue<T> queue, T obj)
    {
        lock (queue)
        {
            foreach (var item in queue)
            {
                if (obj.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static Vector3Int toInt (this Vector3 self)
    {
        return new Vector3Int((int)self.x, (int)self.y, (int)self.z);
    }
    public static Vector3Int roundToInt(this Vector3 self)
    {
        return new Vector3Int(Mathf.RoundToInt(self.x), Mathf.RoundToInt(self.y), Mathf.RoundToInt(self.z));
    }
}
