using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetClient : INetManager
{
    public override NetworkNodeType NodeType { get { return NetworkNodeType.Client; } }

    public override void Connect()
    {
        NetworkEndPoint address = NetworkEndPoint.Parse(IP, Port);
        NetworkConnection conn = _driver.Connect(address);
        if (conn.IsCreated)
        {
            _connections.Add(conn);
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        ShutDown();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateManager();
    }
}
