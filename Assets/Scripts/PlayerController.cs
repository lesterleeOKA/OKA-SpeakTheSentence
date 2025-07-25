using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerController : UserData
{
    public Scoring scoring;
    public BloodController bloodController;
    public string answer = string.Empty;
    public bool IsCorrect = false;
    public bool IsCheckedAnswer = false;
    private CharacterSet characterSet = null;
    public HelpTool helpTool;
    public bool playerAddLifeOnce = false;

    public void Init(CharacterSet characterSet = null, Sprite[] defaultAnswerBoxes = null, Vector3 startPos = default)
    {
        this.updateRetryTimes(-1);
        this.characterSet = characterSet;

        if (this.bloodController == null)
        {
            this.bloodController = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Blood").GetComponent<BloodController>();
        }

        if (this.PlayerIcons[0] == null && GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Icon") != null)
        {
            this.PlayerIcons[0] = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Icon").GetComponent<PlayerIcon>();
        }

        if (this.scoring.scoreTxt == null && GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Score") != null)
        {
            this.scoring.scoreTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Score").GetComponent<TextMeshProUGUI>();
        }

        if (this.scoring.answeredEffectTxt == null && GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_AnswerScore") != null)
        {
            this.scoring.answeredEffectTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_AnswerScore").GetComponent<TextMeshProUGUI>();
        }

        if (this.scoring.resultScoreTxt == null && GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_ResultScore") != null)
        {
            this.scoring.resultScoreTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_ResultScore").GetComponent<TextMeshProUGUI>();
        }

        this.scoring.init();
    }

    void updateRetryTimes(int status = -1)
    {
        switch (status)
        {
            case -1:
                this.NumberOfRetry = LoaderConfig.Instance.gameSetup.retry_times;
                this.Retry = this.NumberOfRetry;
                break;
            case 0:
                if (this.Retry > 0)
                {
                    this.Retry--;
                }

                if (this.bloodController != null)
                {
                    this.bloodController.setBloods(false);
                }
                break;
            case 1:
                if (this.Retry < this.NumberOfRetry)
                {
                    this.Retry++;
                }

                if (this.bloodController != null)
                {
                    this.bloodController.addBlood();
                }
                break;
        }

        if(this.helpTool != null)
        {
            if (this.Retry < this.NumberOfRetry)
            {
                this.helpTool.setHelpTool(true);
                this.playerAddLifeOnce = false;
            }
            else
            {
                this.helpTool.setHelpTool(false);
                this.playerAddLifeOnce = true;
            }
        }
    }

    public void updatePlayerIcon(bool _status = false, string _playerName = "", Sprite _icon = null)
    {
        for (int i = 0; i < this.PlayerIcons.Length; i++)
        {
            if (this.PlayerIcons[i] != null)
            {
                this.PlayerColor = this.characterSet.playerColor;
                this.PlayerIcons[i].playerColor = this.characterSet.playerColor;
                this.PlayerIcons[i].SetStatus(_status, _playerName, _icon);
            }
        }

    }


    string CapitalizeFirstLetter(string str)
    {
        if (string.IsNullOrEmpty(str)) return str; // Return if the string is empty or null
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    public void checkAnswer(string answer, int currentTime, Action onCompleted = null)
    {
        var currentQuestion = QuestionController.Instance?.currentQuestion;
        this.answer = answer.ToLower();
        var lowerQIDAns = currentQuestion.correctAnswer.ToLower();

        if (!this.IsCheckedAnswer)
        {
            this.IsCheckedAnswer = true;
            var loader = LoaderConfig.Instance;
            int eachQAScore = LoaderConfig.Instance.gameSetup.gameSettingScore > 0 ?
                LoaderConfig.Instance.gameSetup.gameSettingScore :
                (currentQuestion.qa.score.full == 0 ? 10 : currentQuestion.qa.score.full);

            int currentScore = this.Score;
            LogController.Instance?.debug("current answer:"+lowerQIDAns);
            int resultScore = this.scoring.score(this.answer, currentScore, lowerQIDAns, eachQAScore);
            this.Score = resultScore;
            this.IsCorrect = this.scoring.correct;
            StartCoroutine(this.showAnswerResult(this.scoring.correct,()=>
            {
                if (this.UserId == 0 && loader != null && loader.apiManager.IsLogined) // For first player
                {
                    float currentQAPercent = 0f;
                    int correctId = 0;
                    float score = 0f;
                    float answeredPercentage;
                    int progress = (int)((float)currentQuestion.answeredQuestion / QuestionManager.Instance.totalItems * 100);

                    if (this.answer == lowerQIDAns)
                    {
                        if (this.CorrectedAnswerNumber < QuestionManager.Instance.totalItems)
                            this.CorrectedAnswerNumber += 1;

                        correctId = 2;
                        score = eachQAScore; // load from question settings score of each question

                        LogController.Instance?.debug("Each QA Score!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + eachQAScore + "______answer" + this.answer);
                        currentQAPercent = 100f;
                    }
                    else
                    {
                        if (this.CorrectedAnswerNumber > 0)
                        {
                            this.CorrectedAnswerNumber -= 1;
                        }
                    }

                    if (this.CorrectedAnswerNumber < QuestionManager.Instance.totalItems)
                    {
                        answeredPercentage = this.AnsweredPercentage(QuestionManager.Instance.totalItems);
                    }
                    else
                    {
                        answeredPercentage = 100f;
                    }

                    this.AnswerTime = currentTime + this.AnswerTime;
                    loader.SubmitAnswer(
                               currentTime,
                               this.Score,
                               answeredPercentage,
                               progress,
                               correctId,
                               this.AnswerTime,
                               currentQuestion.qa.qid,
                               currentQuestion.correctAnswerId,
                               this.CapitalizeFirstLetter(this.answer),
                               currentQuestion.correctAnswer,
                               score,
                               currentQAPercent,
                               onCompleted
                               );
                }
                else
                {
                   onCompleted?.Invoke();
                }
            }, ()=>
            {
                this.IsCheckedAnswer = false;
                //onCompleted?.Invoke();
            }));
        }
    }

    public void submitAnswer(string apiReturn ="", Action onCompleted=null)
    {
        LogController.Instance.debug("finished question!!!!!!!!!!!!!!!!!!!!!!");
        int currentTime = this.GetCurrentTimePercentage();
        this.checkAnswer(apiReturn, currentTime, onCompleted);
    }

    private int GetCurrentTimePercentage()
    {
        var gameTimer = GameController.Instance.gameTimer;
        return Mathf.FloorToInt(((gameTimer.gameDuration - gameTimer.currentTime) / gameTimer.gameDuration) * 100);
    }

    public void resetRetryTime()
    {
        this.scoring.resetText();
        this.updateRetryTimes(-1);
        this.bloodController.setBloods(true);
    }

    public void playerUseHelpTool()
    {
        if (this.helpTool!= null && !this.playerAddLifeOnce)
        {
            this.helpTool.Deduct(() =>
            {
                this.updateRetryTimes(1);
            });
        }
    }

    public IEnumerator showAnswerResult(bool correct, Action onCorrectCompleted = null, Action onFailureCompleted = null)
    {
        var recorder = this.GetComponent<RecordAudio>();
        if (recorder == null)
        {
            LogController.Instance?.debugError("Recorder component is missing.");
            yield break;
        }
        float delay = 2f;
        if (correct)
        {
            LogController.Instance?.debug("Add marks" + this.Score);
            GameController.Instance?.setGetScorePopup(true);
            AudioController.Instance?.PlayAudio(1);
            onCorrectCompleted?.Invoke();
            yield return new WaitForSeconds(delay);
            GameController.Instance?.setGetScorePopup(false);
            recorder.controlResultPage(0);
        }
        else
        {
            GameController.Instance?.setWrongPopup(true);
            AudioController.Instance?.PlayAudio(2);
            this.updateRetryTimes(0);
            yield return new WaitForSeconds(delay);
            GameController.Instance?.setWrongPopup(false);
            recorder.controlResultPage(this.Retry <= 0 ? 2 : 1);
            if(this.Retry > 0) 
                QuestionController.Instance.currentQuestion.QuestionText.text = QuestionController.Instance.currentQuestion.qa.question;
            onFailureCompleted?.Invoke();
        }
        this.scoring.correct = false;
        this.playerReset();
    }

    public void playerReset()
    {             
        this.answer = "";
        this.IsCheckedAnswer = false;
        this.IsCorrect = false;
        this.playerAddLifeOnce = false;
    }
        
}
