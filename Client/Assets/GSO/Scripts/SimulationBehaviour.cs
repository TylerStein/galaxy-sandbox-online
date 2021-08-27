using UnityEngine;

namespace GSO
{
    public abstract class SimulationBehaviour : MonoBehaviour
    {
        public abstract bool TryGetConnectionError(out string message, out int code);
        public abstract bool IsConnected();
        public abstract void AddBody(BodyData data);
        public abstract void ReadBodies(out BodyData[] bodies);
    }
}