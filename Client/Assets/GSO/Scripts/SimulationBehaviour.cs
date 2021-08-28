using UnityEngine;

namespace GSO
{
    public abstract class SimulationBehaviour : MonoBehaviour
    {
        public UnityEngine.Events.UnityEvent ConnectionEvent;

        public abstract void Activate();
        public abstract void Deactivate();
        public abstract void ReActivate();
        public abstract bool IsReady();

        public abstract int GetPlayerCount();
        public abstract int GetObjectCount();
        public abstract bool TryGetConnectionError(out string message, out int code);
        public abstract void AddBody(BodyData data);
        public abstract void ReadBodies(out BodyData[] bodies);
    }
}