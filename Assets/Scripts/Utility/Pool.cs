using UnityEngine;
using System;
using System.Collections.Generic;

public class Pool<T>
{
    private Func<T> factory;
    private Func<T, bool> active;
    private Action<T> activate;
    private List<T> objects;

    public Pool(Func<T> factoryMethod, Func<T, bool> activeChecker, Action<T> activate, int initSize = 5)
    {
        factory = factoryMethod;
        active = activeChecker;
        this.activate = activate;
        objects = new List<T>(initSize);
    }

    public T get()
    {
        foreach (var item in objects)
        {
            if (!active(item))
            {
                activate(item);
                return item;
            }
        }
        var newItem = factory();
        objects.Add(newItem);
        return newItem;
    }

    public void add(T item)
    {
        objects.Add(item);
    }

    //prefill = true fills the pool with initSize GameObjects and disables all of them.
    public static Pool<GameObject> createGameObjectPool(GameObject prefab, World world, int initSize = 5, bool prefill = false)
    {
        Pool<GameObject> pool = new Pool<GameObject>(
            () => UnityEngine.Object.Instantiate(prefab), 
            g => g.activeInHierarchy, 
            g => { g.SetActive(true); world.loadedEntities.Add(g.GetComponent<Entity>()); }, 
            initSize);

        if (prefill)
        {
            for (int i = 0; i < initSize; i++)
            {
                GameObject g = UnityEngine.Object.Instantiate(prefab);
                g.SetActive(false);
                pool.add(g);
            }
        }
        return pool;
    }
}
