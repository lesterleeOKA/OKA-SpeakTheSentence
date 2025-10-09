using UnityEngine;
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

        string result = "";
        string instruction = LoaderConfig.Instance.apiManager.settings.instructionContent;
        if (string.IsNullOrEmpty(instruction))
        {
            result = QuestionManager.Instance.questionData.questions[0].questionHint;
        }
        else
        {
            if (instruction.Contains("-"))
            {
                // Split the string and take the part before the first "-"
                result = instruction.Split('-')[0];
                LogController.Instance.debug(result); // Output: Listen to the audio and speak the words out
                TextToSpeech.Instance?.UpdateTextToAudioFromAPI(result);
            }
            else
            {
                result = instruction;
                LogController.Instance.debug("No '-' found in the string.");
            }
        }
        //Add real time download question hint from API;
        TextToSpeech.Instance?.UpdateTextToAudioFromAPI(result);
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
        this.createPlayer();
    }

    public override void endGame()
    {
        bool showSuccess = false;
        var loader = LoaderConfig.Instance;
        bool isLogined = loader != null && loader.apiManager.IsLogined;

        if (isLogined)
        {
            var resultPageCg = this.endGamePage.messageBg.GetComponent<CanvasGroup>();
            string playerScore = this.playerController.Score.ToString();

            SetUI.SetInteract(resultPageCg, false);
            string scoresJson = "[" + string.Join(",", playerScore) + "]";
            StartCoroutine(
                loader.apiManager.postScoreToStarAPI(scoresJson, (stars) => {
                    LogController.Instance.debug("Score to Star API call completed!");

                    if (this.playerController != null)
                    {
                        if (loader.CurrentHostName.Contains("dev.starwishparty.com") ||
                            loader.CurrentHostName.Contains("uat.starwishparty.com") ||
                            loader.CurrentHostName.Contains("pre.starwishparty.com") ||
                            loader.CurrentHostName.Contains("www.starwishparty.com"))
                        {
                            if (stars[0] > 0)
                            {
                                StartCoroutine(loader.apiManager.AddCurrency(stars[0], () =>
                                {
                                    SetUI.SetInteract(resultPageCg, true);
                                }));
                            }
                            else
                            {
                                SetUI.SetInteract(resultPageCg, true);
                            }
                        }
                        else
                        {
                            SetUI.SetInteract(resultPageCg, true);
                        }

                        int star = (stars != null && stars.Length == 1) ? stars[0] : 0;
                        this.endGamePage.updateFinalScoreWithStar(0, playerController.Score, star, () =>
                        {
                            if (this.endGamePage.scoreEndings[0].starNumber > 0)
                            {
                                showSuccess = true;
                            }
                        });
                    }

                    this.endGamePage.setStatus(true, showSuccess);
                    base.endGame();
                })
            );
        }
        else
        {
            if (playerController != null)
            {
                this.endGamePage.updateFinalScore(0, this.playerController.Score, () =>
                {
                    if (this.endGamePage.scoreEndings[0].starNumber > 0)
                    {
                        showSuccess = true;
                    }
                });
            }
            this.endGamePage.setStatus(true, showSuccess);
            base.endGame();
        }
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
