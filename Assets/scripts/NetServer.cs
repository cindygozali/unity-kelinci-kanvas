using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetServer : INetManager
{
    public override NetworkNodeType NodeType { get { return NetworkNodeType.Server; } }

    public override void Connect()
    {
        NetworkEndPoint address = NetworkEndPoint.AnyIpv4;
        address.Port = Port;
        if (_driver.Bind(address) != 0)
        {
            // TODO
        }
        else
        {
            _driver.Listen();
            // TODOposition
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

    private void Start()
    {

    }

    private void Update()
    {
        UpdateManager();
    }
}