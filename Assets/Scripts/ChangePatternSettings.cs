using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;

public class ChangePatternSettings : MonoBehaviour
{
    public InputField XInputFieldComponent;
    public InputField YInputFieldComponent;
    public InputField ErrorInputFieldComponent;

    [SerializeField]
    private Transform patternEditorObject = null;

    private SimulationManager simulationManager;

    void Awake()
    {
        XInputFieldComponent.onEndEdit.AddListener(OnEndEditX);
        YInputFieldComponent.onEndEdit.AddListener(OnEndEditY);
        ErrorInputFieldComponent.onEndEdit.AddListener(OnEndEditError);

        simulationManager = GameObject.FindGameObjectWithTag("GameController")
            .GetComponent<SimulationManager>();
    }

    void UpdatePatternEditor()
    {
        int sizeX = simulationManager.pattern.patternSizeX;
        int sizeY = simulationManager.pattern.patternSizeY;

        int MAGIC_CONSTANT = 20; // 20 screens maximum in row. Used in transform hierarchy

        int maxChildren = patternEditorObject.childCount;

        for (int i = 0; i < sizeY; i++)
        {
            for (int j = 0; j < sizeX; j++)
            {
                patternEditorObject.GetChild(j + i * MAGIC_CONSTANT).gameObject.SetActive(true);
            }
            for (int j = sizeX; j < MAGIC_CONSTANT; j++)
            {
                patternEditorObject.GetChild(j + i * MAGIC_CONSTANT).gameObject.SetActive(false);
            }
        }

        for (int i = sizeY; i < MAGIC_CONSTANT; i++)
        {
            for (int j = 0; j < MAGIC_CONSTANT; j++)
            {
                patternEditorObject.GetChild(j + i * MAGIC_CONSTANT).gameObject.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        XInputFieldComponent.onEndEdit.RemoveAllListeners();
        YInputFieldComponent.onEndEdit.RemoveAllListeners();
        ErrorInputFieldComponent.onEndEdit.RemoveAllListeners();
    }

    void OnEndEditX(string text)
    {
        if (text != "" && int.TryParse(text, out int res))
        {
            simulationManager.pattern.patternSizeX = (byte)Mathf.Min(res, 20);
            UpdatePatternEditor();
        }
    }

    void OnEndEditY(string text)
    {
        if (text != "" && int.TryParse(text, out int res))
        {
            simulationManager.pattern.patternSizeY = (byte)Mathf.Min(res, 20);
            UpdatePatternEditor();
        }
    }

    void OnEndEditError(string text)
    {
        if (text != "" && byte.TryParse(text, out byte res))
        {
            simulationManager.pattern.patternErrors = res;
        }
    }
}
