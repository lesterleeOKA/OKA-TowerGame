using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GameController : GameBaseController
{
    public static GameController Instance = null;
    public CharacterSet[] characterSets;
    public GridManager gridManager;
    public ObstacleSpawner obstacleSpawner;
    public Cell[,] grid;
    public GameObject playerPrefab;
    public Transform parent;
    public Sprite[] defaultAnswerBox;
    public List<PlayerController> playerControllers = new List<PlayerController>();
    public bool showCells = false;
    public HelpTool[] helpTool;
    public GroupMoving groupMoving;
    public AutoMovingGroup autoMovingGroup;
    public bool reduceMCOptions = false;

    protected override void Awake()
    {
        if (Instance == null) Instance = this;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        LogController.Instance?.debug("ExternalCaller.DeviceType: " + ExternalCaller.DeviceType);
        this.playerNumber = LoaderConfig.Instance.PlayerNumbers;
#if UNITY_WEBGL && !UNITY_EDITOR
        switch (ExternalCaller.DeviceType)
        {
            case 0:
            case 2:
                this.playerNumber = 1;
                break;
            default:
                this.playerNumber = LoaderConfig.Instance.PlayerNumbers;
                break;
        } 
#endif
        LogController.Instance?.debug("playerNumber: " + this.playerNumber);
        this.CreateGrids();
    }

    void CreateGrids()
    {
        this.grid = gridManager.CreateGrid();
    }

    private IEnumerator InitialQuestion()
    {
        this.gridManager.ResetAllCellsToCenter();
        this.createPlayer();

        var questionController = QuestionController.Instance;
        if (questionController == null) yield break;
        questionController.nextQuestion();
        yield return new WaitForEndOfFrame();

        if (QuestionController.Instance.currentQuestion.answersChoics != null &&
            QuestionController.Instance.currentQuestion.answersChoics.Length > 0)
        {
            string[] answers = questionController.currentQuestion.answersChoics;
            this.gridManager.UpdateGridWithWord(answers, null, ()=>
            {
                this.obstacleSpawner.GenerateObstacles(this.gridManager);
            });
        }
        else
        {
            string word = questionController.currentQuestion.correctAnswer;
            this.gridManager.UpdateGridWithWord(null, word, ()=>
            {
                this.obstacleSpawner.GenerateObstacles(this.gridManager);
            });
        }

    }

    void createPlayer()
    {
        bool singlePlayerMode = this.playerNumber == 1;
        if (singlePlayerMode)
        {
            this.parent = this.autoMovingGroup?.transform.parent;
            this.groupMoving?.gameObject.SetActive(false);
            this.autoMovingGroup?.gameObject.SetActive(true);
        }
        else
        {
            this.parent = this.groupMoving?.transform;
            this.groupMoving?.gameObject.SetActive(true);
            this.autoMovingGroup?.gameObject.SetActive(false);
        }
        var loader = LoaderConfig.Instance;
        bool isLogined = loader.apiManager.IsLogined || loader.apiManager.IsLoginedRainbowOne;
        for (int i = 0; i < this.maxPlayers; i++)
        {
            if (i < this.playerNumber)
            {
                bool loginedPlayer = i == 0 && isLogined && loader.apiManager.peopleIcon != null;
                var playerController = GameObject.Instantiate(this.playerPrefab, this.parent).GetComponent<PlayerController>();
                playerController.gameObject.name = "Player_" + i;
                playerController.UserId = i;
                this.playerControllers.Add(playerController);

                if(loginedPlayer) this.characterSets[i].idlingTexture = loader.apiManager.peopleIcon;
                this.playerControllers[i].Init(this.characterSets[i], this.defaultAnswerBox, singlePlayerMode ? true : false);

                if(singlePlayerMode) {
                    this.autoMovingGroup.Init(playerController);
                }
                else
                {
                    playerController.transform.localPosition = new Vector3(i == 0 ? -230f : 230f, 0f, 0);
                }

                if (loginedPlayer)
                {
                    var _playerName = loader.apiManager.loginName;
                    var icon = SetUI.ConvertTextureToSprite(loader.apiManager.peopleIcon as Texture2D);
                    this.playerControllers[i].UserName = _playerName;
                    this.playerControllers[i].updatePlayerIcon(true, _playerName, icon, this.characterSets[i].playersColor);
                }
                else
                {
                    var icon = this.characterSets[i].defaultIcon != null ? 
                        SetUI.ConvertTextureToSprite(this.characterSets[i].defaultIcon as Texture2D) : null;
                    this.playerControllers[i].updatePlayerIcon(true, null, icon, this.characterSets[i].playersColor);
                }
            }
            else
            {
                int notUsedId = i + 1;
                var notUsedPlayerIcon = GameObject.FindGameObjectWithTag("P" + notUsedId + "_Icon");
                if (notUsedPlayerIcon != null) notUsedPlayerIcon.SetActive(false);

                var notUsedPlayerBlood = GameObject.FindGameObjectWithTag("P" + notUsedId + "_Blood");
                if (notUsedPlayerBlood != null) notUsedPlayerBlood.SetActive(false);

                var notUsedPlayerResultScore = GameObject.FindGameObjectWithTag("P" + notUsedId + "_ResultScore");
                if (notUsedPlayerResultScore != null)
                {
                    notUsedPlayerResultScore.SetActive(false);
                }
                if (this.helpTool[i] != null)
                {
                    this.helpTool[i].gameObject.SetActive(false);
                }
            }
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
        var loader = LoaderConfig.Instance;
        bool isLogined = loader != null && loader.apiManager.IsLogined;
        string[] playerScores = new string[this.playerControllers.Count];

        if (isLogined)
        {
            var resultPageCg = this.endGamePage.messageBg.GetComponent<CanvasGroup>();
            for (int i = 0; i < this.playerControllers.Count; i++)
            {
                var playerController = this.playerControllers[i];
                if (playerController != null)
                {
                    playerScores[i] = playerController.Score.ToString();
                }
            }
            SetUI.SetInteract(resultPageCg, false);
            string scoresJson = "[" + string.Join(",", playerScores) + "]";
            StartCoroutine(
                loader.apiManager.postScoreToStarAPI(scoresJson, (stars) => {
                    LogController.Instance.debug("Score to Star API call completed!");

                    for (int i = 0; i < this.playerControllers.Count; i++)
                    {
                        var playerController = this.playerControllers[i];
                        if (playerController != null)
                        {
                            if (loader.CurrentHostName.Contains("dev.starwishparty.com") ||
                                loader.CurrentHostName.Contains("uat.starwishparty.com") ||
                                loader.CurrentHostName.Contains("pre.starwishparty.com") ||
                                loader.CurrentHostName.Contains("www.starwishparty.com"))
                            {
                                if (i == 0 && stars[0] > 0)
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

                            int star = (stars != null && i < stars.Length) ? stars[i] : 0;
                            this.endGamePage.updateFinalScoreWithStar(i, playerController.Score, star, () =>
                            {
                                if (this.endGamePage.scoreEndings[i].starNumber > 0)
                                {
                                    showSuccess = true;
                                }
                            });
                        }
                    }

                    this.endGamePage.setStatus(true, showSuccess);
                    base.endGame();
                })
            );
        }
        else
        {
            for (int i = 0; i < this.playerControllers.Count; i++)
            {
                var playerController = this.playerControllers[i];
                if (playerController != null)
                {
                    this.endGamePage.updateFinalScore(i, playerController.Score, () =>
                    {
                        if (this.endGamePage.scoreEndings[i].starNumber > 0)
                        {
                            showSuccess = true;
                        }
                    });
                }
            }
            this.endGamePage.setStatus(true, showSuccess);
            base.endGame();
        }
    }

    public void UpdateNextQuestion()
    {
        LogController.Instance?.debug("Next Question");
        QuestionController.Instance?.nextQuestion();
        this.gridManager.currentHiddenWrongWord = "";

        if (QuestionController.Instance.currentQuestion.answersChoics != null &&
            QuestionController.Instance.currentQuestion.answersChoics.Length > 0)
        {
            string[] answers = QuestionController.Instance.currentQuestion.answersChoics;
            this.gridManager.UpdateGridWithWord(answers, null, ()=> {
                this.obstacleSpawner.GenerateObstacles(this.gridManager);
            });
        }
        else
        {
            string word = QuestionController.Instance.currentQuestion.correctAnswer;
            this.gridManager.UpdateGridWithWord(null, word, ()=>
            {
                this.obstacleSpawner.GenerateObstacles(this.gridManager);
            });
        }

        for (int i = 0; i < this.playerNumber; i++)
        {
            if (this.playerControllers[i] != null)
            {
                this.playerControllers[i].resetRetryTime();
                this.playerControllers[i].playerReset();
                if (this.helpTool[i] != null)
                    this.helpTool[i].setHelpTool(true);
            }
        }

        this.reduceMCOptions = false;
    }

    public void playerUseHelpTool(int userId)
    {
        if (this.helpTool[userId] != null && 
            this.playerControllers[userId] != null && 
            !this.reduceMCOptions)
        {
            this.reduceMCOptions = true;

            this.gridManager.RemoveOneWrongMCOption(
                QuestionController.Instance.currentQuestion.answersChoics,
                QuestionController.Instance.currentQuestion.correctAnswer,      
                ()=>
                {
                    for (int i = 0; i < this.playerNumber; i++)
                    {
                        if (this.playerControllers[i] != null &&
                            this.playerControllers[i].answer.ToLower() == this.gridManager.currentHiddenWrongWord &&
                            this.playerControllers[i].answer.ToLower() != QuestionController.Instance?.currentQuestion.correctAnswer.ToLower())
                        {
                            this.playerControllers[i].removeGroupMoving();
                        }
                    }
                }
                );



            this.helpTool[userId].Deduct(() =>
            {
                for (int i = 0; i < this.playerNumber; i++)
                {
                    if (this.playerControllers[i] != null)
                    {
                        if (this.helpTool[i] != null)
                            this.helpTool[i].setHelpTool(false);
                    }
                }
            });
        }
    }

    private void Update()
    {
        if(!this.playing) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.UpdateNextQuestion();
        }
        else if(Input.GetKeyDown(KeyCode.F1))
        {
            this.showCells = !this.showCells;
             this.gridManager.setAllCellsStatus(this.showCells);
        }

        if (this.playerControllers.Count == 0) return;

        bool isCoPlayerMode = this.playerNumber > 1;

        if (isCoPlayerMode)
        {
            bool isNextQuestion = true;

            for (int i = 0; i < this.playerNumber; i++)
            {
                if (this.playerControllers[i] == null || !this.playerControllers[i].IsTriggerToNextQuestion)
                {
                    isNextQuestion = false;
                    break;
                }
            }

            if (isNextQuestion)
            {
                this.UpdateNextQuestion();
            }
        }
        else
        {
            if (this.playerControllers[0] != null && this.playerControllers[0].IsTriggerToNextQuestion)
            {
                this.UpdateNextQuestion();
            }
        }


    } 
}
