using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RecordAudio : MonoBehaviour
{
    public enum RecognitionAPI
    {
        roWeb_Azure=0, //roWeb speech to text api
        recognize_tts=1, //語音辨識api
    }

    public enum Stage
    {
        Record=0,
        Recording = 1,
        UploadClip = 2,
        PlaybackResult = 3,
    }
    public Stage stage = Stage.Record;
    public RecognitionAPI recognitionAPI = RecognitionAPI.recognize_tts;
    public TextMeshProUGUI debugText, answerText, submitAudioText;
    public Color32 answerTextOriginalColor;
    public RawImage answerBox;
    public Texture[] answerBoxTexs;
    private AudioClip clip, trimmedClip;
    public bool useTextToRecognize = false;
    public bool useHighPassFilter = false;
    [Header("Audio Pages for different process")]
    public CanvasGroup[] pages;

    [Header("Result Page for different control button")]
    public CanvasGroup[] resultBtns;

    [SerializeField] private AudioSource playbackSource;
    [SerializeField] private GameObject recordButton;
    [SerializeField] private CanvasGroup stopButton;
    [SerializeField] private RawImage playbackButton;
    [SerializeField] private Texture[] playbackBtnTexs;
    [SerializeField] private Text stopRecordText, playbackText, accurancyText;
    [SerializeField] private int maxRecordLength = 10;
    [SerializeField] private WaveformVisualizer waveformVisualizer;
    [SerializeField] private Slider playbackSlider;
    [SerializeField] private CanvasGroup remindRecordTip;
    [SerializeField] private Button qa_audio_btn;
    [SerializeField] public CanvasGroup hintBox, remindRecordBox;

    private bool isRecording = false;
    private bool isPlaying = false;
    private float recordingTime = 0f;
    public SttResponse sttResponse;
    public RecognitionResult recognitionResult;
    private bool grantedMicrophone = false;
    public int passAccuracyScore = 60;
    public int passPronScore = 60;

    private const string ApiUrl = "/RainbowOne/index.php/PHPGateway/proxy/2.8/";
    private const string JwtToken = "eyJ0eXAiOiJqd3QiLCJhbGciOiJIUzI1NiJ9.eyJsb2dfZW5hYmxlZCI6IjEiLCJ0b2tlbiI6IjUyNzcwMS04MTcyNGIyYTIxODk4YTE2NTA0ZTZiMTg0ZWZlMWQ5Mjc2OGIyYWM1YmI2ZmExMDc4NDVlZjM1MDRjNTY3NDBlIiwiZXhwaXJlcyI6MTgwODUzNjQ5NSwicmVuZXdfZW5hYmxlZCI6MSwidGltZSI6IjIwMjUtMDQtMjQgMDM6MTQ6NTUgR01UIiwidWlkIjoiNTI3NzAxIiwidXNlcl9yb2xlIjoiMiIsInNjaG9vbF9pZCI6IjMxNiIsImlwIjoiOjoxIiwidmVyc2lvbiI6bnVsbCwiZGV2aWNlIjoidW5rbm93biJ9.SO79u9MBCflyYh_TcsIBG740pWXgKPZOAsGNZESkoqo";

    void Start()
    {
        StartCoroutine(this.initMicrophonePermission(1f));
        if (this.playbackSlider != null)
        {
            this.playbackSlider.onValueChanged.AddListener(OnSliderValueChanged);
            this.playbackSlider.minValue = 0;
            this.playbackSlider.maxValue = 1;
        }
        this.passAccuracyScore = LoaderConfig.Instance.gameSetup.passAccuracyScore;
        this.passPronScore = LoaderConfig.Instance.gameSetup.passPronScore;
    }

    public void Init()
    {
        this.switchPage(Stage.Record);
    }

    void Update()
    {
        if (this.qa_audio_btn != null) this.qa_audio_btn.interactable = !isRecording;
        if (isRecording && clip)
        {
            recordingTime += Time.deltaTime;

            if (recordingTime >= maxRecordLength)
            {
                StopRecording();
                return;
            }

            // Get the current microphone position
            int micPosition = Microphone.GetPosition(null);

            if (micPosition > 0 && micPosition <= this.clip.samples)
            {
                int sampleCount = Mathf.Min(micPosition, this.clip.samples);
                float[] samples = new float[sampleCount];
                this.clip.GetData(samples, 0);
                this.waveformVisualizer?.UpdateWaveform(samples);
            }

            if (this.stopRecordText != null)
            {
                int totalSeconds = Mathf.Min((int)recordingTime, maxRecordLength);
                TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
                this.stopRecordText.text = timeSpan.ToString(@"hh\:mm\:ss");
            }
            this.UpdateUI($"Recording...");
        }

        if (isPlaying && playbackSource != null)
        {
            // Update the slider value during playback
            if (this.playbackSlider != null && playbackSource.clip != null)
            {
                this.playbackSlider.value = playbackSource.time / playbackSource.clip.length;
                this.updatePlayBackText(playbackSource.time);
            }

            // Stop playback if the audio has reached the end
            if (!playbackSource.isPlaying && Mathf.Approximately(playbackSource.time, playbackSource.clip.length - 0.01f))
            {
                LogController.Instance.debug("Finished playback");
                this.StopPlayback();
            }
        }
    }

    private void ProcessAudioClip(AudioClip audioClip, float gain)
    {
        if (audioClip == null) return;

        // Get the audio data
        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        if(this.useHighPassFilter)
        {
            this.ApplyHighPassFilter(samples, cutoffFrequency: 100f, sampleRate: audioClip.frequency);
        }

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= gain;
        }
        LogController.Instance.debug($"Applied gain of {gain}x to the audio clip.");

        // Set the processed data back to the audio clip
        audioClip.SetData(samples, 0);
    }

    private void ApplyHighPassFilter(float[] samples, float cutoffFrequency, float sampleRate)
    {
        float rc = 1.0f / (cutoffFrequency * 2 * Mathf.PI);
        float dt = 1.0f / sampleRate;
        float alpha = rc / (rc + dt);

        float previousSample = samples[0];
        for (int i = 1; i < samples.Length; i++)
        {
            float currentSample = samples[i];
            samples[i] = alpha * (samples[i] - previousSample);
            previousSample = currentSample;
        }
    }

    void switchPage(Stage _stage)
    {
        this.stage = _stage;
        LogController.Instance.debug($"Current Recording Stage: {this.stage}"); 
        switch (this.stage)
        {
            case Stage.Record:
                this.ResetRecorder();
                this.hintBox.GetComponent<TextToSpeech>()?.PlayAudio(()=>
                {
                    SetUI.Set(this.hintBox, true);
                    SetUI.Set(this.remindRecordBox, false);
                },
                ()=>
                {
                    SetUI.Set(this.hintBox, false);
                    SetUI.Set(this.remindRecordBox, true);
                }
                );
                break;
            case Stage.Recording:
                this.StopPlayback();
                SetUI.SetGroup(this.pages, 1);
                StartCoroutine(this.delayEnableStopRecorder());
                break;
            case Stage.UploadClip:
                AudioController.Instance?.fadingBGM(true, 1f);
                break;
            case Stage.PlaybackResult:
                SetUI.SetGroup(this.pages, 2);
                break;
        }
    }

    IEnumerator delayEnableStopRecorder()
    {
        yield return new WaitForSeconds(1f);
        SetUI.Set(this.stopButton, true, 0f, 1f);
    }

    public void controlResultPage(int showBtnId=-1)
    {
        SetUI.SetGroup(this.resultBtns, showBtnId);
        if(showBtnId == 2)
        {
           this.ShowDirectCorrectAnswer();
        }
    }

    private IEnumerator initMicrophonePermission(float _delay = 1f)
    {
        if (this.grantedMicrophone) yield break;
        Microphone.Start("", true, 1, 16000);
        yield return new WaitForSeconds(_delay);
        Microphone.End(null);
        LogController.Instance?.debug("Microphone access granted.");
        this.grantedMicrophone = true;
    }

    public void StartRecording()
    {
        if (this.hintBox != null)
        {
            var ttS = this.hintBox.GetComponent<TextToSpeech>();
            if (ttS != null)
            {
                ttS.StopAudio();
            }
            SetUI.Set(this.hintBox, false);
        }
        if (this.remindRecordBox != null)
        {
            SetUI.Set(this.remindRecordBox, true);
        }

        if (isRecording || !this.grantedMicrophone) return;
        AudioController.Instance?.fadingBGM(false, 0f);
        LogController.Instance?.debug($"Recording started: {isRecording}");
        var microphoneDevices = LoaderConfig.Instance.microphoneDevice;
        if (!microphoneDevices.HasMicrophoneDevices)
        {
            LogController.Instance?.debug("No microphone devices available. use default microphone");
            this.clip = Microphone.Start("", true, maxRecordLength, 16000);
        }
        else
        {
            this.clip = Microphone.Start(microphoneDevices.selectedDeviceName, true, maxRecordLength, 16000);
        }

        if (this.clip)
        {
            this.waveformVisualizer?.ClearTexture();
            isRecording = true;
            recordingTime = 0f;
            this.switchPage(Stage.Recording);
        }
        else
        {
            this.UpdateUI("Failed to start recording.");
        }
    }

    private float GetPlatformSpecificGain()
    {
#if UNITY_EDITOR
        // Use default gain for the editor
        return 5.0f;
#elif UNITY_WEBGL
    return 2.0f;
#elif UNITY_IOS
    // iPad browsers may already apply AGC, so use lower gain
    return 1.0f;
#else
    // Default gain for other platforms
    return 2f;
#endif
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            LogController.Instance?.debug("StopRecording called but not currently recording.");
            return;
        }

        int micPosition = Microphone.GetPosition(null);
        bool wasRecording = Microphone.IsRecording(null);

        if (!wasRecording || micPosition <= 0)
        {
            LogController.Instance?.debug("Microphone is not ready or no content recorded, resetting state.");
            this.UpdateUI("Microphone is not ready and no contents recorded, please retry");
            isRecording = false;
            // Optionally reset UI to a safe state
            this.ResetRecorder();
            return;
        }

        Microphone.End(null);
        isRecording = false;
        this.switchPage(Stage.UploadClip);
        StartCoroutine(this.TrimAudioClip(micPosition));
    }

    private IEnumerator TrimAudioClip(int micPosition)
    {
        float actualLength = micPosition / (float)this.clip.frequency;
        yield return new WaitForEndOfFrame();
        AudioClip trimmedClip = AudioClip.Create(this.clip.name, micPosition, this.clip.channels, this.clip.frequency, false);
        float[] samples = new float[micPosition * this.clip.channels];
        this.clip.GetData(samples, 0);
        trimmedClip.SetData(samples, 0);

        this.clip = trimmedClip;
        this.trimmedClip = trimmedClip;
        float gain = this.GetPlatformSpecificGain();
        LogController.Instance?.debug("Gain scale: " + gain);
        this.ProcessAudioClip(this.clip, gain);

        switch (this.recognitionAPI)
        {
            case RecognitionAPI.roWeb_Azure:
                yield return SendAudioToApi(this.clip);
                this.switchPage(Stage.PlaybackResult);
                break;
            case RecognitionAPI.recognize_tts:
                bool uploadSuccess = false;
                yield return UploadAudioFile(
                    this.clip,
                    (response) => {
                        this.UpdateUI($"Audio uploaded successfully: {response}");
                        uploadSuccess = true;
                    },
                    (error) => {
                        this.UpdateUI($"Error uploading audio: {error}");
                        this.switchPage(Stage.Record);
                    }
                );
                if (uploadSuccess)
                {
                    this.switchPage(Stage.PlaybackResult);
                }
                break;
        }
    }

    public void StartPlayback()
    {
        if(this.isPlaying && this.playbackButton.texture == this.playbackBtnTexs[1])
        {
            this.PausePlayback();
        }
        else
        {
            if (this.trimmedClip == null)
            {
                this.UpdateUI("No recording available for playback.");
                return;
            }
            if (playbackSource == null)
            {
                playbackSource = gameObject.AddComponent<AudioSource>();
            }

            playbackSource.clip = this.trimmedClip;
            playbackSource.loop = false;

            if (Mathf.Approximately(playbackSource.time, playbackSource.clip.length - 0.01f))
            {
                playbackSource.time = 0f;
            }

            playbackSource.Play();

            isPlaying = true;
            this.playbackButton.texture = this.playbackBtnTexs[1];
        }
    }

    public void PausePlayback()
    {
        if (playbackSource != null && playbackSource.isPlaying)
        {
            playbackSource.Pause();
            this.playbackButton.texture = this.playbackBtnTexs[0];
        }

        this.isPlaying = false;
    }

    public void StopPlayback()
    {
        if (playbackSource != null && playbackSource.isPlaying)
        {
            playbackSource.Stop();
        }
        this.isPlaying = false;

        if (this.playbackSlider != null)
        {
            this.playbackSlider.value = 0;
        }
        this.playbackButton.texture = this.playbackBtnTexs[0];
        if (this.playbackText != null) this.playbackText.text = "00:00:00";
    }

    private void OnSliderValueChanged(float value)
    {
        if (playbackSource != null && playbackSource.clip != null && !isRecording)
        {
            // Clamp the slider value to ensure it stays within the valid range
            value = Mathf.Clamp(value, 0f, 1f);

            // Calculate the playback position in seconds
            float playbackPosition = value * playbackSource.clip.length;

            if (!playbackSource.isPlaying)
            {
                playbackSource.Play();
                playbackSource.Pause();
            }
            this.playbackButton.texture = this.playbackBtnTexs[playbackSource.isPlaying? 1 : 0];

            // Clamp the playback position to ensure it doesn't exceed the clip length
            playbackSource.time = Mathf.Clamp(playbackPosition, 0f, playbackSource.clip.length - 0.01f);

            this.updatePlayBackText(playbackSource.time);
        }
    }

    private void updatePlayBackText(float playBackTime)
    {
        if (this.playbackText != null)
        {
            int totalSeconds = Mathf.FloorToInt(playBackTime);
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
            this.playbackText.text = timeSpan.ToString(@"hh\:mm\:ss");
        }
    }

    private IEnumerator SendAudioToApi(AudioClip audioClip)
    {
        this.UpdateUI("Processing audio...");
        byte[] wavData = ConvertAudioClipToWav(audioClip);

        if (wavData == null)
        {
            this.UpdateUI("Failed to convert audio to WAV.");
            yield break;
        }

        WWWForm form = new WWWForm();
        string fileName = $"audio_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        form.AddField("api", "ROMediaLibrary.getSttFromWav");
        form.AddBinaryData("file", wavData, fileName, "audio/wav");
        form.AddField("json", "[\"en-GB\"]");

        UnityWebRequest request = UnityWebRequest.Post(LoaderConfig.Instance.SpeechAPIHostName + ApiUrl, form);
        request.SetRequestHeader("Authorization", $"Bearer {JwtToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            try
            {
                // Parse the JSON response
                this.sttResponse = JsonUtility.FromJson<SttResponse>(responseText);

                if (this.sttResponse.data != null && this.sttResponse.data.Length > 0)
                {
                    var firstResult = this.sttResponse.data[0];
                    string transcript = firstResult.transcript;
                    float confidence = firstResult.confidence;

                    this.UpdateUI($"Transcription: {transcript}\nConfidence: {confidence:P2}");

                    var playerController = this.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        playerController.submitAnswer(transcript);
                    }

                    if (firstResult.words != null)
                    {
                        foreach (var word in firstResult.words)
                        {
                            LogController.Instance.debug($"Word: {word.word}, Start: {word.startTime}, End: {word.endTime}");
                        }
                    }
                }
                else
                {
                    this.UpdateUI("No transcription data available.");
                }
            }
            catch (Exception ex)
            {
                LogController.Instance.debugError($"Error parsing JSON: {ex.Message}");
                this.UpdateUI("Error processing transcription response.");
            }
        }
        else
        {
            this.UpdateUI($"Error: {request.error}");
        }
    }

    public IEnumerator UploadAudioFile(AudioClip audioClip, 
                                       Action<string> onSuccess, 
                                       Action<string> onError, 
                                       int retryCount = 5, 
                                       float retryDelay = 2f)
    {
        string uploadUrl = "https://dev.openknowledge.hk/RainbowOne/index.php/transport/Slave/upload/2";
        string apiHostName = "https://dev.openknowledge.hk";
#if UNITY_WEBGL && !UNITY_EDITOR
        string hostname = ExternalCaller.GetCurrentDomainName;
        if (hostname.Contains("dev.openknowledge.hk"))
        {
            uploadUrl = "https://dev.openknowledge.hk/RainbowOne/index.php/transport/Slave/upload/2";
        }
        else if (hostname.Contains("www.rainbowone.app"))
        {
            uploadUrl = "https://www.rainbowone.app/RainbowOne/index.php/transport/Slave/upload/2";
            apiHostName = "https://www.rainbowone.app/";
        }
#endif
        //roWeb upload structure:
        /*if (["pdf", "doc", "docx"].indexOf(extension) != -1)
        {
            options = { fileType: "file" };
            uploadType = "DOC";
        }
        else if (["mp3", "m4a", "wav"].indexOf(extension) != -1)
        {
            options = { fileType: "audio" };
            uploadType = 2;
        }
        else if (["jpg", "jpeg", "png", "gif"].indexOf(extension) != -1)
        {
            options = { fileType: "image" };
            uploadType = 1;
        }
        else if (["mp4", "mov"].indexOf(extension) != 1)
        {
            options = { fileType: "video" };
            uploadType = 3;
        }*/
        byte[] audioData = ConvertAudioClipToWav(audioClip);

        if (audioData == null)
        {
            this.UpdateUI("Failed to convert audio to WAV.");
            onError?.Invoke("Failed to convert audio to WAV.");
            yield break;
        }

        int attempt = 0;
        while (attempt < retryCount)
        {
            attempt++;
            WWWForm form = new WWWForm();
            string fileName = $"audio_{DateTime.Now:yyyyMMdd_HHmmss}";
            string file = fileName + ".wav";
            form.AddField("Filename", fileName);
            form.AddBinaryData("Filedata", audioData, file, "audio/wav");

            UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form);
            if (!string.IsNullOrEmpty(JwtToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {JwtToken}");
            }
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LogController.Instance.debug("File uploaded successfully.");
                var uploadResponse = JsonUtility.FromJson<UploadResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(uploadResponse.url))
                {
                    LogController.Instance.debug("uploadResponse url : " + uploadResponse.url);
                    string audioUrl = apiHostName + uploadResponse.url.Replace("\\", "");
#if UNITY_WEBGL && !UNITY_EDITOR
                if (hostname.Contains("www.rainbowone.app"))
                {
                    audioUrl = uploadResponse.url;
                }
#endif
                    yield return StartCoroutine(SendAudioRecognitionRequest(
                        audioUrl,
                        this.useTextToRecognize ? QuestionController.Instance.currentQuestion.correctAnswer.ToLower() : ""
                    ));
                    onSuccess?.Invoke("Upload and recognition success.");
                    yield break;
                }
                else
                {
                    LogController.Instance.debugError("Failed to extract audio URL from upload response.");
                    this.UpdateUI("Failed to extract audio URL from upload response.");
                    onError?.Invoke("Failed to extract audio URL from upload response.");
                    yield break;
                }
            }
            else
            {
                LogController.Instance.debugError($"File upload failed (attempt {attempt}): {request.error}");
                this.UpdateUI($"Upload failed (attempt {attempt}/{retryCount}): {request.error}");
                if(this.submitAudioText != null) this.submitAudioText.text = $"Retry Upload ({attempt}/{retryCount})";
                if (attempt < retryCount)
                    yield return new WaitForSeconds(retryDelay);
            }
        }

        // All attempts failed
        this.UpdateUI("Upload failed after multiple attempts. Please check your network and try again.");
        onError?.Invoke("Upload failed after multiple attempts.");
    }

    private IEnumerator SendAudioRecognitionRequest(string audioUrl, string textToRecognize="", string language= "en-US", string purpose= "enSpeech")
    {
        if (string.IsNullOrEmpty(audioUrl))
        {
            this.UpdateUI("audioUrl is null. Cannot process.");
            yield break;
        }

        this.UpdateUI("Converting AudioClip to binary data...");

        this.UpdateUI("Sending audio recognition request...");
        string jsonPayload = $"[\"{audioUrl}\",\"{textToRecognize}\",\"{language}\",\"{purpose}\"]";

        WWWForm form = new WWWForm();
        form.AddField("api", "ROSpeechRecognition.recognize_tts");
        form.AddField("json", jsonPayload);

        UnityWebRequest request = UnityWebRequest.Post(LoaderConfig.Instance.SpeechAPIHostName + ApiUrl, form);
        request.SetRequestHeader("Authorization", $"Bearer {JwtToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            LogController.Instance.debug($"responseText: {responseText}");
            try
            {
                // Parse the root JSON structure
                var recognitionResponse = JsonUtility.FromJson<RecognitionResponse>(responseText);

                // Extract the result object
                if (recognitionResponse != null && recognitionResponse.result != null)
                {
                    this.recognitionResult = recognitionResponse.result;
                    NBest[] Best = recognitionResult.NBest;
                    bool failure = false;
                    StringBuilder result = new StringBuilder();
                    string transcript = "";
                    string correctAnswer = recognitionResponse.result.DisplayText;

                    var correctAnswerWords = QuestionController.Instance.currentQuestion.correctAnswer
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    WordDetail[] wordDetails = null;
                    // Log the NBest array
                    if (Best != null)
                    {
                        foreach (var nBest in Best)
                        {
                            result.AppendLine($"Score: {nBest.PronScore}");
                            if(this.accurancyText != null) this.accurancyText.text = $"Rating: {nBest.PronScore}%";
                            if (nBest.AccuracyScore <= this.passAccuracyScore && 
                                nBest.PronScore <= this.passPronScore)
                            {
                                failure = true;
                            }

                            if(nBest.Words != null)
                            {
                                wordDetails = nBest.Words;
                            }
                            else
                            {
                                result.AppendLine("No words found in NBest.");
                            }
                        }

                        if(!failure)
                        {
                            transcript = correctAnswer;
                        }


                        var displayText = Regex.Replace(transcript, @"[^\w\s]", "");
                        result.AppendLine($"DisplayText: {displayText}");
                        //transcript = Regex.Replace(recognitionResult.DisplayText, @"[^\w\s]", "").ToLower();
                        this.UpdateUI(result.ToString());
                        //if(this.answerText != null) this.answerText.text = displayText;
                        
                        var playerController = this.GetComponent<PlayerController>();
                        if (playerController != null)
                        {
                            playerController.submitAnswer(displayText, ()=>
                            {
                                this.showCorrectSentence(displayText, wordDetails);
                            });
                        }
                    }
                }
                else
                {
                    LogController.Instance.debugError("Failed to parse recognition result.");
                    this.UpdateUI("Failed to parse recognition result.");
                }
            }
            catch (Exception ex)
            {
                LogController.Instance.debugError($"Error parsing API response: {ex.Message}");
                this.UpdateUI("Error processing audio recognition response.");
            }
        }
        else
        {
            this.UpdateUI($"Error: {request.error}");
        }
    }

    public void showCorrectSentence(string displayText, WordDetail[] wordDetails = null)
    {

        int textCount = QuestionController.Instance.currentQuestion.QuestionTexts.Length;
        string originalQuestion = QuestionController.Instance.currentQuestion.qa.question;

        bool startsWithUnderscore = originalQuestion.StartsWith("_");
        if (!startsWithUnderscore)
        {
            // Capitalize the first non-empty word in displayText
            string lowerWord = displayText.ToLower();
            displayText = lowerWord;

            //Debug.Log("FKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK" + displayText);
        }

        // Split answer into words
        var answerWords = displayText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < textCount; i++)
        {
            TextMeshProUGUI textpro = QuestionController.Instance.currentQuestion.QuestionTexts[i];
            if (textpro != null)
            {
                bool markerText = textpro.gameObject.name == "MarkerText";

                // Find all underscore groups
                var underscoreRegex = new Regex(@"(_+)");
                var matches = underscoreRegex.Matches(originalQuestion);

                var result = new StringBuilder();
                int answerIndex = 0;
                int lastIndex = 0;

                foreach (Match match in matches)
                {
                    // Add text before underscores, make transparent for markerText, normal for non-markerText
                    if (match.Index > lastIndex)
                    {
                        string before = originalQuestion.Substring(lastIndex, match.Index - lastIndex);
                        var words = before.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var w in words)
                        {
                            if (markerText)
                                result.Append($"<color=#00000000>{w}</color> ");
                            else
                                result.Append($"{w} ");
                        }
                    }

                    // For each blank, if there are answer words left, add one marker per word
                    if (answerIndex < answerWords.Length)
                    {
                        int blanks = matches.Count;
                        int remaining = answerWords.Length - answerIndex;
                        int markersToAdd = blanks == 1 ? remaining : 1;
                        WordDetail wordDetail = null;

                        for (int m = 0; m < markersToAdd && answerIndex < answerWords.Length; m++)
                        {
                            bool isError = true;
                            string errorType = null;
                            if (wordDetails != null && answerIndex < wordDetails.Length)
                            {
                                wordDetail = wordDetails[answerIndex];
                                errorType = wordDetail.ErrorType;
                                isError = !string.IsNullOrEmpty(errorType) && errorType != "None";
                            }

                            if (isError)
                            {
                                if(wordDetails != null)
                                {
                                    switch (wordDetail.ErrorType)
                                    {
                                        case "Mispronunciation":
                                            if (markerText)
                                                result.Append($"<mark=#FFFF00 padding='0,12,5,10'>{answerWords[answerIndex]}</mark> ");
                                            else
                                                result.Append($"{answerWords[answerIndex]} ");
                                            break;
                                        case "Omission":
                                            if (markerText)
                                                result.Append($"<mark=#2A1A17 padding='0,12,5,10'>{answerWords[answerIndex]}</mark> ");
                                            else
                                                result.Append($"<color=red>{answerWords[answerIndex]}</color> ");
                                            break;
                                        case "Insertion":
                                        case "Substitution":
                                            result.Append($"<u>{answerWords[answerIndex]}</u>");
                                            break;
                                    }                         
                                }
                                else
                                {
                                    if (markerText)
                                        result.Append($"<mark=#2A1A17 padding='0,12,5,10'>{answerWords[answerIndex]}</mark> ");
                                    else
                                        result.Append($"<color=red>{answerWords[answerIndex]}</color> ");
                                }
                            }
                            else
                                result.Append($"{answerWords[answerIndex]} ");

                            answerIndex++;
                        }
                    }
                    lastIndex = match.Index + match.Length;
                }

                // Add any remaining text after last underscore
                if (lastIndex < originalQuestion.Length)
                {
                    string after = originalQuestion.Substring(lastIndex);
                    var words = after.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var w in words)
                    {
                        if (markerText)
                            result.Append($"<color=#00000000>{w}</color> ");
                        else
                            result.Append($"{w} ");
                    }
                }

                textpro.text = result.ToString().TrimEnd();
            }
        }
    }


    private byte[] ConvertAudioClipToWav(AudioClip audioClip)
    {
        if (audioClip == null) return null;

        using (MemoryStream stream = new MemoryStream())
        {
            int sampleCount = audioClip.samples * audioClip.channels;
            int frequency = audioClip.frequency;

            // Write WAV header
            WriteWavHeader(stream, sampleCount, frequency, audioClip.channels);

            // Write audio data
            float[] samples = new float[sampleCount];
            audioClip.GetData(samples, 0);

            foreach (float sample in samples)
            {
                short intSample = (short)(sample * short.MaxValue);
                stream.WriteByte((byte)(intSample & 0xFF));
                stream.WriteByte((byte)((intSample >> 8) & 0xFF));
            }

            return stream.ToArray();
        }
    }

    private void WriteWavHeader(Stream stream, int sampleCount, int frequency, int channels)
    {
        int byteRate = frequency * channels * 2;

        stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(36 + sampleCount * 2), 0, 4);
        stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((short)1), 0, 2);
        stream.Write(BitConverter.GetBytes((short)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(frequency), 0, 4);
        stream.Write(BitConverter.GetBytes(byteRate), 0, 4);
        stream.Write(BitConverter.GetBytes((short)(channels * 2)), 0, 2);
        stream.Write(BitConverter.GetBytes((short)16), 0, 2);
        stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(sampleCount * 2), 0, 4);
    }

    /*public void UpdateButtonStates()
    {
        this.stopRecordText.GetComponent<CanvasGroup>().alpha = isRecording ? 1f : 0f;
        SetUI.Set(this.remindRecordTip, !isRecording, 0.5f);
        if (this.recordButton) this.recordButton.SetActive(!isRecording);
        if (this.stopButton) this.stopButton.SetActive(isRecording);
        if (this.playbackButton) this.playbackButton.gameObject.SetActive(!isRecording && clip != null);
    }*/

    private void UpdateUI(string message)
    {
        if (debugText != null)
        {
            debugText.text = message;
        }
    }

    public void NextQuestion()
    {
        GameController.Instance?.UpdateNextQuestion();
        var playerController = this.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.resetRetryTime();
        }
        this.switchPage(Stage.Record); // reset
    }

    public void ShowDirectCorrectAnswer()
    {
        /*if(this.answerText != null)
        {
            if(status) this.answerText.text = QuestionController.Instance.currentQuestion.correctAnswer.ToLower();
            this.answerText.color = status ? Color.white : this.answerTextOriginalColor;
            this.answerText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, status? 0f : 0.182f);
        }
        if (this.answerBox != null && this.answerBoxTexs.Length > 0)
        {
            this.answerBox.texture = this.answerBoxTexs[status? 1 : 0];
        }*/

        var correctAnswer = QuestionController.Instance.currentQuestion.correctAnswer;
        var answerWords = correctAnswer.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        this.showCorrectSentence(correctAnswer, null);
    }

    public void ResetRecorder()
    {
        AudioController.Instance?.fadingBGM(true, 1f);
        this.playbackButton.texture = this.playbackBtnTexs[0];
        //this.ShowDirectCorrectAnswer(false);
        if (this.stopRecordText != null) this.stopRecordText.text = "00:00:00";
        if (this.playbackText != null) this.playbackText.text = "00:00:00";
        this.isRecording = false;
        this.recordingTime = 0f;
        //this.UpdateButtonStates();
        SetUI.SetGroup(this.pages, 0);
        this.controlResultPage(-1);
        if (this.submitAudioText != null) this.submitAudioText.text = "Listening...";
        SetUI.Set(this.stopButton, false, 0f, 0.5f);
    }
}

