using System;
using TMPro;
using UnityEngine;

public class FloatSettingsButtonChanger : MonoBehaviour
{
    [SerializeField] private TMP_InputField textField;
    private float value;
    private Action<float> _onChangeValue;

    public void Populate(Action<float> onValueChanged, float startValue)
    {
        value = startValue;
        _onChangeValue = onValueChanged;
        textField.text = value.ToString();
        _onChangeValue += (_) => { GlobalAudioManager.Instance.PlayButtonPressClip(); };
    }

    public void AddValue(float val)
    {
        value += val;
        textField.text = value.ToString();
        if (_onChangeValue != null) _onChangeValue.Invoke(value);
    }

    public void SubtractValue(float val)
    {
        value -= val;
        textField.text = value.ToString();
        if (_onChangeValue != null) _onChangeValue.Invoke(value);
    }

    
}