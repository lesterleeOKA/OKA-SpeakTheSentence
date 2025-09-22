using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class QuestionDataWrapper
{
    public QuestionList[] QuestionDataArray;
}

[Serializable]
public class QuestionData
{
    public string gameTitle;
    public string instruction;
    public List<QuestionList> questions;
}
[Serializable]
public class QuestionList
{
    public int id;
    public string qid;
    public string questionType;
    public string question;
    public string[] answers;
    public string correctAnswer;
    public string fullSentence;
    public string wrongWord;
    public int star;
    public score score;
    public int correctAnswerIndex;
    public int maxScore;
    public learningObjective learningObjective;
    //public media[] media; 
    public string[] media;
    public Texture texture;
    public AudioClip audioClip;
    public string questionHint;
}

[Serializable]
public class media
{
    public string url;
    public string name;
}

[Serializable]
public class score
{
    public int full;
    public int n;
    public int unit;
}

[Serializable]
public class learningObjective
{
}


public enum QuestionType
{
    None = 0,
    Text = 1,
    Picture = 2,
    Audio = 3,
    FillInBlank = 4,
    SentenceCorrect = 5,
}

[Serializable]
public class CurrentQuestion
{
    public Sprite underlineWordRecordIconSprite;
    public GameObject underlineWordRecordIcon;
    [Range(0f, 1f)]
    public float underlineIconScale = 0.85f;
    public bool useSeparatedWordsWithUnderline = false;
    public int numberQuestion = 0;
    public int answeredQuestion = 0;
    public QuestionType questiontype = QuestionType.None;
    public QuestionList qa = null;
    public string fullSentence = "";
    public string displayQuestion = "";
    public string displayHint = "";
    public int correctAnswerId;
    public string correctAnswer;
    public string[] answersChoics;
    public CanvasGroup[] questionBgs;
    private RawImage questionImage;
    public CanvasGroup audioPlayBtn = null;
    private AspectRatioFitter aspecRatioFitter = null;
    public CanvasGroup progressiveBar;
    public Image progressFillImage;
    private TextMeshProUGUI questionText = null;
    public TextMeshProUGUI[] questionTexts = null;
    public void setProgressiveBar(bool status, int totalQuestion)
    {
        if (this.progressiveBar != null)
        {
            this.progressiveBar.DOFade(status ? 1f : 0f, 0f);
            this.progressiveBar.GetComponentInChildren<NumberCounter>().Init(this.numberQuestion.ToString(), "/" + totalQuestion);
        }
    }

    public bool updateProgressiveBar(int totalQuestion, Action onQuestionCompleted = null)
    {
        bool updating = true;
        float progress = 0f;
        if (this.numberQuestion < totalQuestion)
        {
            this.answeredQuestion += 1;
            progress = (float)this.answeredQuestion / totalQuestion;
            updating = true;
        }
        else
        {
            progress = 1f;
            updating = false;
        }

        progress = Mathf.Clamp(progress, 0f, 1f);
        if (this.progressFillImage != null && this.progressiveBar != null)
        {

            if (this.progressiveBar.GetComponent<ProgressBar>() != null)
            {
                this.progressiveBar.GetComponent<ProgressBar>().SetProgress(progress, () =>
                {
                    if (progress >= 1f) onQuestionCompleted?.Invoke();
                });
            }
            else
            {
                this.progressFillImage.DOFillAmount(progress, 0.5f).OnComplete(() =>
                {
                    if (progress >= 1f) onQuestionCompleted?.Invoke();
                });
            }

            //int percentage = (int)(progress * 100);
            //this.progressiveBar.GetComponentInChildren<NumberCounter>().Value = percentage;
            this.progressiveBar.GetComponentInChildren<NumberCounter>().Unit = "/" + totalQuestion;
            this.progressiveBar.GetComponentInChildren<NumberCounter>().Value = this.answeredQuestion;
        }
        return updating;
    }

    private void CreateUnderlineIcon(TextMeshProUGUI targetText)
    {
        if (this.underlineWordRecordIcon == null)
        {
            this.underlineWordRecordIcon = new GameObject("UnderlineIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            this.underlineWordRecordIcon.transform.SetParent(targetText.transform, false);

            var image = this.underlineWordRecordIcon.GetComponent<Image>();
            image.sprite = this.underlineWordRecordIconSprite;
            image.SetNativeSize();
            image.transform.localScale = Vector3.one;
        }
        this.underlineWordRecordIcon?.SetActive(true);
        this.UpdateUnderlineIconPosition(targetText);
    }

    private void UpdateUnderlineIconPosition(TextMeshProUGUI targetText)
    {
        targetText.ForceMeshUpdate();
        TMP_TextInfo textInfo = targetText.textInfo;

        Vector3 underlineStart = Vector3.zero;
        Vector3 underlineEnd = Vector3.zero;
        bool foundUnderline = false;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (charInfo.isVisible && charInfo.style == FontStyles.Underline)
            {
                if (!foundUnderline)
                {
                    underlineStart = charInfo.bottomLeft;
                    foundUnderline = true;
                }
                underlineEnd = charInfo.bottomRight;
            }
        }

        if (foundUnderline)
        {
            Vector3 underlineCenter = (underlineStart + underlineEnd) / 2;
            Vector3 offset = new Vector3(0, 40f, 0);
            Vector3 worldPos = targetText.transform.TransformPoint(underlineCenter + offset);
            Vector3 localPos = underlineWordRecordIcon.transform.parent.InverseTransformPoint(worldPos);

            underlineWordRecordIcon.transform.localPosition = localPos;
            underlineWordRecordIcon.transform.localScale = Vector3.one * this.underlineIconScale;
        }

    }


