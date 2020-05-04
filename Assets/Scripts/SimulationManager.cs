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
    public Pattern[] patterns;
    public Transform fitnessScreen;

    [Header("Performance-based fields"),
     SerializeField] private int fitnessCalculationsNeeded = 5;
    [SerializeField] private int fitnessCalcScreensPerFrame = 32;

    [HideInInspector] public bool simulationPaused = false;

    private const byte ruleInitBitOptimizationBy = 16;
    byte screenInitBitOptimisation = 16;

    [System.Serializable]
    public struct Pattern
    {
        public byte[] pattern;
        public byte patternSizeX;
        public byte patternSizeY;
        public byte patternErrors;

        public Pattern(byte patternSizeX, byte patternSizeY, byte patternErrors, byte[] pattern = null)
        {
            this.patternSizeX = patternSizeX;
            this.patternSizeY = patternSizeY;
            this.patternErrors = patternErrors;
            this.pattern = pattern;
        }
    };

    public struct FitnessRecord
    {
        public float recordID;
        public float maxFitness;
        public float averageGoodFitness;
        public float averageFitness;

        public FitnessRecord(int recordID, float maxFitness, float averageGoodFitness, float averageFitness)
        {
            this.maxFitness = maxFitness;
            this.averageFitness = averageFitness;
            this.averageGoodFitness = averageGoodFitness;
            this.recordID = recordID;
        }

        public void PrintRecord()
        {
            Debug.Log($"<Evolution #{recordID}> Maximum fitness: {maxFitness.ToString("0.0000")}.\n" +
            $"Average good fitness: {averageGoodFitness.ToString("0.0000")}. Average fitness: {averageFitness.ToString("0.0000")}");
        }
    }

    private const short RULE_SIZE = 512;

    void Start()
    {
        // Virtual screens
        for (int i = 0; i < virtualScreensInSimulation; i++)
        {
            allRules.Add(InititalizeRandomRule());
            var newScreen = new byte[screenSizeInPixels * screenSizeInPixels];
            virtualScreens.Add(InitializeVirtualScreen(newScreen));
            nextVirtualScreens.Add(new byte[screenSizeInPixels * screenSizeInPixels]);
        }
        screenSignals = new int[screenSizeInPixels * screenSizeInPixels];
        texturePix = new byte[screenSizeInPixels * screenSizeInPixels];

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
        maxScreenFitness = new float[virtualScreensInSimulation];

        patterns = new Pattern[] { new Pattern(4, 4, 1, 
            new byte[16] {
                1, 1, 1, 1,
                0, 0, 0, 0,
                1, 1, 1, 1,
                0, 0, 0, 0,
            })}; // Save/Load patterns?

        for (int i = 0; i < 3; i++)
        {
            fitnessLineRenderers[i] = fitnessScreen.GetChild(i).GetComponent<LineRenderer>();
        }
        
    }
    
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
    
    public Color32[] InitializeNonVirtualScreen()
    {
        var screenSize = screenSizeInPixels * screenSizeInPixels;
        var screen = new Color32[screenSize];
        var cycleLength = screenSize / screenInitBitOptimisation;
        for (short i = 0; i < cycleLength; i++)
        {
            var generatedRandom = Random.Range(0, 1 << screenInitBitOptimisation);
            for (byte j = 0; j < screenInitBitOptimisation; j++)
            {
                screen[i * screenInitBitOptimisation + j] = new Color32(255, 255, 255, (byte)(255 * (generatedRandom % 2)));
                generatedRandom = generatedRandom >>= 1;
            }
        }
        return screen;
    }

    public byte[] InitializeVirtualScreen(byte[] screen)
    {
        var screenSize = screenSizeInPixels * screenSizeInPixels;
        var cycleLength = screenSize / ruleInitBitOptimizationBy;
        for (short i = 0; i < cycleLength; i++)
        {
            var generatedRandom = Random.Range(0, 1 << ruleInitBitOptimizationBy);
            for (byte j = 0; j < ruleInitBitOptimizationBy; j++)
            {
                screen[i * ruleInitBitOptimizationBy + j] = (byte)(generatedRandom % 2);
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

    void Update()
    {
        // Camera movement

        updateFramesPassed += Time.deltaTime;
        if (updateFramesPassed >= updatePeriod)
        {
            foreach(var texture in customRenderTextures)
            {
                texture.Update();
            }
            for(int i = 0; i < virtualScreensInSimulation; i++)
            {
                var nextScreen = nextVirtualScreens[i];
                UpdateCA(virtualScreens[i], i);
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

    private const bool optimizedUpdateCA = true;
    private void UpdateCA(byte[] CAField, int ind)
    {
        // Оптимизированная версия работает с тройками чисел и побитовыми операциями
        // Предположительно работает только в случае 2 состояний автоматов
        if (optimizedUpdateCA)
        {
            var signal = CAField[screenSizeInPixels - 1] * 2 + CAField[0];
            // пройдемся по строке № n-1
            var size2D = screenSizeInPixels * screenSizeInPixels;
            var lastRow_i = size2D - screenSizeInPixels;
            for (short i = 1; i < screenSizeInPixels; i++)
            {
                signal = (signal << 1) % 8 + CAField[i];
                var im1 = i - 1;
                screenSignals[lastRow_i + im1]          = signal;       // строка -1
                screenSignals[im1]                      = signal << 3;  // строка 0
                screenSignals[screenSizeInPixels + im1] = signal << 6;  // строка 1
            }
            signal = (signal << 1) % 8 + CAField[lastRow_i];
            screenSignals[size2D - 1]                   = signal;       // строка -1, последний символ
            screenSignals[screenSizeInPixels - 1]       = signal << 3;  // строка 0, последний символ
            screenSignals[2 * screenSizeInPixels - 1]   = signal << 6;  // строка 1, последний символ

            // пройдемся по строкам № 0:n-2
            for (short i = 0; i < screenSizeInPixels - 2; i++)
            {
                var iPix = i * screenSizeInPixels;
                signal = CAField[iPix + (screenSizeInPixels)] * 2 + CAField[iPix];
                for (short j = 1; j < screenSizeInPixels; j++)
                {
                    signal = (signal << 1) % 8 + CAField[iPix + j];
                    var jm1 = j - 1;
                    screenSignals[iPix + jm1]                          += signal;
                    screenSignals[iPix + screenSizeInPixels + jm1]     += signal << 3;
                    screenSignals[iPix + 2 * screenSizeInPixels + jm1]  = signal << 6; // да-да, именно присваивание
                }
                signal = (signal << 1) % 8 + CAField[iPix];
                screenSignals[iPix + screenSizeInPixels - 1]           += signal;       // строка -1
                screenSignals[iPix + 2 * screenSizeInPixels - 1]       += signal << 3;  // строка 0
                screenSignals[iPix + 3 * screenSizeInPixels - 1]        = signal << 6;  // строка 1
            }

            // пройдемся по строке № n-2
            for (short i = 1; i < screenSizeInPixels; i++)
            {
                signal = (signal << 1) % 8 + CAField[i];
                var im1 = i - 1;
                screenSignals[lastRow_i - screenSizeInPixels + im1] += signal;      // строка -2
                screenSignals[lastRow_i + im1]                      += signal << 3; // строка -1
                screenSignals[im1]                                  += signal << 6; // строка  0
            }
            signal = (signal << 1) % 8 + CAField[lastRow_i - screenSizeInPixels];
            screenSignals[lastRow_i - 1]                            += signal;      // строка -2, последний символ
            screenSignals[size2D - 1]                               += signal << 3; // строка -1, последний символ
            screenSignals[screenSizeInPixels - 1]                   += signal << 6; // строка  0, последний символ

            for (short i = 0; i < size2D; i++)
            {
                CAField[i] = (byte)allRules[ind][screenSignals[i]];
            }
        }
        else
        {
            var nextCAField = nextVirtualScreens[ind];
            var screen2DSize = screenSizeInPixels * screenSizeInPixels;
            for (short i = 0; i < screenSizeInPixels; i++)
            {
                for (short j = 0; j < screenSizeInPixels; j++)
                {
                    // Смотрим пиксели в окрестности Мура
                    int signal = 0;
                    for (short k = 0; k < 3; k++)
                    {
                        for (short m = 0; m < 3; m++)
                        {
                            signal += CAField[
                                (screen2DSize + screenSizeInPixels * i + j
                                + screenSizeInPixels * (k - 1) + (m - 1)) % (screen2DSize)] << (k * 3 + m);
                        }
                    }
                    nextCAField[i * screenSizeInPixels + j] = (byte)allRules[ind][signal];
                }
            }

            for (short i = 0; i < screen2DSize; i++)
            {
                CAField[i] = nextCAField[i];
            }
        }
    }

    #region evolution-algorithm

    private Color32[] texturePixels;
    private Texture2D[] screenTex2D = null;

    // Optimization for fitness function calculation. We apply XOR on pattern and texture lines-sum
    enum LineDir { vertical, horizontal }

    private float CalculateFitness(Texture2D texture2D, Pattern[] patterns)
    {
        texturePixels = texture2D.GetPixels32();
        return CalculateFitness(texturePixels, texture2D.width, texture2D.height, patterns);
    }

    byte[] texturePix = null;
    private float CalculateFitness(Color32[] texturePixels, int texW, int texH, Pattern[] patterns)
    {
        var texSize = texW * texH;
        texturePix = new byte[texSize];
        for (int i = 0; i < texSize; i++)
        {
            texturePix[i] = texturePixels[i].a;
        }
        return CalculateFitness(texturePix, texW, texH, patterns);
    }

    private float CalculateFitness(byte[] texturePixels, int texW, int texH, Pattern[] patterns)
    {
        float fitness = 0;

        int textureWidth = texW; int textureHeight = texH;

        var patternHeight = patterns[0].patternSizeY;
        var patternWidth = patterns[0].patternSizeX;
        var patternErrors = patterns[0].patternErrors;
        var patternRule = patterns[0].pattern;
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

        for (short i = 0; i < textureHeight; i++)
        {
            for (short j = 0; j < textureWidth; j++)
            {
                int cornerPixel = j + i * textureWidth;
                currentErrors = 0;
                for (byte ir = 0; ir < patternHeight; ir++)
                {
                    for (byte jr = 0; jr < patternWidth; jr++)
                    {
                        // Color32 = (255, 255, 255, 255/0)
                        // I convert it to (1, 1, 1, 1/0)
                        // And compare with corresponding cell in fitness rule
                        int pixelIndex = (cornerPixel + ir * textureWidth + jr) % (textureWidth * textureHeight);
                        //jr or ir ?
                        currentErrors += texturePixels[pixelIndex] == patternRule[ir * patternWidth + jr] ? 0 : 1;
                    }
                    if (currentErrors > patternErrors) break;
                }

                fitness += currentErrors <= patternErrors ? (1 + patternErrors - currentErrors) * 1f / (patternErrors + 1) : 0;
            }
        }

        // Saved for texure, it has different indexing
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

        for (int i = 0; i < virtualScreensInSimulation; i++)
        {
            //TextureProcessor.GetTexture2DFromObject(screens[i], ref screenTex2D[i]);
            //var fitness = CalculateFitness(screenTex2D[i], pattern);
            var fitness = CalculateFitness(virtualScreens[i], screenSizeInPixels, screenSizeInPixels, patterns);
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
        for (int i = 0; i < virtualScreensInSimulation; i++)
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

        var newFitnessRecord = new FitnessRecord(evolutionStep, indexFitness[0].Value, averageGoodFitness, averageFitness);
        fitnessHistory.Add(newFitnessRecord);
        newFitnessRecord.PrintRecord();

        List<float[]> newRules = new List<float[]>();
        
        // >>>Crossbreeding<<<

        // Требуется кратность четырём
        for (int i = 0; i < virtualScreensInSimulation / 4; i++)
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

        for (int i = 0; i < virtualScreensInSimulation; i++)
        {
            allRules[i] = newRules[i];
        }

        for (int i = 0; i < ScreensCount(); i++)
        {
            TextureProcessor.GetTextureFromObject(screens[i]).material.SetFloatArray("_rule", newRules[i]);
        }

        RefreshAllScreens();
        UpdateGenofond();
        UpdateFitnessFigure();
        maxScreenFitness = new float[virtualScreensInSimulation];
    }
    
    private void UpdateGenofond()
    {
        if (genofondScreenTex == null || genofond == null)
        {
            genofondScreenTex = new Texture2D(RULE_SIZE, virtualScreensInSimulation);
        }

        genofond = new Color32[virtualScreensInSimulation * RULE_SIZE];

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
    
    private void UpdateFitnessFigure()
    {
        float sizeMult = 1f / (1 + fitnessHistory.Count / 10);
        foreach (var lineRend in fitnessLineRenderers) {
            lineRend.positionCount = fitnessHistory.Count;
            lineRend.transform.localScale = new Vector3(sizeMult, 1, 1);
        }
        
        var history = fitnessHistory[fitnessHistory.Count - 1];
        fitnessBestPoints.Add(new Vector3(history.recordID, 0, history.maxFitness));
        fitnessAverageGoodPoints.Add(new Vector3(history.recordID, 0, history.averageGoodFitness));
        fitnessAveragePoints.Add(new Vector3(history.recordID, 0, history.averageFitness));
        
        fitnessLineRenderers[0].SetPositions(fitnessBestPoints.ToArray());
        fitnessLineRenderers[1].SetPositions(fitnessAverageGoodPoints.ToArray());
        fitnessLineRenderers[2].SetPositions(fitnessAveragePoints.ToArray());
    }

    #endregion

    private int ScreensCount() => Mathf.Min(transform.childCount, screensInSimulation);

    public void RefreshAllScreens()
    {
        for (int i = 0; i < ScreensCount(); i++)
        {
            RefreshScreen(i);
        }
        for (int i = 0; i < virtualScreensInSimulation; i++)
        {
            InitializeVirtualScreen(virtualScreens[i]);
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

    public MeshRenderer GetScreen(int index) => screens[index];

    public int IndexOfScreen(GameObject screen)
    {
        try { return screens.IndexOf(screen.GetComponent<MeshRenderer>()); }
        catch (System.IndexOutOfRangeException) { return -1; }
    }

    private int IndexOfPatternCell(GameObject screen)
    {
        try { return screens.IndexOf(screen.GetComponent<MeshRenderer>()); }
        catch (System.IndexOutOfRangeException) { return -1; }
    }

    public void ChangeUpdatePeriod(float newUpdatePeriod)
    {
        if (simulationPaused) updatePeriodSaved = newUpdatePeriod == 0 ? 999999999 : 1 / newUpdatePeriod;
        else updatePeriod = newUpdatePeriod == 0 ? 999999999 : 1 / newUpdatePeriod;
    }

    public void ChangeEvolutionPeriod(float newEvolutionPeriod) => timeToEvolution = newEvolutionPeriod;

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
    private List<byte[]> virtualScreens = new List<byte[]>();
    private List<byte[]> nextVirtualScreens = new List<byte[]>();
    private int[] screenSignals;
    private List<MeshRenderer> screens = new List<MeshRenderer>();
    private List<CustomRenderTexture> customRenderTextures = new List<CustomRenderTexture>();

    private float updatePeriodSaved = 0;
    private float updateFramesPassed = 0;
    
    private int fitnessCalculations = 0;
    private float[] maxScreenFitness;
    private List<FitnessRecord> fitnessHistory = new List<FitnessRecord>();

    List<Vector3> fitnessBestPoints = new List<Vector3>();
    List<Vector3> fitnessAverageGoodPoints = new List<Vector3>();
    List<Vector3> fitnessAveragePoints = new List<Vector3>();
    private Color32[] genofond = null;
    private Texture2D genofondScreenTex = null;
    private LineRenderer[] fitnessLineRenderers = new LineRenderer[3];
}
