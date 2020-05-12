using System.Collections.Concurrent;

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
}
