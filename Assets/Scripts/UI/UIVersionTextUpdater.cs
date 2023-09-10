using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIVersionTextUpdater : MonoBehaviour
{
    [SerializeField] TMP_Text _text;
    void Start()
    {
        if (_text == null)
        {
            _text = GetComponent<TMP_Text>();
        }
        UpdateVersionText();
        Destroy(this);
    }

    void UpdateVersionText()
    {
        if (_text == null)
        {
            return;
        }
        _text.text = $"{Application.version}";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateVersionText();
    }
#endif
}
