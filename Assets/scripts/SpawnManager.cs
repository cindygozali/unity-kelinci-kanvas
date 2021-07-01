using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    public AgentType Type;
    public List<GameObject> SpawnObjects;

    private void Awake()
    {
        Instance = this;
        Core.SubscribeEvent("LoadLevel", OnLoadLevel);
    }

    private void OnDestroy()
    {
        Instance = null;
        Core.UnSubscribeEvent("LoadLevel", OnLoadLevel);
    }

    void OnLoadLevel(object sender, object[] args)
    {
        foreach (GameObject obj in SpawnObjects)
        {
            AgentManager.Instance.SpawnAgent(Type, 0, 0, obj.transform.position, obj.transform.rotation);
        }
    }
}
