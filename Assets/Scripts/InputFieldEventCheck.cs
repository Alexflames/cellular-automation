using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;

public class InputFieldEventCheck : MonoBehaviour {

    public TMP_InputField InputFieldComponent;

    private TMP_Text m_TextComponent;
    private SimulationManager simulationManager;

    void Awake()
    {
        m_TextComponent = GetComponent<TMP_Text>();
        
        // Events triggered when Input Field is selected.
        // InputFieldComponent.onSelect.AddListener(OnSelect);
        // InputFieldComponent.onDeselect.AddListener(OnDeselect);

        // Event triggered when Input Field text is changed.
        // InputFieldComponent.onValueChanged.AddListener(OnValueChange);

        // Event triggered when pressing Enter or Return.
        // InputFieldComponent.onSubmit.AddListener(OnSubmit);

        // Event triggered when text is no longer being edited.
        // This occurs OnDeselect, OnSubmit
        InputFieldComponent.onEndEdit.AddListener(OnEndEdit);

        // Events triggered when text is selected and unselected.
        //InputFieldComponent.onTextSelection.AddListener(OnTextSelection);
        //InputFieldComponent.onEndTextSelection.AddListener(OnEndTextSelection);

        //InputFieldComponent.onValidateInput = OnValidateInput;
        simulationManager = GameObject.FindGameObjectWithTag("GameController")
            .GetComponent<simulationManager>();
    }


    void OnDestroy()
    {
        InputFieldComponent.onSelect.RemoveAllListeners();
        InputFieldComponent.onDeselect.RemoveAllListeners();

        InputFieldComponent.onValueChanged.RemoveAllListeners();
        InputFieldComponent.onSubmit.RemoveAllListeners();
        InputFieldComponent.onEndEdit.RemoveAllListeners();

        InputFieldComponent.onTextSelection.RemoveAllListeners();
        InputFieldComponent.onEndTextSelection.RemoveAllListeners();
    }


    void OnValueChange(string text)
    {
        Debug.Log("OnValueChange event received. New text is [" + text + "].");
        //Debug.Log(InputFieldComponent.selectionAnchorPosition + "  " + InputFieldComponent.selectionFocusPosition);
        m_TextComponent.text = string.Empty;

        for (int i = 0; i < text.Length; i++)
            m_TextComponent.text += (int)text[i] + "-";
    }

    void OnTextSelection(string text, int start, int end)
    {
        Debug.Log("Text has been selected. Selection: [" + start + "," + end + "].");
    }

    void OnEndTextSelection(string text, int start, int end)
    {
        Debug.Log("OnEndTextSelection event received.");
    }

    void OnEndEdit(string text)
    {
        //simulationManager.ChangeUpdatePeriod(text);
        //Debug.Log("OnEndEdit event received. text = [" + text + "].");
        //Debug.Log(InputFieldComponent.selectionAnchorPosition + "  " + InputFieldComponent.selectionFocusPosition);
    }

    void OnSubmit(string text)
    {
        m_TextComponent.text = "OnSumbit Event - [" + text + "]";
        //Debug.Log("OnSubmit event received.");
        //Debug.Log(InputFieldComponent.selectionAnchorPosition + "  " + InputFieldComponent.selectionFocusPosition);
    }

    public void OnSelect(string text)
    {
        Debug.Log("Input Field has been Selected.\nThe text is: [" + InputFieldComponent.text + "] with length of [" + InputFieldComponent.text.Length + "].");
        //Debug.Log(InputField.caretPosition + "  " + InputField.selectionFocusPosition);
    }

    void OnDeselect(string text)
    {
        Debug.Log("Input Field has been Deselected.\nThe text is: [" + InputFieldComponent.text + "] with length of [" + InputFieldComponent.text.Length + "].");
    }

    char OnValidateInput(string text, int charIndex, char addedChar)
    {
        Debug.Log(text + "  " + addedChar);
        return addedChar;
    }
}
