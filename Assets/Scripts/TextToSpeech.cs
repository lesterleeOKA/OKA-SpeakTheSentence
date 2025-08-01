using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class TextToSpeech : MonoBehaviour
{
    public enum Language
    {
        Eng,
        CH
    }

    public Language language = Language.Eng;
    public AudioSource audioSource;
    public TextMeshProUGUI contentText;
    [Range(0.1f, 2f)]
    public float speed = 0.9f;
    [Range(0.1f, 2f)]
    public float pitch = 1f;
    // Start is called before the first frame update
    void Start()
    {
        if(this.audioSource == null)
        {
           this.audioSource = this.GetComponent<AudioSource>();
        }

        if (this.contentText == null)
        {
            this.contentText = this.GetComponent<TextMeshProUGUI>();
        }

        if(this.contentText == null || string.IsNullOrEmpty(this.contentText.text))
        {
            LogController.Instance?.debugError("Content text is not set or empty.");
            return;
        }
        else
        {
            this.StartCoroutine(LoadTextToAudioFromAPI(this.contentText.text));
        }
    }

    public IEnumerator LoadTextToAudioFromAPI(string answer)
    {
        string requestUrl = "";

        switch (this.language)
        {
            case Language.Eng:
                requestUrl = $"https://rainbowone.azurewebsites.net/CI2/index.php/TTS/request_token?gender=F&txt={answer}&speed={this.speed}&lang=en-GB&pitch={this.pitch}&name=en-GB-LibbyNeural&redirect=1";
                break;
            case Language.CH:
                requestUrl = $"https://rainbowone.azurewebsites.net/CI2/index.php/TTS/request_token?txt={answer}&redirect=1";
                break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(requestUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogController.Instance?.debugError("Error requesting audio URL: " + request.error);
                yield break;
            }

            string audioUrl = request.url;
            LogController.Instance?.debug("Downloaded Audio URL:" + audioUrl);
            using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
            {
                yield return audioRequest.SendWebRequest();

                if (audioRequest.result != UnityWebRequest.Result.Success)
                {
                    LogController.Instance?.debugError("Error downloading audio: " + audioRequest.error);
                    yield break;
                }

                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                if (audioClip != null)
                {
                    this.audioSource.clip = audioClip;
                }
                else
                {
                    LogController.Instance?.debugError("Error converting audio to AudioClip");
                    yield break;
                }


            }
        }
    }

    public void PlayAudio(Action beforePlay = null, Action afterPlay = null)
    {
        StartCoroutine(this.playAudio(beforePlay, afterPlay));
    }

    private IEnumerator playAudio(Action beforePlay = null, Action afterPlay = null)
    {
        beforePlay?.Invoke();
        if (this.audioSource != null && this.audioSource.clip != null)
        {
            this.audioSource.Play();
            yield return new WaitWhile(() => this.audioSource.isPlaying);
        }
        else
        {
            // If no audio, wait a short time for UX consistency
            yield return new WaitForSeconds(1f);
        }

        afterPlay?.Invoke();
    }

    public void StopAudio()
    {
        if (this.audioSource != null && this.audioSource.isPlaying)
        {
            this.audioSource.Stop();
        }
    }   
}
