using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class GSOInput : MonoBehaviour
    {
        public float baseRadius = 0.25f;
        public float baseMass = 1.0f;
        public float moveSpeed = 6f;
        public float zoomSpeed = 12f;
        public float minOrthoSize = 5f;
        public float maxOrthoSize = 60f;
        public float maxDistance = 100f;
        public float scaleSpeed = 3.0f;
        public float scaleStep = 0.10f;
        public float minScale = 0.15f;
        public float maxScale = 2.0f;
        public float zoomTowardsStrength = 1f;

        public float cameraZ = -10;

        public new Camera camera;
        public GSOManager manager;
        public CursorCameraInput cameraInput;
        public LineRenderer velocityLine;

        public bool placing = false;
        public Vector2 placeStart = Vector2.zero;

        private GSOGhost ghost;

        private void Start() {
            if (!camera) camera = Camera.main;

            velocityLine.enabled = false;
            velocityLine.positionCount = 2;

            GameObject ghostObject = new GameObject("Ghost", new System.Type[] { typeof(SpriteRenderer) });
            ghost = new GSOGhost(ghostObject);
            ghost.Hide();
        }

        private void Update() {
            // 1 -> 10 scale
            float distanceScale = (camera.orthographicSize / maxOrthoSize) * 10f;

            bool mouseActive = cameraInput.lastScreenMousePosition.x > 0 && cameraInput.lastScreenMousePosition.x < Screen.width
                    && cameraInput.lastScreenMousePosition.y > 0 && cameraInput.lastScreenMousePosition.y < Screen.height;

            if (Input.GetButtonDown("Spawn") && mouseActive) {
                placeStart = cameraInput.lastWorldMouse;
                placing = true;
                velocityLine.enabled = true;
                // ghost.radius = 0.15f;
                ghost.spriteRenderer.enabled = true;
                ghost.spriteIndex = manager.textureManager.GetRandomSpriteIndex();
                ghost.spriteRenderer.sprite = manager.textureManager.GetSpriteAtIndex(ghost.spriteIndex);
            } else if (Input.GetButtonUp("Spawn") && placing) {
                Vector2 vel = Vector2.ClampMagnitude((Vector2)cameraInput.lastWorldMouse - placeStart, manager.settings.maxVelocity);
                BodyData data = new BodyData() {
                    i = "",
                    p = new float[] { placeStart.x, placeStart.y },
                    v = new float[] { vel.x, vel.y },
                    r = ghost.radius,
                    c = "#FFFFFF",
                    t = ghost.spriteIndex,
                    m = ghost.radius * 10f,
                };

                manager.SpawnBody(data);

                placing = false;
                velocityLine.enabled = false;
                ghost.spriteRenderer.enabled = false;
            }

            if (placing) {
                if (Input.GetButtonDown("Increment")) {
                    // next sprite
                    ghost.spriteIndex = manager.textureManager.IncrementSpriteIndex(ghost.spriteIndex);
                    ghost.spriteRenderer.sprite = manager.textureManager.GetSpriteAtIndex(ghost.spriteIndex);
                } else if (Input.GetButtonDown("Decrement")) {
                    ghost.spriteIndex = manager.textureManager.DecrementSpriteIndex(ghost.spriteIndex);
                    ghost.spriteRenderer.sprite = manager.textureManager.GetSpriteAtIndex(ghost.spriteIndex);
                }

                if (Input.GetButtonDown("AddSize")) {
                    ghost.radius += scaleStep;
                } else if (Input.GetButtonDown("RemoveSize")) {
                    ghost.radius -= scaleStep;
                } else {
                    ghost.radius += Input.mouseScrollDelta.y * Time.deltaTime * scaleSpeed;
                }

                velocityLine.SetPosition(0, placeStart);
                velocityLine.SetPosition(1, (Vector2)cameraInput.lastWorldMouse);
                ghost.radius = Mathf.Clamp(ghost.radius, minScale, maxScale);
                ghost.Update(placeStart);
            } else {
                // Handle move/zoom
                Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                MoveCamera(move * Time.deltaTime * moveSpeed * distanceScale);

                if (mouseActive) {
                    float scrollInputA = Input.GetAxis("Zoom");
                    float scrollInputB = Input.mouseScrollDelta.y;
                    if (Mathf.Abs(scrollInputA) > Mathf.Abs(scrollInputB)) {
                        ZoomCamera(scrollInputA * Time.deltaTime * -zoomSpeed * distanceScale, cameraInput.lastWorldMouse);
                    } else {
                        ZoomCamera(scrollInputB * Time.deltaTime * -zoomSpeed * distanceScale, cameraInput.lastWorldMouse);
                    }
                }
            }
        }

        public void ZoomCamera(float zoom, Vector2 target) {
            if (zoom == 0) return;
            float newZoom = Mathf.Clamp(camera.orthographicSize + zoom, minOrthoSize, maxOrthoSize);

            if (newZoom > minOrthoSize && newZoom < maxOrthoSize) {
                Vector2 dir = ((Vector2)camera.transform.position - target).normalized * zoomTowardsStrength * zoom;
                camera.transform.position += (Vector3)dir;

                camera.orthographicSize = newZoom;
            }
        }

        public void MoveCamera(Vector3 move) {
            Vector3 newPosition = camera.transform.position + move;
            Vector2 clampedXY = Vector2.ClampMagnitude(newPosition, maxDistance);
            newPosition.x = clampedXY.x;
            newPosition.y = clampedXY.y;
            camera.transform.position = newPosition;
        }
    }

    class GSOGhost
    {
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer spriteRenderer;
        public float radius = 1f;
        public int spriteIndex = 0;

        public GSOGhost(GameObject obj) {
            gameObject = obj;
            transform = gameObject.transform;
            spriteIndex = 0;
            radius = 0.15f;
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        }

        public void Hide() {
            spriteRenderer.enabled = false;
        }

        public void Show() {
            spriteRenderer.enabled = true;
        }

        public void Update(Vector3 position) {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.60f);
            transform.position = position;
            transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        }
    }
}