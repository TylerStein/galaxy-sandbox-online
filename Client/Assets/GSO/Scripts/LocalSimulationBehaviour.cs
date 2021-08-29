using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class LocalSimulationBehaviour : SimulationBehaviour
    {
        public GSOManager manager;
        public List<BodyData> physicsBodies = new List<BodyData>();
        public HashSet<BodyData> toRemove = new HashSet<BodyData>();

        private Stack<ushort> ids = new Stack<ushort>();

        public override void Activate() {
            toRemove.Clear();
            physicsBodies.Clear();
            ids.Clear();
            for (int i = 0; i < 256; i++) {
                ids.Push((ushort)i);
            }
            ConnectionEvent.Invoke();
        }

        public override void Deactivate() {
            toRemove.Clear();
            physicsBodies.Clear();
            ids.Clear();
            ConnectionEvent.Invoke();
        }

        public override void ReActivate() {
            Activate();
        }

        public override void AddBody(BodyData data) {
            data.i = ids.Pop();
            data.m = CalculateMass(data.r, manager.settings.massSizeMultiplier);
            physicsBodies.Add(data);
        }

        public override bool IsReady() {
            return ids.Count > 0;
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
            UpdatePhysicsBodies(Time.deltaTime * manager.settings.timeScaleMultiplier);
        }

        public void UpdatePhysicsBodies(float deltaTime) {
            // Apply Forces
            for (int i = 0; i < physicsBodies.Count; i++) {
                Vector2 forces = Vector2.zero;

                for (int j = 0; j < physicsBodies.Count; j++) {
                    if (i == j) continue;
                    forces += CalculateForces2(
                        manager.settings.gravityConstant,
                        physicsBodies[i].pvec,
                        physicsBodies[i].m,
                        physicsBodies[j].pvec,
                        physicsBodies[j].m
                    );
                }


                forces = Vector2.ClampMagnitude(forces, manager.settings.maxVelocity);

                // Add total forces
                //float m2 = 1f / Mathf.Pow(physicsBodies[i].m, 2f);
                //float m2 = Mathf.Pow(physicsBodies[i].m, 2f);
                float m2 = 1f / Mathf.Pow(1 + physicsBodies[i].m, 2f);
                physicsBodies[i].vvec = Vector2.ClampMagnitude(physicsBodies[i].vvec + (forces * m2) * deltaTime, manager.settings.maxVelocity);
            }

            toRemove.Clear();

            // Update positions
            for (int i = 0; i < physicsBodies.Count; i++) {
                physicsBodies[i].pvec = physicsBodies[i].pvec + physicsBodies[i].vvec * deltaTime;

                float delta = Vector2.Distance(Vector2.zero, physicsBodies[i].pvec);
                if (delta > manager.settings.bounds) {
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

                    float diff = Vector2.Distance(physicsBodies[i].pvec, physicsBodies[j].pvec);
                    if (diff < (physicsBodies[i].r + physicsBodies[j].r)) {
                        if (physicsBodies[i].r > physicsBodies[j].r) {
                            toRemove.Add(physicsBodies[j]);
                            Absorb(physicsBodies[i], physicsBodies[j]);
                        } else {
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

        public static float CalculateMass(float radius, float massSizeMultiplier) {
            return Mathf.Pow(1f + radius, massSizeMultiplier);
        }

        public void Absorb(BodyData self, BodyData other) {
            self.r += other.r * manager.settings.absorbRate;
            self.m = CalculateMass(self.r, manager.settings.massSizeMultiplier);
            // self.vvec = self.vvec + (other.vvec * 0.5f);
        }

        public Vector2 CalculateForces(float g, Vector2 p1, float m1, Vector2 p2, float m2) {
            float d = Mathf.Sqrt(Mathf.Pow(p1.x - p2.x, 2) + Mathf.Pow(p1.y - p2.y, 2));
            Vector2 accel = new Vector2((p2.x - p1.x) / d, (p2.y - p1.y) / d);
            accel *= g * m2 / (d * d);
            return accel;
        }

        public Vector2 CalculateForces2(float g, Vector2 p1, float m1, Vector2 p2, float m2) {
            Vector2 r = p2 - p1;
            float d = Mathf.Max(r.magnitude, 0.5f);
            r.Normalize();
            float s = (g * m1 * m2) / (d * d);
            return r * s;
        }

        public void RemoveBody(BodyData body) {
            ids.Push(body.i);
            physicsBodies.Remove(body);
        }

        public override int GetPlayerCount() {
            return 1;
        }

        public override int GetObjectCount() {
            return physicsBodies.Count;
        }

        public void OnDrawGizmos() {
            Gizmos.color = Color.green;
            foreach (BodyData body in physicsBodies) {
                Gizmos.DrawWireSphere(body.pvec, body.r);
            }
        }
    }
}