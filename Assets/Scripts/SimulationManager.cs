using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [Header("Scene settings")]
    [SerializeField] private Material cellularAutomationMaterial = null;
    [SerializeField] private Material screenMaterialPrefab = null;
    [SerializeField] private MeshRenderer checkScreen = null;

    [Header("Simulation settings"),
     SerializeField] private float updatePeriod = 0.05f;
    [SerializeField] private int screenSizeInPixels = 128;
    [SerializeField] private int virtualScreensInSimulation = 128;
    [SerializeField] private int screensInSimulation = 32;
    [SerializeField] private bool updateCheckScreen = false;

    private float timeToEvolutionPassed = 0f;
    [Header("Evolution"),
     SerializeField] private float timeToEvolution = 3f;
    [SerializeField] private float mutationPercent = 7;
    private int evolutionStep = 0;
    [SerializeField] private TextAsset patternFile = null;

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
        allRules = new float[virtualScreensInSimulation][];
        for (int i = 0; i < virtualScreensInSimulation; i++)
        {
            allRules[i] = InititalizeRandomRule();
            var newScreen = new byte[screenSizeInPixels * screenSizeInPixels];
            virtualScreens.Add(InitializeVirtualScreen(newScreen));
            nextVirtualScreens.Add(new byte[screenSizeInPixels * screenSizeInPixels]);
        }
        screenSignals = new int[screenSizeInPixels * screenSizeInPixels];
        texturePix = new byte[screenSizeInPixels * screenSizeInPixels];

        InitializeScreen(checkScreen, 0, false);
        checkTexture = TextureProcessor.CreateTexture2DFromObject(checkScreen);
        checkPixels = new Color32[screenSizeInPixels * screenSizeInPixels];

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

        patterns = PatternReadWrite.GetPatterns(patternFile);
        //patterns = new Pattern[] { new Pattern(4, 4, 1, 
        //    new byte[16] {
        //        1, 1, 1, 1,
        //        0, 0, 0, 0,
        //        1, 1, 1, 1,
        //        0, 0, 0, 0,
        //    })}; // Save/Load patterns?

        for (int i = 0; i < 3; i++)
        {
            fitnessLineRenderers[i] = fitnessScreen.GetChild(i).GetComponent<LineRenderer>();
        }
        screenLines = new int[patterns[0].patternSizeY];
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
        if (screenSize % ruleInitBitOptimizationBy != 0)
        {
            Debug.LogWarning("Optimisation impassible");
        }
        var cycleLength = screenSize / ruleInitBitOptimizationBy;
        for (short i = 0; i < cycleLength; i++)
        {
            var generatedRandom = Random.Range(0, 1 << ruleInitBitOptimizationBy);
            for (byte j = 0; j < ruleInitBitOptimizationBy; j++)
            {
                screen[i * ruleInitBitOptimizationBy + j] = (byte)(generatedRandom & 1);
                generatedRandom >>= 1;
            }
        }
        return screen;
    }

    public void AddScreenToSimulation(MeshRenderer screen, int screenInd)
    {
        screens.Add(screen);
        InitializeScreen(screen, screenInd);
    }

    private void InitializeScreen(MeshRenderer screen, int screenInd, bool addInList = true)
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
        if (addInList)
        {
            customRenderTextures.Add(customRenderTexture);
        }
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
        if (updateCheckScreen) UpdateCheckScreen();

        if (!simulationPaused && timeToEvolution > 0.5f)
        {
            timeToEvolutionPassed += Time.deltaTime;
            if (timeToEvolutionPassed >= timeToEvolution && fitnessRecalculated)
            {
                if (fitnessCalculations < fitnessCalculationsNeeded)
                {
                    fitnessRecalculated = false;
                    //StartCoroutine(FullRecalculateFitness());
                    FullRecalculateFitness();
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

    private const bool optimizedUpdateCAV2 = true;
    private const bool optimizedUpdateCA = false;
    private void UpdateCA(byte[] CAField, int ind)
    {
        // Оптимизированная версия работает с тройками чисел и побитовыми операциями
        // Предположительно работает только в случае 2 состояний автоматов
        if (optimizedUpdateCAV2)
        {
            var size2D = screenSizeInPixels * screenSizeInPixels;
            int signal = 0;
            // первичная инициализация нулями
            for (int i = screenSizeInPixels - 2; i < screenSizeInPixels; i++)
            {
                var iPix = i * screenSizeInPixels;
                for (int j = 0; j < screenSizeInPixels; j++)
                {
                    screenSignals[iPix + j] = 0;
                }
            }
            for (short i = 0; i < screenSizeInPixels - 2; i++)
            {
                var iPix = i * screenSizeInPixels;
                var iPixm1 = (iPix + size2D - screenSizeInPixels) % size2D;
                var iPixm2 = (iPix + size2D - screenSizeInPixels - screenSizeInPixels) % size2D;
                signal = CAField[iPix + (screenSizeInPixels) - 1] * 2 + CAField[iPix]; // последний + первый символ строки i
                for (short j = 1; j < screenSizeInPixels; j++)
                {
                    signal = (signal << 1) % 8 + CAField[iPix + j];
                    var jm1 = j - 1;
                    screenSignals[iPixm2 + jm1] += signal << 6;
                    screenSignals[iPixm1 + jm1] += signal << 3;
                    screenSignals[iPix   + jm1]  = signal;          // да-да, именно присваивание
                }
                signal = (signal << 1) % 8 + CAField[iPix];                     // + первый символ строки i
                screenSignals[iPixm2 + screenSizeInPixels - 1] += signal << 6;  // строка i-2, последний символ
                screenSignals[iPixm1 + screenSizeInPixels - 1] += signal << 3;  // строка i-1, последний символ
                screenSignals[iPix   + screenSizeInPixels - 1]  = signal;       // строка i,   последний символ, именно присваивание
            }

            for (int i = screenSizeInPixels - 2; i < screenSizeInPixels; i++)
            {
                var iPix = i * screenSizeInPixels;
                var iPixm1 = (iPix + size2D - screenSizeInPixels) % size2D;
                var iPixm2 = (iPix + size2D - screenSizeInPixels - screenSizeInPixels) % size2D;
                signal = CAField[iPix + (screenSizeInPixels) - 1] * 2 + CAField[iPix]; // последний + первый символ строки i
                for (short j = 1; j < screenSizeInPixels; j++)
                {
                    signal = (signal << 1) % 8 + CAField[iPix + j];
                    var jm1 = j - 1;
                    screenSignals[iPixm2 + jm1] += signal << 6;
                    screenSignals[iPixm1 + jm1] += signal << 3;
                    screenSignals[iPix + jm1]   += signal;          // да-да, именно присваивание
                }
                signal = (signal << 1) % 8 + CAField[iPix];                     // + первый символ строки i
                screenSignals[iPixm2 + screenSizeInPixels - 1] += signal << 6;  // строка i-2, последний символ
                screenSignals[iPixm1 + screenSizeInPixels - 1] += signal << 3;  // строка i-1, последний символ
                screenSignals[iPix + screenSizeInPixels - 1] = signal;       // строка i,   последний символ, именно присваивание
            }

            for (short i = 0; i < size2D; i++)
            {
                CAField[i] = (byte)allRules[ind][screenSignals[(i - screenSizeInPixels + size2D) % size2D]];
            }
        }
        else if (optimizedUpdateCA)
        {
            // пройдемся по строке № n-1
            var size2D = screenSizeInPixels * screenSizeInPixels;
            var signal = CAField[size2D - 1] * 2 + CAField[size2D - screenSizeInPixels]; // последний + первый символ строки -1
            var lastRow_i = size2D - screenSizeInPixels;
            for (short i = 1; i < screenSizeInPixels; i++)
            {
                signal = (signal << 1) % 8 + CAField[size2D - screenSizeInPixels + i];
                var im1 = i - 1;
                screenSignals[lastRow_i + im1]          = signal;       // строка -1
                screenSignals[im1]                      = signal << 3;  // строка 0
                screenSignals[screenSizeInPixels + im1] = signal << 6;  // строка 1
            }
            signal = (signal << 1) % 8 + CAField[lastRow_i]; // + первый символ строки -1
            screenSignals[size2D - 1]                   = signal;       // строка -1, последний символ
            screenSignals[screenSizeInPixels - 1]       = signal << 3;  // строка 0, последний символ
            screenSignals[2 * screenSizeInPixels - 1]   = signal << 6;  // строка 1, последний символ

            // пройдемся по строкам № 0:n-2
            for (short i = 0; i < screenSizeInPixels - 2; i++)
            {
                var iPix = i * screenSizeInPixels;
                signal = CAField[iPix + (screenSizeInPixels) - 1] * 2 + CAField[iPix];
                for (short j = 1; j < screenSizeInPixels; j++)
                {
                    signal = (signal << 1) % 8 + CAField[iPix + j];
                    var jm1 = j - 1;
                    screenSignals[iPix + jm1]                          += signal;
                    screenSignals[iPix + screenSizeInPixels + jm1]     += signal << 3;
                    screenSignals[iPix + 2 * screenSizeInPixels + jm1]  = signal << 6; // да-да, именно присваивание
                }
                signal = (signal << 1) % 8 + CAField[iPix];             // + первый символ строки i
                screenSignals[iPix + screenSizeInPixels - 1]           += signal;       // строка i,   последний символ
                screenSignals[iPix + 2 * screenSizeInPixels - 1]       += signal << 3;  // строка i+1, последний символ
                screenSignals[iPix + 3 * screenSizeInPixels - 1]        = signal << 6;  // строка 1+2, последний символ, именно присваивание
            }

            // пройдемся по строке № n-2
            //signal = CAField[size2D - screenSizeInPixels - 1] * 2 + CAField[size2D - 2 * screenSizeInPixels]; // последний + первый символ строки -2
            for (short i = 1; i < screenSizeInPixels; i++)
            {
                signal = (signal << 1) % 8 + CAField[lastRow_i - screenSizeInPixels + i];
                var im1 = i - 1;
                screenSignals[lastRow_i - screenSizeInPixels + im1] += signal;      // строка -2
                screenSignals[lastRow_i + im1]                      += signal << 3; // строка -1
                screenSignals[im1]                                  += signal << 6; // строка  0
            }
            signal = (signal << 1) % 8 + CAField[lastRow_i - screenSizeInPixels]; // + первый символ строки -2
            screenSignals[lastRow_i - 1]                            += signal;      // строка -2, последний символ
            screenSignals[size2D - 1]                               += signal << 3; // строка -1, последний символ
            screenSignals[screenSizeInPixels - 1]                   += signal << 6; // строка  0, последний символ

            for (short i = 0; i < size2D; i++)
            {
                CAField[i] = (byte)allRules[ind][screenSignals[(i + screenSizeInPixels) % size2D]];
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
        //var patternRule = patterns[0].pattern;
        var patternsCount = patterns.Length;
        int[] currentErrors = new int[patternsCount];

        for (short i = 0; i < textureHeight; i++)
        {
            for (short j = 0; j < textureWidth; j++)
            {
                int cornerPixel = j + i * textureWidth;
                //currentErrors = 0;
                for (int p = 0; p < patternsCount; p++)
                {
                    currentErrors[p] = 0;
                }

                for (byte ir = 0; ir < patternHeight; ir++)
                {
                    for (byte jr = 0; jr < patternWidth; jr++)
                    {
                        // Color32 = (255, 255, 255, 255/0)
                        // I convert it to (1, 1, 1, 1/0)
                        // And compare with corresponding cell in fitness rule
                        int pixelIndex = (cornerPixel + ir * textureWidth + jr) % (textureWidth * textureHeight);
                        //jr or ir ?
                        for (int p = 0; p < patternsCount; p++)
                        {
                            currentErrors[p] += texturePixels[pixelIndex] == patterns[p].pattern[ir * patternWidth + jr] ? 0 : 1;
                        }
                        //currentErrors += texturePixels[pixelIndex] == patternRule[ir * patternWidth + jr] ? 0 : 1;
                    }
                    
                    
                    //if (minErrors > patternErrors) break;
                }
                int minErrors = patternErrors + 1;
                for (int p = 0; p < patternsCount; p++)
                {
                    minErrors = currentErrors[p] < minErrors ? currentErrors[p] : minErrors;
                }

                //for (int p = 0; p < patternsCount; p++)
                //{
                //    if (currentErrors[p] < minErrors) minErrors = currentErrors[p];
                //}

                fitness += minErrors <= patternErrors ? (1 + patternErrors - minErrors) * 1f / (patternErrors + 1) : 0;
            }
        }
        
        return fitness * 100 / (textureWidth * textureHeight);
    }

    /// <summary>
    /// Функция оптимизированного подсчета приспособленности
    /// <para>Оптимизация за счет поддержки битовых последовательностей, и побитового сравнения их друг с другом</para> 
    /// <para>Количество операций снизилось</para>
    /// <para>с : Длина текстуры * ширина текстуры * длина паттерна * ширина паттерна</para>
    /// <para>до : Длина текстуры * ширина текстуры * min(длина паттерна, ширина паттерна)</para>
    /// </summary>
    /// <returns></returns>
    private float CalculateFitnessOptimised(byte[] texturePixels, int texW, int texH, Pattern[] patterns)
    {
        float fitness = 0;

        int textureWidth = texW; int textureHeight = texH;
        var tex2D = texW * texH;
        var patternHeight = patterns[0].patternSizeY;
        var patternWidth = patterns[0].patternSizeX;
        var patternErrors = patterns[0].patternErrors;
        var patternRule = patterns[0].pattern;
        int[] currentErrors = new int[patterns.Length];
        int patternCycle = (1 << patternHeight);

        // Подсчет линий в паттерне OK
        int[] patternLines = new int[patterns[0].patternSizeX];
        int newPatternLine = 0;
        for (int i = 0; i < patternHeight; i++)
        {
            newPatternLine = 0;
            for (int j = 0; j < patternWidth; j++)
            {
                newPatternLine = (newPatternLine << 1) + patterns[0].pattern[i * patternWidth + j];
            }
            patternLines[i] = newPatternLine;
        }

        int qOffset = 0;

        // Первичная инициализация очереди. Берем все кроме последней нужной строки! OK
        int newScreenLine = 0;
        for (qOffset = 0; qOffset < patternHeight - 1; qOffset++)
        {
            newScreenLine = 0;
            for (int j = 0; j < patternWidth; j++)
            {
                newScreenLine = (newScreenLine << 1) + texturePixels[qOffset * patternWidth + j];
            }
            screenLines[qOffset] = newScreenLine;
        }

        int newLine = 0, iPix = 0, newPixInd = 0, cornerPixel = 0;
        int screenLine = 0, newErrors = 0, nextPixInd = 0;
        for (short i = 0; i < textureHeight; i++)
        {
            // добавляем новую подсчитанную строку-буфер. Отступ = размер паттерна OK
            newLine = 0;
            iPix = ((i + patternHeight - 1) % textureHeight) * textureWidth;
            //print("iPix: " + iPix);
            for (int j = 0; j < patternWidth; j++)
            {
                newPixInd = (iPix + j);
                newLine = (newLine << 1) + texturePixels[newPixInd];
            }
            qOffset = (qOffset + 1) % patternHeight;
            screenLines[qOffset] = (newLine);

            for (short j = 0; j < textureWidth; j++)
            {
                // сравниваем строки паттерна и текстуры для текущей точки
                // для сравнения используем XOR: a ^ b
                cornerPixel = j + i * textureWidth;
                //List<byte> currentErrorsList = new List<byte>();
                for (byte p = 0; p < patterns.Length; p++)
                {
                    currentErrors[p] = 0;
                }
                int minErrors = patternErrors + 1;
                
                for (byte k = 0; k < patternHeight; k++)
                {
                    int ind = (qOffset + k) % patternHeight; // qOffset + 1 + k ????
                    screenLine = screenLines[ind];
                    newErrors = patternLines[k] ^ screenLine;
                    ///print($"Screen:{screenLine}, Pattern:{patternLines[k]}, Diff:{newErrors}");
                    // тут можно добавить и другие оптимизации? таблица битов?
                    // https://stackoverflow.com/questions/109023/how-to-count-the-number-of-set-bits-in-a-32-bit-integer
                    //////////////
                    for (byte p = 0; p < patterns.Length; p++)
                    {
                        newErrors = newErrors - ((newErrors >> 1) & 0x55555555);
                        newErrors = (newErrors & 0x33333333) + ((newErrors >> 2) & 0x33333333);
                        currentErrors[p] += (((newErrors + (newErrors >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
                    }
                    
                    //while (currentErrors <= patternErrors && newErrors != 0)
                    //{
                    //    currentErrors += newErrors & 1;
                    //    newErrors >>= 1;
                    //}

                    // пересчитываем строку (сдвигаем окно вправо) для следующей точки
                    screenLine <<= 1;
                    if (screenLine >= patternCycle) screenLine -= patternCycle;
                    nextPixInd = (((i + k) % textureHeight) * textureWidth) + (j + patternWidth) % textureWidth;
                    screenLine += texturePixels[nextPixInd];
                    
                    screenLines[(qOffset + k) % patternHeight] = (screenLine);
                }
                #region старая версия
                //for (byte kr = 0; kr < patternLines.Length; kr++)
                //{


                //    for (byte jr = 0; jr < patternWidth; jr++)
                //    {
                //        // Color32 = (255, 255, 255, 255/0)
                //        // I convert it to (1, 1, 1, 1/0)
                //        // And compare with corresponding cell in fitness rule
                //        int pixelIndex = (cornerPixel + ir * textureWidth + jr) % (textureWidth * textureHeight);
                //        //jr or ir ?
                //        currentErrors += texturePixels[pixelIndex] == patternRule[ir * patternWidth + jr] ? 0 : 1;
                //    }
                //    // отсекаем неэффективные паттерны
                //    if (currentErrors > patternErrors) break;
                //}
                #endregion
                // выбираем минимум из ошибок и увеличиваем приспособленность если нужно
                //print($"i:{i}, j:{j}, Err:{currentErrors}, ");
                //print($"X|(j{j}):" + currentErrors);
                //print($"{screenLines[0]}|||{patternLines[0]}");
                for (byte p = 0; p < patterns.Length; p++)
                {
                    if (currentErrors[p] < minErrors) minErrors = currentErrors[p];
                }
                fitness += minErrors <= patternErrors ? (1 + patternErrors - minErrors) * 1f / (patternErrors + 1) : 0;
            }

            // удаляем верхнюю буферизованную строку 
            //screenLines.Dequeue();
        }
        //screenLines.Clear();

        return fitness * 100 / (textureWidth * textureHeight);
    }

    bool fitnessRecalculated = true;
    private void FullRecalculateFitness()
    {
        for (int i = 0; i < virtualScreensInSimulation; i++)
        {
            //TextureProcessor.GetTexture2DFromObject(screens[i], ref screenTex2D[i]);
            //var fitness = CalculateFitness(screenTex2D[i], pattern);
            var fitness = CalculateFitnessOptimised(virtualScreens[i], screenSizeInPixels, screenSizeInPixels, patterns);
            //print($"i: {i} | fitness: {fitness}");
            maxScreenFitness[i] = Mathf.Max(maxScreenFitness[i], fitness);
            //if ((i + 1) % fitnessCalcScreensPerFrame == 0)
            //{
            //    yield return new WaitForEndOfFrame();
            //}
        }

        fitnessCalculations++;
        fitnessRecalculated = true;
        //yield return null;
    }

    public bool crossSeparation = false;
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

            
            var sonRule = new float[RULE_SIZE];
            var daughterRule = new float[RULE_SIZE];
            if (crossSeparation)
            {
                var crossSeparator = Random.Range(0, RULE_SIZE);
                for (int j = 0; j < RULE_SIZE; j++)
                {
                    sonRule[j] = j < crossSeparator ? parent1Rule[j] : parent2Rule[j];
                    daughterRule[j] = j < crossSeparator ? parent2Rule[j] : parent1Rule[j];
                }
            }
            else
            {
                for (int j = 0; j < RULE_SIZE; j++)
                {
                    if (Random.Range(0, 2) == 0)
                    {
                        sonRule[j] = parent1Rule[j];
                        daughterRule[j] = parent2Rule[j];
                    }
                    else
                    {
                        sonRule[j] = parent2Rule[j];
                        daughterRule[j] = parent1Rule[j];
                    }
                }
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

        int add = virtualScreensInSimulation / ScreensCount();
        for (int i = 0; i < ScreensCount(); i++)
        {
            TextureProcessor.GetTextureFromObject(screens[i]).material.SetFloatArray("_rule", newRules[i * add]);
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

        for (int i = 0; i < allRules.Length; i++)
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
        float sizeMultX = 1f / (1 + fitnessHistory.Count / 10);
        var lastRecord = fitnessHistory[fitnessHistory.Count - 1];
        float sizeMultY = 4f / (1 + (lastRecord.averageGoodFitness + lastRecord.maxFitness) / 5f);
        foreach (var lineRend in fitnessLineRenderers) {
            lineRend.positionCount = fitnessHistory.Count;
            lineRend.transform.localScale = new Vector3(sizeMultX, 1, sizeMultY);
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

    public void PrintScreenRule(int index)
    {
        var arr = TextureProcessor.GetTextureFromObject(screens[index]).material.GetFloatArray("_rule");
        if (arr == null || arr.Length == 0) return;
        string output = "";
        foreach (var sym in arr)
        {
            output += sym;
        }
        print(output);
    }

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

    Texture2D checkTexture = null;
    Color32[] checkPixels = null;
    private void UpdateCheckScreen()
    {
        for (int i = 0; i < virtualScreens[0].Length; i++)
        {
            checkPixels[i] = new Color32(255, 255, 255, (byte)(virtualScreens[0][i] * 255));
        }

        checkScreen.material.mainTexture = TextureProcessor.PaintTexture(checkTexture, checkPixels);
    }

    private float[][] allRules = null;
    private List<byte[]> virtualScreens = new List<byte[]>();
    private List<byte[]> nextVirtualScreens = new List<byte[]>();
    private int[] screenSignals;
    private List<MeshRenderer> screens = new List<MeshRenderer>();
    private List<CustomRenderTexture> customRenderTextures = new List<CustomRenderTexture>();

    private float updatePeriodSaved = 0;
    private float updateFramesPassed = 0;
    
    private int fitnessCalculations = 0;
    private float[] maxScreenFitness;
    private int[] screenLines = null; // Для оптимизации
    private List<FitnessRecord> fitnessHistory = new List<FitnessRecord>();

    List<Vector3> fitnessBestPoints = new List<Vector3>();
    List<Vector3> fitnessAverageGoodPoints = new List<Vector3>();
    List<Vector3> fitnessAveragePoints = new List<Vector3>();
    private Color32[] genofond = null;
    private Texture2D genofondScreenTex = null;
    private LineRenderer[] fitnessLineRenderers = new LineRenderer[3];
}
