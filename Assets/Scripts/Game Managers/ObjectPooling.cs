using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class ObjectPooling : MonoBehaviourPunCallbacks
{
    //Cannot Pass any Reference Values. Need Pass Transforms by View Id
    [System.Serializable]
    public struct Pool
    {
        public string tag;
        public bool sceneObj;
        public string prefabName;
        public Transform parent;
        public int poolAmt;
    }

    public static ObjectPooling inst;
    [SerializeField] List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;
    //public List<GameObject> myTestQueue;

    private void Awake()
    {
        inst = this;

        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        foreach (Pool pool in pools) poolDictionary.Add(pool.tag, new Queue<GameObject>());

        InitialisePools();
    }

    #region Initialisation Functions
    void InitialisePools()
    {
        foreach (Pool pool in pools)
        {
            if (pool.sceneObj && !PhotonNetwork.IsMasterClient) continue;

            for (int i = 0; i < pool.poolAmt; i++)
            {
                GameObject obj = pool.sceneObj ? 
                                PhotonNetwork.InstantiateSceneObject(System.IO.Path.Combine("PhotonPrefabs", pool.prefabName), Vector3.zero, Quaternion.identity):
                                PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", pool.prefabName), Vector3.zero, Quaternion.identity);

                if (!pool.sceneObj) poolDictionary[pool.tag].Enqueue(obj);

                IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
                if (!ReferenceEquals(pooledObj, null)) obj.GetPhotonView().RPC("OnCreateObject", RpcTarget.AllBuffered);
            }
        }
    }
	#endregion

	#region For Spawning

	public GameObject SpawnFromPool(string tag, Vector3 spawnPos, Quaternion spawnRot, int parentId = -1)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag '" + tag + "' does not exist.");
            return null;
        }

        Pool pool = GetPool(tag);

        GameObject obj = null;

        //If there is enough Instances of Pooled Object, Get from Pool. If not, instantiate during runtime
        if (poolDictionary[tag].Count > 0)
        {
            obj = poolDictionary[tag].Dequeue();
            if (pool.sceneObj) photonView.RPC("RegisterDequeue", RpcTarget.OthersBuffered, tag);
        }
        else
        {
            obj = pool.sceneObj ? 
                PhotonNetwork.InstantiateSceneObject(System.IO.Path.Combine("PhotonPrefabs", pool.prefabName), Vector3.zero, Quaternion.identity):
                PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", pool.prefabName), Vector3.zero, Quaternion.identity);
        } 

        obj.transform.position = spawnPos;
        obj.transform.rotation = spawnRot;

        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
       
        if (!ReferenceEquals(pooledObj, null)) obj.GetPhotonView().RPC("OnObjectSpawn", RpcTarget.AllBuffered, parentId);

        return obj;
    }
	#endregion

	#region For Despawning

	public void ReturnToPool(GameObject obj, string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag '" + tag + "' does not exist.");
            return;
        }

        Pool pool = GetPool(tag);
        if (!pool.sceneObj) poolDictionary[tag].Enqueue(obj);

        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
        if (!ReferenceEquals(pooledObj, null)) obj.GetPhotonView().RPC("OnObjectDespawn", RpcTarget.AllBuffered);
    }

    #endregion

    [PunRPC]
    void RegisterDequeue(string tag)
    {
        poolDictionary[tag].Dequeue();
    }

	public Pool GetPool(string tag)
    {
        return pools.Find(x => x.tag == tag);
    }
}

public interface IPooledObject
{
    [PunRPC]
    void OnCreateObject();
    [PunRPC]
    void OnObjectSpawn(int parentId);
    [PunRPC]
    void OnObjectDespawn();
    string GetPoolTag();
}
