using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance = null;
    [Header("<<<<<<<<<<<<<Load Question Json methods and Settings>>>>>>>>>>>>>")]
    public LoadMethod loadMethod = LoadMethod.UnityWebRequest;
    public string jsonFileName = "Question.json";
    [Space(10)]
    [Header("<<<<<<<<<<<<<Load Images methods and Settings>>>>>>>>>>>>>")]
    public LoadImage loadImage;
    [Space(10)]
    [Header("<<<<<<<<<<<<<Load Audio methods and Settings>>>>>>>>>>>>>>")]
    public LoadAudio loadAudio;
    [Space(10)]
    public QuestionData questionData;
    public int totalItems;
    public int loadedItems;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }


    public void LoadQuestionFile(string unitKey = "", Action onCompleted = null)
    {
        try
        {
            var questionJson = LoaderConfig.Instance.apiManager.questionJson;
            if (!string.IsNullOrEmpty(questionJson) && LoaderConfig.Instance.apiManager.IsLogined)
            {
                QuestionDataWrapper wrapper = JsonUtility.FromJson<QuestionDataWrapper>("{\"QuestionDataArray\":" + questionJson + "}");
                QuestionData _questionData = new QuestionData
                {
                    questions = new List<QuestionList>(wrapper.QuestionDataArray)
                };
                LogController.Instance?.debug("Load Question from API");
                this.loadQuestionFromAPI(_questionData, onCompleted);
            }
            else
            {
                LogController.Instance?.debug("Missing jwt for loading question from api, switch to load question from json");
                StartCoroutine(this.loadQuestionFile(unitKey, onCompleted));
            }
        }
        catch (Exception ex)
        {
            LogController.Instance?.debugError(ex.Message);
            LoaderConfig.Instance.apiManager.IsShowLoginErrorBox = true;
            StartCoroutine(this.loadQuestionFile(unitKey, onCompleted));
        }
    }

    private void loadQuestionFromAPI(QuestionData _questionData = null, Action onCompleted = null)
    {
        if (_questionData != null)
        {
            this.questionData = _questionData;
            LogController.Instance?.debug($"loaded api questions: {this.questionData.questions.Count}");
            this.GetRandomQuestions(onCompleted);
        }
    }

    IEnumerator loadQuestionFile(string unitKey = "", Action onCompleted = null)
    {
        this.jsonFileName = LoaderConfig.Instance.gameSetup.lang == 1 ? this.jsonFileName + "_ch" : this.jsonFileName;
        var questionPath = Path.Combine(Application.streamingAssetsPath, this.jsonFileName + ".json");

        switch (this.loadMethod)
        {
            case LoadMethod.www:
                WWW www = new WWW(questionPath);
                yield return www;

                if (!string.IsNullOrEmpty(www.error))
                {
                    LogController.Instance?.debugError($"Error loading question json: {www.error}");
                }
                else
                {
                    LogController.Instance?.debug(questionPath);
                    var json = www.text;
                    this.questionData = JsonUtility.FromJson<QuestionData>(json);

                    if (!string.IsNullOrEmpty(unitKey))
                    {
                        this.questionData.questions = this.questionData.questions.Where(q => q.qid != null && q.qid.StartsWith(unitKey)).ToList();
                    }

                    if (this.questionData.questions[0].questionType == "picture" && this.loadImage.loadImageMethod == LoadImageMethod.AssetsBundle)
                    {
                        yield return this.loadImage.loadImageAssetBundleFile(this.questionData.questions[0].qid);
                    }

                    LogController.Instance?.debug($"loaded filtered questions: {this.questionData.questions.Count}");
                    this.GetRandomQuestions(onCompleted);
                }
                break;
            case LoadMethod.UnityWebRequest:
                using (UnityWebRequest uwq = UnityWebRequest.Get(questionPath))
                {
                    yield return uwq.SendWebRequest();

                    if (uwq.result != UnityWebRequest.Result.Success)
                    {
                        LogController.Instance?.debugError($"Error loading question json: {uwq.error}");
                    }
                    else
                    {
                        LogController.Instance?.debug(questionPath);
                        var json = uwq.downloadHandler.text;
                        this.questionData = JsonUtility.FromJson<QuestionData>(json);
                        if (!string.IsNullOrEmpty(unitKey))
                        {
                            this.questionData.questions = this.questionData.questions.Where(q => q.qid != null && q.qid.StartsWith(unitKey)).ToList();
                        }

                        if (this.questionData.questions[0].questionType == "picture" && this.loadImage.loadImageMethod == LoadImageMethod.AssetsBundle)
                        {
                            yield return this.loadImage.loadImageAssetBundleFile(this.questionData.questions[0].qid);
                        }

                        LogController.Instance?.debug($"loaded filtered questions: {this.questionData.questions.Count}");
                        this.GetRandomQuestions(onCompleted);
                    }
                }
                break;
            case LoadMethod.Resources:
                // Remove file extension for Resources.Load
                string resourcePath = this.jsonFileName;
                TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    LogController.Instance?.debugError($"Error loading question json from Resources: {resourcePath}");
                }
                else
                {
                    LogController.Instance?.debug($"Loaded question json from Resources: {resourcePath}");
                    var json = textAsset.text;
                    this.questionData = JsonUtility.FromJson<QuestionData>(json);

                    if (!string.IsNullOrEmpty(unitKey))
                    {
                        this.questionData.questions = this.questionData.questions.Where(q => q.qid != null && q.qid.StartsWith(unitKey)).ToList();
                    }

                    if (this.questionData.questions[0].questionType == "picture" && this.loadImage.loadImageMethod == LoadImageMethod.AssetsBundle)
                    {
                        yield return this.loadImage.loadImageAssetBundleFile(this.questionData.questions[0].qid);
                    }

                    LogController.Instance?.debug($"loaded filtered questions: {this.questionData.questions.Count}");
                    this.GetRandomQuestions(onCompleted);
                }
                break;
        }
    }

    private void ShuffleQuestions(bool rand = true, Action onComplete = null)
    {
        if (rand) this.questionData.questions.Sort((a, b) => UnityEngine.Random.Range(-1, 2));
        var isLogined = LoaderConfig.Instance.apiManager.IsLogined;
        this.totalItems = this.questionData.questions.Count;
        this.loadedItems = 0;

        for (int i = 0; i < this.totalItems; i++)
        {
            var qa = this.questionData.questions[i];
            string folderName = qa.questionType == "fillInBlank" ? "audio" : qa.questionType;
            string qid = qa.qid;
            string mediaUrl = qa.media != null && qa.media.Length > 0 ? APIConstant.blobServerRelativePath + qa.media[0] : "";

            switch (qa.questionType)
            {
                case "Text":
                case "text":
                case "InsertWord":
                case "insertWord":
                case "SentenceCorrect":
                case "sentenceCorrect":
                    this.loadedItems++;
                    if (this.loadedItems == this.totalItems) {
                        ExternalCaller.UpdateLoadBarStatus("Loaded QA");
                        onComplete?.Invoke();
                    }
                    break;
                case "Picture":
                case "picture":
                    if (string.IsNullOrEmpty(qa.correctAnswer) && !string.IsNullOrEmpty(qa.question))
                        qa.correctAnswer = qa.question;
                    StartCoroutine(
                        this.loadImage.Load(
                            isLogined ? "" : folderName,
                            isLogined ? mediaUrl : qid,
                            tex =>
                            {
                                qa.texture = tex;
                                this.loadedItems++;
                                if (this.loadedItems == this.totalItems) {
                                    ExternalCaller.UpdateLoadBarStatus("Loaded Images");
                                    onComplete?.Invoke();
                                }
                            }
                         )
                      );
                    break;
                case "Audio":
                case "audio":
                    StartCoroutine(
                        this.loadAudio.Load(
                            isLogined ? "" : folderName,
                            isLogined ? mediaUrl : qid,
                            audio =>
                            {
                                qa.audioClip = audio;
                                this.loadedItems++;
                                if (this.loadedItems == this.totalItems) {
                                    ExternalCaller.UpdateLoadBarStatus("Loaded Audio");
                                    onComplete?.Invoke();
                                }
                            }
                        )
                    );
                    break;
                case "FillInBlank":
                case "fillInBlank":
                    if (!string.IsNullOrEmpty(mediaUrl))
                    {
                        string extension = Path.GetExtension(mediaUrl).ToLowerInvariant();
                        if (extension == this.loadAudio.AudioExtension)
                        {
                            StartCoroutine(
                                this.loadAudio.Load(
                                    isLogined ? "" : "audio",
                                    isLogined ? mediaUrl : qid,
                                    audio =>
                                    {
                                        qa.audioClip = audio;
                                        this.loadedItems++;
                                        if (this.loadedItems == this.totalItems)
                                        {
                                            ExternalCaller.UpdateLoadBarStatus("Loaded Audio");
                                            onComplete?.Invoke();
                                        }
                                    }
                                )
                            );
                            continue;
                        }
                        else if (extension == this.loadImage.ImageExtension)
                        {
                            StartCoroutine(
                                this.loadImage.Load(
                                    isLogined ? "" : "picture",
                                    isLogined ? mediaUrl : qid,
                                    tex =>
                                    {
                                        qa.texture = tex;
                                        this.loadedItems++;
                                        if (this.loadedItems == this.totalItems)
                                        {
                                            ExternalCaller.UpdateLoadBarStatus("Loaded Images");
                                            onComplete?.Invoke();
                                        }
                                    }
                                )
                            );
                            continue;
                        }
                    }
                    else
                    {
                        this.loadedItems++;
                        if (this.loadedItems == this.totalItems)
                        {
                            ExternalCaller.UpdateLoadBarStatus("Loaded QA");
                            onComplete?.Invoke();
                        }
                    }
                    break;
                default:
                    LogController.Instance?.debug($"Unexpected QuestionType: {qa.questionType}");
                    this.loadedItems++;
                    if (this.loadedItems == this.totalItems)
                    {
                        ExternalCaller.UpdateLoadBarStatus("Loaded QA");
                        onComplete?.Invoke();
                    }
                    break;
            }
        }
    }

    private void GetRandomQuestions(Action onCompleted = null)
    {
        if (this.questionData.questions.Count > 1 && this.questionData.questions[0] != this.questionData.questions[this.questionData.questions.Count - 1])
        {
            this.ShuffleQuestions(true, onCompleted);
        }
        else
        {
            this.ShuffleQuestions(false, onCompleted);
        }
    }

    public void ReorderTheQuestionList()
    {
        LogController.Instance?.debug("Re order the questions list!");
        this.questionData.questions = this.questionData.questions.OrderBy(q => UnityEngine.Random.Range(0f, 1f)).ToList();
    }
}
