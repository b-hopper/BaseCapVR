using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HandUiController : MonoBehaviour
{
    private static HandUiController _instance;

    public static HandUiController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<HandUiController>(false);
            }
            return _instance;
        }
    }

    PlayerRef _playerRef;

    private int _team = -1;

    [FormerlySerializedAs("_leftHandUiElements")] [SerializeField] List<StarbaseUI> _starbaseUis;
    
    [Serializable]
    private struct StarbaseUI
    {
        [SerializeField] internal GameObject _uiObject;
        
        [Header("Base Count")]
        [SerializeField] internal TMP_Text _baseCountTextLeft;
        [SerializeField] internal TMP_Text _baseCountTextRight;
        [SerializeField] internal Image _baseCountImageLeft;
        [SerializeField] internal Image _baseCountImageRight;

        [Header("Drone Count")] 
        [SerializeField] internal TMP_Text _droneCountTextLeft;
        [SerializeField] internal TMP_Text _droneCountTextRight;
        [SerializeField] internal Image _droneCountImageLeft;
        [SerializeField] internal Image _droneCountImageRight;
        [SerializeField] internal Image _selectedDroneCountBorder;
        [SerializeField] internal TMP_Text _selectedDroneCountText;

        [Header("Drones Per Minute")] 
        [SerializeField] internal TMP_Text _dronePerMinuteTextLeft;
        [SerializeField] internal TMP_Text _dronePerMinuteTextRight;
        [SerializeField] internal Image _dronePerMinuteImageLeft;
        [SerializeField] internal Image _dronePerMinuteImageRight;

        [Header("Combat Results")] 
        [SerializeField] internal Image _combatResultsBorder;
        [SerializeField] internal TMP_Text _combatResultsText;
        
        [Header("Upgrade Tool")]
        [SerializeField] internal Transform _upgradeToolTransform;

    }

    private bool _initialized = false;

    private void Awake()
    {
        GameStateManager.GameOverEvent.AddListener(() =>
        {
            _initialized = false;
        });
    }

    private void FixedUpdate()
    {
        if (!_initialized)
        {
            _initialized = GameStateManager.Instance != null && GameStateManager.Instance.IsGameRunning && StarmapManager.Instance.initialized;

            foreach (var ui in _starbaseUis)
            {
                if (ui._uiObject != null)
                    ui._uiObject.SetActive(_initialized);
            }

            return;
        }

        if (_initialized) UpdateUIs();
    }

    private void UpdateUIs()
    {
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsGameRunning 
           || StarmapManager.Instance == null || !StarmapManager.Instance.initialized)
        {
            ResetUIs();
            return;
        }
        
        // # bases, # drones, # drones per minute
        if (_team == -1) _team = PlayerTeamAssignmentManager.Instance.GetPlayerTeam(_playerRef);
        UpdateStarBaseCount();
        UpdateDroneCount();
        UpdateGeneratedPerMinuteCount();
    }

    private void ResetUIs()
    {
        _initialized = false;
        _team = -1;
        foreach (var ui in _starbaseUis)
        {
            if (ui._uiObject != null)
                ui._uiObject.SetActive(false);
        }
    }

    public void AssignPlayer(PlayerRef newPlayer)
    {
        _playerRef = newPlayer;
    }

    List<int> _lastStarBaseCounts = new List<int>();

    private void UpdateStarBaseCount()
    {
        List<int> amounts = new List<int>();
        var teamCount = TeamAnalyticsManager.Instance.GetTeamCount();
        bool changed = false;
        for (int i = 0; i < teamCount; i++)
        {
            var count = TeamAnalyticsManager.Instance.GetStarBaseCountForTeam(i);
            if (_lastStarBaseCounts.Count <= i || count != _lastStarBaseCounts[i])
                changed = true;
            amounts.Add(count);
        }

        if (!changed) return;

        _lastStarBaseCounts.Clear();

        var totalCount = TeamAnalyticsManager.Instance.GetTotalStarBaseCount();

        for (int i = 0; i < teamCount; i++)
        {
            _lastStarBaseCounts.Add(amounts[i]);
            var ratio = (float)amounts[i] / (float)totalCount;
            var color = TeamAnalyticsManager.Instance.teamSettings.GetTeamData(i).teamColor;

            if (i == _team)
            {            
                foreach (var ui in _starbaseUis)
                {
                    if (ui._baseCountImageLeft != null)
                    {
                        ui._baseCountImageLeft.color = color;
                        ui._baseCountImageLeft.fillAmount = ratio;
                    }

                    if (ui._baseCountTextLeft != null)
                    {
                        ui._baseCountTextLeft.text = $"{amounts[i]}";
                        ui._baseCountTextLeft.color = color;
                    }
                }
            }
            else
            {
                foreach (var ui in _starbaseUis)
                {
                    if (ui._baseCountImageRight != null)
                    {
                        ui._baseCountImageRight.color = color;
                        ui._baseCountImageRight.fillAmount = ratio;
                    }

                    if (ui._baseCountTextRight != null)
                    {
                        ui._baseCountTextRight.text = $"{amounts[i]}";
                        ui._baseCountTextRight.color = color;
                    }
                }
            }
        }
    }

    List<int> _lastDroneCounts = new List<int>();
    int _lastSelectedDroneCount = 0;
    private bool _lastShowCombatResult;

    private void UpdateDroneCount()
    {
        List<int> amounts = new List<int>();
        var teamCount = TeamAnalyticsManager.Instance.GetTeamCount();
        bool changed = false;
        for (int i = 0; i < teamCount; i++)
        {
            var count = TeamAnalyticsManager.Instance.GetDroneCountForTeam(i);
            if (_lastDroneCounts.Count <= i || count != _lastDroneCounts[i])
                changed = true;
            amounts.Add(count);
        }

        var selectedCount = StarBaseManager.Instance.GetSelectedDroneCount();
        if (_lastSelectedDroneCount != selectedCount)
            changed = true;

        bool showCombatResult = StarBaseManager.Instance.combatResult.useResult;
        if (_lastShowCombatResult != showCombatResult) changed = true;

        if (!changed) return;

        _lastDroneCounts.Clear();

        var totalCount = TeamAnalyticsManager.Instance.GetTotalDroneCount();

        for (int i = 0; i < teamCount; i++)
        {
            _lastDroneCounts.Add(amounts[i]);
            var ratio = (float)amounts[i] / (float)totalCount;
            var color = TeamAnalyticsManager.Instance.teamSettings.GetTeamData(i).teamColor;

            if (i == _team)
            {
                foreach (var ui in _starbaseUis)
                {
                    if (ui._droneCountImageLeft != null)
                    {
                        ui._droneCountImageLeft.color = color;
                        ui._droneCountImageLeft.fillAmount = ratio;
                    }

                    if (ui._droneCountTextLeft != null)
                    {
                        ui._droneCountTextLeft.text = $"{amounts[i]}";
                        ui._droneCountTextLeft.color = color;
                    }
                }
            }
            else
            {
                foreach (var ui in _starbaseUis)
                {
                    if (ui._droneCountImageRight != null)
                    {
                        ui._droneCountImageRight.color = color;
                        ui._droneCountImageRight.fillAmount = ratio;
                    }

                    if (ui._droneCountTextRight != null)
                    {
                        ui._droneCountTextRight.text = $"{amounts[i]}";
                        ui._droneCountTextRight.color = color;
                    }
                }
            }
        }

        foreach (var ui in _starbaseUis)
        {
            if (ui._selectedDroneCountBorder != null)
            {
                ui._selectedDroneCountBorder.gameObject.SetActive(selectedCount > 0);
                ui._selectedDroneCountBorder.color =
                    TeamAnalyticsManager.Instance.teamSettings.GetTeamData(_team).teamColor;
            }
            if (ui._selectedDroneCountText != null)
            {
                ui._selectedDroneCountText.text = $"{selectedCount}";
                ui._selectedDroneCountText.color =
                    TeamAnalyticsManager.Instance.teamSettings.GetTeamData(_team).teamColorSecondary;
            }

            if (ui._combatResultsBorder != null)
            {
                ui._combatResultsBorder.gameObject.SetActive(StarBaseManager.Instance.combatResult.useResult);
            }
        }

        FormatCombatResultText(StarBaseManager.Instance.combatResult.combatDelta);

        _lastSelectedDroneCount = selectedCount;
        _lastShowCombatResult = StarBaseManager.Instance.combatResult.useResult;
    }

    private void FormatCombatResultText(int result)
    {
        if (result < 0)
        {
            foreach (var ui in _starbaseUis)
            {
                if (ui._combatResultsText != null)
                {
                    ui._combatResultsText.color = Color.red;
                    ui._combatResultsText.text = result.ToString();
                }
            }
        }
        else if (result > 0)
        {
            foreach (var ui in _starbaseUis)
            {
                if (ui._combatResultsText != null)
                {
                    ui._combatResultsText.color =  Color.green;
                    ui._combatResultsText.text = "+" + result.ToString();
                }
            }
        }
        else
        {
            foreach (var ui in _starbaseUis)
            {
                if (ui._combatResultsText != null)
                {
                    ui._combatResultsText.color =  Color.black;
                    ui._combatResultsText.text = result.ToString();
                }
            }
        }
    }

    private List<float> _lastGeneratedPerMinuteCounts = new List<float>();

    private void UpdateGeneratedPerMinuteCount()
    {
        List<float> amounts = new List<float>();
        var teamCount = TeamAnalyticsManager.Instance.GetTeamCount();
        bool changed = false;
        float totalCount = 0f;
        for (int i = 0; i < teamCount; i++)
        {
            var prodPerSec = TeamAnalyticsManager.Instance.GetDroneProductionForTeam(i);
            if (_lastGeneratedPerMinuteCounts.Count <= i || prodPerSec != _lastGeneratedPerMinuteCounts[i])
                changed = true;
            amounts.Add(prodPerSec);
            totalCount += prodPerSec;
        }

        if (!changed) return;

        _lastGeneratedPerMinuteCounts.Clear();


        for (int i = 0; i < teamCount; i++)
        {
            _lastGeneratedPerMinuteCounts.Add(amounts[i]);
            var ratio = (float)amounts[i] / (float)totalCount;
            var color = TeamAnalyticsManager.Instance.teamSettings.GetTeamData(i).teamColor;

            if (i == _team)
            {
                foreach (var ui in _starbaseUis)
                {
                    if (ui._dronePerMinuteImageLeft != null)
                    {
                        ui._dronePerMinuteImageLeft.color = color;
                        ui._dronePerMinuteImageLeft.fillAmount = ratio;
                    }
                    
                    if (ui._dronePerMinuteTextLeft != null)
                    {
                        ui._dronePerMinuteTextLeft.text = $"{(amounts[i] * 60f):F0}";
                        ui._dronePerMinuteTextLeft.color = color;
                    }
                }
            }
            else
            {
                foreach (var ui in _starbaseUis)
                {
                    if (ui._dronePerMinuteImageRight != null)
                    {
                        ui._dronePerMinuteImageRight.color = color;
                        ui._dronePerMinuteImageRight.fillAmount = ratio;
                    }
                    
                    if (ui._dronePerMinuteTextRight != null)
                    {
                        ui._dronePerMinuteTextRight.text = $"{(amounts[i] * 60f):F0}";
                        ui._dronePerMinuteTextRight.color = color;
                    }
                }
            }
        }
    }
    
    public void ResetUpgradeToolPosition()
    {
        foreach (var ui in _starbaseUis)
        {
            if (ui._upgradeToolTransform != null)
            {
                ui._upgradeToolTransform.localPosition = Vector3.zero;
                ui._upgradeToolTransform.localRotation = Quaternion.identity;
                ui._upgradeToolTransform.localScale = Vector3.one;
            }
        }
    }
}