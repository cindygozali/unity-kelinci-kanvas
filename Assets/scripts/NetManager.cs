using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetManager : MonoBehaviour
{
    public static NetManager Instance { get; private set; }

    public string IP;
    public ushort Port;
    public NetworkNodeType Type;
    
    public INetManager NetNode;
    public float SyncInterval = 0.1f;
    public float PingInterval = 5f;

    [Header("UI")]
    public GameObject ServerClientUI;

    Dictionary<uint, string> _accounts = new Dictionary<uint, string>();
    Dictionary<uint, AgentType> _players = new Dictionary<uint, AgentType>();
    Dictionary<int, uint> _connIDToOwnerID = new Dictionary<int, uint>();

    bool _hasShowOwnUserData;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;

        if (Type == NetworkNodeType.Server)
        {
            NetNode.UnregisterCallback((ushort)NetMessageType.IDRequest, OnReceiveIDRequestServer);
            NetNode.UnregisterCallback((ushort)NetMessageType.EnterGameMessage, OnReceiveEnterGameMessageServer);
            NetNode.UnregisterCallback((ushort)NetMessageType.ChatMessage, OnReceiveChatMessageServer);
            NetNode.UnregisterCallback((ushort)NetMessageType.StateMessage, OnReceiveStateMessageServer);
            NetNode.UnregisterCallback((ushort)NetMessageType.TimeMessage, OnReceiveTimeMessageServer);
            NetNode.UnregisterCallback((ushort)NetMessageType.ScoreMessage, OnReceiveScoreMessageServer);
            NetNode.UnregisterCallback((ushort)NetMessageType.DeleteMessage, OnReceiveDeleteMessageServer);
        }
        else
        {
            NetNode.UnregisterCallback((ushort)NetMessageType.IDRequest, OnReceiveIDRequestClient);
            NetNode.UnregisterCallback((ushort)NetMessageType.ChatMessage, OnReceiveChatMessageClient);
            NetNode.UnregisterCallback((ushort)NetMessageType.UserData, OnReceiveUserDataClient);
            NetNode.UnregisterCallback((ushort)NetMessageType.StateMessage, OnReceiveStateMessageClient);
            NetNode.UnregisterCallback((ushort)NetMessageType.TimeMessage, OnReceiveTimeMessageClient);
            NetNode.UnregisterCallback((ushort)NetMessageType.ScoreMessage, OnReceiveScoreMessageClient);
            NetNode.UnregisterCallback((ushort)NetMessageType.DeleteMessage, OnReceiveDeleteMessageClient);
        }
        
        NetNode.UnregisterCallback((ushort)NetMessageType.SpawnMessage, OnReceiveSpawnMessage);
        NetNode.UnregisterCallback((ushort)NetMessageType.GroupMessage, OnReceiveGroupMessage);
        NetNode.UnregisterCallback((ushort)NetMessageType.CharacterSyncMessage, OnReceiveCharacterSyncMessage);
        NetNode.UnregisterCallback((ushort)NetMessageType.DrawingSyncMessage, OnReceiveDrawingSyncMessage);
        NetNode.UnregisterCallback((ushort)NetMessageType.PlatformSyncMessage, OnReceivePlatformSyncMessage);
        NetNode.UnregisterCallback((ushort)NetMessageType.PingMessage, OnReceivePingMessage);
    }
    
    void Start()
    {
        ServerClientUI.SetActive(true);
    }
    
    void OnReceiveGroupMessage(ref NetworkConnection conn, NetMessageBase m)
    {
        GroupMessage msg = (GroupMessage)m;
        foreach (NetMessageBase b in msg.Messages)
        {
            NetNode.ExecuteFlagToCallback(b, ref conn);
        }
    }
    
    void OnReceiveCharacterSyncMessage(ref NetworkConnection conn, NetMessageBase m)
    {
        CharacterSyncMessage msg = (CharacterSyncMessage)m;
        AgentManager.Instance.ApplySyncData(msg.AgentID, msg);
    }

    void OnReceiveDrawingSyncMessage(ref NetworkConnection conn, NetMessageBase m)
    {
        DrawingSyncMessage msg = (DrawingSyncMessage)m;
        AgentManager.Instance.ApplySyncData(msg.AgentID, msg);
    }

    void OnReceivePlatformSyncMessage(ref NetworkConnection conn, NetMessageBase m)
    {
        PlatformSyncMessage msg = (PlatformSyncMessage)m;
        AgentManager.Instance.ApplySyncData(msg.AgentID, msg);
    }

    public void OnReceiveIDRequestServer(ref NetworkConnection conn, NetMessageBase m)
    {
        IDRequest msg = (IDRequest)m;
        msg.OwnerID = ++NetNode.CurrentControlID;
        _accounts[msg.OwnerID] = msg.UserName;
        _connIDToOwnerID[conn.InternalId] = msg.OwnerID;

        DataStreamWriter writer = NetMessageBase.ParseFrom(msg);
        conn.Send(NetNode._driver, writer);

        GroupMessage group = new GroupMessage();

        foreach (uint id in _accounts.Keys)
        {
            UserData ud = new UserData();
            ud.OwnerID = id;
            ud.UserName = _accounts[id];
            group.Messages.Add(ud);
        }
        
        if (group.Messages.Count > 0)
        {
            NetNode.SendMessage(group);
        }
        
        ChatManager.Instance.ShowConnectedUser(msg.UserName);
    }

    void OnReceiveIDRequestClient(ref NetworkConnection conn, NetMessageBase m)
    {
        IDRequest msg = (IDRequest)m;
        NetPlayerController.Instance.ControlID = msg.OwnerID;
    }

    void OnReceiveUserDataClient(ref NetworkConnection conn, NetMessageBase m)
    {
        UserData msg = (UserData)m;
        _accounts[msg.OwnerID] = msg.UserName;
        
        if (msg.OwnerID == NetPlayerController.Instance.ControlID && !_hasShowOwnUserData)
        {
            ChatManager.Instance.ShowConnectedUser("you", "are");
            _hasShowOwnUserData = true;
        }
        else if (msg.OwnerID != NetPlayerController.Instance.ControlID)
        {
            ChatManager.Instance.ShowConnectedUser(msg.UserName);
        }
    }
    
    void OnReceiveSpawnMessage(ref NetworkConnection conn, NetMessageBase m)
    {
        SpawnMessage msg = (SpawnMessage)m;
        AgentManager.Instance.SpawnAgent(msg.Type, msg.Owner, msg.AgentID, msg.Position, msg.Rotation);
    }

    void OnReceiveEnterGameMessageServer(ref NetworkConnection conn, NetMessageBase m)
    {
        EnterGameMessage msg = (EnterGameMessage)m;
        AgentType type = (AgentType)_players.Count;
        _players[msg.OwnerID] = type;
        
        if (_players.Count == 2)
        {
            NetServerController.Instance.ClientUI.SetActive(false);

            foreach (var player in _players)
            {
                AgentManager.Instance.SpawnAgent(player.Value, player.Key, 0, Vector3.zero, Quaternion.identity);
            }
            
            IAgent[] agents = AgentManager.Instance.GetAllAgents();
            GroupMessage group = new GroupMessage();

            foreach (IAgent agent in agents)
            {
                SpawnMessage sd = new SpawnMessage();
                sd.Position = agent.transform.position;
                sd.Rotation = agent.transform.rotation;
                sd.Type = agent.Type;
                sd.Owner = agent.OwnerID;
                sd.AgentID = agent.AgentID;

                group.Messages.Add(sd);
            }

            if (group.Messages.Count > 0)
            {
                NetNode.SendMessage(group);
            }
        }
        else
        {
            ChatMessage cmsg = new ChatMessage();
            cmsg.SenderID = msg.OwnerID;
            cmsg.Message = "<i>has start the game!</i>";
            NetNode.SendMessage(cmsg);
        }
    }

    void OnReceiveChatMessageServer(ref NetworkConnection conn, NetMessageBase m)
    {
        ChatMessage msg = (ChatMessage)m;
        NetNode.SendMessage(msg);
        ShowChat(msg);
    }

    void OnReceiveChatMessageClient(ref NetworkConnection conn, NetMessageBase m)
    {
        ChatMessage msg = (ChatMessage)m;
        ShowChat(msg);
    }

    void OnReceiveStateMessageServer(ref NetworkConnection conn, NetMessageBase m)
    {
        StateMessage msg = (StateMessage)m;
        NetNode.SendMessage(msg);
        MyGameManager.Instance.ReceiveStateMessage(msg);
    }

    void OnReceiveStateMessageClient(ref NetworkConnection conn, NetMessageBase m)
    {
        StateMessage msg = (StateMessage)m;
        MyGameManager.Instance.ReceiveStateMessage(msg);
    }

    void OnReceiveTimeMessageServer(ref NetworkConnection conn, NetMessageBase m)
    {
        TimeMessage msg = (TimeMessage)m;
        NetNode.SendMessage(msg);
        MyGameManager.Instance.TimeCD = msg.Time;
    }

    void OnReceiveTimeMessageClient(ref NetworkConnection conn, NetMessageBase m)
    {
        TimeMessage msg = (TimeMessage)m;
        MyGameManager.Instance.TimeCD = msg.Time;
    }

    void OnReceiveScoreMessageServer(ref NetworkConnection conn, NetMessageBase m)
    {
        ScoreMessage msg = (ScoreMessage)m;
        NetNode.SendMessage(msg);
        MyGameManager.Instance.Score = msg.Score;
    }

    void OnReceiveScoreMessageClient(ref NetworkConnection conn, NetMessageBase m)
    {
        ScoreMessage msg = (ScoreMessage)m;
        MyGameManager.Instance.Score = msg.Score;
    }

    void OnReceivePingMessage(ref NetworkConnection conn, NetMessageBase m)
    {
        PingMessage msg = (PingMessage)m;
    }

    void OnReceiveDeleteMessageServer(ref NetworkConnection conn, NetMessageBase m)
    {
        int id = conn.InternalId;
        uint ownerid = _connIDToOwnerID[id];
        uint[] deletedAgentIds = AgentManager.Instance.DeleteAgentByOwner(ownerid);

        // create group message
        // iterate deletedAgentIds to create a deletemessage
        // send to all
        GroupMessage group = new GroupMessage();

        foreach (uint agentid in deletedAgentIds)
        {
            DeleteMessage msg = new DeleteMessage();
            msg.OwnerID = ownerid;
            msg.AgentID = agentid;
            group.Messages.Add(msg);
        }

        if (group.Messages.Count > 0)
        {
            NetNode.SendMessage(group);
        }
    }

    void OnReceiveDeleteMessageClient(ref NetworkConnection conn, NetMessageBase m)
    {
        if (m == null)
            return;

        DeleteMessage msg = (DeleteMessage)m;
        uint[] deletedAgentIds = AgentManager.Instance.DeleteAgentByOwner(msg.OwnerID);
    }

    void ShowChat(ChatMessage msg)
    {
        if (NetPlayerController.Instance.ControlID == msg.SenderID)
        {
            ChatManager.Instance.ReceiveChat(msg, "you");
            return;
        }

        foreach (uint id in _accounts.Keys)
        {
            if (id == msg.SenderID)
            {
                ChatManager.Instance.ReceiveChat(msg, _accounts[id]);
                break;
            }
        }
    }

    IEnumerator UpdateSyncData()
    {
        while (true)
        {
            SyncData();
            yield return new WaitForSecondsRealtime(SyncInterval);
        }
    }

    public void SyncData()
    {
        IAgent[] agents = AgentManager.Instance.GetAllAgents(true);
        GroupMessage group = new GroupMessage();

        foreach (IAgent agent in agents)
        {
            NetMessageBase msg = agent.GetSyncData();
            group.Messages.Add(msg);
        }

        if (group.Messages.Count > 0)
        {
            NetNode.SendMessage(group);
        }
    }

    IEnumerator UpdatePingData()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(PingInterval);
            PingData();
        }
    }

    public void PingData()
    {
        PingMessage msg = new PingMessage();
        msg.OwnerID = NetPlayerController.Instance.ControlID;
        NetNode.SendMessage(msg);
    }

    public void StartConnection()
    {
        INetManager net = null;

        switch (Type)
        {
            case NetworkNodeType.Server:
                {
                    net = gameObject.AddComponent<NetServer>();
                    break;
                }

            case NetworkNodeType.Client:
                {
                    net = this.gameObject.AddComponent<NetClient>();
                    break;
                }

            default:
                throw new System.Exception("Network node not supported. " + Type);
        }

        NetNode = net;
        net.IP = IP;
        net.Port = Port;

        if (Type == NetworkNodeType.Server)
        {
            NetNode.RegisterCallback((ushort)NetMessageType.IDRequest, OnReceiveIDRequestServer);
            NetNode.RegisterCallback((ushort)NetMessageType.EnterGameMessage, OnReceiveEnterGameMessageServer);
            NetNode.RegisterCallback((ushort)NetMessageType.ChatMessage, OnReceiveChatMessageServer);
            NetNode.RegisterCallback((ushort)NetMessageType.StateMessage, OnReceiveStateMessageServer);
            NetNode.RegisterCallback((ushort)NetMessageType.TimeMessage, OnReceiveTimeMessageServer);
            NetNode.RegisterCallback((ushort)NetMessageType.ScoreMessage, OnReceiveScoreMessageServer);
            NetNode.RegisterCallback((ushort)NetMessageType.DeleteMessage, OnReceiveDeleteMessageServer);
        }
        else
        {
            NetNode.RegisterCallback((ushort)NetMessageType.IDRequest, OnReceiveIDRequestClient);
            NetNode.RegisterCallback((ushort)NetMessageType.ChatMessage, OnReceiveChatMessageClient);
            NetNode.RegisterCallback((ushort)NetMessageType.UserData, OnReceiveUserDataClient);
            NetNode.RegisterCallback((ushort)NetMessageType.StateMessage, OnReceiveStateMessageClient);
            NetNode.RegisterCallback((ushort)NetMessageType.TimeMessage, OnReceiveTimeMessageClient);
            NetNode.RegisterCallback((ushort)NetMessageType.ScoreMessage, OnReceiveScoreMessageClient);
            NetNode.RegisterCallback((ushort)NetMessageType.DeleteMessage, OnReceiveDeleteMessageClient);

            StartCoroutine(UpdatePingData());
        }

        NetNode.RegisterCallback((ushort)NetMessageType.SpawnMessage, OnReceiveSpawnMessage);
        NetNode.RegisterCallback((ushort)NetMessageType.GroupMessage, OnReceiveGroupMessage);
        NetNode.RegisterCallback((ushort)NetMessageType.CharacterSyncMessage, OnReceiveCharacterSyncMessage);
        NetNode.RegisterCallback((ushort)NetMessageType.DrawingSyncMessage, OnReceiveDrawingSyncMessage);
        NetNode.RegisterCallback((ushort)NetMessageType.PlatformSyncMessage, OnReceivePlatformSyncMessage);
        NetNode.RegisterCallback((ushort)NetMessageType.PingMessage, OnReceivePingMessage);

        NetNode.Connect();

        StartCoroutine(UpdateSyncData());

        ServerClientUI.SetActive(false);
    }
}
