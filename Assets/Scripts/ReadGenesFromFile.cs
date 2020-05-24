using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ReadGenesFromFile : MonoBehaviour
{
    public TextAsset genesFiles = null;
    private int index = 0;
    private int genesCount = 0;
    private SimulationManager simulationManager;
    private MeshRenderer screen = null;
    private float[][] allRules;
    private float[] testRule;

    // Start is called before the first frame update
    void Start()
    {
        simulationManager = GetComponent<SimulationManager>();
        screen = simulationManager.firstScreen();
        ReadGenes();
        SetCurrentIndexRule();
    }

    void ReadGenes()
    {
        string text = genesFiles.text;
        string[] splittedText = text.Split('\n');
        allRules = new float[splittedText.Length - 1][];
        foreach (var gene in splittedText)
        {
            if (string.IsNullOrEmpty(gene)) continue;
            allRules[genesCount] = new float[SimulationManager.RULE_SIZE];
            int counter = 0;
            foreach (var bit in gene)
            {
                if (char.IsDigit(bit))
                {
                    allRules[genesCount][counter] = bit - '0';
                    counter++;
                }
            }
            genesCount++;
        }
        testRule = allRules[0];
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            index = (index + genesCount - 1) % genesCount;
            SetCurrentIndexRule();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            index = (index + 1) % genesCount;
            SetCurrentIndexRule();
        }
    }

    void SetCurrentIndexRule()
    {
        CustomRenderTexture texture = TextureProcessor.GetTextureFromObject(screen);
        texture.material.SetFloatArray("_rule", allRules[index]);
        texture.initializationTexture = TextureProcessor.PaintRandomTexture(texture.initializationTexture);
        texture.Initialize();
    }
}
