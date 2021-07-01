using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using Unity.Collections;

public abstract class INetManager : MonoBehaviour
{
    public string IP;
    public ushort Port;
    public uint CurrentControlID;
    
    protected Dictionary<ushort, NetEvent> _messageEvent = new Dictionary<ushort, NetEvent>();
    public UdpNetworkDriver _driver;
    protected NativeList<NetworkConnection> _connections;
    
    public virtual void Initialize()
    {
        _connections = new NativeList<NetworkConnection>(Allocator.Persistent);
        _driver = new UdpNetworkDriver(new ReliableUtility.Parameters { WindowSize = 32 });
    }

    public virtual void ShutDown()
    {
        _connections.Dispose();
        _driver.Dispose();
    }
    
    public abstract void Connect();

    public abstract NetworkNodeType NodeType { get; }

    public void UpdateManager()
    {
        _driver.ScheduleUpdate().Complete();

        NetworkConnection conn;
        do
        {
            conn = _driver.Accept();
            if (conn.IsCreated)
                _connections.Add(conn);

        } while (conn.IsCreated);

        NetworkEvent.Type eventType;

        do
        {
            DataStreamReader reader;
            DataStreamReader.Context context = new DataStreamReader.Context();
            eventType = _driver.PopEvent(out conn, out reader);

            switch (eventType)
            {
                case NetworkEvent.Type.Data:
                    {
                        ushort flag = reader.ReadUShort(ref context);
                        ExecuteFlagToCallback(flag, ref conn, ref reader, ref context);
                        break;
                    }

                case NetworkEvent.Type.Disconnect:
                    {
                        int connid = conn.InternalId;

                        for (int i = 0; i < _connections.Length; i++)
                        {
                            NetworkConnection c = _connections[i];
                            if (connid == c.InternalId)
                            {
                                _connections.RemoveAtSwapBack(connid);

                                NetEvent netEvent;
                                if (_messageEvent.TryGetValue((ushort)NetMessageType.DeleteMessage, out netEvent))
                                {
                                    netEvent(ref c, null);
                                }

                                break;
                            }
                        }

                        break;
                    }
            }
        } while (eventType != NetworkEvent.Type.Empty);
    }
    
    public void ExecuteFlagToCallback(ushort flag, ref NetworkConnection conx, ref DataStreamReader reader, ref DataStreamReader.Context context)
    {
        NetEvent netEvent;
        if (_messageEvent.TryGetValue(flag, out netEvent))
        {
            DataStreamReader.Context msgContext = new DataStreamReader.Context();
            NetMessageBase msg = NetMessageBase.Create((NetMessageType)flag);
            msg.Deserialize(reader, ref msgContext);
            netEvent(ref conx, msg);
        }
    }

    public void ExecuteFlagToCallback(NetMessageBase msg, ref NetworkConnection conx)
    {
        ushort flag = msg.GetFlag();
        NetEvent netEvent;
        if (_messageEvent.TryGetValue(flag, out netEvent))
        {
            netEvent(ref conx, msg);
        }
    }
    
    public void RegisterCallback(ushort flag, NetEvent netEvent)
    {
        NetEvent existing = null;
        if (_messageEvent.TryGetValue(flag, out existing))
        {
            existing += netEvent;
        }
        else
        {
            existing = netEvent;
        }

        _messageEvent[flag] = existing;
    }

    public void UnregisterCallback(ushort flag, NetEvent netEvent)
    {
        NetEvent existing = null;
        if (_messageEvent.TryGetValue(flag, out existing))
        {
            existing -= netEvent;
        }

        if (existing == null)
        {
            _messageEvent.Remove(flag);
        }
    }

    public void SendMessage(NetMessageBase msg)
    {
        DataStreamWriter writer = NetMessageBase.ParseFrom(msg);
        foreach (NetworkConnection conn in _connections.ToArray())
        {
            conn.Send(_driver, writer);
        }
    }
}

public delegate void NetEvent(ref NetworkConnection conn, NetMessageBase msg);

public enum NetworkNodeType
{
    Server,
    Client
}

public abstract class NetMessageBase
{
    public abstract ushort GetFlag();

    public virtual void Serialize(DataStreamWriter writer)
    {
        ushort flag = GetFlag();
        writer.Write(flag);
    }

    public virtual void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        ushort flag = reader.ReadUShort(ref context);

        if (flag != GetFlag())
            throw new System.Exception(string.Format("Message with {0} flag is incompatible with {1}.", GetFlag(), flag));
    }
    
    public virtual int GetSize()
    {
        return sizeof(ushort);
    }
    
    public static T Parse<T>(DataStreamReader reader, ref DataStreamReader.Context context) where T : NetMessageBase, new()
    {
        T msg = new T();
        msg.Deserialize(reader, ref context);
        return msg;
    }
    
    public static DataStreamWriter ParseFrom(NetMessageBase msg)
    {
        int size = msg.GetSize();
        DataStreamWriter writer = new DataStreamWriter(size, Allocator.Temp);
        msg.Serialize(writer);
        return writer;
    }

    public static NetMessageBase Create(NetMessageType flag)
    {
        switch (flag)
        {
            case NetMessageType.SpawnMessage:
                return new SpawnMessage();
            case NetMessageType.IDRequest:
                return new IDRequest();
            case NetMessageType.EnterGameMessage:
                return new EnterGameMessage();
            case NetMessageType.CharacterSyncMessage:
                return new CharacterSyncMessage();
            case NetMessageType.DrawingSyncMessage:
                return new DrawingSyncMessage();
            case NetMessageType.PlatformSyncMessage:
                return new PlatformSyncMessage();
            case NetMessageType.GroupMessage:
                return new GroupMessage();
            case NetMessageType.ChatMessage:
                return new ChatMessage();
            case NetMessageType.UserData:
                return new UserData();
            case NetMessageType.StateMessage:
                return new StateMessage();
            case NetMessageType.TimeMessage:
                return new TimeMessage();
            case NetMessageType.ScoreMessage:
                return new ScoreMessage();
            case NetMessageType.DeleteMessage:
                return new DeleteMessage();
            case NetMessageType.PingMessage:
                return new PingMessage();
            default:
                return null;
        }
    }
}