[Serializable]
public class UploadResponse
{
    public string url;
    public string path;
    public int id;
    public string key;
    public string token;
    public string filename;
    public string checksum;
    public int len;
    public int log_id;
    public int server;
    public int code;
}

[Serializable]
public class RecognitionResponse
{
    public int code;
    public RecognitionResult result;
    public int version;
}

[Serializable]
public class RecognitionResult
{
    public string RecognitionStatus;
    public float Offset;
    public float Duration;
    public string DisplayText;
    public float SNR;
    public NBest[] NBest;
}

[Serializable]
public class NBest
{
    public float Confidence;
    public string Lexical;
    public string ITN;
    public string MaskedITN;
    public string Display;
    public int AccuracyScore;
    public int FluencyScore;
    public int CompletenessScore;
    public float PronScore;
    public WordDetail[] Words;
}

[Serializable]
public class WordDetail
{
    public string Word;
    public float Offset;
    public float Duration;
    public float Confidence;
    public int AccuracyScore;
    public string ErrorType;
    public SyllableDetail[] Syllables;
    public PhonemeDetail[] Phonemes;
}

[Serializable]
public class SyllableDetail
{
    public string Syllable;
    public string Grapheme;
    public float Offset;
    public float Duration;
    public int AccuracyScore;
}

[Serializable]
public class PhonemeDetail
{
    public string Phoneme;
    public float Offset;
    public float Duration;
    public int AccuracyScore;
}

[Serializable]
public class SttResponse
{
    public SttData[] data;
}

[Serializable]
public class SttData
{
    public string transcript;
    public float confidence;
    public WordData[] words;
}

[Serializable]
public class WordData
{
    public string word;
    public string startTime;
    public string endTime;
}

