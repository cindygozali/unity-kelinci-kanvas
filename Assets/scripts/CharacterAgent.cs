using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class CharacterAgent : IAgent
{
    public float ForceFactor = 100;
    public float JumpForce = 1000f;
    public LayerMask WhatIsGround;

    public int Score;

    [Header("Clamp")]
    public Vector2 ClampPosMin = new Vector2(0, 0);
    public Vector2 ClampPosMax = new Vector2(0, 0);

    [Header("Sfx")]
    public AudioClip JumpSfx;
    public AudioClip WinSFX,
        LoseSFX,
        CoinSFX;
    bool _hasCalledlWinLoseFunction;
    bool _hasCalledRestartFunction;

    [Header("Face")]
    public SkinnedMeshRenderer FaceMesh;
    public Material NormalFaceMat,
        WinFaceMat,
        LoseFaceMat;

    Vector2 _rayDiff = new Vector2(0.2f, 0);

    Rigidbody2D _rigid;
    Animator _anim;

    Camera _camera;

    CharacterSyncMessage _syncMessage = new CharacterSyncMessage();
    Vector3 _prevPosition;
    Vector3 _targetPosition;
    float _prevRotationY;
    public float SyncFactor = 2;

    public override bool NeedToSync
    {
        get
        {
            if (!ForceSync)
                return false;

            bool sync = _prevPosition != transform.position
                || _prevRotationY != transform.eulerAngles.y;

            return sync;
        }
    }
    
    public override void ApplySyncData(NetMessageBase msg)
    {
        if (OwnerID == NetPlayerController.Instance.ControlID)
            return;

        if (msg is CharacterSyncMessage)
        {
            CharacterSyncMessage pmsg = (CharacterSyncMessage)msg;

            _targetPosition = pmsg.Position;
            transform.eulerAngles = new Vector3(0, pmsg.RotationY, 0);
        }
    }

    public override NetMessageBase GetSyncData()
    {
        Vector3 currentPos = transform.position;
        float currentRotY = transform.eulerAngles.y;

        _syncMessage.OwnerID = OwnerID;
        _syncMessage.AgentID = AgentID;
        _syncMessage.Position = currentPos;
        _syncMessage.RotationY = currentRotY;

        _prevPosition = currentPos;
        _prevRotationY = currentRotY;

        return _syncMessage;
    }
    
    void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _camera = Camera.main;

        Core.SubscribeEvent("RestartGame", OnRestartGame);
    }

    private void OnDestroy()
    {
        Core.UnSubscribeEvent("RestartGame", OnRestartGame);
    }

    private void Start()
    {
        bool isOwner = OwnerID == NetPlayerController.Instance.ControlID;
        
        if (!ForceSync)
        {
            ForceSync = isOwner;
        }

        if (!isOwner)
        {
            _rigid.isKinematic = true;
        }

        _targetPosition = _prevPosition = transform.position;
        _prevRotationY = transform.eulerAngles.y;
        
        CameraManager.Instance.GetComponent<CinemachineVirtualCamera>().m_Follow = gameObject.transform;
    }

    // Update is called once per frame
    void Update()
    {
        _anim.SetFloat("speed", Mathf.Abs(_rigid.velocity.x));

        if (!_hasCalledlWinLoseFunction)
        {
            if (MyGameManager.Instance.State == AgentState.Win)
            {
                FaceMesh.material = WinFaceMat;
                _anim.SetBool("win", true);
                PlayAudio(WinSFX, 25f);
                _hasCalledlWinLoseFunction = true;
            }
            else if (MyGameManager.Instance.State == AgentState.Lose)
            {
                FaceMesh.material = LoseFaceMat;
                _anim.SetBool("lose", true);
                PlayAudio(LoseSFX, 20f);
                _hasCalledlWinLoseFunction = true;
            }
        }

        if (!_hasCalledRestartFunction)
        {
            if (MyGameManager.Instance.State != AgentState.Win && MyGameManager.Instance.State != AgentState.Lose)
            {
                FaceMesh.material = NormalFaceMat;
                _anim.SetBool("win", false);
                _anim.SetBool("lose", false);
                _hasCalledRestartFunction = true;
            }
        }

        if (OwnerID != NetPlayerController.Instance.ControlID)
        {
            Vector3 ghostCurrentPos = transform.position;
            Vector3 newPos = Vector3.Lerp(ghostCurrentPos, _targetPosition, Time.deltaTime * SyncFactor);
            transform.position = newPos;

            return;
        }
        
        if (MyGameManager.Instance.State != AgentState.StartGame)
        {
            return;
        }

        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, ClampPosMin.x, ClampPosMax.x);
        currentPos.y = Mathf.Clamp(currentPos.y, ClampPosMin.y, ClampPosMax.y);
        transform.position = currentPos;

        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }

        float x_axis = Input.GetAxis("Horizontal");
        
        Vector2 force = new Vector2(x_axis, 0f);

        if (force != Vector2.zero)
        {
            _rigid.AddForce(force * ForceFactor * Time.deltaTime);

            float lookAt = x_axis > 0 ? 0 : 180;
            transform.eulerAngles = new Vector3(0, lookAt, 0);
        }
    }

    public void Jump()
    {
        if (IsOnGround())
        {
            _anim.SetTrigger("jump");
            PlayAudio(JumpSfx, 6f);

            Vector2 jumpForce = new Vector2(0f, JumpForce);
            _rigid.AddForce(jumpForce);
        }
    }

    public bool IsOnGround()
    {
        Vector2 currentPos = transform.position;

        return Physics2D.Raycast(currentPos, -transform.up, 0.5f, WhatIsGround)
            || Physics2D.Raycast(currentPos + _rayDiff, -transform.up, 0.5f, WhatIsGround)
            || Physics2D.Raycast(currentPos - _rayDiff, -transform.up, 0.5f, WhatIsGround);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        bool isOwner = OwnerID == NetPlayerController.Instance.ControlID;

        GameObject other = collision.gameObject;

        if (other.CompareTag("Coin"))
        {
            other.SetActive(false);
            PlayAudio(CoinSFX);

            if (!isOwner)
                return;

            Score++;
            SendScoreMessage();
        }

        if (!isOwner)
            return;

        if (other.CompareTag("Finish"))
        {
            SendStateMessage(AgentState.Win);
        }

        if (other.CompareTag("Spike") || other.CompareTag("Obstacle"))
        {
            SendStateMessage(AgentState.Lose);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        Vector3 min = Vector3.Min(ClampPosMin, ClampPosMax);
        Vector3 max = Vector3.Max(ClampPosMin, ClampPosMax);

        Vector3 minXMinY = new Vector3(min.x, min.y);
        Vector3 minXMaxY = new Vector3(min.x, max.y);
        Vector3 maxXMinY = new Vector3(max.x, min.y);
        Vector3 maxXMaxY = new Vector3(max.x, max.y);

        Gizmos.DrawLine(minXMinY, minXMaxY);
        Gizmos.DrawLine(minXMinY, maxXMinY);
        Gizmos.DrawLine(maxXMaxY, minXMaxY);
        Gizmos.DrawLine(maxXMaxY, maxXMinY);
    }

    void SendStateMessage(AgentState state)
    {
        StateMessage msg = new StateMessage();
        msg.State = state;
        NetManager.Instance.NetNode.SendMessage(msg);
    }

    void SendScoreMessage()
    {
        ScoreMessage msg = new ScoreMessage();
        msg.Score = Score;
        NetManager.Instance.NetNode.SendMessage(msg);
    }

    void PlayAudio(AudioClip clip, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(clip, _camera.transform.position, volume);
    }

    void OnRestartGame(object sender, object[] args)
    {
        _hasCalledlWinLoseFunction = false;
        _hasCalledRestartFunction = false;

        if (OwnerID != NetPlayerController.Instance.ControlID)
            return;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        Score = 0;
        SendScoreMessage();
    }
}
