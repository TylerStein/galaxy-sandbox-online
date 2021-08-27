using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class GSOBodyPool : MonoBehaviour
    {
        public int poolSize => pool.Count;

        public GameObject prefab;
        public Transform root;

        private int initialPoolSize = 100;
        private int poolSizeIncrement = 50;
        private Stack<GameObject> pool = new Stack<GameObject>();

        private void Awake() {
            if (root == null) root = transform;
            SpawnPoolObjects(initialPoolSize);
        }

        private void SpawnPoolObjects(int count) {
            for (int i = 0; i < count; i++) {
                SpawnPoolObject();
            }
        }

        private void SpawnPoolObject() {
            GameObject obj = Instantiate(prefab, root);
            obj.transform.position = root.position;
            obj.SetActive(false);
            pool.Push(obj);
        }

        public void RecycleToPool(GameObject obj) {
            obj.transform.parent = root;
            obj.transform.position = root.position;
            obj.SetActive(false);
            pool.Push(obj);
        }

        public GameObject GetFromPool() {
            if (poolSize == 0) {
                SpawnPoolObjects(poolSizeIncrement);
            }

            GameObject obj = pool.Pop();
            obj.SetActive(true);
            return obj;
        }
    }
}