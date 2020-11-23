﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeTrafficSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider speedSlider = default;
    [SerializeField] private Text speedText = default;

    [SerializeField] private Toggle startCarToggle = default;
    // Start is called before the first frame update
    void Start()
    {
        speedSlider.onValueChanged.AddListener(delegate { UpdateVehicleSpeed(); });
        speedSlider.value = TrafficSimulator.Instance.carSpeed;
        startCarToggle.onValueChanged.AddListener(delegate { StartCars(); });
    }

    /// <summary>
    /// Changes the vehicle speed through an UI element
    /// </summary>
    public void UpdateVehicleSpeed()
    {
        speedText.text = speedSlider.value.ToString();
        TrafficSimulator.Instance.carSpeed = Mathf.RoundToInt(speedSlider.value);
    }

    /// <summary>
    /// Start/Stops all cars through an UI element
    /// </summary>
    public void StartCars()
    {
        TrafficSimulator.Instance.StartSimulation(startCarToggle.isOn);
    }
}