    public void setNewQuestion(QuestionList qa = null, int totalQuestion = 0, bool isLogined = false, Action onQuestionCompleted = null)
    {
        this.setProgressiveBar(isLogined, totalQuestion);

        if (isLogined)
        {
            bool updating = this.updateProgressiveBar(totalQuestion, onQuestionCompleted);
            if (!updating)
            {
                return;
            }
        }

        if (qa == null || this.answeredQuestion >= totalQuestion) return;
        this.qa = qa;
        this.questionText = null;
        this.questionTexts = null;
        this.displayQuestion = "";
        this.displayHint = "";
        switch (qa.questionType)
        {
            case "Picture":
            case "picture":
                SetUI.SetGroup(this.questionBgs, 0, 0f);
                this.questionImage = this.questionBgs[0].GetComponentInChildren<RawImage>();
                this.questiontype = QuestionType.Picture;
                var qaImage = qa.texture;
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;

                if (this.questionImage != null && qaImage != null)
                {
                    this.questionImage.enabled = true;
                    this.aspecRatioFitter = this.questionImage.GetComponent<AspectRatioFitter>();
                    this.questionImage.texture = qaImage;
                    var width = this.questionImage.GetComponent<RectTransform>().sizeDelta.x;
                    if (qaImage.width > qaImage.height)
                    {
                        this.questionImage.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 235f);
                    }
                    else
                    {
                        this.questionImage.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 310f);
                    }
                    this.aspecRatioFitter.aspectRatio = (float)qaImage.width / (float)qaImage.height;
                }
                break;
            case "Audio":
            case "audio":
                SetUI.SetGroup(this.questionBgs, 1, 0f);
                this.questionTexts = this.questionBgs[1].GetComponentsInChildren<TextMeshProUGUI>();
                this.fullSentence = qa.correctAnswer;

                for (int i = 0; i < this.questionTexts.Length; i++)
                {
                    this.questionTexts[i].text = "";
                    if (this.questionTexts[i].gameObject.name == "MarkerText")
                    {
                        this.questionText = questionTexts[i];
                        this.questionText.text = !string.IsNullOrEmpty(qa.question) ? qa.question : "";
                    }
                }
                this.audioPlayBtn = this.questionBgs[1].GetComponentInChildren<CanvasGroup>();
                if (this.audioPlayBtn != null)
                {
                    SetUI.Set(this.audioPlayBtn, true, 0f);
                }
                this.questiontype = QuestionType.Audio;
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;
                this.playAudio();
                break;
            case "Text":
            case "text":
                SetUI.SetGroup(this.questionBgs, 2, 0f);
                this.questiontype = QuestionType.Text;
                this.questionText = this.questionBgs[2].GetComponentInChildren<TextMeshProUGUI>();
                if (this.questionText != null)
                {
                    this.questionText.text = qa.question;
                }
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;
                break;
            case "FillInBlank":
            case "fillInBlank":
                SetUI.SetGroup(this.questionBgs, 3, 0f);
                this.questionTexts = this.questionBgs[3].GetComponentsInChildren<TextMeshProUGUI>();
                this.fullSentence = qa.fullSentence;
                for (int i = 0; i < this.questionTexts.Length; i++)
                {
                    this.questionTexts[i].text = "";
                    if (this.questionTexts[i].gameObject.name == "MarkerText")
                    {
                        this.questionText = questionTexts[i];
                    }

                    // Get the full sentence
                    string fullSentence = qa.fullSentence;
                    // Get the correct answer
                    string correctAnswer = qa.correctAnswer; // Assuming qa.correctAnswer contains "years old."
                    string extendedUnderline = "";

                    if (this.useSeparatedWordsWithUnderline)
                    {
                        // Split the correct answer into individual parts
                         string[] correctAnswerParts = correctAnswer.Split(' ');
                         foreach (var part in correctAnswerParts)
                         {
                             if (fullSentence.Contains(part))
                             {
                                 fullSentence = fullSentence.Replace(part, $"<u><color=#00000000>{part}</color></u>");
                             }
                         }
                    }
                    else
                    {
                        int wordCount = correctAnswer.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                        string combinedAnswer = string.Concat(correctAnswer.Split(' '));
                        extendedUnderline = combinedAnswer;
                        if (wordCount == 1)
                        {
                            int extraChars = 2;
                            string pad = new string('_', extraChars);
                            extendedUnderline += $"<color=#00000000>{pad}</color>";
                        }

                        if (fullSentence.Contains(correctAnswer))
                        {
                            fullSentence = fullSentence.Replace(
                                correctAnswer,
                                $"<u><color=#00000000>{extendedUnderline}</color></u>"
                            );
                        }

                        this.CreateUnderlineIcon(this.questionText);
                    }
                    // Set the text with the formatted string
                    //this.questionText.text = fullSentence;

                    string underline = $"<u><color=#00000000>{extendedUnderline}</color></u>";
                    this.displayQuestion = Regex.Replace(
                        qa.question,
                        @"(?<=[\?\!\.])\s*_+",
                        match => "\n" + underline
                    );

                    // Fallback: replace any remaining underscores normally
                    this.displayQuestion = Regex.Replace(
                        this.displayQuestion,
                        @"_+",
                        underline
                    );
                    this.questionText.text = this.displayQuestion;
                    //this.displayQuestion = fullSentence;
                    this.displayHint = qa.questionHint;
                }

