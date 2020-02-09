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

	#region Initialisation Functions

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
                photonView.RPC("OnPoolObjectCreated", RpcTarget.AllBuffered, pool.parent.gameObject, pooledObj);
                objPool.Enqueue(pooledObj);
            }
            photonView.RPC("UpdatePoolDictionary", RpcTarget.AllBuffered, pool.tag, objPool);
        }
    }

	#region For Networking
	[PunRPC]
    void OnPoolObjectCreated(GameObject parent, GameObject pooledObj)
    {
        pooledObj.transform.parent = parent.transform;
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
        photonView.RPC("HandleDequeue", RpcTarget.AllBuffered, obj, parent.gameObject, pool.parent.gameObject);

        IPooledObject pooledObject = obj.GetComponent<IPooledObject>();
        if (pooledObject != null) pooledObject.OnObjectSpawn();

        return obj;
    }

	#region Networking Functions

	[PunRPC]
    void HandleDequeue(GameObject obj, GameObject spawnParent, GameObject poolParent)
    {
        if (spawnParent) obj.transform.parent = spawnParent.transform; //If Parent is not Null, Set New Parent
        else obj.transform.parent = poolParent.transform;
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

        photonView.RPC("HanldeEnqueue", RpcTarget.AllBuffered, obj);

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
