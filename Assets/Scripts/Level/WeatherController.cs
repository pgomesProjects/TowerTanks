using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum WeatherConditions { NORMAL, RAIN }

[System.Serializable]
public class Weather
{
    public Color backgroundColor;
    public Color globalLightingColor;
    public float globalLightingIntensity = 1;
    public Color tankLightColor;
    public float tankLightIntensity;
}

public class WeatherController : MonoBehaviour
{
    [Header("Weather Settings:")]
    [SerializeField, Tooltip("The different settings for the different weather types.")] private Weather[] weatherSettings;
    [SerializeField, Tooltip("The animation curve for the weather transition.")] private AnimationCurve weatherAnimationCurve;
    [Space(10)]

    [Header("Light Objects")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Light2D tankLight;

    public void ChangeWeather(WeatherConditions newWeather, float duration)
    {
        StartCoroutine(WeatherTransition(newWeather, duration));
    }

    private IEnumerator WeatherTransition(WeatherConditions newWeather, float duration)
    {
        float elapsedTime = 0f;

        Color startingBackgroundColor = Camera.main.backgroundColor;
        Color startingGlobalLightColor = globalLight.color;
        float startingGlobalLightIntensity = globalLight.intensity;
        Color startingTankLightColor = tankLight.color;
        float startingTankLightIntensity = tankLight.intensity;

        while (elapsedTime < duration)
        {
            //Animation curve to make the transition feel smoother
            float t = weatherAnimationCurve.Evaluate(elapsedTime / duration);

            Camera.main.backgroundColor = Color.Lerp(startingBackgroundColor, weatherSettings[(int)newWeather].backgroundColor, t);
            globalLight.color = Color.Lerp(startingGlobalLightColor, weatherSettings[(int)newWeather].globalLightingColor, t);
            globalLight.intensity = Mathf.Lerp(startingGlobalLightIntensity, weatherSettings[(int)newWeather].globalLightingIntensity, t);
            tankLight.color = Color.Lerp(startingTankLightColor, weatherSettings[(int)newWeather].tankLightColor, t);
            tankLight.intensity = Mathf.Lerp(startingTankLightIntensity, weatherSettings[(int)newWeather].tankLightIntensity, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Camera.main.backgroundColor = weatherSettings[(int)newWeather].backgroundColor;
        globalLight.color = weatherSettings[(int)newWeather].globalLightingColor;
        globalLight.intensity = weatherSettings[(int)newWeather].globalLightingIntensity;
        tankLight.color = weatherSettings[(int)newWeather].tankLightColor;
        tankLight.intensity = weatherSettings[(int)newWeather].tankLightIntensity;

        LevelManager.Instance.currentWeatherConditions = newWeather;
    }
}