                this.audioPlayBtn = this.questionBgs[3].GetComponentInChildren<CanvasGroup>();
                if(this.audioPlayBtn != null)
                {
                    SetUI.Set(this.audioPlayBtn, true, 0f);
                    this.audioPlayBtn.GetComponentInChildren<Button>()?.gameObject.SetActive(this.currentAudioClip != null? true : false);
                }
                this.questiontype = QuestionType.FillInBlank;
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;
                this.playAudio();
                break;
            case "SentenceCorrect":
            case "sentenceCorrect":
                SetUI.SetGroup(this.questionBgs, 3, 0f);
                this.questionTexts = this.questionBgs[3].GetComponentsInChildren<TextMeshProUGUI>();
                this.fullSentence = qa.fullSentence;
                for (int i = 0; i < this.questionTexts.Length; i++)
                {
                    this.questionTexts[i].text = "";
                    if (this.questionTexts[i].gameObject.name == "MarkerText")
                    {
                        this.questionText = questionTexts[i];
                    }

                    // Get the full sentence
                    string wrongSentence = qa.question;
                    string[] wrongAnswerParts = qa.wrongWord.Split(' ');
                    int wrongWordCount = qa.wrongWord.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

                    string fullSentence = qa.fullSentence;
                    string correctAnswer = qa.correctAnswer;
                    foreach (var part in wrongAnswerParts)
                    {
                        if (wrongSentence.Contains(part))
                        {
                            this.displayQuestion = wrongSentence.Replace(part, $"<u><b>{part}</b></u>");
                        }
                    }

                    this.questionText.text = this.displayQuestion;
                    //this.displayQuestion = fullSentence;
                    this.displayHint = qa.questionHint;
                }

                this.audioPlayBtn = this.questionBgs[3].GetComponentInChildren<CanvasGroup>();
                if (this.audioPlayBtn != null)
                {
                    SetUI.Set(this.audioPlayBtn, true, 0f);
                    this.audioPlayBtn.GetComponentInChildren<Button>()?.gameObject.SetActive(this.currentAudioClip != null ? true : false);
                }
                this.questiontype = QuestionType.SentenceCorrect;
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;
                this.playAudio();
                break;
        }

        if (LogController.Instance != null)
        {
            LogController.Instance.debug($"Get new {nameof(this.questiontype)} question");
        }

        if (this.numberQuestion < totalQuestion - 1)
            this.numberQuestion += 1;
        else
            this.numberQuestion = 0;
    }

    public TextMeshProUGUI QuestionText
    {
        get
        {
            return this.questionText;
        }
    }
    public TextMeshProUGUI[] QuestionTexts
    {
        get
        {
            return this.questionTexts;
        }
    }

    public AudioClip currentAudioClip
    {
        get{
            return this.qa.audioClip;
        }
    }

    public void setInteractiveOfQuestionBoards(bool status)
    {
        foreach (var board in this.questionBgs)
        {
            var boardCg = board.GetComponent<CanvasGroup>();
            if (boardCg != null)
            {
                boardCg.interactable = status;
                boardCg.blocksRaycasts = status;
            }
        }
    }

    public void stopAudio()
    {
        if (this.audioPlayBtn != null && this.currentAudioClip != null)
        {
            var audio = this.audioPlayBtn.GetComponentInChildren<AudioSource>();
            if (audio != null)
            {
                audio.Stop();
            }
        }
    }

    public void playAudio()
    {
        if (this.audioPlayBtn != null && this.currentAudioClip != null)
        {
            var audio = this.audioPlayBtn.GetComponentInChildren<AudioSource>();
            if (audio != null)
            {
                audio.Stop();
                TextToSpeech.Instance?.StopAudio();
                audio.clip = this.currentAudioClip;
                audio.Play();
            }
        }
    }
}

public static class SortExtensions
{
    // Fisher-Yates shuffle algorithm
    public static void Shuffle<T>(this List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1); // Use Unity's Random class
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public static void ShuffleArray<T>(T[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, array.Length);
            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
}