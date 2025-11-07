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
    public string checkSingleWord;
    public string prompt;
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
    public Button audioPlayBtn = null;
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
                    }
                }
                this.setQuestionText(!string.IsNullOrEmpty(qa.question) ? qa.question : "");
                this.audioPlayBtn = this.questionBgs[1].GetComponentInChildren<Button>();
                if (this.audioPlayBtn != null)
                {
                    SetUI.Set(this.audioPlayBtn.GetComponent<CanvasGroup>(), true, 0f);
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
                if (this.questionText != null) this.setQuestionText(qa.question);
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

                    this.setQuestionText(this.displayQuestion);
                    this.questionText.ForceMeshUpdate();
                    RectTransform rt = this.questionText.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, this.questionText.preferredHeight);
                    //this.displayQuestion = fullSentence;
                    this.displayHint = qa.questionHint;
                }
                this.questiontype = QuestionType.FillInBlank;
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;
                this.controlMediaElements(this.questionBgs[3]);
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

                    // Prepare sentences
                    string wrongSentence = qa.question ?? "";
                    string fullSentence = qa.fullSentence ?? "";

                    // Tokenize using regex to preserve original spacing/punctuation when rebuilding
                    var wrongMatches = Regex.Matches(wrongSentence, @"\S+");
                    var fullMatches = Regex.Matches(fullSentence, @"\S+");

                    int diffIndex = -1;
                    int minLen = Math.Min(wrongMatches.Count, fullMatches.Count);

                    // Find first differing token (compare cleaned words case-insensitively)
                    for (int t = 0; t < minLen; t++)
                    {
                        string wToken = Regex.Replace(wrongMatches[t].Value, @"[^\w]", "").ToLower();
                        string fToken = Regex.Replace(fullMatches[t].Value, @"[^\w]", "").ToLower();
                        if (!string.Equals(wToken, fToken, StringComparison.Ordinal))
                        {
                            diffIndex = t;
                            break;
                        }
                    }

                    // If all compared tokens equal but lengths differ, mark the extra token in wrong sentence (e.g. missing/extra word)
                    if (diffIndex == -1 && wrongMatches.Count != fullMatches.Count)
                    {
                        diffIndex = minLen; // this will underline the first extra/missing word position (if exists)
                    }

                    // Rebuild display string preserving original separators and underline only the differing token
                    string display = wrongSentence;
                    if (diffIndex >= 0 && wrongMatches.Count > 0)
                    {
                        var sb = new System.Text.StringBuilder();
                        int prevEnd = 0;
                        for (int t = 0; t < wrongMatches.Count; t++)
                        {
                            var match = wrongMatches[t];
                            // append intermediate separators (spaces, punctuation) between tokens exactly as original
                            if (match.Index > prevEnd)
                            {
                                sb.Append(wrongSentence.Substring(prevEnd, match.Index - prevEnd));
                            }

                            string tokenText = match.Value;
                            if (t == diffIndex)
                            {
                                // underline only the first differing token
                                sb.Append($"<u><b>{tokenText}</b></u>");
                            }
                            else
                            {
                                sb.Append(tokenText);
                            }

                            prevEnd = match.Index + match.Length;
                        }
                        // append remaining tail (if any)
                        if (prevEnd < wrongSentence.Length)
                        {
                            sb.Append(wrongSentence.Substring(prevEnd));
                        }

                        display = sb.ToString();
                    }

                    // Fallback: if no tokens found, just underline the wrongWord (if provided)
                    if ((wrongMatches.Count == 0 || diffIndex < 0) && !string.IsNullOrEmpty(qa.wrongWord))
                    {
                        // underline first occurrence of qa.wrongWord (case-sensitive to preserve original)
                        int idx = wrongSentence.IndexOf(qa.wrongWord, StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            display = wrongSentence.Substring(0, idx)
                                + $"<u><b>{qa.wrongWord}</b></u>"
                                + wrongSentence.Substring(idx + qa.wrongWord.Length);
                        }
                    }

                    this.displayQuestion = display;
                    this.setQuestionText(this.displayQuestion);
                    this.displayHint = qa.questionHint;
                }
                this.questiontype = QuestionType.SentenceCorrect;
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;
                this.controlMediaElements(this.questionBgs[3]);
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

    void controlMediaElements(CanvasGroup questionCg = null)
    {
        var qaFillInBlank_image = qa.texture;
        var qaFillInBlank_audio = qa.audioClip;
        this.questionImage = questionCg.GetComponentInChildren<RawImage>();
        this.audioPlayBtn = questionCg.GetComponentInChildren<Button>();

        if (qaFillInBlank_image != null)
        {
            this.audioPlayBtn?.gameObject.SetActive(false);
            if (this.questionImage != null && qaFillInBlank_image != null)
            {
                this.aspecRatioFitter = this.questionImage.GetComponent<AspectRatioFitter>();
                this.questionImage.texture = qaFillInBlank_image;

                var width = this.questionImage.GetComponent<RectTransform>().sizeDelta.x;
                var height = this.questionImage.GetComponent<RectTransform>().sizeDelta.y;

                this.questionImage.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 280f);
                if (qaFillInBlank_image.width > qaFillInBlank_image.height)
                {
                    this.aspecRatioFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                    this.questionImage.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, height);
                }
                else
                {
                    this.aspecRatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                    this.questionImage.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 280f);
                }

                this.aspecRatioFitter.aspectRatio = (float)qaFillInBlank_image.width / (float)qaFillInBlank_image.height;
            }
            questionCg.GetComponent<VerticalLayoutGroup>().spacing = 0f;
        }
        else if (qaFillInBlank_audio != null)
        {
            this.questionImage?.gameObject.SetActive(false);
            questionCg.GetComponent<VerticalLayoutGroup>().spacing = 50f;
            SetUI.Set(this.audioPlayBtn.GetComponent<CanvasGroup>(), true, 0f);
            this.playAudio();
        }
        else
        {
            this.audioPlayBtn?.gameObject.SetActive(false);
            this.questionImage?.gameObject.SetActive(false);
        }
    }

    void setQuestionText(string displayQuestion = "")
    {
        for (int i = 0; i < this.questionTexts.Length; i++)
        {
            this.questionTexts[i].text = "";
            switch (LoaderConfig.Instance.gameSetup.qa_font_alignment)
            {
                case 1:
                    this.questionTexts[i].alignment = TextAlignmentOptions.Left;
                    break;
                case 2:
                    this.questionTexts[i].alignment = TextAlignmentOptions.Center;
                    break;
                case 3:
                    this.questionTexts[i].alignment = TextAlignmentOptions.Right;
                    break;
                default:
                    this.questionTexts[i].alignment = TextAlignmentOptions.Left;
                    break;
            }
        }
        this.questionText.text = displayQuestion;
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
            if (boardCg != null && boardCg.alpha == 1f)
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