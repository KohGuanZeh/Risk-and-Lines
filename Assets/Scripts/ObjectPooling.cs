using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class ObjectPooling : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public struct Pool
    {
        public string tag;
        public string prefabName;
        public Transform parent;
        public int poolAmt;
    }

    public static ObjectPooling inst;
    [SerializeField] List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        inst = this;
        if (PhotonNetwork.IsMasterClient) InitialisePools();
    }

    [PunRPC]
    void InitialisePools()
    {
        //Setting up all Object Pools at Awake
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objPool = new Queue<GameObject>();
            for (int i = 0; i < pool.poolAmt; i++)
            {
                GameObject pooledObj = PhotonNetwork.InstantiateSceneObject(System.IO.Path.Combine("PhotonPrefabs", pool.prefabName), Vector3.zero, Quaternion.identity); //Instantiate(pool.prefab, pool.parent);
                pooledObj.transform.parent = pool.parent;
                pooledObj.SetActive(false);
                objPool.Enqueue(pooledObj);
            }
            poolDictionary.Add(pool.tag, objPool);
        }
    }

    [PunRPC]
    public GameObject SpawnFromPool(string tag, Vector3 spawnPos, Quaternion spawnRot, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag '" + tag + "' does not exist.");
            return null;
        }

        Pool pool = GetPool(tag);

        //If there is enough Instances of Pooled Object, Get from Pool. If not, instantiate during runtime
        GameObject obj = poolDictionary[tag].Count > 0 ? poolDictionary[tag].Dequeue() : PhotonNetwork.InstantiateSceneObject(System.IO.Path.Combine("PhotonPrefabs", pool.prefabName), Vector3.zero, Quaternion.identity);
        //Instantiate(GetPool(tag).prefab, GetPool(tag).parent);

        obj.transform.position = spawnPos;
        obj.transform.rotation = spawnRot;
        if (parent) obj.transform.parent = parent; //If Parent is not Null, Set New Parent
        else obj.transform.parent = pool.parent;
        obj.SetActive(true);

        IPooledObject pooledObject = obj.GetComponent<IPooledObject>();
        if (pooledObject != null) pooledObject.OnObjectSpawn();

        return obj;
    }

    [PunRPC]
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
