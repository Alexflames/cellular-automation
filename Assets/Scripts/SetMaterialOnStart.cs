using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetMaterialOnStart : MonoBehaviour
{
    [SerializeField]
    private Renderer[] objectsToSetMaterial = null;
    [SerializeField]
    private Material materialToSet = null;

    void Start()
    {
        foreach (var obj in objectsToSetMaterial)
        {
            obj.material = new Material(materialToSet);
        }
    }
}
