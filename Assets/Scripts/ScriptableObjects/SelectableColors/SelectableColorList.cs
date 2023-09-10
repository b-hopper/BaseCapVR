using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SelectableColorList", menuName = "ScriptableObjects/SelectableColorList")]
public class SelectableColorList : ScriptableObject
{
    public List<SelectableColor> selectableColors;
}

[Serializable]
public struct SelectableColor
{
    public string name;
    public Color color; // the value used by the bases
    public Color secondaryColor; // value used by the lines
    public Color highlightColor; // value used by the highlighter
    public Sprite colorSwatch; // to display in the dropdown menus
}