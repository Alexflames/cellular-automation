using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimulationManager : MonoBehaviour
{
    [SerializeField]
    private CustomRenderTexture screenTexture = null;
    [SerializeField]
    private Material cellularAutomationMaterial = null;
    [SerializeField]
    private List<MeshRenderer> screens = new List<MeshRenderer>();
    [SerializeField]
    private Material screenMaterialPrefab = null;

    [SerializeField]
    private GameObject screenSettingsPrefab = null;
    [SerializeField]
    private GameObject simulationSettingsPrefab = null;

    [SerializeField]
    private float updatePeriod = 0.05f;

    private List<CustomRenderTexture> customRenderTextures = new List<CustomRenderTexture>();

    [SerializeField]
    private List<List<float>> allRules = new List<List<float>>();

    [SerializeField]
    private GameObject blackBoard = null;

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
    }

    void Start()
    {
        if (screenTexture == null)
        {
            Debug.LogError(new UnassignedReferenceException("No link to custom render texture prefab"));
            return;
        }

        foreach (var screen in screens)
        {
            InitializeScreen(screen);
        }

        mainCamera = Camera.main;
    }

    private void InitializeScreen(MeshRenderer screen)
    {
        var customRenderTexture = new CustomRenderTexture(48, 48);
        customRenderTexture.initializationTexture = TextureProcessor.CreateRandomTexture(screenTexture.width, screenTexture.height);
        customRenderTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
        customRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        customRenderTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
        customRenderTexture.doubleBuffered = true;
        customRenderTexture.wrapMode = TextureWrapMode.Repeat;
        customRenderTexture.material = new Material(cellularAutomationMaterial);

        var rule = InitializeRandomRules(512);
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
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Raycast mouse click to find screen to choose (stored in hitinfo)
            Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitinfo, 100);
            if (hitinfo.collider != null)
            {
                OpenScreenSettings(hitinfo.collider.gameObject);
            }
        }

        updateFramesPassed += Time.deltaTime;
        if (updateFramesPassed >= updatePeriod)
        {
            foreach(var texture in customRenderTextures)
            {
                texture.Update();
            }
            updateFramesPassed = 0;
        }
    }

    private int IndexOfScreen(GameObject screen)
    {
        return screens.IndexOf(screen.GetComponent<MeshRenderer>());
    }

    private int focusedScreenIndex = -1;

    private void OpenScreenSettings(GameObject screen)
    {
        //if (!simulationPaused) PauseResumeSimulation();
        //simulationSettingsPrefab.SetActive(false);
        screenSettingsPrefab.SetActive(true);
        focusedScreenIndex = IndexOfScreen(screen);
        FocusMiddleScreen(focusedScreenIndex);
        blackBoard.SetActive(true);
    }

    public void CloseScreenSettings()
    {
        //PauseResumeSimulation();
        //simulationSettingsPrefab.SetActive(true);
        screenSettingsPrefab.SetActive(false);
        UnFocusMiddleScreen(focusedScreenIndex);
        blackBoard.SetActive(false);
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
        screen.transform.position = screen.transform.parent.position;
    }

    private void UnFocusMiddleScreen(int index)
    {
        var screen = screens[index];
        screen.transform.localScale = screenSavedScale;
        screen.transform.position = screenSavedPosition;
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
