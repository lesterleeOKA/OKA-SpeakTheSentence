using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBaseController : MonoBehaviour
{
    public Timer gameTimer;
    public CanvasGroup GameUILayer, TopUILayer, TopRightUILayer, getScorePopup, wrongPopup;
    protected Vector2 originalGetScorePos = Vector2.zero;
    public EndGamePage endGamePage;
    public int playerNumber = 0;
    public int maxPlayers = 2;
    public bool playing = false;
    public bool showingPopup = false;
    public bool leaveGame = false;

    protected virtual void Awake()
    {
        LoaderConfig.Instance?.InitialGameSetup();
    }

    protected virtual void Start()
    {
        if (LoaderConfig.Instance != null) this.playerNumber = LoaderConfig.Instance.PlayerNumbers;
        SetUI.Set(this.TopUILayer, false, 0f);
        SetUI.Set(this.GameUILayer, false, 0f);
        SetUI.Set(this.getScorePopup, false, 0f);
        SetUI.Set(this.TopRightUILayer, true, 0f);
        if (this.getScorePopup != null) this.originalGetScorePos = this.getScorePopup.transform.localPosition;
        this.endGamePage.init(this.playerNumber);
    }

    public virtual void enterGame()
    {
        SetUI.Set(this.TopUILayer, true, 0.5f);
        SetUI.Set(this.GameUILayer, true, 0.5f);
        this.playing = true;
    }

    public virtual void endGame()
    {
        SetUI.Set(this.TopUILayer, false, 0f);
        SetUI.Set(this.GameUILayer, false, 0f);
        SetUI.Set(this.TopRightUILayer, false, 0f);
        this.setGetScorePopup(false);
        this.setWrongPopup(false);
        this.playing = false;
    }

    public void retryGame()
    {
        if (!this.leaveGame)
        {
            QuestionManager.Instance?.ReorderTheQuestionList();
            if (AudioController.Instance != null) AudioController.Instance.changeBGMStatus(true);

            if (LoaderConfig.Instance.apiManager.IsLogined)
            {
                LoaderConfig.Instance?.restartGameAPI(() => SceneManager.LoadScene(2));
            }
            else
            {
                LoaderConfig.Instance?.exitPage(false, "Replay", null, () => SceneManager.LoadScene(2));
            }
            this.leaveGame = true;
        }
    }

    public void setGetScorePopup(bool status)
    {
        this.showingPopup = status;
        SetUI.SetMove(this.getScorePopup, status, status ? new Vector2(this.originalGetScorePos.x, this.originalGetScorePos.y - 150f) : this.originalGetScorePos, status ? 0.5f : 0f);
    }

    public void setWrongPopup(bool status)
    {
        this.showingPopup = status;
        SetUI.SetMove(this.wrongPopup, status, status ? new Vector2(this.originalGetScorePos.x, this.originalGetScorePos.y - 150f) : this.originalGetScorePos, status ? 0.5f : 0f);
    }


    public void BackToWebpage(bool useExitAPI = false)
    {
        LoaderConfig.Instance?.exitPage(useExitAPI, "Leave Game", ExternalCaller.BackToHomeUrlPage, null);
    }
}
