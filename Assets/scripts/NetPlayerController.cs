using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NetPlayerController : MonoBehaviour
{
    public static NetPlayerController Instance { get; private set; }

    public uint ControlID;

    public GameObject ClientIdUI,
        ClientLobbyUI;

    public TMP_InputField UserNameInput,
        IPInput,
        PortInput;

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
        ClientIdUI.SetActive(false);
        ClientLobbyUI.SetActive(false);

        UserNameInput.onSubmit.AddListener((s) =>
        {
            RequestID();
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChooseClient()
    {
        NetManager.Instance.Type = NetworkNodeType.Client;
        if (!string.IsNullOrEmpty(IPInput.text))
        {
            NetManager.Instance.IP = IPInput.text;
        }

        if (!string.IsNullOrEmpty(PortInput.text))
        {
            NetManager.Instance.Port = ushort.Parse(PortInput.text);
        }

        NetManager.Instance.StartConnection();
        ClientIdUI.SetActive(true);
        UserNameInput.ActivateInputField();
    }

    public void RequestID()
    {
        if (!string.IsNullOrEmpty(UserNameInput.text))
        {
            IDRequest idreq = new IDRequest();
            idreq.UserName = UserNameInput.text;
            NetManager.Instance.NetNode.SendMessage(idreq);

            ClientIdUI.SetActive(false);
            ClientLobbyUI.SetActive(true);
            MyGameManager.Instance.TutorialUI.SetActive(true);
        }
    }

    public void PlayGame()
    {
        EnterGameMessage msg = new EnterGameMessage();
        msg.OwnerID = ControlID;
        NetManager.Instance.NetNode.SendMessage(msg);
        ClientLobbyUI.SetActive(false);
    }
}
