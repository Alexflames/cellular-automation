using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InterfaceManager : MonoBehaviour
{
    [SerializeField] private GameObject screenSettingsObject = null;
    [SerializeField] private Transform patternDrawer = null;

    // Start is called before the first frame update
    void Start()
    {
        simulationManager = GetComponent<SimulationManager>();
        mainCamera = Camera.main;

        var patternDrawerTransform = patternDrawer.transform;
        var patternDrawerCellsCount = patternDrawerTransform.childCount;
        for (int i = 0; i < patternDrawerCellsCount; i++)
        {
            var meshRenderer = patternDrawerTransform.GetChild(i).GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(meshRenderer.material);
            patternCellsMat[i] = meshRenderer.material;
        }
    }
    
    void Update()
    {
        mainCamera.transform.Translate(
            new Vector3(Input.GetAxis("Horizontal") * 6, 
            Input.GetAxis("Vertical") * 6, 
            Input.GetAxis("Mouse ScrollWheel") * 750) * Time.deltaTime);

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
                    var screenNumber = simulationManager.IndexOfScreen(hitinfo.collider.gameObject);
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
                simulationManager.RefreshScreen(focusedScreenIndex);
            }
            else
            {
                simulationManager.RefreshAllScreens();
            }
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            simulationManager.PauseResumeSimulation();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            ShowHideGenofond();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            //ShowHidePatternDrawer();
        }
    }

    // Pattern Settings
    public void UpdatePattern(byte sizeX, byte sizeY, byte errors)
    {
        simulationManager.patterns[0] = new SimulationManager.Pattern(sizeX, sizeY, errors);
    }

    // Pattern itself
    public void UpdatePattern()
    {
        var patterns = simulationManager.patterns;
        byte[] newPattern = new byte[patterns[0].patternSizeX * patterns[0].patternSizeY];

        int MAGIC_CONSTANT = 20; // 20 screens maximum in row. Used in transform hierarchy
        for (int i = 0; i < patterns[0].patternSizeY; i++)
        {
            for (int j = 0; j < patterns[0].patternSizeX; j++)
            {
                byte color = 1;
                if (patternCellsMat[j + i * MAGIC_CONSTANT].color == Color.black) color = 0;
                newPattern[j + i * patterns[0].patternSizeX] = color;
            }
        }

        simulationManager.patterns[0].pattern = newPattern;
    }

    private void ShowHideGenofond()
    {
        if (cameraSavedPositionG == Vector3.zero) ShowGenofond();
        else HideGenofond();
    }

    private void ShowGenofond()
    {
        cameraSavedPositionG = mainCamera.transform.position;
        mainCamera.transform.position = simulationManager.genofondScreen.transform.position - new Vector3(0, 0, 10);
    }

    private void HideGenofond()
    {
        mainCamera.transform.position = cameraSavedPositionG;
        cameraSavedPositionG = Vector3.zero;
    }

    private void ShowHidePatternDrawer()
    {
        if (cameraSavedPositionP == Vector3.zero) ShowPatternDrawer();
        else HidePatternDrawer();
    }

    private void ShowPatternDrawer()
    {
        if (!simulationManager.simulationPaused) simulationManager.PauseResumeSimulation();
        cameraSavedPositionP = mainCamera.transform.position;
        mainCamera.transform.position = patternDrawer.position - new Vector3(0, 0, 20);
    }

    private void HidePatternDrawer()
    {
        if (simulationManager.simulationPaused) simulationManager.PauseResumeSimulation();
        UpdatePattern();
        mainCamera.transform.position = cameraSavedPositionP;
        cameraSavedPositionP = Vector3.zero;
    }

    private void OpenScreenSettings(GameObject screen)
    {
        //if (!simulationPaused) PauseResumeSimulation();
        //simulationSettingsPrefab.SetActive(false);
        screenSettingsObject.SetActive(true);
        focusedScreenIndex = simulationManager.IndexOfScreen(screen);
        simulationManager.PrintScreenRule(focusedScreenIndex);
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

    private void FocusMiddleScreen(int index)
    {
        var screen = simulationManager.GetScreen(index);
        screenSavedPosition = screen.transform.position;
        screenSavedScale = screen.transform.localScale;
        screen.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
        mainCamera.transform.Translate(100, 0, 0);
        screen.transform.position = mainCamera.transform.position + new Vector3(0, 0, 5);
    }

    private void UnFocusMiddleScreen(int index)
    {
        var screen = simulationManager.GetScreen(index);
        screen.transform.localScale = screenSavedScale;
        screen.transform.position = screenSavedPosition;
        mainCamera.transform.Translate(-100, 0, 0);
    }

    private Vector3 cameraSavedPositionP = Vector3.zero;
    private Vector3 cameraSavedPositionG = Vector3.zero;
    private Camera mainCamera = null;
    private Vector3 screenSavedPosition;
    private Vector3 screenSavedScale;
    private int focusedScreenIndex = -1;

    private SimulationManager simulationManager;
    private Material[] patternCellsMat = new Material[400];
}
