using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is used to track the state of the hand controller input devices.
/// This decouples input from the object that is being interacted with, allowing for the same
/// input to be used for multiple objects, as well as holding down a button before colliding with an object,
/// but still being a valid interaction
/// </summary>
public class InputTracker : MonoBehaviour
{
    public bool isHoldingTrigger;
    public bool isHoldingUp;
    public bool isHoldingLeft;
    public bool isHoldingDown;
    public bool isHoldingRight;
    
    public void SetTrigger(bool value)
    {
        isHoldingTrigger = value;
    }
    
    public void SetUp(bool value)
    {
        isHoldingUp = value;
    }
    
    public void SetLeft(bool value)
    {
        isHoldingLeft = value;
    }
    
    public void SetDown(bool value)
    {
        isHoldingDown = value;
    }
    
    public void SetRight(bool value)
    {
        isHoldingRight = value;
    }
}
