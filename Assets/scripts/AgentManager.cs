using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    public static AgentManager Instance { get; private set; }

    #region variables
    [SerializeField]
    public AgentPrefab[] Agents;
    public uint CurrentAgentID = 0;
    
    Dictionary<AgentType, AgentPrefab> _agentPrefabs = new Dictionary<AgentType, AgentPrefab>();
    Dictionary<uint, IAgent> _agents = new Dictionary<uint, IAgent>();
    Dictionary<uint, uint> _ownerMap = new Dictionary<uint, uint>();
    #endregion

    private void Awake()
    {
        Instance = this;

        foreach (AgentPrefab ap in Agents)
        {
            _agentPrefabs[ap.Type] = ap;
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    #region agent manager functions

    public GameObject SpawnAgent(AgentType type, uint owner, uint agentID,
                                Vector3 pos, Quaternion rot)
    {
        AgentPrefab agentPrefab;

        if (_agentPrefabs.TryGetValue(type, out agentPrefab))
        {
            GameObject prefab = null;
            bool forceSync = false;

            switch (NetManager.Instance.Type)
            {
                case NetworkNodeType.Server:
                    prefab = agentPrefab.ServerPrefab;
                    agentID = ++CurrentAgentID;
                    forceSync = true;
                    break;

                case NetworkNodeType.Client:
                    if (_agents.ContainsKey(agentID))
                        return null;

                    prefab = agentPrefab.ClientPrefab;
                    break;

                default:
                    throw new System.Exception("Wrong Node Type! " + NetManager.Instance.Type);
            }

            GameObject obj = Instantiate(prefab, pos, rot);
            IAgent agent = obj.GetComponent<IAgent>();
            agent.OwnerID = owner;
            agent.AgentID = agentID;
            agent.Type = agentPrefab.Type;
            agent.ForceSync = forceSync;

            _agents[agentID] = agent;
            _ownerMap[agentID] = owner;
            return obj;
        }
        else
            return null;
    }

    public uint[] DeleteAgentByOwner(uint ownerid)
    {
        List<uint> agentIdsDeleted = new List<uint>();
        foreach (uint agentid in _ownerMap.Keys)
        {
            if (_ownerMap[agentid] == ownerid)
            {
                agentIdsDeleted.Add(agentid);
                IAgent agent;
                if (_agents.TryGetValue(agentid, out agent))
                {
                    Destroy(agent.gameObject);
                }
            }
        }
        return agentIdsDeleted.ToArray();
        // remove agent id with owner id
    }

    public IAgent[] GetAllAgents(bool onlyNeedsToSync = false)
    {
        if (onlyNeedsToSync)
        {
            return _agents.Values.Where((x) =>
            {
                return x.NeedToSync;
            }).ToArray();
        }
        else
            return _agents.Values.ToArray();
    }

    public void ApplySyncData(uint agentID, NetMessageBase msg)
    {
        IAgent agent;
        if (_agents.TryGetValue(agentID, out agent))
        {
            agent.ApplySyncData(msg);
        }
    }
    #endregion
}

public enum AgentType
{
    CharacterPlayer,
    DrawingPlayer,
    NPC,
}

public enum AgentState
{
    Connecting,
    CountDown,
    StartGame,
    Win,
    Lose,
}

[System.Serializable]
public class AgentPrefab
{
    public string Name;
    public AgentType Type;
    public GameObject ServerPrefab;
    public GameObject ClientPrefab;
}

public abstract class IAgent : MonoBehaviour
{
    public uint OwnerID;
    public uint AgentID;
    public AgentType Type;
    public bool ForceSync = false;
    public abstract void ApplySyncData(NetMessageBase msg);
    public abstract NetMessageBase GetSyncData();
    public abstract bool NeedToSync { get; }
}