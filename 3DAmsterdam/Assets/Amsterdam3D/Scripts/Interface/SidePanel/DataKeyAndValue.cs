﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DataKeyAndValue : MonoBehaviour
{
    [SerializeField]
    private Text keyText;

    [SerializeField]
    private Text valueText;
    
    public void SetTexts(string key, string value)
    {
        keyText.text = key;
        valueText.text = value;
    }
}