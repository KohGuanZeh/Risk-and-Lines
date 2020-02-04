﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    [System.Serializable]
    public struct Pool
    {
        public string tag;
        public GameObject prefab;
        public Transform parent;
        public int poolAmt;
    }

    public static ObjectPooling inst;
    [SerializeField] List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        inst = this;

        //Setting up all Object Pools at Awake
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objPool = new Queue<GameObject>();
            for (int i = 0; i < pool.poolAmt; i++)
            {
                GameObject pooledObj = Instantiate(pool.prefab, pool.parent);
                pooledObj.SetActive(false);
                objPool.Enqueue(pooledObj);
            }
            poolDictionary.Add(pool.tag, objPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 spawnPos, Quaternion spawnRot, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag '" + tag + "' does not exist.");
            return null;
        }

        //If there is enough Instances of Pooled Object, Get from Pool. If not, instantiate during runtime
        GameObject obj = poolDictionary[tag].Count > 0 ? poolDictionary[tag].Dequeue() : Instantiate(GetPool(tag).prefab, GetPool(tag).parent);

        obj.transform.position = spawnPos;
        obj.transform.rotation = spawnRot;
        if (parent) obj.transform.parent = parent; //If Parent is not Null, Set New Parent
        obj.SetActive(true);

        IPooledObject pooledObject = obj.GetComponent<IPooledObject>();
        if (pooledObject != null) pooledObject.OnObjectSpawn();

        return obj;
    }

    public void ReturnToPool(GameObject obj, string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag '" + tag + "' does not exist.");
            return;
        }

        obj.SetActive(false);

        IPooledObject pooledObject = obj.GetComponent<IPooledObject>();
        if (pooledObject != null) pooledObject.OnObjectDespawn();

        poolDictionary[tag].Enqueue(obj);
    }

    public Pool GetPool(string tag)
    {
        return pools.Find(x => x.tag == tag);
    }
}

public interface IPooledObject
{
    void OnObjectSpawn();
    void OnObjectDespawn();
    string GetPoolTag();
}
