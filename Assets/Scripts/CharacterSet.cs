using System;
using UnityEngine;

[Serializable]
public class CharacterSet
{
    public string name;
    public int playerNumber;
    public Color playersColor;
    public Texture boardTexture;
    public Texture defaultIcon, idlingTexture;
    public Texture[] walkingAnimationTextures;
    public Vector3 positionRangeX;
    public Vector3 positionRangeY;
}
