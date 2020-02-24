using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopResumeSimulation : MonoBehaviour
{
    private SimulationManager simulationManager;

    void Start()
    {
        simulationManager = GameObject.FindGameObjectWithTag("GameController")
            .GetComponent<SimulationManager>();
    }

    public void PauseResumeButton()
    {
        simulationManager.PauseResumeSimulation();
    }

    private float savedSimulationSpeed;
}
