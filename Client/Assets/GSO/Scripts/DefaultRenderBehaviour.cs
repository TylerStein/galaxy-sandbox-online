using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class RenderBodyWrapper
    {
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
        public BodyData data;

        public override int GetHashCode() {
            return data.i.GetHashCode();
        }

        public override bool Equals(object obj) {
            return obj is RenderBodyWrapper
                && obj.GetHashCode() == GetHashCode();
        }
    }

    public class DefaultRenderBehaviour : SimulationRenderBehaviour
    {
        public GSOManager manager;
        public Transform simulationSpace;
        public Dictionary<string, RenderBodyWrapper> wrappers = new Dictionary<string, RenderBodyWrapper>();
        private Dictionary<string, BodyData> wrappersBuffer = new Dictionary<string, BodyData>();
        private Dictionary<string, int> wrappersCounter = new Dictionary<string, int>();
        private List<string> removeBuffer = new List<string>();

        public override void SetBodies(BodyData[] data) {
            wrappersCounter.Clear();
            wrappersBuffer.Clear();
            removeBuffer.Clear();

            foreach (BodyData body in data) {
                // create dictionary with all new bodies
                wrappersCounter.Add(body.i, 1);
                wrappersBuffer.Add(body.i, body);
            }
            
            foreach (RenderBodyWrapper wrapper in wrappers.Values) {
                // decrement dictionary where wrapper exists
                if (wrappersCounter.ContainsKey(wrapper.data.i)) {
                    wrappersCounter[wrapper.data.i]--;
                } else {
                    // wrapper was removed
                    removeBuffer.Add(wrapper.data.i);
                    DestroyWrapper(wrapper);
                }
            }

            foreach (string key in removeBuffer) {
                wrappers.Remove(key);
            }

            foreach (KeyValuePair<string, int> kvp in wrappersCounter) {
                // if object exists, count will be 0
                // if object is new, count will be 1
                if (kvp.Value == 0) {
                    wrappers[kvp.Key].data = wrappersBuffer[kvp.Key];
                    UpdateWrapper(wrappers[kvp.Key]);
                } else {
                    wrappers[kvp.Key] = CreateWrapper(wrappersBuffer[kvp.Key]);
                }
            }
        }

        private void DestroyWrapper(RenderBodyWrapper wrapper) {
            manager.pool.RecycleToPool(wrapper.gameObject);
        }

        private void UpdateWrapper(RenderBodyWrapper wrapper) {
            wrapper.gameObject.transform.localScale = new Vector3(wrapper.data.r, wrapper.data.r, 1f);
            wrapper.gameObject.transform.position = wrapper.data.pvec;
            wrapper.spriteRenderer.color = ParseColor(wrapper.data.c);
        }

        private RenderBodyWrapper CreateWrapper(BodyData data) {
            GameObject obj = manager.pool.GetFromPool();
            obj.transform.parent = simulationSpace;

            RenderBodyWrapper wrapper = new RenderBodyWrapper() {
                gameObject = obj,
                spriteRenderer = obj.GetComponent<SpriteRenderer>(),
                data = data,
            };
            UpdateWrapper(wrapper);
            return wrapper;
        }

        private Color ParseColor(string hex) {
            // TODO: Parse the color
            return Color.white;
        }
    }
}