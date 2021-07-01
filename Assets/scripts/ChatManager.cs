using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    public TMP_InputField ChatText;
    public TextMeshProUGUI ChatContent;
    public Scrollbar ChatBar;
    public RectTransform ContentBar;

    public AudioClip ChatSFX;

    Camera _camera;

    private void Awake()
    {
        Instance = this;
        ChatText.onSubmit.AddListener((s) =>
        {
            SendChat();
            ChatText.text = "";
            ChatText.ActivateInputField();
        });
        _camera = Camera.main;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void SendChat()
    {
        if (!string.IsNullOrEmpty(ChatText.text) && NetManager.Instance.Type == NetworkNodeType.Client)
        {
            ChatMessage msg = new ChatMessage();

            msg.SenderID = NetPlayerController.Instance.ControlID;
            msg.Message = ChatText.text;
            NetManager.Instance.NetNode.SendMessage(msg);
        }
    }

    public void ReceiveChat(ChatMessage msg, string senderName)
    {
        string currentTime = System.DateTime.Now.ToString("HH:mm");
        
        ChatContent.text += "<color=#b8c5d3ff><b>\n\n" + senderName + ": </b></color>"
            + msg.Message + "\n<color=#c0c0c0ff><size=35>Sent at " + currentTime + "</size></color>";

        BiggerChat(2);

        PlayAudio(ChatSFX);
    }

    public void ShowConnectedUser(string username, string word = "is")
    {
        string currentTime = System.DateTime.Now.ToString("HH:mm");

        ChatContent.text += "\n\n<i>" + username + " " + word + " connected!</i>"
            + "\n<color=#c0c0c0ff><size=35>Sent at " + currentTime + "</size></color>";

        BiggerChat(2);
    }

    void BiggerChat(int line)
    {
        Vector2 contentSize = ContentBar.sizeDelta;
        ContentBar.sizeDelta = new Vector2(contentSize.x, contentSize.y + line * 40);
        ChatBar.value -= line * 0.04f;
    }

    void PlayAudio(AudioClip clip, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(clip, _camera.transform.position, volume);
    }
}
