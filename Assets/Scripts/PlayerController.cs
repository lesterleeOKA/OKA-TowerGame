using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : UserData
{
    public BloodController bloodController;
    public Scoring scoring;
    public string answer = string.Empty;
    public bool IsCorrect = false;
    public bool IsTriggerToNextQuestion = false;
    public bool IsCheckedAnswer = false;
    public Image answerBoxFrame;
    public float speed;
    [HideInInspector]
    public Transform characterTransform;
    public Vector3 startPosition = Vector3.zero;
    public CharacterAnimation characterAnimation = null;
    public List<Cell> collectedCell = new List<Cell>();
    public CanvasGroup[] playerNo;

    public void Init(CharacterSet characterSet = null, Sprite[] defaultAnswerBoxes = null, bool enableMove = false)
    {
        this.characterTransform = this.transform;
        //this.characterTransform.localPosition = this.startPosition;
        this.characterAnimation = this.GetComponent<CharacterAnimation>();
        this.characterAnimation.characterSet = characterSet;
        SetUI.Set(this.GetComponent<CanvasGroup>(), true);

        this.characterTransform.localScale = new Vector3(this.UserId > 0 ? 1 : -1, 1, 1);

        if (this.bloodController == null)
        {
            if(GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Blood") != null)
            {
                this.bloodController = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Blood").GetComponent<BloodController>();
            }
        }

        if (this.PlayerIcons[0] == null)
        {
            if(GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Icon") != null)
            {
                this.PlayerIcons[0] = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Icon").GetComponent<PlayerIcon>();
            } 
        }

        if (this.scoring.scoreTxt == null)
        {
            if(GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Score") != null)
            {
                this.scoring.scoreTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Score").GetComponent<TextMeshProUGUI>();
            }
        }

        if (this.scoring.resultScoreTxt == null)
        {
            if(GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_ResultScore") != null)
            {
                this.scoring.resultScoreTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_ResultScore").GetComponent<TextMeshProUGUI>();
            }
        }

        this.scoring.init();
        this.updateRetryTimes(-1);

        /*if (this.playerNo[this.UserId] != null)
        {
            this.playerNo[this.UserId].alpha = 1f;
            this.playerNo[this.UserId].transform.localScale = new Vector3(this.UserId > 0 ? 1 : -1, 1, 1);

            var text = this.playerNo[this.UserId].GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = characterSet.playersColor;
            }
        }*/

        this.GetComponent<GroupMoving>().enabled = enableMove;
        this.GetComponent<EdgeCollider2D>().enabled = enableMove;
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

        /*if (this.helpTool != null)
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
        }*/
    }

    public void updatePlayerIcon(bool _status = false, string _playerName = "", Sprite _icon = null, Color32 _color = default)
    {
        for (int i = 0; i < this.PlayerIcons.Length; i++)
        {
            if (this.PlayerIcons[i] != null)
            {
                this.PlayerColor = _color;
                this.PlayerIcons[i].playerColor = _color;
                //this.joystick.handle.GetComponent<Image>().color = _color;
                this.PlayerIcons[i].SetStatus(_status, _playerName, _icon);
            }
        }

    }

    string CapitalizeFirstLetter(string str)
    {
        if (string.IsNullOrEmpty(str)) return str; // Return if the string is empty or null
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    public void checkAnswer(int currentTime, Action onCompleted = null)
    {
        if (!this.IsCheckedAnswer)
        {
            this.IsCheckedAnswer = true;
            var loader = LoaderConfig.Instance;
            var currentQuestion = QuestionController.Instance?.currentQuestion;

            int eachQAScore = LoaderConfig.Instance.gameSetup.gameSettingScore > 0 ?
                LoaderConfig.Instance.gameSetup.gameSettingScore :
                (currentQuestion.qa.score.full == 0 ? 10 : currentQuestion.qa.score.full);

            int currentScore = this.Score;
            this.answer = GroupMoving.Instance.answerText.text.ToLower();
            var lowerQIDAns = currentQuestion.correctAnswer.ToLower();
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

                    loader.SubmitAnswer(
                               currentTime,
                               this.Score,
                               answeredPercentage,
                               progress,
                               correctId,
                               currentTime,
                               currentQuestion.qa.qid,
                               currentQuestion.correctAnswerId,
                               this.CapitalizeFirstLetter(this.answer),
                               currentQuestion.correctAnswer,
                               score,
                               currentQAPercent,
                               null
                               );
                }
            }, ()=>
            {
                onCompleted?.Invoke();
            }));
        }
    }

    public void resetRetryTime()
    {
        this.updateRetryTimes(-1);
        this.bloodController?.setBloods(true);
        this.IsTriggerToNextQuestion = false;
    }

    public IEnumerator showAnswerResult(bool correct, Action onCorrectCompleted = null, Action onFailureCompleted = null)
    {
        float delay = 2f;
        if (correct)
        {
            this.collectedCell.Clear();
            LogController.Instance?.debug("Add marks" + this.Score);
            QuestionController.Instance?.currentQuestion.showFullSentenceAfterCompleted();
            GameController.Instance?.setGetScorePopup(true);
            AudioController.Instance?.PlayAudio(1);
            onCorrectCompleted?.Invoke();
            yield return new WaitForSeconds(delay);
            GameController.Instance?.setGetScorePopup(false);
            GameController.Instance?.UpdateNextQuestion();
        }
        else
        {
            GameController.Instance?.setWrongPopup(true);
            AudioController.Instance?.PlayAudio(2);
            this.updateRetryTimes(0);
            yield return new WaitForSeconds(delay);
            GameController.Instance?.setWrongPopup(false);
            
            if (this.Retry <= 0)
            {
                onCorrectCompleted?.Invoke();
                this.IsTriggerToNextQuestion = true;
            }
            onFailureCompleted?.Invoke();
        }
        this.scoring.correct = false;
    }

    public void characterReset()
    {
        // this.characterTransform.localPosition = this.startPosition;
        this.collectedCell.Clear();
    }

    public void removeGroupMoving()
    {
        this.setAnswer("");
        GroupMoving.Instance.ResetAnswerBoard();
        this.collectedCell.Clear();
    }

    public void playerReset()
    {
        this.characterAnimation.setIdling();
        this.deductAnswer();
        this.setAnswer("");
        GroupMoving.Instance.ResetMovingPlane();
        this.characterReset();
        this.IsCheckedAnswer = false;
        this.IsCorrect = false;
    }


    public void setAnswer(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            this.answer = "";
        }
        else
        {
            if(content.Length > 1) { 
                this.answer = content;
            }
            else
            {
                this.answer += content;
            }
        }
    }

    public void deductAnswer()
    {
       var gridManager = GameController.Instance.gridManager;
        if (this.answer.Length > 0)
        {
            string deductedChar;
            if (gridManager.isMCType)
            {
                deductedChar = this.answer;
                this.setAnswer("");
            }
            else
            {
                deductedChar = this.answer[this.answer.Length - 1].ToString();
                this.answer = this.answer.Substring(0, this.answer.Length - 1);
                if (GroupMoving.Instance.answerText != null)
                    GroupMoving.Instance.answerText.text = this.answer;

                if (this.answer.Length == 0)
                {
                    SetUI.Set(GroupMoving.Instance.textCg, false);
                }
            }

            if (this.collectedCell.Count > 0)
            {
                var latestCell= this.collectedCell[this.collectedCell.Count - 1];
                latestCell.SetTextStatus(true);
                this.collectedCell.RemoveAt(this.collectedCell.Count - 1);
            }
        }
    }

}
