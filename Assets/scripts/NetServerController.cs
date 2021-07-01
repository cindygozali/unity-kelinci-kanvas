using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NetServerController : MonoBehaviour
{
    public static NetServerController Instance { get; private set; }
    public GameObject ClientUI;
    public GameObject PlayButton;

    public TMP_InputField PortInput;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        ClientUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ChooseServer()
    {
        NetManager.Instance.Type = NetworkNodeType.Server;
        if (!string.IsNullOrEmpty(PortInput.text))
        {
            NetManager.Instance.Port = ushort.Parse(PortInput.text);
        }
        NetManager.Instance.StartConnection();
        PlayButton.SetActive(false);
        ClientUI.SetActive(true);
        Core.BroadcastEvent("LoadLevel", this);
    }
}
