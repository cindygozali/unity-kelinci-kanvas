using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using System.Text;

public static partial class Core
{
    public static int SizeOf(this Vector3 v)
    {
        return sizeof(float) * 3;
    }

    public static int SizeOf(this Vector2 v)
    {
        return sizeof(float) * 2;
    }

    public static int SizeOf(this Quaternion q)
    {
        return sizeof(float) * 4;
    }

    public static int SizeOf(this string s)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(s);
        return buffer.Length;
    }
    
    public static void Write(this DataStreamWriter writer, Vector3 v)
    {
        writer.Write(v.x);
        writer.Write(v.y);
        writer.Write(v.z);
    }

    public static void Write(this DataStreamWriter writer, Vector2 v)
    {
        writer.Write(v.x);
        writer.Write(v.y);
    }

    public static void Write(this DataStreamWriter writer, Quaternion q)
    {
        writer.Write(q.x);
        writer.Write(q.y);
        writer.Write(q.z);
        writer.Write(q.w);
    }

    public static void Write(this DataStreamWriter writer, string s)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(s);
        writer.Write(buffer.Length);
        writer.Write(buffer);
    }

    public static Vector3 ReadVector3(this DataStreamReader reader, ref DataStreamReader.Context context)
    {
        Vector3 v = new Vector3(
                    reader.ReadFloat(ref context),
                    reader.ReadFloat(ref context),
                    reader.ReadFloat(ref context));
        return v;
    }

    public static Vector2 ReadVector2(this DataStreamReader reader, ref DataStreamReader.Context context)
    {
        Vector2 v = new Vector2(
                    reader.ReadFloat(ref context),
                    reader.ReadFloat(ref context));
        return v;
    }

    public static Quaternion ReadQuaternion(this DataStreamReader reader, ref DataStreamReader.Context context)
    {
        Quaternion q = new Quaternion(
                    reader.ReadFloat(ref context),
                    reader.ReadFloat(ref context),
                    reader.ReadFloat(ref context),
                    reader.ReadFloat(ref context));
        return q;
    }

    public static string ReadString(this DataStreamReader reader, ref DataStreamReader.Context context)
    {
        int count = reader.ReadInt(ref context);
        byte[] buffer = reader.ReadBytesAsArray(ref context, count);
        return Encoding.ASCII.GetString(buffer);
    }
}