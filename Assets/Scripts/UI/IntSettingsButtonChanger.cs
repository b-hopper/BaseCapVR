using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntSettingsButtonChanger : MonoBehaviour
{
    [SerializeField] private TMP_InputField textField;
    private int value;

    private Action<int> _onChangeValue;

    public void Populate(Action<int> onValueChanged, int startValue)
    {
        value = startValue;
        _onChangeValue = onValueChanged;
        textField.text = value.ToString();
        _onChangeValue += (_) => { GlobalAudioManager.Instance.PlayButtonPressClip(); };
    }

    public void AddValue(int val)
    {
        value += val;
        textField.text = value.ToString();
        if (_onChangeValue != null) _onChangeValue.Invoke(value);
    }

    public void SubtractValue(int val)
    {
        value -= val;
        textField.text = value.ToString();
        if (_onChangeValue != null) _onChangeValue.Invoke(value);
    }
}