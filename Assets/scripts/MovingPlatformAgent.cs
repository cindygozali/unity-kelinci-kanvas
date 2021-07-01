using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformAgent : IAgent
{
    [Header("Movement")]
    public float MinPos = 15f;
    public float MaxPos = 1f;
    public float Speed = 2f;
    int _direction = 1;

    [Header("Ray")]
    public Vector2 MinRay;
    public Vector2 MaxRay;
    public float RayLenght = 3.5f;
    
    public LayerMask WhatIsGround;

    PlatformSyncMessage _syncMessage = new PlatformSyncMessage();
    Vector3 _prevPosition;
    Vector3 _targetPosition;
    public float SyncFactor = 2;

    public override bool NeedToSync
    {
        get
        {
            if (!ForceSync)
                return false;

            bool sync = _prevPosition != transform.position;

            return sync;
        }
    }

    public override void ApplySyncData(NetMessageBase msg)
    {
        if (OwnerID == NetPlayerController.Instance.ControlID)
            return;

        if (msg is PlatformSyncMessage)
        {
            PlatformSyncMessage pmsg = (PlatformSyncMessage)msg;
            
            _targetPosition = pmsg.Position;
        }
    }

    public override NetMessageBase GetSyncData()
    {
        _syncMessage.OwnerID = OwnerID;
        _syncMessage.AgentID = AgentID;
        _syncMessage.Position = transform.position;

        _prevPosition = _syncMessage.Position;

        return _syncMessage;
    }

    void Awake()
    {
        Core.SubscribeEvent("RestartGame", OnRestartGame);
    }

    private void OnDestroy()
    {
        Core.UnSubscribeEvent("RestartGame", OnRestartGame);
    }

    // Start is called before the first frame update
    void Start()
    {
        bool isOwner = OwnerID == NetPlayerController.Instance.ControlID;
        
        if (!ForceSync)
        {
            ForceSync = isOwner;
        }

        _targetPosition = _prevPosition = transform.position = new Vector2(transform.position.x, MinPos);
    }

    // Update is called once per frame
    void Update()
    {
        if (OwnerID != NetPlayerController.Instance.ControlID)
        {
            Vector3 currentPos = transform.position;
            Vector3 newPos = Vector3.Lerp(currentPos, _targetPosition, Time.deltaTime * SyncFactor);
            transform.position = newPos;

            return;
        }

        if (MyGameManager.Instance.State != AgentState.StartGame)
            return;

        float posY = transform.position.y;

        if (posY >= MinPos)
        {
            _direction = -1;
        }
        else if (posY <= MaxPos)
        {
            _direction = 1;
        }

        if (IsHitGround())
        {
            _direction = 1;
        }

        transform.position += Vector3.up * _direction * Speed * Time.deltaTime;
    }

    public bool IsHitGround()
    {
        Vector2 currentPos = transform.position;

        return Physics2D.Raycast(currentPos + MinRay, -transform.up, RayLenght, WhatIsGround)
            && Physics2D.Raycast(currentPos + MaxRay, -transform.up, RayLenght, WhatIsGround);
    }

    private void OnDrawGizmos()
    {
        Vector2 currentPos = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(currentPos + MinRay, currentPos + MinRay - (Vector2)transform.up * RayLenght);
        Gizmos.DrawLine(currentPos + MaxRay, currentPos + MaxRay - (Vector2)transform.up * RayLenght);
    }

    void OnRestartGame(object sender, object[] args)
    {
        _targetPosition = transform.position = new Vector2(transform.position.x, MinPos);
    }
}
