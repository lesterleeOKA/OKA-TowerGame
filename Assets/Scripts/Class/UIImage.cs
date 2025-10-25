using DG.Tweening;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class UIImage
{
    public CanvasGroup[] cg;
    public float duration = 0f;
    public int currentId = 0;
    public bool isAnimated = false;

    public void Init()
    {
        this.currentId = 0;
        isAnimated = false;
        this.toImage(this.currentId);
    }
    public void toImage(int _nextId, bool useScale = true)
    {
        if (this.isAnimated) return;
        for (int i = 0; i < this.cg.Length; i++)
        {
            if (this.cg[i] != null)
            {
                if (i == _nextId)
                {
                    this.currentId = _nextId;
                    this.isAnimated = true;
                    this.cg[_nextId].DOFade(1f, this.duration).OnComplete(() => this.isAnimated = false);
                    if (useScale) this.cg[_nextId].transform.DOScale(0.95f, this.duration).SetEase(Ease.OutBack);
                    this.cg[_nextId].interactable = true;
                    this.cg[_nextId].blocksRaycasts = true;
                    this.cg[_nextId].gameObject.SetActive(true);
                }
                else
                {
                    /* this.cg[i].DOFade(0f, 0f);
                     this.cg[i].interactable = false;
                     this.cg[i].blocksRaycasts = false;*/
                    this.cg[i].gameObject.SetActive(false);
                }
            }
        }
    }
}


public static class SetUI
{
    public static void SetObject(GameObject[] _objs = null, int _showId = -1)
    {
        if (_objs != null)
            for (int i = 0; i < _objs.Length; i++)
            {
                if (_objs[i] != null)
                {

                    if (i == _showId)
                    {
                        _objs[_showId].SetActive(true);
                    }
                    else
                    {
                        _objs[i].SetActive(false);
                    }
                }
            }
    }
    public static void Set(CanvasGroup _cg=null, bool _status=false, float _duration=0f, float _endValue = 0f, Action _onComplete = null)
    {
        if (_cg != null)
        {
            _cg.DOFade(_status? 1f : _endValue, _duration).OnComplete(()=> { 
                if (_onComplete != null) 
                    _onComplete.Invoke(); 
            });
            _cg.interactable = _status;
            _cg.blocksRaycasts = _status;
        }
    }

    public static void SetInteract(CanvasGroup _cg = null, bool _status = false)
    {
        if (_cg != null)
        {
            _cg.interactable = _status;
            _cg.blocksRaycasts = _status;
        }
    }

    public static void SetTarget(CanvasGroup _cg = null, bool _status = false, float _target = 0f, float _duration = 0f, float _delay = 0f, TweenCallback _onComplete = null)
    {
        if (_cg != null)
        {
            _cg.DOFade(_target, _duration).SetDelay(_delay).OnComplete(() => {
                if (_onComplete != null)
                    _onComplete.Invoke();
            });
            _cg.interactable = _status;
            _cg.blocksRaycasts = _status;
        }
    }

    public static void SetGroup(CanvasGroup[] _cgs = null, int _showId=-1, float _duration = 0f)
    {
        for (int i = 0; i < _cgs.Length; i++) {
            if (_cgs[i] != null) {

                if (i == _showId)
                {
                    _cgs[_showId].DOFade(1f, _duration);
                    _cgs[_showId].interactable = true;
                    _cgs[_showId].blocksRaycasts = true;
                }
                else
                {
                    _cgs[i].DOFade(0f, _duration);
                    _cgs[i].interactable = false;
                    _cgs[i].blocksRaycasts = false;
                }
            }
        }
    }

    public static void SetMove(CanvasGroup _cg = null, bool _status = false, Vector2 _targetPos= default, float _duration = 0f, Action _onComplete = null)
    {
        if (_cg != null)
        {
            _cg.DOFade(_status ? 1f : 0f, _duration);
            _cg.transform.DOLocalMove(_targetPos, _duration).OnComplete(() => {
                if (_onComplete != null)
                    _onComplete.Invoke();
            });
            _cg.interactable = _status;
            _cg.blocksRaycasts = _status;
        }
    }

    public static void SetScale(CanvasGroup _cg = null, bool _status = false, float endValue = 1f, float _duration = 0f, Ease easeType = default, Action _onComplete = null)
    {
        if (_cg != null)
        {
            //_cg.transform.DOScale(0f, 0f);
            _cg.transform.DOScale(_status ? endValue : 0f, _duration).SetEase(easeType).OnComplete(() => {
                if (_onComplete != null)
                    _onComplete.Invoke();
            });
            //_cg.interactable = _status;
            //_cg.blocksRaycasts = _status;
        }
    }

    public static Sprite ConvertTextureToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public static Texture2D FlipTextureVertically(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height, original.format, false);
        for (int y = 0; y < original.height; y++)
        {
            flipped.SetPixels(0, y, original.width, 1, original.GetPixels(0, original.height - y - 1, original.width, 1));
        }
        flipped.Apply();
        return flipped;
    }

    public static string GetFileNameFromUrl(string url)
    {
        // Use Uri to ensure the URL is well-formed
        Uri uri = new Uri(url);
        string fileName = Path.GetFileNameWithoutExtension(uri.LocalPath);
        // Extract the file name from the last segment of the path
        return fileName;
    }

    public static bool ContainsChinese(string input)
    {
        // Chinese Unicode range: \u4e00-\u9fff (CJK Unified Ideographs)
        return Regex.IsMatch(input, @"[\u4e00-\u9fff]");
    }

}
