using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class GSOManager : MonoBehaviour
    {

        public SimulationBehaviour simulationService;
        public SimulationRenderBehaviour renderService;

        public Transform simulationSpace;

        public GSOSettings settings;
        public GSOBodyPool pool;

        public BodyData[] bodies = new BodyData[0];

        public void Awake() {
            Application.targetFrameRate = settings.targetFramerate;
        }

        public void SpawnBody(BodyData data) {
            simulationService.AddBody(data);
        }

        public void Update() {
            simulationService.ReadBodies(out bodies);
            renderService.SetBodies(bodies);
        }
    }
}