using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LinePath : MonoBehaviour
{
    [SerializeField] internal LineRenderer lineRenderer;
    [SerializeField] private float lineOffset = 1f;

    // needed for pathfinding
    public float LineDistance { get; private set; }
    public StarBase StartBase { get; private set; }
    public StarBase EndBase { get; private set; }

    public Material marchingAntsMaterial;

    [SerializeField] private TextMeshProUGUI startBaseDroneCountText;
    [SerializeField] private Transform startBaseDroneCount;
    [SerializeField] private Image startBaseDroneCountBorder;
    [SerializeField] private TextMeshProUGUI endBaseDroneCountText;
    [SerializeField] private Transform endBaseDroneCount;
    [SerializeField] private Image endBaseDroneCountBorder;
    
    public bool CheckIds(int id1, int id2)
    {
        return (id1 == StartBase.BaseId && id2 == EndBase.BaseId) || (id2 == StartBase.BaseId && id1 == EndBase.BaseId);
    }

    public void DrawLine(StarBase startBase, StarBase endBase)
    {
        Vector3 start = startBase.transform.localPosition;
        Vector3 end = endBase.transform.localPosition;

        StartBase = startBase;
        EndBase = endBase;
        LineDistance = Math.Abs(Vector3.Distance(start, end));

        Vector3 direction = (end - start).normalized;
        var pos0 = start + direction * lineOffset;
        var pos1 = end - direction * lineOffset;
        
        lineRenderer.SetPosition(0, pos0);
        lineRenderer.SetPosition(1, pos1);

        startBaseDroneCountText.text = "";
        endBaseDroneCountText.text = "";
        
        startBaseDroneCount.gameObject.SetActive(false);
        endBaseDroneCount.gameObject.SetActive(false);
        
        // align the text to the line
        Vector3 dir = (pos1 - pos0).normalized * 90f;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        startBaseDroneCount.localRotation = Quaternion.Euler(dir);
        endBaseDroneCount.localRotation = Quaternion.Euler(dir);
        

        StartBase.teamUpdated.AddListener(UpdateLineColor);
        EndBase.teamUpdated.AddListener(UpdateLineColor);

        UpdateLineColor();
        
        // if the colors are changed, update the colors
        if (TeamAnalyticsManager.Instance != null)
            TeamAnalyticsManager.Instance.teamSettings.onColorChanged.AddListener((team, data) => UpdateLineColor());
    }

    private void UpdateLineColor()
    {
        Color teamColor;
        Color secondTeamColor;
        
        if (TeamAnalyticsManager.Instance == null)
        { // this is for the editor only
            teamColor = Color.blue;
            secondTeamColor = Color.red;
        }
        else if (StartBase.TeamIndex == EndBase.TeamIndex)
        {
            teamColor = TeamAnalyticsManager.Instance.teamSettings.GetTeamData(StartBase.TeamIndex).teamColorSecondary;
            secondTeamColor = teamColor;
        }
        else
        {
            teamColor = TeamAnalyticsManager.Instance.teamSettings.GetTeamData(StartBase.TeamIndex).teamColorSecondary;
            secondTeamColor = TeamAnalyticsManager.Instance.teamSettings.GetTeamData(EndBase.TeamIndex)
                .teamColorSecondary;
        }

        if (lineRenderer.colorGradient.colorKeys[0].color != teamColor ||
            lineRenderer.colorGradient.colorKeys[1].color != secondTeamColor)
        {
            StartCoroutine(UpdateLineColorCoroutine(teamColor, secondTeamColor, 0.5f));
        }
    }

    bool _isUpdatingLineColor = false;

    private IEnumerator UpdateLineColorCoroutine(Color c1, Color c2, float duration = 0.0f)
    {
        if (_isUpdatingLineColor)
        {
            yield break;
        }

        _isUpdatingLineColor = true;
        var startColor = lineRenderer.colorGradient;
        float time = 0f;
        while (time < duration)
        {
            Color c1lerp = Color.Lerp(startColor.colorKeys[0].color, c1, time / duration);
            Color c2lerp = Color.Lerp(startColor.colorKeys[1].color, c2, time / duration);
            lineRenderer.colorGradient = new Gradient()
            {
                colorKeys = new[]
                {
                    new GradientColorKey(c1lerp, 0.0f),
                    new GradientColorKey(c2lerp, 1.0f)
                }
            };
            startBaseDroneCountBorder.color = c1lerp;
            endBaseDroneCountBorder.color = c2lerp;
            
            //lineRenderer.material.color = Color.Lerp(startColor, c1, time / duration);
            time += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        lineRenderer.colorGradient = new Gradient()
        {
            colorKeys = new[]
            {
                new GradientColorKey(c1, 0.0f),
                new GradientColorKey(c2, 1.0f)
            }
        };
        startBaseDroneCountBorder.color = c1;
        endBaseDroneCountBorder.color = c2;

        _isUpdatingLineColor = false;
    }

    private bool _isHighlighting => _highlightCount > 0;

    [ContextMenu("Highlight Line")]
    public void HighlightLine()
    {
        _highlightCount++;
        if (_highlightCount == 1) StartCoroutine(HighlightLineCoroutine());
    }

    private int _highlightCount = 0;
    [ContextMenu("Unhighlight Line")]
    public void UnhighlightLine()
    {
        _highlightCount--;
    }

    private IEnumerator HighlightLineCoroutine()
    {
        float time = 0f;
        float duration = 0.5f;
        var startGradient = lineRenderer.colorGradient;
        float oldWidth = lineRenderer.widthMultiplier;
        var _oldMat = lineRenderer.material;
        while (_isHighlighting)
        {
            lineRenderer.material = marchingAntsMaterial;
            
            float lerpVal = Mathf.Abs(Mathf.Sin(time / duration));

            float width = Mathf.Lerp(oldWidth, oldWidth * 1.2f, lerpVal);
            lineRenderer.widthMultiplier = width;

            Color c1lerp = Color.Lerp(startGradient.colorKeys[0].color, Color.white, lerpVal);
            Color c2lerp = Color.Lerp(startGradient.colorKeys[1].color, Color.white, lerpVal);
            lineRenderer.colorGradient = new Gradient()
            {
                colorKeys = new[]
                {
                    new GradientColorKey(c1lerp, 0.0f),
                    new GradientColorKey(c2lerp, 1.0f)
                }
            };

            time += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        
        lineRenderer.material = _oldMat;

        lineRenderer.widthMultiplier = oldWidth;
        lineRenderer.colorGradient = startGradient;
    }

    private float _oldScale = -1f;
    private float _originalWidth;

    private void FixedUpdate()
    {
        if (_oldScale != StarmapManager.CurrentStarfieldScale)
        {
            _oldScale = StarmapManager.CurrentStarfieldScale;
            lineRenderer.widthMultiplier = _oldScale * _originalWidth;
        }
    }

    private void Awake()
    {
        _originalWidth = lineRenderer.widthMultiplier;
    }

    private (int, int) _droneCounts = (0, 0);
    
    public void AddDroneToLine(StarBase b)
    {
        if (b == StartBase)
        {
            _droneCounts.Item1++;
            SetDroneCountText(_droneCounts.Item1, 0);
        }
        else if (b == EndBase)
        {
            _droneCounts.Item2++;
            SetDroneCountText(_droneCounts.Item2, 1);
        }
    }
    
    public void RemoveDroneFromLine(StarBase b)
    {
        if (b == StartBase)
        {
            _droneCounts.Item1--;
            SetDroneCountText(_droneCounts.Item1, 0);
        }
        else if (b == EndBase)
        {
            _droneCounts.Item2--;
            SetDroneCountText(_droneCounts.Item2, 1);
        }
    }
    
    public void SetDroneCountText(int count, int idx)
    {
        if (idx == 0) // start base
        {
            startBaseDroneCountText.text = count.ToString();
            startBaseDroneCount.gameObject.SetActive(count != 0);
        }
        else
        {
            endBaseDroneCountText.text = count.ToString();
            endBaseDroneCount.gameObject.SetActive(count != 0);
        }
        SetDroneCountPositions();
    }

    private bool _isDroneCountPosSet = false;
    private void SetDroneCountPositions()
    {
        if (_isDroneCountPosSet)
        { // no need to set positions again
            return;
        }

        Vector3 start = lineRenderer.GetPosition(0);
        Vector3 end = lineRenderer.GetPosition(1);

        Vector3 direction = (end - start).normalized;
        var textPos0 = start + direction * lineOffset +
            new Vector3(0, -0.5f, 0) + direction * 0.5f;
        var textPos1 = end - direction * lineOffset + 
            new Vector3(0, -0.5f, 0) - direction * 0.5f;

        startBaseDroneCount.localPosition = textPos0;
        endBaseDroneCount.localPosition = textPos1;

        var rot = Quaternion.LookRotation(direction);
        rot *= Quaternion.Euler(0, 90, 0);

        startBaseDroneCount.transform.localRotation = rot;
        endBaseDroneCount.transform.localRotation = rot;

        _isDroneCountPosSet = true;
    }

}