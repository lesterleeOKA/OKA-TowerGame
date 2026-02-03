using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Simple reusable GameObject pool. Not a MonoBehaviour so it can be used from any script.
/// The pool will Instantiate(prefab) under the provided parent when empty.
/// </summary>
public class GameObjectPool
{
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private GameObject _prefab;
    private Transform _parent;
    private bool _setInactiveOnRelease = true;

    public void Init(GameObject prefab, Transform parent = null, int initialSize = 0, bool setInactiveOnRelease = true)
    {
        _prefab = prefab;
        _parent = parent;
        _setInactiveOnRelease = setInactiveOnRelease;

        for (int i = 0; i < initialSize; i++)
        {
            var go = CreateInstance();
            Release(go);
        }
    }

    private GameObject CreateInstance()
    {
        if (_prefab != null)
        {
            var go = Object.Instantiate(_prefab, _parent);
            go.SetActive(true);
            return go;
        }

        // Fallback: create a minimal GameObject if no prefab assigned
        var fallback = new GameObject("PooledObject");
        if (_parent != null) fallback.transform.SetParent(_parent, false);
        return fallback;
    }

    public GameObject Get()
    {
        GameObject go = null;
        if (_pool.Count > 0)
        {
            go = _pool.Dequeue();
            if (go == null)
            {
                // Defensive: create new if dequeued item was destroyed
                go = CreateInstance();
            }
        }
        else
        {
            go = CreateInstance();
        }

        if (go != null) go.SetActive(true);
        return go;
    }

    public void Release(GameObject go)
    {
        if (go == null) return;

        if (_setInactiveOnRelease)
            go.SetActive(false);

        // Reparent to pool owner if available
        if (_parent != null)
            go.transform.SetParent(_parent, false);

        _pool.Enqueue(go);
    }

    public void Clear()
    {
        while (_pool.Count > 0)
        {
            var go = _pool.Dequeue();
            if (go != null) Object.Destroy(go);
        }
    }
}