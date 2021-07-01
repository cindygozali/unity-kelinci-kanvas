using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class DrawingAgent : IAgent
{
    Camera _camera;
    LineRenderer _lineRenderer;
    EdgeCollider2D _edgeCollider2D;
    List<Vector2> _points = new List<Vector2>();

    public float MaxLenghtLine = 10f;
    float _lineZ = -8f;
    Color _lineColor;
    
    public AudioClip DrawSFX;
    
    DrawingSyncMessage _syncMessage = new DrawingSyncMessage();
    Vector3 _syncStartMousePos;
    Vector3 _prevStartMousePos;
    Vector3 _syncEndMousePos;
    Vector3 _prevEndMousePos;

    Vector3 _startMousePos;
    Vector3 _currentMousePos;

    public override bool NeedToSync
    {
        get
        {
            if (!ForceSync)
                return false;

            bool sync = _prevStartMousePos != _syncStartMousePos
                || _prevEndMousePos != _syncEndMousePos;

            return sync;
        }
    }
    
    public override void ApplySyncData(NetMessageBase msg)
    {
        if (OwnerID == NetPlayerController.Instance.ControlID)
            return;
        
        if (msg is DrawingSyncMessage)
        {
            DrawingSyncMessage dmsg = (DrawingSyncMessage)msg;

            _syncStartMousePos = dmsg.StartMousePos;
            _syncEndMousePos = dmsg.EndMousePos;

            if (_syncStartMousePos != Vector3.zero)
            {
                PlayAudio(DrawSFX);
            }

            _syncStartMousePos.z = _lineZ;
            _syncEndMousePos.z = _lineZ;

            _lineRenderer.SetPosition(0, _syncStartMousePos);
            _lineRenderer.SetPosition(1, _syncEndMousePos);

            _points[0] = _syncStartMousePos;
            _points[1] = _syncEndMousePos;
            _edgeCollider2D.points = _points.ToArray();
        }
    }

    public override NetMessageBase GetSyncData()
    {
        _syncMessage.OwnerID = OwnerID;
        _syncMessage.AgentID = AgentID;
        _syncMessage.StartMousePos = _syncStartMousePos;
        _syncMessage.EndMousePos = _syncEndMousePos;

        _prevStartMousePos = _syncStartMousePos;
        _prevEndMousePos = _syncEndMousePos;

        return _syncMessage;
    }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, Vector3.zero);
        _lineRenderer.SetPosition(1, Vector3.zero);
        _lineRenderer.endWidth = _lineRenderer.startWidth = 0.4f;
        _lineColor = _lineRenderer.material.color;

        _edgeCollider2D = GetComponent<EdgeCollider2D>();
        _points.Add(Vector2.zero);
        _points.Add(Vector2.zero);
        _edgeCollider2D.points = _points.ToArray();

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

        if (isOwner)
        {
            StateMessage msg = new StateMessage();
            msg.State = AgentState.CountDown;
            NetManager.Instance.NetNode.SendMessage(msg);
        }
    }

    private void Update()
    {
        if (OwnerID != NetPlayerController.Instance.ControlID)
            return;
        
        if (MyGameManager.Instance.State != AgentState.StartGame)
            return;

        if (!IsPointerOverUIObject(Input.mousePosition))
        {
            if (Input.GetMouseButtonDown(0))
            {
                Restart();

                ChangeLineRendererAlpha(0.5f);

                _startMousePos = GetMousePos();
                _startMousePos.z = _lineZ;
                _lineRenderer.SetPosition(0, _startMousePos);
            }

            if (Input.GetMouseButton(0))
            {
                _currentMousePos = GetMousePos();
                _currentMousePos.z = _lineZ;

                Vector3 dir = _currentMousePos - _startMousePos;
                float dist = Mathf.Clamp(Vector3.Distance(_startMousePos, _currentMousePos), 0, MaxLenghtLine);
                _currentMousePos = _startMousePos + (dir.normalized * dist);

                _lineRenderer.SetPosition(1, _currentMousePos);
            }

            if (Input.GetMouseButtonUp(0))
            {
                ChangeLineRendererAlpha(1f);

                _points[0] = _startMousePos;
                _points[1] = _currentMousePos;
                _edgeCollider2D.points = _points.ToArray();

                _syncStartMousePos = _startMousePos;
                _syncEndMousePos = _currentMousePos;

                PlayAudio(DrawSFX);
            }
        }
        else
        {
            if (Input.GetMouseButtonUp(0))
            {
                Restart();
            }
        }
    }

    Vector2 GetMousePos()
    {
        return _camera.ScreenToWorldPoint(Input.mousePosition); 
    }

    void ChangeLineRendererAlpha(float alpha)
    {
        _lineColor.a = alpha;
        _lineRenderer.material.color = _lineColor;
    }

    void Restart()
    {
        _currentMousePos = _startMousePos = Vector3.zero;
        _syncEndMousePos = _syncStartMousePos = Vector3.zero;

        _lineRenderer.SetPosition(0, Vector3.zero);
        _lineRenderer.SetPosition(1, Vector3.zero);

        _points[0] = Vector3.zero;
        _points[1] = Vector3.zero;
        _edgeCollider2D.points = _points.ToArray();
    }

    void PlayAudio(AudioClip clip, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(clip, _camera.transform.position, volume);
    }

    void OnRestartGame(object sender, object[] args)
    {
        if (OwnerID != NetPlayerController.Instance.ControlID)
            return;

        Restart();
    }

    bool IsPointerOverUIObject(Vector2 pos)
    {
        if (EventSystem.current == null)
            return false;
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(pos.x, pos.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}
