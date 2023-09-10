using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class StarbaseAudioManager : MonoBehaviour
{
    [SerializeField] private StarBase _starBase;
    [SerializeField] private AudioSource _audioSource;

    [SerializeField] private StarBaseAudioSettings _audioSettings;
    private void Start()
    {
        if (_starBase == null)
        {
            _starBase = GetComponentInParent<StarBase>();
        }
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }  
        _startPitch = _audioSource.pitch;
    }
    
    public void PlayHoverClip(bool isEnemyBase)
    {
        // 1.3.4.1 : Friendly Base Hover
        // 1.3.4.2 : Enemy Base Hover (w/ units selected) 
        
        var sbClip = isEnemyBase 
            ? _audioSettings.HoverEnemyBaseAudioClips[Random.Range(0, _audioSettings.HoverEnemyBaseAudioClips.Count)]
            : _audioSettings.HoverFriendlyBaseAudioClips[Random.Range(0, _audioSettings.HoverFriendlyBaseAudioClips.Count)];
        
        _audioSource.pitch = _startPitch;
        _audioSource.volume = sbClip.Volume * GameSettingsManager.SFXVolume;

        _audioSource.PlayOneShot(sbClip.Clip);
    }

    public void PlaySelectUnitsClip(int numSelected)
    {
        // 1.3.4.4 : Unit Selection (numSelected is positive)
        // 1.3.4.5 : Unit Deselection (numSelected is negative)

        var sbClip = numSelected > 0
            ? _audioSettings.UnitSelectionAudioClips[Random.Range(0, _audioSettings.UnitSelectionAudioClips.Count)]
            : _audioSettings.UnitDeselectionAudioClips[Random.Range(0, _audioSettings.UnitDeselectionAudioClips.Count)];
            
        _audioSource.volume = sbClip.Volume;
        
        List<AudioClip> clips = new List<AudioClip>();
        
        if (numSelected > 0)
        {
            while (numSelected > 0)
            {
                clips.Add(sbClip.Clip);
                numSelected--;
            }
            
            PlayListOfSounds(clips, 0.05f);
        }
        else
        {
            while (numSelected < 0)
            {
                clips.Add(sbClip.Clip);
                numSelected++;
            }
            PlayListOfSounds(clips, -0.05f);
        }
    }
    
    public void PlayMoveClip(bool isEnemyBase)
    {
        // 1.3.4.6 : Initiating a move command to a controlled base
        // 1.3.4.7 : Initiating an attack command to an uncontrolled base 

        var sbClip = isEnemyBase
            ? _audioSettings.AttackCommandAudioClips[Random.Range(0, _audioSettings.AttackCommandAudioClips.Count)]
            : _audioSettings.MoveCommandAudioClips[Random.Range(0, _audioSettings.MoveCommandAudioClips.Count)];
        
        _audioSource.pitch = _startPitch;
        _audioSource.volume = sbClip.Volume * GameSettingsManager.SFXVolume;
        
        _audioSource.PlayOneShot(sbClip.Clip);
    }

    public void PlayBaseUpgradedClip(bool isFriendly)
    {
        // 1.3.4.9 : Player Upgrades a base
        // 1.3.4.12 : Enemy Upgrades a base

        var sbClip = isFriendly
            ? _audioSettings.BaseUpgradedByPlayerAudioClips[Random.Range(0, _audioSettings.BaseUpgradedByPlayerAudioClips.Count)]
            : _audioSettings.BaseUpgradedByEnemyAudioClips[Random.Range(0, _audioSettings.BaseUpgradedByEnemyAudioClips.Count)];
        
        _audioSource.pitch = _startPitch;
        _audioSource.volume = sbClip.Volume * GameSettingsManager.SFXVolume;
        
        _audioSource.PlayOneShot(sbClip.Clip);
    }

    public void PlayBaseCapturedClip(bool isFriendly, bool capturedWasFriendly = false)
    {
        // 1.3.4.8 : Player captures a base (isFriendly is true)
        // 1.3.4.10 : An Enemy captures a Player base (isFriendly is false, capturedIsFriendly is true)
        // 1.3.4.11 : An Enemy captures a neutral or other Enemy base (isFriendly is false, capturedIsFriendly is false) 
        
        var sbClip = isFriendly
            ? _audioSettings.BaseCapturedByPlayerAudioClips[Random.Range(0, _audioSettings.BaseCapturedByPlayerAudioClips.Count)]
            : capturedWasFriendly
                ? _audioSettings.BaseCapturedByEnemyFromPlayerAudioClips[Random.Range(0, _audioSettings.BaseCapturedByEnemyFromPlayerAudioClips.Count)]
                : _audioSettings.BaseCapturedByEnemyFromNeutralOrEnemyAudioClips[Random.Range(0, _audioSettings.BaseCapturedByEnemyFromNeutralOrEnemyAudioClips.Count)];
        
        _audioSource.pitch = _startPitch;
        _audioSource.volume = sbClip.Volume * GameSettingsManager.SFXVolume;
        
        _audioSource.PlayOneShot(sbClip.Clip);
    }

    public void PlayUnitLaunchClip()
    {
        // 1.3.4.13 : Player unit is launched
        
        var sbClip = _audioSettings.UnitLaunchAudioClips[Random.Range(0, _audioSettings.UnitLaunchAudioClips.Count)];
        
        _audioSource.pitch = _startPitch;
        _audioSource.volume = sbClip.Volume * GameSettingsManager.SFXVolume;
        
        _audioSource.PlayOneShot(sbClip.Clip);
    }
    
    public void PlayUnitLimitReachedClip(bool isAtMax)
    {
        // 1.3.4.14 : A Player controlled base hits maximum units
        // 1.3.4.15 : A Player controlled base has exceeded maximum units

        if (_starBase.TeamIndex != PlayerTeamAssignmentManager.Instance.GetLocalPlayerTeam()) // Only play for local player's team
            return;
        
        var sbClip = isAtMax
            ? _audioSettings.BaseAtMaxUnitsAudioClips[Random.Range(0, _audioSettings.BaseAtMaxUnitsAudioClips.Count)]
            : _audioSettings.BaseExceedsMaxUnitsAudioClips[Random.Range(0, _audioSettings.BaseExceedsMaxUnitsAudioClips.Count)];

        _audioSource.volume = sbClip.Volume * GameSettingsManager.SFXVolume;
        _audioSource.pitch = _startPitch;
        
        _audioSource.PlayOneShot(sbClip.Clip);
    }
    
    private void PlayListOfSounds(List<AudioClip> clips, float pitchChange = 0.0f)
    {
        if (_startPitch < 0f)
        {
            _startPitch = _audioSource.pitch;
            _currentPitch = _startPitch;
        }

        if (Time.time - _lastPitchResetTime > 0.5f)
        {
            _currentPitch = _startPitch;
        }
        
        _audioSource.pitch = _currentPitch;
        PlayListOfSoundsAsync(clips, pitchChange).Forget();
    }

    private static float _startPitch = -1f;
    private float _currentPitch = 0f;
    private float _lastPitchResetTime = 0f;
    private async UniTask PlayListOfSoundsAsync(List<AudioClip> clips, float pitchChange = 0.0f)
    {
        foreach (var clip in clips)
        {
            _audioSource.volume = GameSettingsManager.SFXVolume;
            _audioSource.PlayOneShot(clip);
            await UniTask.WaitForSeconds(0.1f);
            
            _currentPitch += pitchChange;
            _audioSource.pitch = _currentPitch;
            _lastPitchResetTime = Time.time;
        }
    }
    
    
