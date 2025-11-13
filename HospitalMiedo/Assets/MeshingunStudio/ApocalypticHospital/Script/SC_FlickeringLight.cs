using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.0f;
    public float flickerSpeed = 0.5f;
    public float transitionSpeed = 5.0f; // Speed of the intensity transitions.
    public float initialDelay = 2.0f;    // Delay before the flickering starts.

    private Light lightComponent;
    private float timeElapsed;
    private float targetIntensity;

    private void Start()
    {
        lightComponent = GetComponent<Light>();
        timeElapsed = 0f;
        targetIntensity = minIntensity; // Start with the minimum intensity.

        // Apply the initial delay.
        Invoke("StartFlickering", initialDelay);
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        // Check if it's time to change intensity.
        if (timeElapsed >= flickerSpeed)
        {
            // Toggle between min and max intensity.
            if (Mathf.Approximately(targetIntensity, minIntensity))
            {
                targetIntensity = maxIntensity;
            }
            else
            {
                targetIntensity = minIntensity;
            }

            timeElapsed = 0f;
        }

        // Smoothly transition the current intensity towards the target intensity.
        lightComponent.intensity = Mathf.Lerp(lightComponent.intensity, targetIntensity, Time.deltaTime * transitionSpeed);
    }

    private void StartFlickering()
    {
        // Start the flickering after the initial delay.
        enabled = true;
    }
}