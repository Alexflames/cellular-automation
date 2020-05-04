using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PatternReadWrite
{
    public static SimulationManager.Pattern[] GetPatterns(TextAsset textAsset)
    {
        string text = textAsset.text;
        return GetPatterns(text);
    }

    public static SimulationManager.Pattern[] GetPatterns(string text)
    {
        string[] splittedText = text.Split('\n');
        var patternParams = splittedText[0].Split(' ');
        byte patternHeight = System.Convert.ToByte(patternParams[0]);
        byte patternWidth = System.Convert.ToByte(patternParams[1]);
        byte patternErrors = System.Convert.ToByte(patternParams[2]);
        Debug.Log(patternParams);
        var tags = splittedText[1].Split(' ');
        Debug.Log(tags);
        int patternsN = System.Convert.ToInt32(splittedText[2]);
        int patternsCount = 0;
        int stringInd = 3;

        SimulationManager.Pattern[] patterns = new SimulationManager.Pattern[patternsN];
        SimulationManager.Pattern patternToAdd = 
            new SimulationManager.Pattern(patternWidth, patternHeight, patternErrors, new byte[patternHeight * patternWidth]);
        var patternRow = 0;
        while (patternsCount < patternsN) {
            if (stringInd == splittedText.Length) return patterns;
            if (splittedText[stringInd] == "")
            {
                patterns[patternsCount] = patternToAdd;
                patternRow = 0;
                patternsCount++;
            }
            else
            {
                var splitString = splittedText[stringInd].Split(' ');
                for (int i = 0; i < splitString.Length; i++)
                {
                    patternToAdd.pattern[patternRow * patternWidth + i] = System.Convert.ToByte(splitString[i]);
                }
                patternRow++;
            }
            stringInd++;
        }
        return patterns;
    }
}
