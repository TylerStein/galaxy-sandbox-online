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
        public Dictionary<ushort, RenderBodyWrapper> wrappers = new Dictionary<ushort, RenderBodyWrapper>();
        private Dictionary<ushort, BodyData> wrappersBuffer = new Dictionary<ushort, BodyData>();
        private Dictionary<ushort, int> wrappersCounter = new Dictionary<ushort, int>();
        private List<ushort> removeBuffer = new List<ushort>();

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

            foreach (ushort key in removeBuffer) {
                wrappers.Remove(key);
            }

            foreach (KeyValuePair<ushort, int> kvp in wrappersCounter) {
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
            wrapper.gameObject.transform.localScale = new Vector3(wrapper.data.r * 2f, wrapper.data.r * 2f, 1f);
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

            wrapper.spriteRenderer.sprite = manager.textureManager.GetSpriteAtIndex(data.t);
            UpdateWrapper(wrapper);
            return wrapper;
        }

        private Color ParseColor(string hex) {
            // TODO: Parse the color
            return Color.white;
        }
    }
}