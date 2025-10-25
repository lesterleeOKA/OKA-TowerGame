using System;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Linq;

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
    public int star;
    public score score;
    public int correctAnswerIndex;
    public int maxScore;
    public learningObjective learningObjective;
    //public media[] media; 
    public string[] media;
    public Texture texture;
    public AudioClip audioClip;
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
    FillInBlank = 4
}

[Serializable]
public class CurrentQuestion
{
    public int numberQuestion = 0;
    public int answeredQuestion = 0;
    public QuestionType questiontype = QuestionType.None;
    public QuestionList qa = null;
    public string[] correctAnswerParts;
    public string displayQuestion = "";
    public string fullSentence = "";
    public int correctAnswerId;
    public string correctAnswer;
    public string[] answersChoics;
    public CanvasGroup[] questionBgs;
    private RawImage questionImage;
    public CanvasGroup audioPlayBtn = null;
    private AspectRatioFitter aspecRatioFitter = null;
    public CanvasGroup progressiveBar;
    public Image progressFillImage;
    public TextMeshProUGUI questionText;

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
        switch (qa.questionType)
        {
            case "Picture":
            case "picture":
                SetUI.SetGroup(this.questionBgs, 0, 0f);
                this.questionImage = this.questionBgs[0].GetComponentInChildren<RawImage>();
                this.questionText = this.questionBgs[0].GetComponentInChildren<TextMeshProUGUI>();
                if (this.questionText != null) this.questionText.text = this.NewQuestionUnderlineStyle();
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

                    var parentRectTransform = this.questionImage.transform.parent.GetComponent<RectTransform>();
                    var parentWidth = parentRectTransform.sizeDelta.x;
                    if (qaImage.width > qaImage.height)
                    {
                        this.questionImage.GetComponent<RectTransform>().sizeDelta = new Vector2(parentWidth, 300f);
                    }
                    else
                    {
                        this.questionImage.GetComponent<RectTransform>().sizeDelta = new Vector2(parentWidth, 430f);
                    }
                    this.aspecRatioFitter.aspectRatio = (float)qaImage.width / (float)qaImage.height;
                }
                break;
            case "Audio":
            case "audio":
                SetUI.SetGroup(this.questionBgs, 1, 0f);
                this.questionText = this.questionBgs[1].GetComponentInChildren<TextMeshProUGUI>();
                if (this.questionText != null) this.questionText.text = this.NewQuestionUnderlineStyle();
                this.audioPlayBtn = this.questionBgs[1].GetComponentInChildren<CanvasGroup>();
                if (this.audioPlayBtn != null)
                {
                    SetUI.Set(this.audioPlayBtn, true, 0f);
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
                if (this.questionText != null)
                {
                    switch (LoaderConfig.Instance.gameSetup.qa_font_alignment)
                    {
                        case 1:
                            this.questionText.alignment = TextAlignmentOptions.Left;
                            break;
                        case 2:
                            this.questionText.alignment = TextAlignmentOptions.Center;
                            break;
                        case 3:
                            this.questionText.alignment = TextAlignmentOptions.Right;
                            break;
                        default:
                            this.questionText.alignment = TextAlignmentOptions.Left;
                            break;
                    }
                    this.questionText.text = this.NewQuestionUnderlineStyle();
                }
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;
                break;
            case "FillInBlank":
            case "fillInBlank":
                SetUI.SetGroup(this.questionBgs, 3, 0f);
                this.questionText = this.questionBgs[3].GetComponentInChildren<TextMeshProUGUI>();
                if (this.questionText != null)
                {
                    switch (LoaderConfig.Instance.gameSetup.qa_font_alignment)
                    {
                        case 1:
                            this.questionText.alignment = TextAlignmentOptions.Left;
                            break;
                        case 2:
                            this.questionText.alignment = TextAlignmentOptions.Center;
                            break;
                        case 3:
                            this.questionText.alignment = TextAlignmentOptions.Right;
                            break;
                        default:
                            this.questionText.alignment = TextAlignmentOptions.Left;
                            break;
                    }
                    this.questionText.text = this.NewQuestionUnderlineStyle();
                }

                this.audioPlayBtn = this.questionBgs[3].GetComponentInChildren<CanvasGroup>();
                if(this.audioPlayBtn != null)
                {
                    SetUI.Set(this.audioPlayBtn, true, 0f);
                    this.audioPlayBtn.GetComponentInChildren<Button>()?.gameObject.SetActive(qa.audioClip != null);

                }
                this.questiontype = QuestionType.FillInBlank;
                this.correctAnswer = qa.correctAnswer;
                this.answersChoics = qa.answers;
                this.correctAnswerId = this.answersChoics != null ? Array.IndexOf(this.answersChoics, this.correctAnswer) : 0;
                this.playAudio();
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

    public string NewQuestionUnderlineStyle()
    {
        if (this.qa.question.Contains("_"))
        {
            this.correctAnswerParts = qa.correctAnswer.Split(' ');
            int numBlanks = correctAnswerParts.Length;

            this.displayQuestion = Regex.Replace(
                this.qa.question,
                "_+",
                match =>
                {
                    // Generate underlined blanks for each word in the correct answer
                    string blanks = string.Join(" ", this.correctAnswerParts.Select(word => $"<u><color=#00000000>{word}</color></u>"));
                    bool hasSpaceBefore = match.Index > 0 && char.IsWhiteSpace(this.qa.question[match.Index - 1]);
                    bool hasSpaceAfter = match.Index + match.Length < this.qa.question.Length && char.IsWhiteSpace(this.qa.question[match.Index + match.Length]);
                    string before = hasSpaceBefore ? "" : " ";
                    string after = hasSpaceAfter ? "" : " ";
                    return $"{before}{blanks}{after}";
                },
                RegexOptions.None
            );

            // For fullSentence, show the actual answer underlined
            this.fullSentence = Regex.Replace(
                this.qa.question,
                "_+",
                match =>
                {
                    string answer = qa.correctAnswer;
                    bool hasSpaceBefore = match.Index > 0 && char.IsWhiteSpace(this.qa.question[match.Index - 1]);
                    bool hasSpaceAfter = match.Index + match.Length < this.qa.question.Length && char.IsWhiteSpace(this.qa.question[match.Index + match.Length]);
                    string before = hasSpaceBefore ? "" : " ";
                    string after = hasSpaceAfter ? "" : " ";
                    return $"{before}{answer}{after}";
                },
                RegexOptions.None
            );
        }
        else
        {
            this.displayQuestion = (this.qa.question != this.qa.correctAnswer) ? this.qa.question : "";
        }
        return this.displayQuestion;
    }

    public void showFullSentenceAfterCompleted()
    {
        if (this.questionText != null && !string.IsNullOrEmpty(this.fullSentence))
        {
            this.questionText.text = this.fullSentence;
        }
    }

    public AudioClip currentAudioClip
    {
        get
        {
            return this.qa.audioClip;
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