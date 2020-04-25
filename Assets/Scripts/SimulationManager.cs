using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [Header("Scene settings")]
    [SerializeField] private Material cellularAutomationMaterial = null;
    [SerializeField] private Material screenMaterialPrefab = null;

    [Header("Simulation settings"),
     SerializeField] private float updatePeriod = 0.05f;
    [SerializeField] private int screenSizeInPixels = 128;
    [SerializeField] private int virtualScreensInSimulation = 128;
    [SerializeField] private int screensInSimulation = 32;

    private float timeToEvolutionPassed = 0f;
    [Header("Evolution"),
     SerializeField] private float timeToEvolution = 3f;
    [SerializeField] private float mutationPercent = 7;
    private int evolutionStep = 0;

    public MeshRenderer genofondScreen = null;
    public Pattern pattern;

    [Header("Performance-based fields"),
     SerializeField] private int fitnessCalculationsNeeded = 5;
    [SerializeField] private int fitnessCalcScreensPerFrame = 32;

    [HideInInspector] public bool simulationPaused = false;

    [System.Serializable]
    public struct Pattern
    {
        public short[] pattern;
        public int patternSizeX;
        public int patternSizeY;
        public int patternErrors;

        public Pattern(int patternSizeX, int patternSizeY, int patternErrors, short[] pattern = null)
        {
            this.patternSizeX = patternSizeX;
            this.patternSizeY = patternSizeY;
            this.patternErrors = patternErrors;
            this.pattern = pattern;
        }
    };

    private const short RULE_SIZE = 512;

    void Start()
    {
        // Virtual screens
        for (int i = 0; i < virtualScreensInSimulation; i++)
        {
            allRules.Add(InititalizeRandomRule());
        }

        // Visual screens
        for (int i = 0; i < ScreensCount(); i++)
        {
            AddScreenToSimulation(transform.GetChild(i).GetComponent<MeshRenderer>(), i);
        }

        screenTex2D = new Texture2D[ScreensCount()];
        for (int i = 0; i < ScreensCount(); i++)
        {
            screenTex2D[i] = TextureProcessor.CreateTexture2DFromObject(screens[i]);
        }

        // Pattern & evolution
        maxScreenFitness = new float[ScreensCount()];

        pattern = new Pattern(3, 3, 1, new short[9] { 1, 1, 1, 0, 0, 0, 1, 1, 1 }); // Save/Load patterns?
    }

    private const byte ruleInitBitOptimizationBy = 16;
    private float[] InititalizeRandomRule()
    {
        var rule = new float[RULE_SIZE];
        var cycleLength = RULE_SIZE / ruleInitBitOptimizationBy;
        for (short i = 0; i < cycleLength; i++)
        {
            var generatedRandom = Random.Range(0, 1 << ruleInitBitOptimizationBy);
            for (byte j = 0; j < ruleInitBitOptimizationBy; j++)
            {
                rule[i * ruleInitBitOptimizationBy + j] = generatedRandom % 2;
                generatedRandom = generatedRandom >>= 1;
            }
        }
        return rule;
    }

    byte screenInitBitOptimisation = 16;
    public Color32[] InitializeVirtualScreen()
    {
        var screenSize = screenSizeInPixels * screenSizeInPixels;
        var screen = new Color32[screenSize];
        var cycleLength = screenSize / ruleInitBitOptimizationBy;
        for (short i = 0; i < cycleLength; i++)
        {
            var generatedRandom = Random.Range(0, 1 << ruleInitBitOptimizationBy);
            for (byte j = 0; j < ruleInitBitOptimizationBy; j++)
            {
                screen[i * ruleInitBitOptimizationBy + j] = new Color32(255, 255, 255, (byte)(255 * (generatedRandom % 2)));
                generatedRandom = generatedRandom >>= 1;
            }
        }
        return screen;
    }

    public void AddScreenToSimulation(MeshRenderer screen, int screenInd)
    {
        screens.Add(screen);
        InitializeScreen(screen, screenInd);
    }

    private void InitializeScreen(MeshRenderer screen, int screenInd)
    {
        var customRenderTexture = new CustomRenderTexture(screenSizeInPixels, screenSizeInPixels);
        customRenderTexture.initializationTexture = TextureProcessor.CreateRandomTexture(screenSizeInPixels, screenSizeInPixels);
        customRenderTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
        customRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        customRenderTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
        customRenderTexture.doubleBuffered = true;
        customRenderTexture.wrapMode = TextureWrapMode.Repeat;
        customRenderTexture.material = new Material(cellularAutomationMaterial);
        
        customRenderTexture.material.SetFloatArray("_rule", allRules[screenInd]);
        customRenderTexture.Initialize();

        var material = new Material(screenMaterialPrefab);
        material.SetTexture("_MainTex", customRenderTexture);
        screen.material = material;
        customRenderTextures.Add(customRenderTexture);
    }

    void FixedUpdate()
    {
        // Camera movement

        updateFramesPassed += Time.deltaTime;
        if (updateFramesPassed >= updatePeriod)
        {
            foreach(var texture in customRenderTextures)
            {
                texture.Update();
            }
            updateFramesPassed = 0;
        }

        if (!simulationPaused && timeToEvolution > 0.5f)
        {
            timeToEvolutionPassed += Time.fixedDeltaTime;
            if (timeToEvolutionPassed >= timeToEvolution && fitnessRecalculated)
            {
                if (fitnessCalculations < fitnessCalculationsNeeded)
                {
                    fitnessCalculations++;
                    StartCoroutine(FullRecalculateFitness());
                }
                else if (fitnessCalculations == fitnessCalculationsNeeded)
                {
                    evolutionStep++;
                    Evolve();
                    timeToEvolutionPassed = 0;
                    fitnessCalculations = 0;
                }
            }
        }
    }

    private void UpdateCA()
    {

    }

    #region evolution-algorithm

    private Color32[] texturePixels;
    private Texture2D[] screenTex2D = null;

    // Optimization for fitness function calculation. We apply XOR on pattern and texture lines-sum
    enum LineDir
    {
        vertical,
        horizontal
    }

    private float CalculateFitness(Texture2D texture2D, Pattern pattern)
    {
        texturePixels = texture2D.GetPixels32();
        return CalculateFitness(texturePixels, texture2D.width, texture2D.height, pattern);
    }

    private float CalculateFitness(Color32[] texturePixels, int texW, int texH, Pattern pattern)
    {
        float fitness = 0;

        int textureWidth = texW; int textureHeight = texH;

        var patternHeight = pattern.patternSizeY;
        var patternWidth = pattern.patternSizeX;
        var patternErrors = pattern.patternErrors;
        var patternRule = pattern.pattern;
        int currentErrors = 0;

        //LineDir lineDir;
        //lineDir = patternHeight > patternWidth ? LineDir.vertical : LineDir.horizontal;

        //int[] patternLines = new int[patternHeight > patternWidth ? patternWidth : patternHeight];
        //Queue<int> textureLines = new Queue<int>();
        //if (lineDir == LineDir.horizontal)
        //{
        //    for (int i = 0; i < patternHeight; i++)
        //    {
        //        int textureLine = 0;
        //        for (int j = 0; j < patternWidth; j++)
        //        {
        //            patternLines[i] += patternRule[i * patternWidth + j] << j;
        //            textureLine += (texturePixels[i + patternWidth * j].a == 255 ? 1 : 0) << j;
        //        }
        //        textureLines.Enqueue(textureLine);
        //    }
        //}
        //else
        //{
        //    for (int i = 0; i < patternWidth; i++)
        //    {
        //        int textureLine = 0;
        //        for (int j = 0; j < patternHeight; j++)
        //        {
        //            patternLines[i] += patternRule[i * patternHeight + j] << j;
        //            textureLine += (texturePixels[i + patternHeight * j].a == 255 ? 1 : 0) << j;
        //        }
        //        textureLines.Enqueue(textureLine);
        //    }
        //}

        //if (lineDir == LineDir.vertical)
        //{
        //    for (int i = 0; i < textureHeight; i++)
        //    {
        //        currentErrors = 0;
        //        for (int j = 0; j < textureWidth; j++)
        //        {
        //            var textureLine = textureLines.Dequeue();
        //            var errorBits = patternLines[j] ^ textureLine;
        //            while (errorBits != 0)
        //            {
        //                currentErrors += errorBits % 2 != 0 ? 1 : 0;
        //                currentErrors <<= 1;
        //            }

        //            // если j больше-равно паттерна, начать этап поощрения
        //            // хранить очередь ошибок, окно ошибок, чтобы можно было вычислять поощрения для каждого пикселя

        //            // или сначала подсчитать первое окно?

        //            var newLine = 0;
        //            for (int pi = 0; pi < patternHeight; pi++)
        //            {
        //                // посчитать новую линию
        //            }
        //            textureLines.Enqueue(newLine);
        //        }
                

                
        //    }
        //}
        //else
        //{

        //}

        for (int i = 0; i < textureHeight; i++)
        {
            for (int j = 0; j < textureWidth; j++)
            {
                int cornerPixel = j + i * textureWidth;
                currentErrors = 0;
                for (int ir = 0; ir < patternHeight; ir++)
                {
                    for (int jr = 0; jr < patternWidth; jr++)
                    {
                        // Color32 = (255, 255, 255, 255/0)
                        // I convert it to (1, 1, 1, 1/0)
                        // And compare with corresponding cell in fitness rule
                        int pixelIndex = (cornerPixel + ir + jr * textureHeight) % (textureWidth * textureHeight);
                        currentErrors += (texturePixels[pixelIndex].a == 255 ? 1 : 0)
                            == patternRule[jr + ir * patternWidth] ? 1 : 0;
                    }
                    if (currentErrors > patternErrors) break;
                }

                fitness += currentErrors <= patternErrors ? (patternErrors - currentErrors) * 1f / patternErrors : 0;
            }
        }

        //for (int i = 0; i < textureHeight; i++)
        //{
        //    for (int j = 0; j < textureWidth; j++)
        //    {
        //        int cornerPixel = j + i * textureWidth;
        //        currentErrors = 0;
        //        for (int ir = 0; ir < patternHeight; ir++)
        //        {
        //            for (int jr = 0; jr < patternWidth; jr++)
        //            {
        //                // Color32 = (255, 255, 255, 255/0)
        //                // I convert it to (1, 1, 1, 1/0)
        //                // And compare with corresponding cell in fitness rule
        //                int pixelIndex = (cornerPixel + ir + jr * textureHeight) % (textureWidth * textureHeight);
        //                currentErrors += (texturePixels[pixelIndex].a == 255 ? 1 : 0)
        //                    == patternRule[jr + ir * patternWidth] ? 1 : 0;
        //            }
        //            if (currentErrors > patternErrors) break;
        //        }

        //        fitness += currentErrors <= patternErrors ? (patternErrors - currentErrors) * 1f / patternErrors : 0;
        //    }
        //}
        
        return fitness * 100 / (textureWidth * textureHeight);
        //return 1f - (currentErrors / (patternHeight * patternWidth)) * 1f / (textureWidth * textureHeight) ;
    }

    bool fitnessRecalculated = true;
    private IEnumerator FullRecalculateFitness()
    {
        fitnessRecalculated = false;

        for (int i = 0; i < ScreensCount(); i++)
        {
            TextureProcessor.GetTexture2DFromObject(screens[i], ref screenTex2D[i]);
            var fitness = CalculateFitness(screenTex2D[i], pattern);
            maxScreenFitness[i] = Mathf.Max(maxScreenFitness[i], fitness);
            if ((i + 1) % fitnessCalcScreensPerFrame == 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        fitnessRecalculated = true;
        yield return null;
    }

    private void Evolve()
    {
        List<KeyValuePair<int, float>> indexFitness = new List<KeyValuePair<int, float>>();
        for (int i = 0; i < ScreensCount(); i++)
        {
            indexFitness.Add(new KeyValuePair<int, float>(i, maxScreenFitness[i]));
        }


        indexFitness.Sort((x, y) => x.Value.CompareTo(y.Value));
        indexFitness.Reverse();

        // Fitness metrics
        float averageFitness = 0;
        foreach (var fitness in indexFitness)
        {
            averageFitness += fitness.Value;
        }
        averageFitness /= indexFitness.Count;

        // Leave only good genes. Требуется чётность
        indexFitness.RemoveRange(indexFitness.Count / 2, indexFitness.Count / 2);

        float averageGoodFitness = 0;
        foreach (var fitness in indexFitness)
        {
            averageGoodFitness += fitness.Value;
        }
        averageGoodFitness /= indexFitness.Count;
        
        Debug.Log($"<Evolution #{evolutionStep}> Maximum fitness: {indexFitness[0].Value.ToString("0.0000")}%.\n" +
            $"Average good fitness: {averageGoodFitness.ToString("0.0000")}%. Average fitness: {averageFitness.ToString("0.0000")}%");

        List<float[]> newRules = new List<float[]>();
        
        // >>>Crossbreeding<<<

        // Требуется кратность четырём
        for (int i = 0; i < screensInSimulation / 4; i++)
        {
            var parent1i = Random.Range(0, indexFitness.Count);
            var parent1Rule = allRules[indexFitness[parent1i].Key];
            newRules.Add(parent1Rule);
            indexFitness.RemoveAt(parent1i);

            var parent2i = Random.Range(0, indexFitness.Count);
            var parent2Rule = allRules[indexFitness[parent2i].Key];
            newRules.Add(parent2Rule);
            indexFitness.RemoveAt(parent2i);

            var crossSeparator = Random.Range(0, RULE_SIZE);
            var sonRule = new float[RULE_SIZE];
            var daughterRule = new float[RULE_SIZE];
            for (int j = 0; j < RULE_SIZE; j++)
            {
                sonRule[j] = j < crossSeparator ? parent1Rule[j] : parent2Rule[j];
                daughterRule[j] = j < crossSeparator ? parent2Rule[j] : parent1Rule[j];
            }
            newRules.Add(sonRule);
            newRules.Add(daughterRule);
        }

        // >>>Mutations<<<
        foreach (var rule in newRules)
        {
            if (Random.Range(0, 1f) <= mutationPercent / 100f)
            {
                var gene = Random.Range(0, RULE_SIZE);
                rule[gene] = Mathf.Abs(1 - rule[gene]);
            }
        }

        for (int i = 0; i < ScreensCount(); i++)
        {
            allRules[i] = newRules[i];
            TextureProcessor.GetTextureFromObject(screens[i]).material.SetFloatArray("_rule", newRules[i]);
        }

        RefreshAllScreens();
        UpdateGenofond();
        maxScreenFitness = new float[ScreensCount()];
    }
    
    private Color32[] genofond = null;
    private Texture2D genofondScreenTex = null;
    private void UpdateGenofond()
    {
        if (genofondScreenTex == null || genofond == null)
        {
            genofondScreenTex = new Texture2D(RULE_SIZE, ScreensCount());
        }

        genofond = new Color32[ScreensCount() * RULE_SIZE];

        for (int i = 0; i < allRules.Count; i++)
        {
            for (int j = 0; j < RULE_SIZE; j++)
            {
                byte ruleByte = 0;
                if (allRules[i][j] == 1) ruleByte = 255;
                else ruleByte = 0;
                genofond[j + i * RULE_SIZE] = new Color32(255, 255, 255, ruleByte);
            }
        }

        genofondScreen.material.mainTexture = genofondScreenTex;
        genofondScreenTex.SetPixels32(genofond);
        genofondScreenTex.Apply();
    }

    

    

    #endregion

    private int ScreensCount()
    {
        return Mathf.Min(transform.childCount, screensInSimulation);
    }

    public void RefreshAllScreens()
    {
        for (int i = 0; i < ScreensCount(); i++)
        {
            RefreshScreen(i);
        }
    }

    public void RefreshScreen(int index)
    {
        var renderTexture = TextureProcessor.GetTextureFromObject(screens[index]);
        RefreshScreen(renderTexture);
    }

    private void RefreshScreen(CustomRenderTexture renderTexture)
    {
        renderTexture.initializationTexture = TextureProcessor.PaintRandomTexture(renderTexture.initializationTexture);
        renderTexture.Initialize();
    }

    public MeshRenderer GetScreen(int index)
    {
        return screens[index];
    } 

    public int IndexOfScreen(GameObject screen)
    {
        try
        {
            return screens.IndexOf(screen.GetComponent<MeshRenderer>());
        }
        catch (System.IndexOutOfRangeException)
        {
            return -1;
        }
    }

    private int IndexOfPatternCell(GameObject screen)
    {
        try
        {
            return screens.IndexOf(screen.GetComponent<MeshRenderer>());
        }
        catch (System.IndexOutOfRangeException)
        {
            return -1;
        }
    }

    public void ChangeUpdatePeriod(float newUpdatePeriod)
    {
        if (simulationPaused)
        {
            updatePeriodSaved = newUpdatePeriod == 0 ? 999999999 : 1 / newUpdatePeriod;
        }
        else
        {
            updatePeriod = newUpdatePeriod == 0 ? 999999999 : 1 / newUpdatePeriod;
        }
        
    }

    public void ChangeEvolutionPeriod(float newEvolutionPeriod)
    {
        timeToEvolution = newEvolutionPeriod;
    }

    public void PauseResumeSimulation()
    {
        simulationPaused = !simulationPaused;
        if (simulationPaused)
        {
            updatePeriodSaved = updatePeriod;
            updatePeriod = 5000000000;
        }
        else
        {
            updatePeriod = updatePeriodSaved;
        }
    }

    private List<float[]> allRules = new List<float[]>();
    private List<Color32[]> virtualScreens = new List<Color32[]>();
    private List<MeshRenderer> screens = new List<MeshRenderer>();
    private List<CustomRenderTexture> customRenderTextures = new List<CustomRenderTexture>();

    private float updatePeriodSaved = 0;
    private float updateFramesPassed = 0;
    
    private int fitnessCalculations = 0;
    private float[] maxScreenFitness;
}
