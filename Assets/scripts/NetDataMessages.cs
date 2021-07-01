using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public enum NetMessageType : ushort
{
    SpawnMessage = 1,
    IDRequest,
    EnterGameMessage,
    CharacterSyncMessage,
    DrawingSyncMessage,
    PlatformSyncMessage,
    GroupMessage,
    ChatMessage,
    UserData,
    StateMessage,
    TimeMessage,
    ScoreMessage,
    DeleteMessage,
    PingMessage,
}

public class SpawnMessage : NetMessageBase
{
    public override ushort GetFlag() { return (ushort)NetMessageType.SpawnMessage; }

    public AgentType Type;
    public uint Owner;
    public uint AgentID;
    public Vector3 Position;
    public Quaternion Rotation;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(byte);
        size += sizeof(uint);
        size += sizeof(uint);
        size += Position.SizeOf();
        size += Rotation.SizeOf();
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write((byte)Type);
        writer.Write(Owner);
        writer.Write(AgentID);
        writer.Write(Position);
        writer.Write(Rotation);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        Type = (AgentType)reader.ReadByte(ref context);
        Owner = reader.ReadUInt(ref context);
        AgentID = reader.ReadUInt(ref context);
        Position = reader.ReadVector3(ref context);
        Rotation = reader.ReadQuaternion(ref context);
    }
}

public class IDRequest : NetMessageBase
{
    public override ushort GetFlag()
    {
        return (ushort)NetMessageType.IDRequest;
    }
    
    public uint OwnerID;
    public string UserName;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        size += sizeof(int);
        size += UserName.SizeOf();
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(OwnerID);
        writer.Write(UserName);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        OwnerID = reader.ReadUInt(ref context);
        UserName = reader.ReadString(ref context);
    }
}

public class EnterGameMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return (ushort)NetMessageType.EnterGameMessage;
    }
    
    public uint OwnerID;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(OwnerID);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        OwnerID = reader.ReadUInt(ref context);
    }
}

public class CharacterSyncMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return ((ushort)NetMessageType.CharacterSyncMessage);
    }

    public uint OwnerID;
    public uint AgentID;
    public Vector3 Position;
    public float RotationY;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        size += sizeof(uint);
        size += ((Vector2)Position).SizeOf();
        size += sizeof(float);
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(OwnerID);
        writer.Write(AgentID);
        writer.Write((Vector2)Position);
        writer.Write(RotationY);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        OwnerID = reader.ReadUInt(ref context);
        AgentID = reader.ReadUInt(ref context);
        Position = reader.ReadVector2(ref context);
        RotationY = reader.ReadFloat(ref context);
    }
}

public class DrawingSyncMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return ((ushort)NetMessageType.DrawingSyncMessage);
    }

    public uint OwnerID;
    public uint AgentID;
    public Vector2 StartMousePos;
    public Vector2 EndMousePos;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        size += sizeof(uint);
        size += StartMousePos.SizeOf();
        size += EndMousePos.SizeOf();
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(OwnerID);
        writer.Write(AgentID);
        writer.Write(StartMousePos);
        writer.Write(EndMousePos);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        OwnerID = reader.ReadUInt(ref context);
        AgentID = reader.ReadUInt(ref context);
        StartMousePos = reader.ReadVector2(ref context);
        EndMousePos = reader.ReadVector2(ref context);
    }
}

public class PlatformSyncMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return ((ushort)NetMessageType.PlatformSyncMessage);
    }

    public uint OwnerID;
    public uint AgentID;
    public Vector3 Position;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        size += sizeof(uint);
        size += ((Vector2)Position).SizeOf();
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(OwnerID);
        writer.Write(AgentID);
        writer.Write((Vector2)Position);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        OwnerID = reader.ReadUInt(ref context);
        AgentID = reader.ReadUInt(ref context);
        Position = reader.ReadVector2(ref context);
    }
}

