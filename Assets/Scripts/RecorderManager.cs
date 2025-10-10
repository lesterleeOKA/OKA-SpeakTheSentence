using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class RecorderManager : MonoBehaviour
{
    public DetectMethod detectMethod = DetectMethod.FullSentence;
    public string textToRecognize = "";
    public bool ttsFailure = false;
    public bool ttsDone = false;
    public bool hasErrorWord = false;
    public AudioClip clip, originalTrimmedClip;
    public enum DetectMethod
    {
        None = 0,
        Word = 1,
        FullSentence = 2,
        Spelling = 3
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


    protected void checkSpeech(WordDetail[] wordDetails = null, StringBuilder result = null, Text accurancyText = null)
    {
        if (wordDetails != null)
        {
            switch (this.detectMethod)
            {
                case DetectMethod.None:
                    break;
                case DetectMethod.Word:
                    break;
                case DetectMethod.FullSentence:
                    var correctAnswerWords = QuestionController.Instance.currentQuestion.correctAnswer
               .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var correctAnswerWordsCount = QuestionController.Instance.currentQuestion.correctAnswer
.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    var correctAnswerWordSet = new HashSet<string>(
                        correctAnswerWords.Select(w => Regex.Replace(w, @"[^\w]", "").ToLower())
                    );

                    foreach (var word in wordDetails)
                    {
                        string cleanWord = Regex.Replace(word.Word, @"[^\w]", "").ToLower();

                        if (word.ErrorType == "Omission")
                        {
                            this.ttsFailure = true;
                            if (accurancyText != null)
                                accurancyText.text = $"Word missing, please retry";
                            this.hasErrorWord = true;
                        }
                        else
                        {
                            if (correctAnswerWordSet.Contains(cleanWord))
                            {
                                if (word.ErrorType == "Mispronunciation")
                                {
                                    this.ttsFailure = true;
                                    if (accurancyText != null)
                                        accurancyText.text = $"Mispronunciation detected, please retry";
                                    hasErrorWord = true;
                                }
                                else if (word.AccuracyScore < 85 && (QuestionController.Instance.currentQuestion.questiontype == QuestionType.SentenceCorrect || correctAnswerWordsCount == 1))
                                {
                                    this.ttsFailure = true;
                                    if (accurancyText != null)
                                        accurancyText.text = $"Mispronunciation detected, please retry";
                                    hasErrorWord = true;
                                }
                            }
                        }
                    }
                    break;
                case DetectMethod.Spelling:
                    break;
            }
        }
        else
        {
            result.AppendLine("No words found in NBest.");
        }
    }
    
}
