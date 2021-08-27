using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class LocalSimulationBehaviour : SimulationBehaviour
    {
        public GSOManager manager;

        public float distanceBounds = 100f;
        public float gravitationalConstant = 1f;
        public float maxDistance = 1000f;
        public int rounds = 3;
        public List<BodyData> physicsBodies = new List<BodyData>();
        public HashSet<BodyData> toRemove = new HashSet<BodyData>();

        private Stack<string> ids = new Stack<string>();

        private void Awake() {
            for (int i = 0; i < 256; i++) {
                string str = i.ToString();
                int zeroes = 16 - str.Length;
                ids.Push(str.PadLeft(zeroes, '0'));
            }
        }

        public override void AddBody(BodyData data) {
            data.i = ids.Pop();
            physicsBodies.Add(data);
        }

        public override bool IsConnected() {
            return true;
        }

        public override void ReadBodies(out BodyData[] bodies) {
            bodies = physicsBodies.ToArray();
        }

        public override bool TryGetConnectionError(out string message, out int code) {
            message = default;
            code = default;
            return false;
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
                        physicsBodies[i].pvec,
                        physicsBodies[i].m,
                        physicsBodies[i].pvec,
                        physicsBodies[j].m
                    );

                    forces = Vector2.ClampMagnitude(forces, manager.settings.maxVelocity);
                }

                // Add total forces
                physicsBodies[i].vvec = physicsBodies[i].vvec + (forces / physicsBodies[i].m) * deltaTime;
            }

            toRemove.Clear();

            // Update positions
            for (int i = 0; i < physicsBodies.Count; i++) {
                physicsBodies[i].pvec = physicsBodies[i].pvec + physicsBodies[i].vvec * deltaTime;

                float delta = Vector2.Distance(Vector2.zero, physicsBodies[i].pvec);
                if (delta > distanceBounds) {
                    toRemove.Add(physicsBodies[i]);
                    //physicsBodies[i].p = Vector2.zero;
                    //physicsBodies[i].v = Vector2.zero;
                }
            }

            // Solve constraints
            for (int i = 0; i < physicsBodies.Count; i++) {
                if (toRemove.Contains(physicsBodies[i])) continue;

                for (int j = 0; j < physicsBodies.Count; j++) {
                    if (i == j) continue;
                    if (toRemove.Contains(physicsBodies[j])) continue;

                    Vector2 diff = physicsBodies[i].pvec - physicsBodies[j].pvec;
                    if (physicsBodies[i].r > physicsBodies[j].r) {
                        if (diff.magnitude < physicsBodies[i].r) {
                            // i bigger
                            toRemove.Add(physicsBodies[j]);
                            Absorb(physicsBodies[i], physicsBodies[j]);
                        }
                    } else {
                        if (diff.magnitude < physicsBodies[j].r) {
                            // j bigger
                            toRemove.Add(physicsBodies[i]);
                            Absorb(physicsBodies[j], physicsBodies[i]);
                        }
                    }
                }
            }

            foreach (BodyData body in toRemove) {
                physicsBodies.Remove(body);
            }
        }

        public void Absorb(BodyData self, BodyData other) {
            self.r += other.r * 0.5f;
            self.m += other.m * 0.5f;
            self.vvec = self.vvec + (other.vvec * 0.5f);
        }

        public Vector2 CalculateForces(float g, Vector2 p1, float m1, Vector2 p2, float m2) {
            float d = Mathf.Sqrt(Mathf.Pow(p1.x - p2.x, 2) + Mathf.Pow(p1.y - p2.y, 2));
            Vector2 accel = new Vector2((p2.x - p1.x) / d, (p2.y - p1.y) / d);
            accel *= g * m2 / (d * d);
            return accel;
        }

        public void RemoveBody(BodyData body) {
            ids.Push(body.i);
            physicsBodies.Remove(body);
        }
    }
}