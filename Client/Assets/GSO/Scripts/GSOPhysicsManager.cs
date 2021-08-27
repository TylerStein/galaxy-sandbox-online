using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class GSOPhysicsManager : MonoBehaviour
    {
        public float distanceBounds = 100f;
        public float maxVelocity = 10f;
        public float gravitationalConstant = 1f;
        public float maxDistance = 1000f;
        public int rounds = 3;
        public List<GSOPhysicsBody> physicsBodies;
        public HashSet<GSOPhysicsBody> toRemove;

        private void Awake() {
            physicsBodies = new List<GSOPhysicsBody>(FindObjectsOfType<GSOPhysicsBody>());
            toRemove = new HashSet<GSOPhysicsBody>();
        }

        private void Update() {
            UpdatePhysicsBodies(Time.deltaTime);
        }

        public void UpdatePhysicsBodies(float deltaTime) {
            // Apply Forces
            for (int i = 0; i < physicsBodies.Count; i++) {
                Vector2 forces = Vector2.zero;

                for (int j = 0; j < physicsBodies.Count; j++) {
                    if (i == j) continue;
                    forces += CalculateForces(
                        gravitationalConstant,
                        physicsBodies[i].transform.position,
                        physicsBodies[i].mass,
                        physicsBodies[j].transform.position,
                        physicsBodies[j].mass
                    );

                    forces = Vector2.ClampMagnitude(forces, maxVelocity);
                }

                // Add total forces
                physicsBodies[i].velocity += (forces / physicsBodies[i].mass) * deltaTime;
            }

            // Update positions
            for (int i = 0; i < physicsBodies.Count; i++) {
                physicsBodies[i].transform.position += (Vector3)physicsBodies[i].velocity * deltaTime;

                float delta = Vector2.Distance(Vector2.zero, physicsBodies[i].transform.position);
                if (delta > distanceBounds) {
                    Debug.Log("Reset PhysicsBody [" + i + "]");
                    physicsBodies[i].transform.position = Vector2.zero;
                    physicsBodies[i].velocity = Vector2.zero; 
                }
            }

            toRemove.Clear();

            // Solve constraints
            for (int i = 0; i < physicsBodies.Count; i++) {
                if (toRemove.Contains(physicsBodies[i])) continue;

                for (int j = 0; j < physicsBodies.Count; j++) {
                    if (i == j) continue;
                    if (toRemove.Contains(physicsBodies[j])) continue;

                    Vector2 diff = physicsBodies[i].transform.position - physicsBodies[j].transform.position;
                    if (physicsBodies[i].radius > physicsBodies[j].radius) {
                        if (diff.magnitude < physicsBodies[i].radius) {
                            // i bigger
                            toRemove.Add(physicsBodies[j]);
                            physicsBodies[i].Absorb(physicsBodies[j]);
                        }
                    } else {
                        if (diff.magnitude < physicsBodies[j].radius) {
                            // j bigger
                            toRemove.Add(physicsBodies[i]);
                            physicsBodies[j].Absorb(physicsBodies[i]);
                        }
                    }
                }
            }

            foreach (GSOPhysicsBody body in toRemove) {
                physicsBodies.Remove(body);
                Destroy(body.gameObject);
            }
        }

        public Vector2 CalculateForces(float g, Vector2 p1, float m1, Vector2 p2, float m2) {
            float d = Mathf.Sqrt(Mathf.Pow(p1.x - p2.x, 2) + Mathf.Pow(p1.y - p2.y, 2));
            Vector2 accel = new Vector2((p2.x - p1.x) / d, (p2.y - p1.y) / d);
            accel *= g * m2 / (d * d);
            return accel;
        }

        public void AddBody(GSOPhysicsBody body) {
            physicsBodies.Add(body);
        }

        public void RemoveBody(GSOPhysicsBody body) {
            physicsBodies.Remove(body);
        }
    }
}