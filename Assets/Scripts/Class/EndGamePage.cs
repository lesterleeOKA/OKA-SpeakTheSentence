using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EndGamePage
{
    public CanvasGroup EndGameLayer;
    public GameObject[] PlayerIcons;
    public Image messageBg;
    public Sprite[] messageImages;
    public Sprite[] starsImageSprites;
    public ScoreEnding[] scoreEndings;

    public void init(int playerNumber)
    {
        this.setStatus(false);
        foreach(var scoreEnding in scoreEndings)
        {
            scoreEnding.init();
        }

        for(int i =0; i< this.PlayerIcons.Length; i++)
        {
            if (this.PlayerIcons[i] != null)
            {
                this.PlayerIcons[i].SetActive(true);
            }
        }
    }
    public void setStatus(bool status, bool success=false)
    {
        if (this.messageBg != null && 
            this.messageImages != null && 
            AudioController.Instance != null) 
        {
            if (status)
            {
                this.messageBg.sprite = this.messageImages[success ? 1 : 0];
                AudioController.Instance.showResultAudio(success);
                AudioController.Instance.changeBGMStatus(false);
            }
            else
            {
                this.messageBg.sprite = this.messageImages[0];
            }
        }
        SetUI.Set(this.EndGameLayer, status, status ? 0.5f : 0f);
    }

    public void updateFinalScore(int _playerId, int _score, Action onCompleted = null)
    {
        if (this.scoreEndings != null && this.scoreEndings[_playerId] != null)
        {
            this.scoreEndings[_playerId].updateFinalScore(_score, -1, starsImageSprites, onCompleted);
        }
    }

    public void updateFinalScoreWithStar(int _playerId, int _score, int _star = -1, Action onCompleted = null)
    {
        if (this.scoreEndings != null && this.scoreEndings[_playerId] != null)
        {
            this.scoreEndings[_playerId].updateFinalScore(_score, _star, starsImageSprites, onCompleted);
        }
    }

}


[System.Serializable]
public class ScoreEnding
{
    public string name;
    public int starNumber;
    public NumberCounter scoreText;
    public List<Image> stars_list = new List<Image>();
    public List<Image> show_stars_list = new List<Image>();

    public void init()
    {
        for (int i = 0; i < this.show_stars_list.Count; i++)
        {
            if (this.show_stars_list[i] != null)
            {
                this.show_stars_list[i].transform.DOScale(Vector3.zero, 0f);
            }
        }
    }
    public void updateFinalScore(int score, int star, Sprite[] starsImageSprites, Action onCompleted = null)
    {
        if (starsImageSprites == null) return;

        if (this.scoreText != null)
        {
            this.scoreText.Value = score;
        }

        var loaderApiManager = LoaderConfig.Instance.apiManager;
        if (loaderApiManager.IsLogined)
        {
            if (star > -1)
                this.starNumber = star;
            else
                this.calculateByGameRate(score);
        }
        else if (loaderApiManager.IsLoginedRainbowOne)
        {
            this.calculateByGameRate(score);
        }
        else
        {
            if (score > 30 && score <= 60)
                this.starNumber = 1;
            else if (score > 60 && score <= 90)
                this.starNumber = 2;
            else if (score > 90)
                this.starNumber = 3;
            else
                this.starNumber = 0;
        }

        for (int i = 0; i < this.starNumber; i++)
        {
            if (this.show_stars_list[i] != null)
            {
                float delay = 1f * i; // Incremental delay of 1 second per star
                this.show_stars_list[i].transform.DOScale(Vector3.one, 1f).SetDelay(0.5f + delay);
            }
        }

        onCompleted?.Invoke();
    }

    void calculateByGameRate(int score)
    {
        int totalQuestions = 0;
        int eachQuestionScore = 10;

        if (QuestionManager.Instance != null)
            totalQuestions = QuestionManager.Instance.totalItems;

        if (QuestionController.Instance != null && QuestionController.Instance.currentQuestion != null)
        {
            var qa = QuestionController.Instance.currentQuestion.qa;
            if (qa != null && qa.score != null && qa.score.full > 0)
                eachQuestionScore = qa.score.full;
        }

        int maxScore = totalQuestions * eachQuestionScore;
        float percent = (float)score / maxScore;

        if (percent > 0.9f)
            this.starNumber = 3;
        else if (percent > 0.6f && percent <= 0.9f)
            this.starNumber = 2;
        else if (percent > 0.3f && percent <= 0.6f)
            this.starNumber = 1;
        else
            this.starNumber = 0;
    }
}
