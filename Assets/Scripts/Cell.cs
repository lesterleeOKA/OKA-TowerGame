using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Cell : MonoBehaviour
{
    public int playerId = -1;
    public TextMeshProUGUI content;
    private CanvasGroup cellImage = null;
    public Texture[] cellTextures;
    public RawImage cellTexture;
    public Color32 defaultColor = Color.black;
    public Color32 selectedColor = Color.white;
    public int row;
    public int col;
    public bool isSelected = false;
    public bool isPlayerStayed = false;
    public int cellId = -1;

    public void SetTextContent(string letter="", Color _color = default)
    {
        this.SetTextStatus(false, 0f);
        if (this.cellImage == null) 
            this.cellImage = this.GetComponent<CanvasGroup>();

        //this.SetButtonColor(_color);
        int raomIndex = Random.Range(0, this.cellTextures.Length);
        if(this.cellTexture == null) this.cellTexture = this.cellImage.GetComponentInChildren<RawImage>();
        this.cellTexture.texture = this.cellTextures[raomIndex];

        if (this.content != null && !string.IsNullOrEmpty(letter)) {
            this.SetTextStatus(true);
            this.content.text = letter;
            if (SetUI.ContainsChinese(letter))
            {
                this.content.enableAutoSizing = false;
                this.content.enableWordWrapping = false;
                this.content.fontSize = 75f;
            }

            this.isSelected = true;
            this.setCellStatus(true);
        }
        else
        {
            this.content.text = "";
            this.setCellStatus(false);
        }
    }

    public void SetTextStatus(bool show, float duration=0.5f)
    {
        this.transform.DOScale(show ? 1f : 0f, duration).SetEase(Ease.InOutSine);
        this.isSelected = show ? true : false;
    }

   /* public void SetButtonColor(Color _color = default)
    {
        if (_color != default(Color))
            this.cellImage.color = _color;
        else
            this.cellImage.color = Color.white;
    }*/

    public void SetTextColor(Color _color = default)
    {
        if (this.content != null)
        {
            if (_color != default(Color))
                this.content.color = _color;
            else
                this.content.color = Color.black;
        }
    }

    public void setCellStatus(bool show=false)
    {
        if(this.cellImage != null)
        {
            this.cellImage.alpha = show? 1f:0f;
        }
    }

    public void setCellDebugStatus(bool show = false)
    {
        this.GetComponent<RawImage>().enabled = show;
    }

    public void setCellEnterColor(bool stay = false, bool show = false)
    {
        if (this.cellImage != null)
        {
            this.isPlayerStayed = stay;
            this.cellImage.GetComponent<RawImage>().color = show ? Color.yellow : Color.white;
        }
    }


}
