﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WKBPInstance : MonoBehaviour
{
    [SerializeField] private Text displayText = default;
    [SerializeField] private Text idText = default;
    [SerializeField] private Text inschrijfnummerText = default;
    [SerializeField] private Text datum_in_werkingText = default;
    [SerializeField] private Text documentUrlText = default;
    [SerializeField] private Button documentUrlButton = default;
    private string url = "";

    /// <summary>
    /// Adds all data to the WKBP Instance Object
    /// </summary>
    /// <param name="result"></param>
    public void Initialize(WKBP.Result result)
    {
        // displays all the wkbp instances
        displayText.text = result._display;
        idText.text = result.beperking.id;
        inschrijfnummerText.text = result.beperking.inschrijfnummer;
        datum_in_werkingText.text = result.beperking.datum_in_werking;
        documentUrlText.text = result.beperking.documenten.href;
        url = result.beperking.documenten.href;
        documentUrlButton.name = url;
    }

    public void OpenLink()
    {
        // old display URL
        Application.OpenURL(url);
    }
}