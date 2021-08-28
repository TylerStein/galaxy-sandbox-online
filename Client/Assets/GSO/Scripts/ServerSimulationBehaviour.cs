using UnityEngine;

using UnityEngine.Events;
using System;
using System.Text;

using HybridWebSocket;
// using Utf8Json;

using System.Security.Authentication;
using System.Collections.Generic;
using UnityEngine.Serialization;
using System.Collections;

namespace GSO
{
    public class ServerSimulationBehaviour : SimulationBehaviour
    {
        [SerializeField] public SslProtocols sslProtocols = SslProtocols.Default | SslProtocols.Tls12;
        public string wsAddress = "ws://localhost:8080/ws";
        public GSOManager manager;

        private BodyData[] lastBodyData = new BodyData[0];

        private int connErrCode = 0;
        private string connErrMessage = "";

        private WebSocket websocket;
        private int playerCount = 1;
        private int bodyCount = 0;

        private bool pendingConnectNotify = false;
        private bool pendingTryReconnect = false;

        public void Update() {
            if (pendingConnectNotify) {
                pendingConnectNotify = false;
                ConnectionEvent.Invoke();
            }

            if (pendingTryReconnect) {
                pendingTryReconnect = false;
                StartCoroutine(TryReconnectRoutine());
            }
        }

        public void OnDestroy() {
            if (websocket != null && websocket.GetState() == WebSocketState.Open) {
                websocket.Close();
            }
        }

        public override void Activate() {
            if (websocket == null) {
                Debug.Log("Creating websocket");
                websocket = WebSocketFactory.CreateInstance(wsAddress, sslProtocols);
               // websocket.WaitTime = TimeSpan.FromSeconds(30);
            }

            websocket.OnOpen += onEvent_wsOpen;
            websocket.OnClose += onEvent_wsClose;
            websocket.OnError += onEvent_wsError;
            websocket.OnMessage += onEvent_wsMessage;

            Connect();
        }

        public override void Deactivate() {
            if (websocket != null) {
                if (websocket.GetState() == WebSocketState.Open || websocket.GetState() == WebSocketState.Connecting) {
                    try {
                        websocket.Close();
                    } catch (Exception e) {
                        Debug.LogError(e);
                    }
                }
                websocket.OnOpen -= onEvent_wsOpen;
                websocket.OnClose -= onEvent_wsClose;
                websocket.OnError -= onEvent_wsError;
                websocket.OnMessage -= onEvent_wsMessage;
                websocket = null;
            }
            lastBodyData = new BodyData[0];
            playerCount = 1;
            bodyCount = 0;
            ConnectionEvent.Invoke();
        }

        public void Connect() {
            if (websocket != null) {
                WebSocketState state = websocket.GetState();
                Debug.Log("Connect Websocket with state " + state.ToString());
                if (state != WebSocketState.Open) {
                    websocket.Connect();
                }
            }
        }

        public override void ReActivate() {
            lastBodyData = new BodyData[0];
            playerCount = 0;
            bodyCount = 0;
            if (websocket != null && websocket.GetState() == WebSocketState.Closed) {
                Connect();
            } else {
                StartCoroutine(TryReconnectRoutine());
            }
        }

        public override void AddBody(BodyData data) {
            if (!IsReady()) return;

            string serializedString = JsonUtility.ToJson(data);
            byte[] serialized = UTF8Encoding.UTF8.GetBytes(serializedString);
            websocket.Send(serialized);
        }

        public override bool IsReady() {
            return websocket != null && websocket.GetState() == WebSocketState.Open;
        }

        public override void ReadBodies(out BodyData[] bodies) {
            bodies = lastBodyData;
        }

        public override bool TryGetConnectionError(out string message, out int code) {
            if (connErrCode != 0) {
                code = connErrCode;
                message = connErrMessage;
                return true;
            } else {
                code = 0;
                message = "";
                return false;
            }
        }

        private void onEvent_wsOpen() {
            Debug.Log("Websocket Open");
            pendingConnectNotify = true;
        }

        private void onEvent_wsClose(WebSocketCloseCode code) {
            Debug.Log("Websocket Closed with Code " + code);
            pendingConnectNotify = true;
            pendingTryReconnect = true;
        }

        private void onEvent_wsError(string errMsg) {
            Debug.LogError(errMsg);
            connErrCode = 500;
            connErrMessage = "Websocket error";
        }


        private void onEvent_wsMessage(byte[] msg) {
            try {
                // BodyData[] data = JsonSerializer.Deserialize<BodyData[]>(args.Data);
                string msgString = Encoding.UTF8.GetString(msg);
                FrameData data = JsonUtility.FromJson<FrameData>(msgString);
                lastBodyData = data.d;
                playerCount = data.p;
                bodyCount = lastBodyData.Length;
            } catch (Exception e) {
                Debug.LogError(e);
                connErrCode = 500;
                connErrMessage = "Failed to parse websocket message";
            }
        }

        private IEnumerator TryReconnectRoutine() {
            yield return new WaitForSeconds(1);

            if (websocket == null || websocket.GetState() == WebSocketState.Open) {
                Debug.Log("Skipping retry, already connected or websocket null");
            } else {
                Debug.Log("Retrying connection");
                Connect();
            }
        }

        public override int GetPlayerCount() {
            return playerCount;
        }

        public override int GetObjectCount() {
            return bodyCount;
        }
        public void OnDrawGizmos() {
            Gizmos.color = Color.green;
            foreach (BodyData body in lastBodyData) {
                Gizmos.DrawWireSphere(body.pvec, body.r);
            }
        }
    }
}