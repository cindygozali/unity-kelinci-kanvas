using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MyGameManager : MonoBehaviour
{
    public static MyGameManager Instance { get; private set; }
    
    public AgentState State;

    [Header("Text")]
    public TextMeshProUGUI TextStartTime;
    public float StartTimer = 3;
    float _startCD;

    public TextMeshProUGUI TextTimeLimit;
    public float TimeTimer = 40;
    public float TimeCD;

    public TextMeshProUGUI TextScore;
    public int Score;
    bool _hasCalculateScore;

    [Header("UI")]
    public GameObject ServerClientUI;
    public GameObject ChoosePlayerUI,
        TitleUI,
        CountDownUI,
        GameUI,
        WinUI,
        LoseUI,
        SettingUI,
        TutorialUI;

    [Header("Button")]
    public Button RestartButton;

    public float TimeSyncInterval = 10f;

    public List<GameObject> Coins = new List<GameObject>();

    bool _showSetting;
    bool _hasCalledCountdownFunction;

    [Header("Audio")]
    public AudioClip IntroMusic;
    public AudioClip BGM,
        ClickButtonSFX;
    AudioSource _audio;
    Camera _camera;

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
        _audio = GetComponent<AudioSource>();
        _audio.clip = IntroMusic;
        _audio.Play();

        _camera = Camera.main;

        TitleUI.SetActive(true);
        
        Restart();
        _startCD = StartTimer;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            OpenOrCloseSetting();
        }

        switch (State)
        {
            case AgentState.Connecting:
                {
                    CountDownUI.SetActive(false);
                    GameUI.SetActive(false);

                    break;
                }

            case AgentState.CountDown:
                {
                    if (_startCD < 0)
                    {
                        foreach (GameObject coin in Coins)
                        {
                            coin.SetActive(true);
                        }

                        CountDownUI.gameObject.SetActive(false);
                        GameUI.SetActive(true);

                        StartCoroutine(UpdateTimeData());

                        State = AgentState.StartGame;
                    }
                    else
                    {
                        TextStartTime.text = _startCD.ToString("F0");
                        _startCD -= Time.deltaTime;

                        if (!_hasCalledCountdownFunction)
                        {
                            Core.BroadcastEvent("RestartGame", this);

                            CountDownUI.gameObject.SetActive(true);

                            if (_audio.clip != BGM)
                            {
                                _audio.clip = BGM;
                                _audio.Play();
                            }

                            Restart();

                            _hasCalledCountdownFunction = true;
                        }
                        
                    }

                    break;
                }

            case AgentState.StartGame:
                {
                    TextTimeLimit.text = TimeCD.ToString("F0");
                    TimeCD -= Time.deltaTime;

                    TextScore.text = Score.ToString() + "/" + Coins.Count;
                    GameUI.SetActive(true);

                    if (TimeCD < 0)
                    {
                        State = AgentState.Lose;
                    }

                    break;
                }

            case AgentState.Win:
                {
                    WinUI.SetActive(true);

                    StopCoroutine(UpdateTimeData());
                    
                    TextScore.text = Score + " + " + (int)TimeCD;

                    break;
                }

            case AgentState.Lose:
                {
                    LoseUI.SetActive(true);

                    StopCoroutine(UpdateTimeData());

                    break;
                }
        }
    }

    public void GameStart()
    {
        TitleUI.SetActive(false);
        ServerClientUI.SetActive(true);
    }

    public void GameRestart()
    {
        SendStateMessage(AgentState.CountDown);
    }

    void Restart()
    {
        TimeCD = TimeTimer;

        ServerClientUI.SetActive(false);
        ChoosePlayerUI.SetActive(false);
        GameUI.SetActive(false);
        WinUI.SetActive(false);
        LoseUI.SetActive(false);
        SettingUI.SetActive(false);
        TutorialUI.SetActive(false);

        Score = 0;
        _hasCalculateScore = false;

        foreach (GameObject coin in Coins)
        {
            coin.SetActive(false);
        }
    }

    public void GameQuit()
    {
        Application.Quit();
    }

    public void OpenSetting()
    {
        _showSetting = true;
        SettingUI.SetActive(true);
    }

    public void CloseSetting()
    {
        _showSetting = false;
        SettingUI.SetActive(false);
    }

    public void OpenOrCloseSetting()
    {
        _showSetting = !_showSetting;
        SettingUI.SetActive(_showSetting);
        if (State == AgentState.Connecting)
        {
            RestartButton.gameObject.SetActive(false);
        }
        else
        {
            RestartButton.gameObject.SetActive(true);
        }
    }

    public void OpenTutorial()
    {
        TutorialUI.SetActive(true);
    }

    public void CloseTutorial()
    {
        TutorialUI.SetActive(false);
    }

    void SendStateMessageServer(AgentState state)
    {
        if (NetManager.Instance.Type == NetworkNodeType.Server)
        {
            State = state;
            SendStateMessage(state);
        }
    }

    void SendStateMessage(AgentState state)
    {
        StateMessage msg = new StateMessage();
        msg.State = state;
        NetManager.Instance.NetNode.SendMessage(msg);
    }

    void SendTimeMessageServer()
    {
        if (NetManager.Instance.Type == NetworkNodeType.Server)
        {
            TimeMessage msg = new TimeMessage();
            msg.Time = TimeCD;
            NetManager.Instance.NetNode.SendMessage(msg);
        }
    }

    IEnumerator UpdateTimeData()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(TimeSyncInterval);
            SendTimeMessageServer();
        }
    }

    public void ReceiveStateMessage(StateMessage msg)
    {
        State = msg.State;
        if (State == AgentState.CountDown)
        {
            _startCD = StartTimer;
            _hasCalledCountdownFunction = false;
        }
    }

    public void PlayButtonSFX()
    {
        AudioSource.PlayClipAtPoint(ClickButtonSFX, _camera.transform.position, 20f);
    }
}
