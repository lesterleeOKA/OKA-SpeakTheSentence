using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RecordAudio : MonoBehaviour
{
    /*public enum RecognitionAPI
    {
        roWeb_Azure=0, //roWeb speech to text api
        recognize_tts=1, //語音辨識api
    }*/

    public enum DetectMethod
    {
        None = 0,
        Word = 1,
        FullSentence = 2,
        Spelling = 3
    }

    public enum Stage
    {
        Record=0,
        Recording = 1,
        UploadClip = 2,
        PlaybackResult = 3,
    }
    public DetectMethod detectMethod = DetectMethod.FullSentence;
    public Stage stage = Stage.Record;
    //public RecognitionAPI recognitionAPI = RecognitionAPI.recognize_tts;
    public TextMeshProUGUI debugText, answerText, submitAudioText;
    public Color32 answerTextOriginalColor;
    public RawImage answerBox;
    public Texture[] answerBoxTexs;
    private AudioClip clip, originalTrimmedClip;
    public bool useHighPassFilter = false;
    [Header("Audio Pages for different process")]
    public CanvasGroup[] pages;

    [Header("Result Page for different control button")]
    public CanvasGroup[] resultBtns;

    [SerializeField] private AudioSource playbackSource;
    [SerializeField] private CanvasGroup recordButton;
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
    public UnityEngine.Audio.AudioMixerGroup recordingMixerGroup;

    private bool isRecording = false;
    public bool isPlaying = false;
    private float recordingTime = 0f;
    public SttResponse sttResponse;
    public RecognitionResult recognitionResult;
    private bool grantedMicrophone = false;
    public int passAccuracyScore = 60;
    public int passPronScore = 60;
    public bool ttsFailure = false;
    public bool ttsDone = false;
    public bool isInitialized = false;
    public string textToRecognize = "";
    private string ApiUrl = "";
    private string JwtToken = "eyJ0eXAiOiJqd3QiLCJhbGciOiJIUzI1NiJ9.eyJsb2dfZW5hYmxlZCI6IjEiLCJ0b2tlbiI6IjUyNzcwMS04MTcyNGIyYTIxODk4YTE2NTA0ZTZiMTg0ZWZlMWQ5Mjc2OGIyYWM1YmI2ZmExMDc4NDVlZjM1MDRjNTY3NDBlIiwiZXhwaXJlcyI6MTgwODUzNjQ5NSwicmVuZXdfZW5hYmxlZCI6MSwidGltZSI6IjIwMjUtMDQtMjQgMDM6MTQ6NTUgR01UIiwidWlkIjoiNTI3NzAxIiwidXNlcl9yb2xlIjoiMiIsInNjaG9vbF9pZCI6IjMxNiIsImlwIjoiOjoxIiwidmVyc2lvbiI6bnVsbCwiZGV2aWNlIjoidW5rbm93biJ9.SO79u9MBCflyYh_TcsIBG740pWXgKPZOAsGNZESkoqo";

    public WordDetail[] wordDetails;
    private Coroutine loadingTextCoroutine;

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

        SetUI.Set(this.recordButton, false, 0f, 0.5f);
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
                float gain = this.GetPlatformSpecificGain();
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
                }
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

    private void ApplyHighPassFilter(float[] samples, int channels, float cutoffFrequency, float sampleRate)
    {
        float rc = 1.0f / (cutoffFrequency * 2 * Mathf.PI);
        float dt = 1.0f / sampleRate;
        float alpha = rc / (rc + dt);

        // Process each channel independently
        for (int channel = 0; channel < channels; channel++)
        {
            float previousSample = samples[channel];
            for (int i = channel; i < samples.Length; i += channels)
            {
                float currentSample = samples[i];
                samples[i] = alpha * (samples[i] - previousSample);
                previousSample = currentSample;
            }
        }
    }

    public void PlayAgainHint()
    {
        QuestionController.Instance?.currentQuestion.stopAudio();
        float defaultDelay = !this.isInitialized ? 1f : 0f;
        this.hintBox.GetComponent<TextToSpeech>()?.PlayAudio(() =>
        {
            SetUI.Set(this.hintBox, true);
            SetUI.Set(this.remindRecordBox, false);
        },
        () =>
        {
            SetUI.Set(this.hintBox, false);
            SetUI.Set(this.remindRecordBox, true);
            if (!this.isInitialized)
            {
                GameController.Instance?.UpdateNextQuestion();
                SetUI.Set(this.recordButton, true, 0f);
                this.isInitialized = true;
            }
        },
        defaultDelay
        );
    }

    void switchPage(Stage _stage)
    {
        this.stage = _stage;
        LogController.Instance.debug($"Current Recording Stage: {this.stage}"); 
        switch (this.stage)
        {
            case Stage.Record:
                this.ResetRecorder();
                if (!this.isInitialized) this.PlayAgainHint();
                break;
            case Stage.Recording:
                this.StopPlayback();
                QuestionController.Instance?.currentQuestion.stopAudio();
                QuestionController.Instance?.currentQuestion.setInteractiveOfQuestionBoards(false);
                SetUI.SetGroup(this.pages, 1);
                StartCoroutine(this.delayEnableStopRecorder());
                break;
            case Stage.UploadClip:
                AudioController.Instance?.fadingBGM(true, 1f);
                QuestionController.Instance?.currentQuestion.setInteractiveOfQuestionBoards(true);
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
        if(this.isInitialized)
        {
            SetUI.Set(this.recordButton, true, 0f, 0.5f);
        }
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

        if (this.isRecording) return;
        if (!this.grantedMicrophone)
        {
            LogController.Instance.debug("Microphone permission not granted. Please allow access and try again.");
            // Optionally, re-trigger permission request
            StartCoroutine(this.initMicrophonePermission(1f));
            return;
        }
        AudioController.Instance?.fadingBGM(false, 0f);
        LogController.Instance?.debug($"Recording started: {this.isRecording}");
        var microphoneDevices = LoaderConfig.Instance.microphoneDevice;
        if (!microphoneDevices.HasMicrophoneDevices)
        {
            LogController.Instance?.debug("No microphone devices available. use default microphone");
            this.clip = Microphone.Start("", true, maxRecordLength, 44100);
        }
        else
        {
            this.clip = Microphone.Start(microphoneDevices.selectedDeviceName, true, maxRecordLength, 44100);
        }

        if (this.clip)
        {
            this.waveformVisualizer?.ClearTexture();
            this.isRecording = true;
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
    return 1f;
#elif UNITY_IOS
    // iPad browsers may already apply AGC, so use lower gain
    return 1.0f;
#else
    // Default gain for other platforms
    return 2f;
#endif
    }

    private void NormalizeAndAmplifyAudioClip(AudioClip audioClip, float gain)
    {
        if (audioClip == null) return;

        // Get audio samples
        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

#if UNITY_WEBGL && !UNITY_EDITOR
        // For WebGL, skip normalization/amplification to avoid distortion
        audioClip.SetData(samples, 0);
        return;
#endif
        // Apply high-pass filter if enabled
        if (this.useHighPassFilter)
        {
            // Assuming the audio has multiple channels, process each separately
            this.ApplyHighPassFilter(samples, audioClip.channels, cutoffFrequency: 50f, sampleRate: audioClip.frequency);
        }

        // Normalize samples
        float maxAmplitude = 0f;
        foreach (var sample in samples)
        {
            maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(sample));
        }

        // Avoid division by zero
        if (maxAmplitude > 0f)
        {
            float scale = gain / maxAmplitude;
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Clamp(samples[i] * scale, -1f, 1f);
        }

        // Write the processed samples back to the audio clip
        audioClip.SetData(samples, 0);
    }

    private IEnumerator AnimateLoadingText(string content = "")
    {
        string[] loadingStates = { $"{content}...", $"{content}..", $"{content}." };
        int index = 0;
        while (true)
        {
            if (this.submitAudioText != null)
                this.submitAudioText.text = loadingStates[index];
            index = (index + 1) % loadingStates.Length;
            yield return new WaitForSeconds(0.5f); // Adjust speed as needed
        }
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            LogController.Instance?.debug("StopRecording called but not currently recording.");
            return;
        }

        if (this.loadingTextCoroutine != null)
            StopCoroutine(this.loadingTextCoroutine);

        this.loadingTextCoroutine = StartCoroutine(AnimateLoadingText("Loading"));

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
        this.originalTrimmedClip = AudioClip.Create(trimmedClip.name + "_original", trimmedClip.samples, trimmedClip.channels, trimmedClip.frequency, false);
        float[] originalSamples = new float[trimmedClip.samples * trimmedClip.channels];
        trimmedClip.GetData(originalSamples, 0);
        this.originalTrimmedClip.SetData(originalSamples, 0);

        //float gain = this.GetPlatformSpecificGain();
        //LogController.Instance?.debug("Gain scale: " + gain);
        //this.NormalizeAndAmplifyAudioClip(this.clip, gain);

        // Parallel API calls
        bool azureDone = false;
        this.ttsDone = false;
        string azureTranscript = null;
        string azureError = null, ttsError = null;
        this.ttsFailure = false;

        // Start Azure STT
        StartCoroutine(SendAudioToAzureApi(this.clip,
            (transcript) => {
                azureTranscript = transcript;
                azureDone = true;
            },
            (error) => {
                azureError = error;
                azureDone = true;
            }
        ));

        // Start TTS recognition
        StartCoroutine(UploadAudioFileServer(this.clip,
            (response) => {
                LogController.Instance.debug($"Start to pass to recognition request");
            },
            (error) => {
                ttsError = error;
                this.ttsDone = true;
            }
        ));

        // Wait for both to finish
        while (!azureDone || !this.ttsDone)
            yield return null;

        if (ttsError != null && azureError != null)
        {
            this.UpdateUI($"Both recognitions failed.\nAzure error: {azureError}\nTTS error: {ttsError}");
            this.switchPage(Stage.Record);
        }
        else
        {
            // Final UI/page update
            if (!this.ttsFailure)
            {
                this.UpdateUI("TTS recognition passed.");
            }
            else
            {
                this.UpdateUI("TTS failed, fallback to Azure STT result.");
                this.switchPage(Stage.PlaybackResult);
                var playerController = this.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.submitAnswer(this.answerText.text);
                }
            }
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
            if (this.originalTrimmedClip == null)
            {
                this.UpdateUI("No recording available for playback.");
                return;
            }
            if (playbackSource == null)
            {
                playbackSource = gameObject.AddComponent<AudioSource>();
            }

#if UNITY_EDITOR
            playbackSource.outputAudioMixerGroup = this.recordingMixerGroup;
#endif
            playbackSource.clip = this.originalTrimmedClip;
            playbackSource.loop = false;
            playbackSource.volume = 2.0f;

            if (Mathf.Approximately(playbackSource.time, playbackSource.clip.length - 0.01f))
            {
                playbackSource.time = 0f;
            }

            playbackSource.Play();

            this.isPlaying = true;
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

    public string TextToRecognize
    {
        get
        {
            this.textToRecognize = "";
            switch (this.detectMethod)
            {
                case DetectMethod.None:
                    this.textToRecognize = "";
                    break;
                case DetectMethod.Word:
                    this.textToRecognize = QuestionController.Instance.currentQuestion.correctAnswer;
                    break;
                case DetectMethod.FullSentence:
                    this.textToRecognize = QuestionController.Instance.currentQuestion.fullSentence;
                    break;
                case DetectMethod.Spelling:
                    var word = QuestionController.Instance.currentQuestion.correctAnswer;
                    this.textToRecognize = string.Join(" ", word.ToCharArray());
                    break;
            }
            return this.textToRecognize;
        }
    }

    private IEnumerator SendAudioToAzureApi(AudioClip audioClip, Action<string> onSuccess, Action<string> onError)
    {
        if (this.answerText != null) this.answerText.text = "";
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

        string hostName = string.IsNullOrEmpty(LoaderConfig.Instance.CurrentHostName) ?
                          "dev.openknowledge.hk" : LoaderConfig.Instance.CurrentHostName;

        this.ApiUrl = $"https://{hostName}/RainbowOne/index.php/PHPGateway/proxy/2.8/";
        LogController.Instance.debug($"SendAudioToAzureApi: {this.ApiUrl}");
        UnityWebRequest request = UnityWebRequest.Post(this.ApiUrl, form);
        request.SetRequestHeader("Authorization", $"Bearer {JwtToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            try
            {
                // Parse the JSON response
                this.sttResponse = JsonUtility.FromJson<SttResponse>(responseText);
                LogController.Instance.debug($"STT Response: {responseText}");
                if (this.sttResponse.data != null && this.sttResponse.data.Length > 0)
                {

                    StringBuilder transcriptBuilder = new StringBuilder();
                    foreach (var result in this.sttResponse.data)
                    {
                        if (!string.IsNullOrWhiteSpace(result.transcript))
                        {
                            transcriptBuilder.Append(result.transcript.Trim());
                            transcriptBuilder.Append(" ");
                        }
                    }
                    string combinedTranscript = transcriptBuilder.ToString().Trim();
                    if (this.answerText != null && string.IsNullOrEmpty(this.answerText.text))
                        this.answerText.text = combinedTranscript;

                    onSuccess.Invoke(combinedTranscript);
                }
                else
                {
                    this.UpdateUI("No transcription data available.");
                    onError.Invoke("No transcription data response.");
                }
            }
            catch (Exception ex)
            {
                LogController.Instance.debugError($"Error parsing JSON: {ex.Message}");
                this.UpdateUI("Error processing transcription response.");
                onError.Invoke("Error processing transcription response.");
            }
        }
        else
        {
            this.UpdateUI($"Error: {request.error}");
            onError.Invoke("Error processing response.");
        }
    }

    public IEnumerator UploadAudioFileServer(AudioClip audioClip, 
                                       Action<string> onSuccess, 
                                       Action<string> onError, 
                                       int retryCount = 5, 
                                       float retryDelay = 2f)
    {
        string hostName = string.IsNullOrEmpty(LoaderConfig.Instance.CurrentHostName) ?
                    "dev.openknowledge.hk" : LoaderConfig.Instance.CurrentHostName;

        string uploadUrl = $"https://{hostName}/RainbowOne/index.php/transport/Slave/upload/2";

        LogController.Instance.debug($"Upload URL: {uploadUrl}");
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
            if (!string.IsNullOrEmpty(this.JwtToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {this.JwtToken}");
            }
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LogController.Instance.debug("File uploaded successfully.");
                var uploadResponse = JsonUtility.FromJson<UploadResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(uploadResponse.url))
                {
                    LogController.Instance.debug("uploadResponse url : " + uploadResponse.url);
                    string audioUrl = "//" + hostName + uploadResponse.url.Replace("\\", "");

                    if (!string.IsNullOrEmpty(LoaderConfig.Instance.CurrentHostName))
                    {
                        if (LoaderConfig.Instance.CurrentHostName.Contains("www.rainbowone.app") ||
                            LoaderConfig.Instance.CurrentHostName.Contains("www.starwishparty.com"))
                        {
                            audioUrl = uploadResponse.url;
                        }
                    }
                    onSuccess?.Invoke("Success to pass to recognition request.");
                    yield return StartCoroutine(this.SendAudioRecognitionRequest(
                        audioUrl,
                        this.TextToRecognize
                    ));
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
                if (this.loadingTextCoroutine != null)
                {
                    StopCoroutine(this.loadingTextCoroutine);
                    this.loadingTextCoroutine = null;
                }
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
        yield return new WaitForSeconds(1f);
        this.UpdateUI("Converting AudioClip to binary data...");

        this.UpdateUI("Sending audio recognition request...");
        string jsonPayload = $"[\"{audioUrl}\",\"{textToRecognize}\",\"{language}\",\"{purpose}\"]";

        LogController.Instance.debug("jsonPayload: " + jsonPayload);

        WWWForm form = new WWWForm();
        form.AddField("api", "ROSpeechRecognition.recognize_tts");
        form.AddField("json", jsonPayload);

        string hostName = string.IsNullOrEmpty(LoaderConfig.Instance.CurrentHostName) ?
                  "dev.openknowledge.hk" : LoaderConfig.Instance.CurrentHostName;
        this.ApiUrl = $"https://{hostName}/RainbowOne/index.php/PHPGateway/proxy/2.8/";

        UnityWebRequest request = UnityWebRequest.Post(this.ApiUrl, form);
        request.SetRequestHeader("Authorization", $"Bearer {this.JwtToken}");

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
                    StringBuilder result = new StringBuilder();
                    string transcript = "";
                    string displayText = "";
                    string correctAnswer = recognitionResponse.result.DisplayText;
                    bool hasErrorWord = false;
                    //var correctAnswerWords = QuestionController.Instance.currentQuestion.correctAnswer.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    this.wordDetails = null;
                    // Log the NBest array
                    if (Best != null)
                    {
                        foreach (var nBest in Best)
                        {
                            result.AppendLine($"Score: {nBest.PronScore}");
                            if(nBest.Words != null)
                            {
                                this.wordDetails = nBest.Words;
                                var correctAnswerWords = QuestionController.Instance.currentQuestion.correctAnswer
                                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                var correctAnswerWordsCount = QuestionController.Instance.currentQuestion.correctAnswer
    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                                var correctAnswerWordSet = new HashSet<string>(
                                    correctAnswerWords.Select(w => Regex.Replace(w, @"[^\w]", "").ToLower())
                                );

                                foreach (var word in nBest.Words)
                                {
                                    string cleanWord = Regex.Replace(word.Word, @"[^\w]", "").ToLower();

                                    if (word.ErrorType == "Omission")
                                    {
                                        this.ttsFailure = true;
                                        if (this.accurancyText != null)
                                            this.accurancyText.text = $"Word missing, please retry";
                                        hasErrorWord = true;
                                    }
                                    else {
                                        if (correctAnswerWordSet.Contains(cleanWord))
                                        {
                                            if (word.ErrorType == "Mispronunciation")
                                            {
                                                this.ttsFailure = true;
                                                if (this.accurancyText != null)
                                                    this.accurancyText.text = $"Mispronunciation detected, please retry";
                                                hasErrorWord = true;
                                            }
                                            else if (word.AccuracyScore < 85 && (QuestionController.Instance.currentQuestion.questiontype == QuestionType.SentenceCorrect || correctAnswerWordsCount == 1))
                                            {
                                                this.ttsFailure = true;
                                                if (this.accurancyText != null)
                                                    this.accurancyText.text = $"Mispronunciation detected, please retry";
                                                hasErrorWord = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                result.AppendLine("No words found in NBest.");
                            }

                            if (this.accurancyText != null && !hasErrorWord) 
                                this.accurancyText.text = $"Rating: {nBest.PronScore}%";

                            if (nBest.AccuracyScore <= this.passAccuracyScore &&
                                nBest.PronScore <= this.passPronScore)
                            {
                                this.ttsFailure = true;
                            }
                        }
                        this.ttsDone = true;

                        if (!this.ttsFailure)
                        {
                            transcript = correctAnswer;
                            string correctAns = QuestionController.Instance.currentQuestion.fullSentence;
                            displayText = Regex.Replace(transcript, @"[^\w\s]", "").ToLower();
                            result.AppendLine($"DisplayText: {displayText}");
                            //transcript = Regex.Replace(recognitionResult.DisplayText, @"[^\w\s]", "").ToLower();
                            this.UpdateUI(result.ToString());
                            if (this.answerText != null) this.answerText.text = correctAns;
                            this.switchPage(Stage.PlaybackResult);

                            var playerController = this.GetComponent<PlayerController>();
                            if (playerController != null)
                            {
                                playerController.submitAnswer(
                                    correctAns, 
                                    () =>
                                    {
                                        this.showCorrectSentence(
                                            QuestionController.Instance.currentQuestion.fullSentence,
                                            this.wordDetails);
                                    }
                                 );
                            }
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
                if (this.accurancyText != null) this.accurancyText.text = $"Rating: {0}%";
                if (this.answerText != null) this.answerText.text = "";
                var playerController = this.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.submitAnswer("");
                }
            }
        }
        else
        {
            this.UpdateUI($"Error: {request.error}");
        }
    }

    public void showCorrectSentence(string displayText, WordDetail[] wordDetails = null)
    {
        var currentQuestion = QuestionController.Instance.currentQuestion;
        if (currentQuestion.underlineWordRecordIcon != null) currentQuestion.underlineWordRecordIcon.SetActive(false);
        TextMeshProUGUI questionTextpro = currentQuestion.QuestionTexts[0];

        int textCount = currentQuestion.QuestionTexts.Length;
        string originalQuestion = currentQuestion.qa.question;

        displayText = displayText.TrimStart();

        // Split the sentence into words (preserve punctuation if needed)
        string[] sentenceWords = displayText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < textCount; i++)
        {
            TextMeshProUGUI textpro = currentQuestion.QuestionTexts[i];
            if (textpro != null)
            {
                bool markerText = textpro.gameObject.name == "MarkerText";
                var result = new StringBuilder();

                foreach (string word in sentenceWords)
                {
                    WordDetail wordDetail = null;
                    string errorType = null;
                    bool isError = false;

                    if (wordDetails != null)
                    {
                        // Find the WordDetail for this word (case-insensitive, ignore punctuation)
                        string cleanWord = Regex.Replace(word, @"[^\w]", "");
                        wordDetail = Array.Find(wordDetails, wd =>
                            string.Equals(wd.Word, cleanWord, StringComparison.OrdinalIgnoreCase));
                        if (wordDetail != null)
                        {
                            errorType = wordDetail.ErrorType;
                            isError = !string.IsNullOrEmpty(errorType) && errorType != "None";
                        }
                    }

                    if (isError)
                    {

                        LogController.Instance.debug("result: " + errorType);
                        switch (errorType)
                        {
                            case "Mispronunciation":
                                if (markerText)
                                    result.Append($"<mark=#FFFF00 padding='0,12,5,10'>{word}</mark> ");
                                else
                                    result.Append($"{word} ");
                                break;
                            case "Omission":
                                if (markerText)
                                    result.Append($"<mark=#2A1A17 padding='0,12,5,10'>{word}</mark> ");
                                else
                                    result.Append($"<color=red>{word}</color> ");
                                break;
                            case "Insertion":
                            case "Substitution":
                                result.Append($"<u>{word}</u> ");
                                break;
                            default:
                                result.Append($"{word} ");
                                break;
                        }
                    }
                    else
                    {
                        result.Append($"{word} ");
                    }
                }

                //Debug.Log("result: " + result.ToString().TrimEnd());
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

        var questionType = QuestionController.Instance.currentQuestion.questiontype;

        switch (questionType)
        {
            case QuestionType.Audio:
                this.showCorrectSentence(QuestionController.Instance.currentQuestion.correctAnswer, this.wordDetails);
                break;
            case QuestionType.FillInBlank:
                this.showCorrectSentence(QuestionController.Instance.currentQuestion.fullSentence, this.wordDetails);
                break;
        }

        if (this.accurancyText != null)
            this.accurancyText.text = $"Word missing.";
    }

    public void ResetRecorder()
    {
        this.ttsDone = false;
        this.ttsFailure = false;
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

        if (this.loadingTextCoroutine != null)
        {
            StopCoroutine(this.loadingTextCoroutine);
            this.loadingTextCoroutine = null;
        }
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

