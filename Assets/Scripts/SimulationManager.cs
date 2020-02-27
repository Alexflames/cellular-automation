using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimulationManager : MonoBehaviour
{
    [SerializeField, Header("Scene settings")]
    private CustomRenderTexture screenTexture = null;
    [SerializeField]
    private Material cellularAutomationMaterial = null;
    [SerializeField]
    private List<MeshRenderer> screens = new List<MeshRenderer>();
    [SerializeField]
    private Material screenMaterialPrefab = null;

    [SerializeField]
    private GameObject screenSettingsObject = null;

    [SerializeField, Header("Simulation settings")]
    private float updatePeriod = 0.05f;
    [SerializeField]
    private int screenSizeInPixels = 128;
    [SerializeField]
    private int screensInSimulation = 128;

    private List<CustomRenderTexture> customRenderTextures = new List<CustomRenderTexture>();
    
    private List<List<float>> allRules = new List<List<float>>();

    private List<float> InitializeRandomRules(int size)
    {
        List<float> rules = new List<float>();
        for (int i = 0; i < size; i++)
        {
            rules.Add(Random.Range(0, 2)); // {0, 1}
        }
        return rules;
    }

    public void AddScreenToSimulation(MeshRenderer screen)
    {
        screens.Add(screen);
        InitializeScreen(screen);
    }

    private float timeToEvolutionPassed = 0f;
    [SerializeField, Header("Evolution")]
    private float timeToEvolution = 3f;
    [SerializeField]
    private float mutationPercent = 7;

    [SerializeField]
    private MeshRenderer genofondScreen = null;
    [SerializeField]
    private Transform patternDrawer = null;
    private Material[] patternCellsMat = new Material[400];


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

    public Pattern pattern;

    private const int RULE_SIZE = 512;

    void Start()
    {
        if (screenTexture == null)
        {
            Debug.LogError(new UnassignedReferenceException("No link to custom render texture prefab"));
            return;
        }

        for (int i = 0; i < ScreensCount(); i++)
        {
            AddScreenToSimulation(transform.GetChild(i).GetComponent<MeshRenderer>());
        }

        var patternDrawerTransform = patternDrawer.transform;
        var patternDrawerCellsCount = patternDrawerTransform.childCount;
        for (int i = 0; i < patternDrawerCellsCount; i++)
        {
            var meshRenderer = patternDrawerTransform.GetChild(i).GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(meshRenderer.material);
            patternCellsMat[i] = meshRenderer.material;
        }

        pattern = new Pattern(3, 3, 2, new short[9] { 0, 0, 0, 0, 1, 0, 0, 0, 0 }); // Save/Load patterns?

        mainCamera = Camera.main;
    }

    private void InitializeScreen(MeshRenderer screen)
    {
        var customRenderTexture = new CustomRenderTexture(screenSizeInPixels, screenSizeInPixels);
        customRenderTexture.initializationTexture = TextureProcessor.CreateRandomTexture(screenTexture.width, screenTexture.height);
        customRenderTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
        customRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        customRenderTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
        customRenderTexture.doubleBuffered = true;
        customRenderTexture.wrapMode = TextureWrapMode.Repeat;
        customRenderTexture.material = new Material(cellularAutomationMaterial);

        var rule = InitializeRandomRules(RULE_SIZE);
        customRenderTexture.material.SetFloatArray("_rule", rule);
        allRules.Add(rule);
        customRenderTexture.Initialize();

        var material = new Material(screenMaterialPrefab);
        material.SetTexture("_MainTex", customRenderTexture);
        screen.material = material;
        customRenderTextures.Add(customRenderTexture);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftShift))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if (Input.GetMouseButtonDown(0) && screenSettingsObject.activeSelf == false)
        {
            if (cameraSavedPositionP != Vector3.zero)
            {
                Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitinfo, 100);
                if (hitinfo.collider != null)
                {
                    var material = hitinfo.collider.GetComponent<MeshRenderer>().material;
                    if (material.color == Color.white) material.color = Color.black;
                    else material.color = Color.white;
                }
            }
            else
            {
                // Raycast mouse click to find screen to choose (stored in hitinfo)
                Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitinfo, 100);
                // Клик попал в экран + костыль для того чтобы при нажатии на интерфейс не задевало
                if (hitinfo.collider != null)
                {
                    var screenNumber = IndexOfScreen(hitinfo.collider.gameObject);
                    if (screenNumber != -1 && (Input.mousePosition.y > 66f || Input.mousePosition.x > 600 || Input.mousePosition.x < 350))
                    {
                        OpenScreenSettings(hitinfo.collider.gameObject);
                    }
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (focusedScreenIndex != -1)
            {
                RefreshScreen(focusedScreenIndex);
            }
            else
            {
                StartCoroutine(RefreshAllScreens());
            }
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            PauseResumeSimulation();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            ShowHideGenofond();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            ShowHidePatternDrawer();
        }
    }

    void FixedUpdate()
    {
        // Camera movement
        mainCamera.transform.Translate(new Vector3(Input.GetAxis("Horizontal") * 6, Input.GetAxis("Vertical") * 6, Input.GetAxis("Mouse ScrollWheel") * 750) * Time.fixedDeltaTime);

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
            if (timeToEvolutionPassed >= timeToEvolution)
            {
                timeToEvolutionPassed = 0;
                StartCoroutine(Evolve());
            }
        }
    }

    #region evolution-algorithm
    
    /// <summary>
    /// Calculates fitness of current cellular state machine.
    /// More fitness -> more likely that it stays and evolves in future generations
    /// </summary>
    /// <param name="texture2D">Texture with pixels to process</param>
    /// <param name="blockFitnessRule">Pattern to match</param>
    /// <param name="fitnessBlockWidth">Pattern width</param>
    /// <param name="fitnessBlockHeight">Pattern height</param>
    /// <param name="errors">The maximum allowed errors in pattern</param>
    /// <returns></returns>
    private float CalculateFitness(Texture2D texture2D, Pattern pattern, int frames = 1)
    {
        var texturePixels = texture2D.GetRawTextureData<Color32>();

        int fitness = 0;

        int textureWidth = texture2D.width; int textureHeight = texture2D.height;

        var patternHeight = pattern.patternSizeY;
        var patternWidth = pattern.patternSizeX;
        var patternErrors = pattern.patternErrors;
        var patternRule = pattern.pattern;
        int currentErrors = 0;
        for (int f = 0; f < frames; f++)
        {
            //yield return new WaitForEndOfFrame();
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
                    }

                    fitness += currentErrors <= patternErrors ? 1 : 0;
                }
            }
        }


        //yield 
        return fitness * 1f / (textureWidth * textureHeight);
        //return 1f - (currentErrors / (patternHeight * patternWidth)) * 1f / (textureWidth * textureHeight) ;
    }

    private IEnumerator Evolve()
    {
        // >>>Fitness calculation<<<


        List<KeyValuePair<int, float>> indexFitness = new List<KeyValuePair<int, float>>();
        for (int i = 0; i < ScreensCount(); i++)
        {
            var fitness = CalculateFitness(TextureProcessor.GetTexture2DFromObject(screens[i]), pattern);
            indexFitness.Add(new KeyValuePair<int, float>(i, fitness));

            //if (i % 8 == 0)
            //{
            //    yield return new WaitForEndOfFrame();
            //}
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
        
        Debug.Log($"Maximum fitness: {indexFitness[0].Value.ToString("0.00000")}. Average fitness: {averageFitness.ToString("0.00000")}. Average good fitness: {averageGoodFitness.ToString("0.00000")}");

        List<List<float>> newRules = new List<List<float>>();
        
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
            var sonRule = new List<float>();
            var daughterRule = new List<float>();
            for (int j = 0; j < RULE_SIZE; j++)
            {
                sonRule.Add(j < crossSeparator ? parent1Rule[j] : parent2Rule[j]);
                daughterRule.Add(j < crossSeparator ? parent2Rule[j] : parent1Rule[j]);
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

        StartCoroutine(RefreshAllScreens());
        UpdateGenofond();
        yield return null;
    }

    // Pattern Settings
    private void UpdatePattern(int sizeX, int sizeY, int errors)
    {
        pattern = new Pattern(sizeX, sizeY, errors);
    }

    // Pattern itself
    private void UpdatePattern()
    {
        short[] newPattern = new short[pattern.patternSizeX * pattern.patternSizeY];

        int MAGIC_CONSTANT = 20; // 20 screens maximum in row. Used in transform hierarchy
        for (int i = 0; i < pattern.patternSizeY; i++)
        {
            for (int j = 0; j < pattern.patternSizeX; j++)
            {
                short color = 1;
                if (patternCellsMat[j + i * MAGIC_CONSTANT].color == Color.white) color = 0;
                newPattern[j + i * pattern.patternSizeX] = color;
            }
        }

        pattern.pattern = newPattern;
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

    private Vector3 cameraSavedPositionG = Vector3.zero;

    private void ShowHideGenofond()
    {
        if (cameraSavedPositionG == Vector3.zero) ShowGenofond();
        else HideGenofond();
    }

    private void ShowGenofond()
    {
        cameraSavedPositionG = mainCamera.transform.position;
        mainCamera.transform.position = genofondScreen.transform.position - new Vector3(0, 0, 10);
    }

    private void HideGenofond()
    {
        mainCamera.transform.position = cameraSavedPositionG;
        cameraSavedPositionG = Vector3.zero;
    }

    private Vector3 cameraSavedPositionP = Vector3.zero;

    private void ShowHidePatternDrawer()
    {
        if (cameraSavedPositionP == Vector3.zero) ShowPatternDrawer();
        else HidePatternDrawer();
    }

    private void ShowPatternDrawer()
    {
        if (!simulationPaused) PauseResumeSimulation();
        cameraSavedPositionP = mainCamera.transform.position;
        mainCamera.transform.position = patternDrawer.position - new Vector3(0, 0, 20);
    }

    private void HidePatternDrawer()
    {
        if (simulationPaused) PauseResumeSimulation();
        UpdatePattern();
        mainCamera.transform.position = cameraSavedPositionP;
        cameraSavedPositionP = Vector3.zero;
    }

    #endregion

    private int ScreensCount()
    {
        return Mathf.Min(transform.childCount, screensInSimulation);
    }

    private IEnumerator RefreshAllScreens()
    {
        for (int i = 0; i < ScreensCount(); i++)
        {
            RefreshScreen(i);
            if (i % 8 == 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        yield return null;
    }

    private void RefreshScreen(int index)
    {
        var renderTexture = TextureProcessor.GetTextureFromObject(screens[index]);
        RefreshScreen(renderTexture);
    }

    private void RefreshScreen(CustomRenderTexture renderTexture)
    {
        renderTexture.initializationTexture = TextureProcessor.PaintRandomTexture(renderTexture.initializationTexture);
        renderTexture.Initialize();
    }

    private int IndexOfScreen(GameObject screen)
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

    private int focusedScreenIndex = -1;

    private void OpenScreenSettings(GameObject screen)
    {
        //if (!simulationPaused) PauseResumeSimulation();
        //simulationSettingsPrefab.SetActive(false);
        screenSettingsObject.SetActive(true);
        focusedScreenIndex = IndexOfScreen(screen);
        FocusMiddleScreen(focusedScreenIndex);
    }

    public void CloseScreenSettings()
    {
        //PauseResumeSimulation();
        //simulationSettingsPrefab.SetActive(true);
        screenSettingsObject.SetActive(false);
        UnFocusMiddleScreen(focusedScreenIndex);
        focusedScreenIndex = -1;
    }

    private Vector3 screenSavedPosition;
    private Vector3 screenSavedScale;
    private void FocusMiddleScreen(int index)
    {
        var screen = screens[index];
        screenSavedPosition = screen.transform.position;
        screenSavedScale = screen.transform.localScale;
        screen.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
        mainCamera.transform.Translate(100, 0, 0);
        screen.transform.position = mainCamera.transform.position + new Vector3(0, 0, 5);
    }

    private void UnFocusMiddleScreen(int index)
    {
        var screen = screens[index];
        screen.transform.localScale = screenSavedScale;
        screen.transform.position = screenSavedPosition;
        mainCamera.transform.Translate(-100, 0, 0);
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

    private float updatePeriodSaved = 0;
    private bool simulationPaused = false;

    private float updateFramesPassed = 0;
    
    private Camera mainCamera = null;
}
