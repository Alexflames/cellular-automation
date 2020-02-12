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
    private MeshRenderer screen = null;
    [SerializeField]
    private Material screenMaterialPrefab = null;

    [SerializeField]
    private uint updatePeriodInFrames = 3;

    private CustomRenderTexture customRenderTexture = null;

    [SerializeField]
    private float[] rules;

    private float[] InitializeRandomRules(int size)
    {
        float[] rules = new float[size];
        for (int i = 0; i < size; i++)
        {
            rules[i] = Random.Range(0, 2); // {0, 1}
        }
        return rules;
    }

    void Awake()
    {

    }

    void Start()
    {
        if (screenTexture == null)
        {
            Debug.LogError(new UnassignedReferenceException("No link to custom render texture prefab"));
            return;
        }

        customRenderTexture = new CustomRenderTexture(256, 256);
        customRenderTexture.initializationTexture = TextureProcessor.CreateRandomTexture(screenTexture.width, screenTexture.height);
        customRenderTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
        customRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        customRenderTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
        customRenderTexture.doubleBuffered = true;
        customRenderTexture.wrapMode = TextureWrapMode.Repeat;
        customRenderTexture.material = new Material(cellularAutomationMaterial);

        rules = InitializeRandomRules(512);
        customRenderTexture.material.SetFloatArray("_rule", rules);
        customRenderTexture.Initialize();

        var material = new Material(screenMaterialPrefab);
        material.SetTexture("_MainTex", customRenderTexture);
        screen.material = material;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        updateFramesPassed++;
        if (updateFramesPassed == updatePeriodInFrames)
        {
            customRenderTexture.Update();
            updateFramesPassed = 0;
        }
    }

    private uint updateFramesPassed = 0;
}
