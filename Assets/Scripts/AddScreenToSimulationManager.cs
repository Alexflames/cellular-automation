using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddScreenToSimulationManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        GameObject.FindGameObjectWithTag("GameController")
            .GetComponent<SimulationManager>()
            .AddScreenToSimulation(GetComponent<MeshRenderer>());   
    }
}
