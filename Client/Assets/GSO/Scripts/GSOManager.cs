using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class GSOManager : MonoBehaviour
    {
        public bool useOnline = true;

        public SimulationBehaviour simulationServiceOnline;
        public SimulationBehaviour simulationServiceOffline;

        public SimulationRenderBehaviour renderService;

        public Transform simulationSpace;

        public GSOSettings settings;
        public GSOBodyPool pool;
        public GSOTextureManager textureManager;

        public BodyData[] bodies = new BodyData[0];

        public UnityEngine.Events.UnityEvent ConnectionChangeEvent = new UnityEngine.Events.UnityEvent();
        public SimulationBehaviour simulationService { get => useOnline ? simulationServiceOnline : simulationServiceOffline; }

        public void Awake() {
            Application.targetFrameRate = settings.targetFramerate;
            simulationServiceOnline.ConnectionEvent.AddListener(() => {
                ConnectionChangeEvent.Invoke();
            });

            simulationServiceOffline.ConnectionEvent.AddListener(() => {
                ConnectionChangeEvent.Invoke();
            });

            SetSimulationService(true);
        }

        public void SpawnBody(BodyData data) {
            simulationService.AddBody(data);
        }

        public void Update() {
            simulationService.ReadBodies(out bodies);
            renderService.SetBodies(bodies);

            if (Input.GetButtonDown("Reset")) {
                ResetState();
            }

            if (Input.GetButtonDown("Switch")) {
                SetSimulationService(!useOnline);
            }
        }

        public void ResetState() {
            renderService.SetBodies(new BodyData[0]);
            simulationService.ReActivate();
        }

        public void SetSimulationService(bool online) {
            simulationService.Deactivate();
            useOnline = online;
            simulationService.Activate();
        }
    }
}