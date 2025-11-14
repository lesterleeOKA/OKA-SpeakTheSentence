using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class RecorderManager : MonoBehaviour
{
    public DetectMethod detectMethod = DetectMethod.FullSentence;
    public string textToRecognize = "";
    public bool ttsFailure = false;
    public bool ttsDone = false;
    public bool hasErrorWord = false;
    public AudioClip clip, playBackClip;
    public enum DetectMethod
    {
        None = 0,
        Word = 1,
        FullSentence = 2,
        Spelling = 3,
        prompt = 4
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
                case DetectMethod.prompt:
                    this.textToRecognize = QuestionController.Instance.currentQuestion.qa.prompt;
                    break;
                case DetectMethod.Word:
                    this.textToRecognize = QuestionController.Instance.currentQuestion.correctAnswer;
                    break;
                case DetectMethod.FullSentence:
                    if(!string.IsNullOrEmpty(QuestionController.Instance.currentQuestion.wrongSentence) && QuestionController.Instance.currentQuestion.wrongSentence != "null")
                        this.textToRecognize = QuestionController.Instance.currentQuestion.wrongSentence;
                    else
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


    protected void checkSpeech(WordDetail[] wordDetails = null, StringBuilder result = null)
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

                    bool useWrongSentenceCheckCase = !string.IsNullOrEmpty(QuestionController.Instance.currentQuestion.wrongSentence) &&
                        QuestionController.Instance.currentQuestion.wrongSentence != "null" &&
                        !string.IsNullOrEmpty(QuestionController.Instance.currentQuestion.qa.wrongWord);

                    foreach (var word in wordDetails)
                    {
                        string cleanWord = Regex.Replace(word.Word, @"[^\w]", "").ToLower();

                        if(useWrongSentenceCheckCase)
                        {
                            if(word.Word.Equals(QuestionController.Instance.currentQuestion.qa.wrongWord, StringComparison.OrdinalIgnoreCase))
                            {
                                if(word.ErrorType != "Omission")
                                {
                                    this.ttsFailure = true;
                                    this.hasErrorWord = true;
                                }
                            }
                            else
                            {
                                if (word.ErrorType == "Omission")
                                {
                                    this.ttsFailure = true;
                                    this.hasErrorWord = true;
                                }
                            }
                        }
                        else
                        {
                            if (word.ErrorType == "Omission")
                            {
                                this.ttsFailure = true;
                                this.hasErrorWord = true;
                            }
                            else
                            {
                                if (correctAnswerWordSet.Contains(cleanWord))
                                {
                                    if (word.ErrorType == "Mispronunciation")
                                    {
                                        if (word.AccuracyScore < 20) this.ttsFailure = true;
                                        hasErrorWord = true;
                                    }
                                    else
                                    {
                                        if (QuestionController.Instance.currentQuestion.questiontype == QuestionType.SentenceCorrect)
                                        {
                                            if (word.AccuracyScore < 85 && correctAnswerWordsCount == 1)
                                            {
                                                this.ttsFailure = true;
                                                hasErrorWord = true;
                                            }
                                        }
                                    }
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
