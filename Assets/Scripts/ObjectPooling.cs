using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Photon.Pun;

public class ObjectPooling : MonoBehaviourPunCallbacks
{
    //Cannot Pass any Reference Values. Need Pass Transforms by View Id
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
    [SerializeField] List<GameObject> testQueue;

    private void Awake()
    {
        inst = this;
        testQueue = new List<GameObject>();
        if (PhotonNetwork.IsMasterClient) InitialisePools();
        testQueue = poolDictionary[pools[0].tag].ToList();
    }

	#region Initialisation Functions

	void InitialisePools()
    {
        //Setting up all Object Pools at Awake
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        { 
            Queue<GameObject> objPool = new Queue<GameObject>();
            for (int i = 0; i < pool.poolAmt; i++) //Do if isMaster Check here. Only if Master - Spawn Object. Not sure if it will work
            {
                GameObject pooledObj = PhotonNetwork.InstantiateSceneObject(System.IO.Path.Combine("PhotonPrefabs", pool.prefabName), Vector3.zero, Quaternion.identity); //Instantiate(pool.prefab, pool.parent);
                
                //photonView.RPC("OnPoolObjectCreated", RpcTarget.AllBuffered, pool, pooledObj); //Use Photon View ID instead
                pooledObj.transform.parent = pool.parent;
                pooledObj.SetActive(false);

                objPool.Enqueue(pooledObj);
            }
            //photonView.RPC("UpdatePoolDictionary", RpcTarget.AllBuffered, pool, objPool);
            poolDictionary.Add(tag, objPool);
        }
    }

	#region For Networking
	[PunRPC]
    void OnPoolObjectCreated(Pool pool, GameObject pooledObj)
    {
        pooledObj.transform.parent = pool.parent;
        pooledObj.SetActive(false);
    }

    [PunRPC]
    void UpdatePoolDictionary(string tag, Queue<GameObject> objPool)
    {
        poolDictionary.Add(tag, objPool);
    }
	#endregion

	#endregion

	#region For Spawning

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

        //photonView.RPC("HandleDequeue", RpcTarget.AllBuffered, pool, obj, parent);
        if (parent) obj.transform.parent = parent; //If Parent is not Null, Set New Parent
        else obj.transform.parent = pool.parent;
        obj.SetActive(true);

        IPooledObject pooledObject = obj.GetComponent<IPooledObject>();
        if (pooledObject != null) pooledObject.OnObjectSpawn();

        return obj;
    }

	#region Networking Functions

	[PunRPC]
    void HandleDequeue(Pool pool, GameObject obj, Transform parent)
    {
        if (parent) obj.transform.parent = parent; //If Parent is not Null, Set New Parent
        else obj.transform.parent = pool.parent;
        obj.SetActive(true);
    }

	#endregion

	#endregion

	#region For Despawning

	public void ReturnToPool(GameObject obj, string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag '" + tag + "' does not exist.");
            return;
        }

        // photonView.RPC("HanldeEnqueue", RpcTarget.AllBuffered, obj);
        obj.SetActive(false);

        IPooledObject pooledObject = obj.GetComponent<IPooledObject>();
        if (pooledObject != null) pooledObject.OnObjectDespawn();

        poolDictionary[tag].Enqueue(obj);
    }

	#region Networking Functions
	
    [PunRPC]
    void HandleEnqueue(GameObject obj)
    {
        obj.SetActive(false);
    }

	#endregion

	#endregion

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