#region Audio Clip Management
    
    [Serializable]
    private struct StarBaseAudioSettings
    {
        [Header("[1.3.4.1] Hover Audio Clips - Friendly Base")]
        public List<StarbaseAudioClip> HoverFriendlyBaseAudioClips;
    
        [Header("[1.3.4.2] Hover Audio Clips - Enemy Base")]
        public List<StarbaseAudioClip> HoverEnemyBaseAudioClips;

        [Header("[1.3.4.4] Selection Audio Clips - Units")]
        public List<StarbaseAudioClip> UnitSelectionAudioClips;
    
        [Header("[1.3.4.5] Deselection Audio Clips - Units")]
        public List<StarbaseAudioClip> UnitDeselectionAudioClips;

        [Header("[1.3.4.6] Command Audio Clips - Move Command")]
        public List<StarbaseAudioClip> MoveCommandAudioClips;
    
        [Header("[1.3.4.7] Command Audio Clips - Attack Command")]
        public List<StarbaseAudioClip> AttackCommandAudioClips;

        [Header("[1.3.4.8] Base Capture Audio Clips - Captured By Player")]
        public List<StarbaseAudioClip> BaseCapturedByPlayerAudioClips;
        
        [Header("[1.3.4.9] Player Upgrades a base")]
        public List<StarbaseAudioClip> BaseUpgradedByPlayerAudioClips;
        
        [Header("[1.3.4.12] Enemy Upgrades a base")]
        public List<StarbaseAudioClip> BaseUpgradedByEnemyAudioClips;
        
    
        [Header("[1.3.4.10] Base Capture Audio Clips - Captured By Enemy From Player")]
        public List<StarbaseAudioClip> BaseCapturedByEnemyFromPlayerAudioClips;
    
        [Header("[1.3.4.11] Base Capture Audio Clips - Captured By Enemy From Neutral Or Enemy")]
        public List<StarbaseAudioClip> BaseCapturedByEnemyFromNeutralOrEnemyAudioClips;

        [Header("[1.3.4.13] Unit Launch Audio Clips")]
        public List<StarbaseAudioClip> UnitLaunchAudioClips;
        
        [Header("[1.3.4.14] Base Unit Limit Audio Clips - At Maximum")]
        public List<StarbaseAudioClip> BaseAtMaxUnitsAudioClips;
    
        [Header("[1.3.4.15] Base Unit Limit Audio Clips - Exceeded Maximum")]
        public List<StarbaseAudioClip> BaseExceedsMaxUnitsAudioClips;
    }


    
    
    [Serializable]
    private struct StarbaseAudioClip
    {
        public AudioClip Clip;
        [Range(0f,1f)]
        public float Volume;
    }
#endregion

}
