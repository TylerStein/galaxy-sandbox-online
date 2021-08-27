using UnityEngine;
using System.Collections;

namespace GSO
{
    /// <summary>
    ///     Generic mouse input handler
    /// </summary>
    public class CursorCameraInput : MonoBehaviour
    {
        public Vector3 lastScreenMousePosition = Vector3.zero;
        public Vector3 lastWorldMouse = Vector3.zero;

        public Camera inputCamera;

        void Start() {
            lastScreenMousePosition = Vector3.zero;
            inputCamera = Camera.main;
        }

        void Update() {
            lastScreenMousePosition = Input.mousePosition;
            lastWorldMouse = inputCamera.ScreenToWorldPoint(Input.mousePosition);
            lastWorldMouse.z = 0;
        }
    }
}