public class GroupMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return ((ushort)NetMessageType.GroupMessage);
    }

    public List<NetMessageBase> Messages = new List<NetMessageBase>();

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(int);
        
        foreach (NetMessageBase msg in Messages)
        {
            size += msg.GetSize();
        }
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(Messages.Count);
        
        foreach (NetMessageBase msg in Messages)
        {
            msg.Serialize(writer);
        }
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        int count = reader.ReadInt(ref context);
        Messages = new List<NetMessageBase>(count);
        DataStreamReader.Context groupMsgContext = context;

        for (int i = 0; i < count; i++)
        {
            DataStreamReader.Context msgContext = groupMsgContext;
            NetMessageType type = (NetMessageType)reader.ReadUShort(ref msgContext);
            NetMessageBase msg = null;

            switch (type)
            {
                case NetMessageType.SpawnMessage:
                    msg = NetMessageBase.Parse<SpawnMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.EnterGameMessage:
                    msg = NetMessageBase.Parse<EnterGameMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.CharacterSyncMessage:
                    msg = NetMessageBase.Parse<CharacterSyncMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.DrawingSyncMessage:
                    msg = NetMessageBase.Parse<DrawingSyncMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.PlatformSyncMessage:
                    msg = NetMessageBase.Parse<PlatformSyncMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.ChatMessage:
                    msg = NetMessageBase.Parse<ChatMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.UserData:
                    msg = NetMessageBase.Parse<UserData>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.StateMessage:
                    msg = NetMessageBase.Parse<StateMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.TimeMessage:
                    msg = NetMessageBase.Parse<TimeMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.ScoreMessage:
                    msg = NetMessageBase.Parse<ScoreMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.DeleteMessage:
                    msg = NetMessageBase.Parse<DeleteMessage>(reader, ref groupMsgContext);
                    break;
                case NetMessageType.PingMessage:
                    msg = NetMessageBase.Parse<PingMessage>(reader, ref groupMsgContext);
                    break;
                default:
                    throw new System.Exception(type + " is not supported in group message.");
            }
            
            if (msg != null)
            {
                Messages.Add(msg);
            }
        }

        context = groupMsgContext;
    }
}

public class ChatMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return (ushort)NetMessageType.ChatMessage;
    }
    
    public uint SenderID;
    public string Message;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        size += sizeof(int);
        size += Message.SizeOf();

        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(SenderID);
        writer.Write(Message);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        SenderID = reader.ReadUInt(ref context);
        Message = reader.ReadString(ref context);
    }
}

public class UserData : NetMessageBase
{
    public override ushort GetFlag()
    {
        return (ushort)NetMessageType.UserData;
    }

    public uint OwnerID;
    public string UserName;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        size += sizeof(int);
        size += UserName.SizeOf();

        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(OwnerID);
        writer.Write(UserName);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        OwnerID = reader.ReadUInt(ref context);
        UserName = reader.ReadString(ref context);
    }
}

public class StateMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return ((ushort)NetMessageType.StateMessage);
    }
    
    public AgentState State;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(byte);
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write((byte)State);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        State = (AgentState)reader.ReadByte(ref context);
    }
}

public class TimeMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return ((ushort)NetMessageType.TimeMessage);
    }
    
    public float Time;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(int);
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write((int)Time);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        Time = reader.ReadInt(ref context);
    }
}

public class ScoreMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return ((ushort)NetMessageType.ScoreMessage);
    }
    
    public int Score;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(int);
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(Score);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        Score = reader.ReadInt(ref context);
    }
}

public class DeleteMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return (ushort)NetMessageType.DeleteMessage;
    }
    
    public uint OwnerID;
    public uint AgentID;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        size += sizeof(uint);
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(OwnerID);
        writer.Write(AgentID);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        OwnerID = reader.ReadUInt(ref context);
        AgentID = reader.ReadUInt(ref context);
    }
}

public class PingMessage : NetMessageBase
{
    public override ushort GetFlag()
    {
        return (ushort)NetMessageType.PingMessage;
    }
    
    public uint OwnerID;

    public override int GetSize()
    {
        int size = base.GetSize();
        size += sizeof(uint);
        return size;
    }

    public override void Serialize(DataStreamWriter writer)
    {
        base.Serialize(writer);
        writer.Write(OwnerID);
    }

    public override void Deserialize(DataStreamReader reader, ref DataStreamReader.Context context)
    {
        base.Deserialize(reader, ref context);
        OwnerID = reader.ReadUInt(ref context);
    }
}