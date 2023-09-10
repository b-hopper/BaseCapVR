using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class UIInteractorFeedback : MonoBehaviour
{
    private XRRayInteractor interactor;
    
    private void Start()
    {
        interactor = GetComponent<XRRayInteractor>();
        
        UiEvents.ShowUi.AddListener((show) => gameObject.SetActive(show));
        
        //GameStateManager.GameOverEvent.AddListener(OnGameOver);
        //GameStateManager.OnGameStart.Add(OnGameStart);
    }

    bool _wasHovering = false;
    private void Update()
    {
        if (interactor.TryGetUIModel(out var model))
        {
            if (model.currentRaycast.gameObject == null) return;
            
            var button = model.currentRaycast.gameObject.GetComponentInParent<Button>(); // todo get rid of this GetComponentInParent every frame
            if (button != null)
            {
                if (!_wasHovering)
                {
                    _wasHovering = true;
                    
                    //HapticsManager.Instance.PlayHaptics(false, 0);
                    
                    // Fire haptics here - There is currently an issue where VRTK requires a Tracked Alias to be assigned
                    // in order to fire haptics. The tracked alias is not assigned until the game starts and players spawned
                    // (on the UserPrefab root object)
                    // so, we need to find a way to fire haptics without a tracked alias. Until then, no UI haptics.

                    GlobalAudioManager.Instance.PlayButtonHoverClip();
                }
            }
            else if (_wasHovering)
            {
                _wasHovering = false;
            }
        }
    }
}