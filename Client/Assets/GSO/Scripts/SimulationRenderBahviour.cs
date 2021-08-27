using UnityEngine;

namespace GSO
{
    public abstract class SimulationRenderBehaviour : MonoBehaviour
    {
        public abstract void SetBodies(BodyData[] data);
    }
}