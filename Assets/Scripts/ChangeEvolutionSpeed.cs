using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;

public class ChangeEvolutionSpeed : MonoBehaviour
{
    public TMP_InputField InputFieldComponent;

    private TMP_Text m_TextComponent;
    private SimulationManager simulationManager;

    void Awake()
    {
        m_TextComponent = GetComponent<TMP_Text>();

        InputFieldComponent.onEndEdit.AddListener(OnEndEdit);

        simulationManager = GameObject.FindGameObjectWithTag("GameController")
            .GetComponent<SimulationManager>();
    }

    void OnDestroy()
    {
        InputFieldComponent.onEndEdit.RemoveAllListeners();
    }

    string ChangeCommaToDotInString(string text)
    {
        var textToReturn = "";
        foreach (var chr in text)
        {
            textToReturn += chr == '.' ? ',' : chr;
        }
        return textToReturn;
    }

    void OnEndEdit(string text)
    {
        var textNormalized = ChangeCommaToDotInString(text);
        if (textNormalized != "" && float.TryParse(textNormalized, out float res))
        {
            simulationManager.ChangeEvolutionPeriod((float)Convert.ToDouble(textNormalized));
        }
    }
}
