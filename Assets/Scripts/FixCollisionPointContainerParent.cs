using System.Collections;
using System.Collections.Generic;
using Tilia.Interactions.Interactables.Interactables;
using UnityEngine;

public class FixCollisionPointContainerParent : MonoBehaviour
{
    private InteractableFacade _interactableFacade;
    public void FixCollisionPointContainer(GameObject collisionPointContainer)
    {
        if (_interactableFacade == null)
        {
            _interactableFacade = GetComponentInParent<InteractableFacade>();
        }
        
        collisionPointContainer.transform.SetParent(_interactableFacade.transform);
    }
}
