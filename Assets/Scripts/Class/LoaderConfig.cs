using System;
using UnityEngine;


public class LoaderConfig : GameSetting
{
    public static LoaderConfig Instance = null;
    public MicrophoneDevice microphoneDevice;
    protected override void Awake()
    {
        if (Instance == null)
            Instance = this;

        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        this.LoadGameData();
        this.OnMicrophoneAccessGranted();
#endif
    }

    protected override void Update()
    {
        base.Update();


    }

    public void LoadGameData()
    {
        this.apiManager.PostGameSetting(this.GetParseURLParams,
                                        () => StartCoroutine(this.apiManager.postGameSetting(this.LoadQuestions)),
                                        () => StartCoroutine(this.apiManager.postGameAppSetting(this.LoadQuestions)),
                                        this.LoadQuestions
                                       );
    }

    public void OnMicrophoneAccessGranted()
    {
        LogController.Instance?.debug("Microphone access granted.");
        this.microphoneDevice.ListMicrophoneDevices();
    }

    public void LoadQuestions()
    {
        this.InitialGameImages(() =>
        {
            QuestionManager.Instance?.LoadQuestionFile(this.unitKey, () => this.finishedLoadQuestion());
        });
    }

    public void getHashValue(string hashValue)
    {
        LogController.Instance.debug("Received hash value from JavaScript: " + hashValue);
    }

    void finishedLoadQuestion()
    {
        var title = QuestionManager.Instance.questionData.gameTitle;
        if (!string.IsNullOrEmpty(title))
            this.gameSetup.gamePageName = title;

        var pageName = this.gameSetup.gamePageName;
        if (!string.IsNullOrEmpty(pageName))
        {
            ExternalCaller.SetWebPageTitle(pageName);
            LogController.Instance?.debug($"Setup Current GameName: {pageName}");
        }

        ExternalCaller.HiddenLoadingBar();
        this.changeScene(1);
    } 

    public void SubmitAnswer(int duration, int playerScore, float statePercent, int stateProgress,
                             int correctId, float currentQADuration, string qid, int answerId, string answerText,
                             string correctAnswerText, float currentQAscore, float currentQAPercent, Action onCompleted = null)
    {
        /*        string jsonPayload = $"[{{\"payloads\":{playloads}," +
        $"\"role\":{{\"uid\":{uid}}}," +
        $"\"state\":{{\"duration\":{stateDuration},\"score\":{stateScore},\"percent\":{statePercent},\"progress\":{stateProgress}}}," +
        $"\"currentQuestion\":{{\"correct\":{correct},\"duration\":{currentQADuration},\"qid\":\"{currentqid}\",\"answer\":{answerId},\"answerText\":\"{answerText}\",\"correctAnswerText\":\"{correctAnswerText}\",\"score\":{currentQAscore},\"percent\":{currentQAPercent}}}}}]";*/

        var answer = this.apiManager.answer;
        answer.state.duration = duration;
        answer.state.score = playerScore;
        answer.state.percent = statePercent;
        answer.state.progress = stateProgress;

        answer.currentQA.correctId = correctId;
        answer.currentQA.duration = currentQADuration;
        answer.currentQA.qid = qid;
        answer.currentQA.answerId = answerId;
        answer.currentQA.answerText = answerText;
        answer.currentQA.correctAnswerText = correctAnswerText;
        answer.currentQA.score = currentQAscore;
        answer.currentQA.percent = currentQAPercent;


        StartCoroutine(this.apiManager.SubmitAnswer(onCompleted));
    }

    public void closeLoginErrorBox()
    {
        this.apiManager.resetLoginErrorBox();
    }

    public void exitPage(string state = "", Action<bool> leavePageWithValue = null, Action leavePageWithoutValue = null)
    {
        bool isLogined = this.apiManager.IsLogined;
        if (isLogined)
        {
            LogController.Instance?.debug($"{state}, called exit api.");
            StartCoroutine(this.apiManager.ExitGameRecord(() =>
            {
                leavePageWithValue?.Invoke(true);
                leavePageWithoutValue?.Invoke();
            }));
        }
        else
        {
            leavePageWithValue?.Invoke(false);
            leavePageWithoutValue?.Invoke();
            LogController.Instance?.debug($"{state}.");
        }
    }

    public void QuitGame()
    {
        this.exitPage("Quit Game", null);
    }

    private void OnApplicationQuit()
    {
        this.QuitGame();
    }
}


[Serializable]
public class MicrophoneDevice
{
    [Header("Microphone Devices")]
    public string[] microphoneDevices;
    public int selectedDeviceIndex = 0;
    public string selectedDeviceName => Microphone.devices[this.selectedDeviceIndex];
    public bool HasMicrophoneDevices => Microphone.devices != null && Microphone.devices.Length > 0;

    public void ListMicrophoneDevices()
    {
        this.microphoneDevices = Microphone.devices;
        if (Microphone.devices.Length > 0)
        {
            this.selectedDeviceIndex = Mathf.Clamp(this.selectedDeviceIndex, 0, Microphone.devices.Length - 1);

            string deviceList = "Detected Microphone Devices:\n";
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                deviceList += $"{i}: {Microphone.devices[i]}\n";
            }
            LogController.Instance.debug(deviceList);
        }
        else
        {
            LogController.Instance.debug("No microphone devices detected.");
        }
    }
}
