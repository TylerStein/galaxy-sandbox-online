using UnityEngine;

using UnityEngine.Events;
using System;
using System.Text;
using WebSocketSharp;
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

        public override void Activate() {
            if (websocket == null) {
                Debug.Log("Creating websocket");
                websocket = new WebSocket(wsAddress);
                websocket.WaitTime = TimeSpan.FromSeconds(30);
            }

            websocket.OnOpen += onEvent_wsOpen;
            websocket.OnClose += onEvent_wsClose;
            websocket.OnError += onEvent_wsError;
            websocket.OnMessage += onEvent_wsMessage;

            websocket.SslConfiguration.EnabledSslProtocols = sslProtocols;
            Connect();
        }

        public override void Deactivate() {
            if (websocket != null) {
                websocket.Close();
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
            websocket.Connect();
        }

        public override void ReActivate() {
            lastBodyData = new BodyData[0];
            playerCount = 0;
            bodyCount = 0;
            websocket.Close();
            StartCoroutine(TryReconnectRoutine());
        }

        public override void AddBody(BodyData data) {
            if (!IsReady()) return;

            string serializedString = JsonUtility.ToJson(data);
            byte[] serialized = UTF8Encoding.UTF8.GetBytes(serializedString);
            websocket.Send(serialized);
        }

        public override bool IsReady() {
            return websocket != null && websocket.IsAlive;
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

        private void onEvent_wsOpen(object sender, EventArgs args) {
            Debug.Log("Websocket Open");
            ConnectionEvent.Invoke();
        }

        private void onEvent_wsClose(object sender, EventArgs args) {
            Debug.Log("Websocket Closed");
            ConnectionEvent.Invoke();
            StartCoroutine(TryReconnectRoutine());
        }

        private void onEvent_wsError(object sender, ErrorEventArgs args) {
            Debug.LogError(args.Message);
            connErrCode = 500;
            connErrMessage = "Websocket error";
        }


        private void onEvent_wsMessage(object sender, MessageEventArgs args) {
            try {
                // BodyData[] data = JsonSerializer.Deserialize<BodyData[]>(args.Data);
                FrameData data = JsonUtility.FromJson<FrameData>(args.Data);
                lastBodyData = data.d;
                playerCount = data.p; // todo
                bodyCount = lastBodyData.Length;
            } catch (Exception e) {
                Debug.LogError(e);
                connErrCode = 500;
                connErrMessage = "Failed to parse websocket message";
            }
        }

        private IEnumerator TryReconnectRoutine() {
            yield return new WaitForSeconds(1);

            if (websocket == null || websocket.IsAlive) {
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
    }
}