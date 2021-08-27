using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class GSOInput : MonoBehaviour
    {
        public float baseRadius = 0.25f;
        public float baseMass = 1.0f;

        public GSOManager manager;
        public CursorCameraInput cameraInput;
        public LineRenderer velocityLine;

        public bool placing = false;
        public Vector2 placeStart = Vector2.zero;

        private void Start() {
            velocityLine.enabled = false;
            velocityLine.positionCount = 2;
        }

        private void Update() {
            if (Input.GetButtonDown("Fire1")) {
                placeStart = cameraInput.lastWorldMouse;
                placing = true;
                velocityLine.enabled = true;
            } else if (Input.GetButtonUp("Fire1") && placing) {
                BodyData data = new BodyData() {
                    i = "",
                    p = placeStart,
                    v = Vector2.ClampMagnitude((Vector2)cameraInput.lastWorldMouse - placeStart, manager.settings.maxVelocity),
                    r = baseRadius,
                    c = "#FFFFFF",
                    t = "",
                    m = baseMass,
                };

                manager.SpawnBody(data);

                placing = false;
                velocityLine.enabled = false;
            }

            if (placing) {
                velocityLine.SetPosition(0, placeStart);
                velocityLine.SetPosition(1, (Vector2)cameraInput.lastWorldMouse);
            }
        }
    }
}