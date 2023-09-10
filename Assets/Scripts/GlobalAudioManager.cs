using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GlobalAudioManager : MonoBehaviour
{
    public AudioSource _audioSource;
    
    public static GlobalAudioManager Instance;
    
    [Serializable] public struct ButtonAudioClips
    {
        public AudioClip hoverAudioClip;
        public AudioClip clickAudioClip;
    }
    
    [Serializable] public struct GameEventAudioClips
    {
        public AudioClip gameStartedAudioClip;
        public AudioClip gameEndedAudioClip;
    }

    public ButtonAudioClips _UIFeedbackAudioClips;
    public GameEventAudioClips _gameEventAudioClips;
    
    private void Start()
    {
        if (Instance != null)
        {
            Debug.LogError("GlobalAudioManager already exists");
            Destroy(this);
            return;
        }
        Instance = this;

        GameStateManager.OnGameStart.Add(PlayGameStartClip);
        GameStateManager.GameOverEvent.AddListener(PlayGameEndClip);
    }

    public void PlayAudio(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        _audioSource.volume = volume * GameSettingsManager.SFXVolume;
        _audioSource.pitch = pitch;
        _audioSource.PlayOneShot(clip);
    }
    
    public void PlayButtonHoverClip()
    {
        PlayAudio(_UIFeedbackAudioClips.hoverAudioClip);
    }
    
    public void PlayButtonPressClip()
    {
        PlayAudio(_UIFeedbackAudioClips.clickAudioClip);
    }
    
    private void PlayGameStartClip()
    {
        if (_gameEventAudioClips.gameStartedAudioClip != null)
        {
            PlayAudio(_gameEventAudioClips.gameStartedAudioClip);
        }
    }
    
    private void PlayGameEndClip()
    {
        if (_gameEventAudioClips.gameEndedAudioClip != null)
        {
            PlayAudio(_gameEventAudioClips.gameEndedAudioClip);
        }
    }
}
