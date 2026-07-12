using UnityEngine;
using TMPro;

public class DayNightCycle : MonoBehaviour
{
    [Header("Components")]
    public Light globalLight;
    public TextMeshProUGUI uiText;

    [Header("Duration Settings")]
    private float dayDuration = 165f;
    private float nightDuration = 75f;
    public float transitionTime = 10f;

    [Header("Light Colors")]
    public Color dayColor = Color.white;
    public Color nightColor = new Color(0.12f, 0.15f, 0.28f);

    [Header("Light Intensity")]
    public float dayIntensity = 1f;
    public float nightIntensity = 0.2f;

    [Header("Test Runtime")]
    public bool testSkipToDay;
    public bool testSkipToNight;

    private float totalCycleTime;
    private float currentTime;

    void Start()
    {
        totalCycleTime = dayDuration + nightDuration;
        currentTime = 0f;
    }

    void Update()
    {
        if (testSkipToDay)
        {
            currentTime = 0f;
            testSkipToDay = false;
        }

        if (testSkipToNight)
        {
            currentTime = dayDuration;
            testSkipToNight = false;
        }

        currentTime += Time.deltaTime;
        if (currentTime >= totalCycleTime)
        {
            currentTime = 0f;
        }

        UpdateLightAndUI();
    }

    void UpdateLightAndUI()
    {
        bool isDay = currentTime < dayDuration;
        float targetIntensity;
        Color targetColor;
        float timeLeft;

        if (isDay)
        {
            timeLeft = dayDuration - currentTime;

            if (timeLeft < transitionTime)
            {
                float t = (transitionTime - timeLeft) / transitionTime;
                targetIntensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
                targetColor = Color.Lerp(dayColor, nightColor, t);
            }
            else
            {
                targetIntensity = dayIntensity;
                targetColor = dayColor;
            }
        }
        else
        {
            float nightElapsed = currentTime - dayDuration;
            timeLeft = nightDuration - nightElapsed;

            if (timeLeft < transitionTime)
            {
                float t = (transitionTime - timeLeft) / transitionTime;
                targetIntensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
                targetColor = Color.Lerp(nightColor, dayColor, t);
            }
            else
            {
                targetIntensity = nightIntensity;
                targetColor = nightColor;
            }
        }

        if (globalLight != null)
        {
            globalLight.intensity = targetIntensity;
            globalLight.color = targetColor;
        }

        if (uiText != null)
        {
            string stateName = isDay ? "BAN NGÀY" : "BAN ĐÊM";
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);

            uiText.text = $"{stateName}\n{minutes:00}:{seconds:00}";
        }
    }
}