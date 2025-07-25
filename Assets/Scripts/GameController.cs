using UnityEngine;
using System.Collections;
public class GameController : GameBaseController
{
    public static GameController Instance = null;
    public CharacterSet[] characterSets;
    public Transform parent;
    public Sprite[] defaultAnswerBox;
    public PlayerController playerController;

    protected override void Awake()
    {
        if (Instance == null) Instance = this;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    private IEnumerator InitialQuestion()
    {
        this.createPlayer();

        var questionController = QuestionController.Instance;
        if(questionController == null) yield break;
        questionController.nextQuestion();
    }

    void createPlayer()
    {
        this.playerController.gameObject.name = "Player_" + 0;
        this.playerController.UserId = 0;
        this.playerController.Init(this.characterSets[0], this.defaultAnswerBox);

        if (LoaderConfig.Instance != null && LoaderConfig.Instance.apiManager.IsLogined)
        {
            var _playerName = LoaderConfig.Instance?.apiManager.loginName;
            var icon = LoaderConfig.Instance.apiManager.peopleIcon != null ?
                SetUI.ConvertTextureToSprite(LoaderConfig.Instance.apiManager.peopleIcon as Texture2D) :
                SetUI.ConvertTextureToSprite(this.characterSets[0].defaultIcon as Texture2D);

            this.playerController.UserName = _playerName;
            this.playerController.updatePlayerIcon(true, _playerName, icon);
        }
        else
        {
            var icon = SetUI.ConvertTextureToSprite(this.characterSets[0].defaultIcon as Texture2D);
            this.playerController.updatePlayerIcon(true, "", icon);
        }
    }


    public override void enterGame()
    {
        base.enterGame();
        StartCoroutine(this.InitialQuestion());
    }

    public override void endGame()
    {
        bool showSuccess = false;
        if (this.playerController != null)
        {
            if (this.playerController.Score >= 30)
            {
                showSuccess = true;
            }
            this.endGamePage.updateFinalScore(0, this.playerController.Score);
        }
        this.endGamePage.setStatus(true, showSuccess);
        base.endGame();
    }

    public void UpdateNextQuestion()
    {
        LogController.Instance?.debug("Next Question");
        var questionController = QuestionController.Instance;

        if (questionController != null) {
            questionController.nextQuestion();
        }       
    }

   
    private void Update()
    {
        if(!this.playing) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            this.UpdateNextQuestion();
        }
    } 
}
