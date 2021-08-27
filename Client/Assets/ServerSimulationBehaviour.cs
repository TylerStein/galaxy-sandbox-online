using UnityEngine;

using System;
using System.Text;
using WebSocketSharp;
// using Utf8Json;

using System.Security.Authentication;
using UnityEngine.Serialization;

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

        private void Start() {
            if (websocket == null) {
                Debug.Log("Creating websocket");
                websocket = new WebSocket(wsAddress);
                websocket.WaitTime = TimeSpan.FromSeconds(30);
            }

            websocket.OnOpen += onEvent_wsOpen;
            websocket.OnClose += onEvent_wsClose;
            websocket.OnError += onEvent_wsError;
            websocket.OnMessage += onEvent_wsMessage;

            websocket.Connect();
        }

        public override void AddBody(BodyData data) {
            if (!IsConnected()) return;

            string serializedString = JsonUtility.ToJson(data);
            byte[] serialized = UTF8Encoding.UTF8.GetBytes(serializedString);
            websocket.Send(serialized);
        }

        public override bool IsConnected() {
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
        }

        private void onEvent_wsClose(object sender, EventArgs args) {
            Debug.Log("Websocket Closed");
        }

        private void onEvent_wsError(object sender, ErrorEventArgs args) {
            Debug.LogError(args.Message);
            connErrCode = 500;
            connErrMessage = "Websocket error";
        }


        private void onEvent_wsMessage(object sender, MessageEventArgs args) {
            try {
                // BodyData[] data = JsonSerializer.Deserialize<BodyData[]>(args.Data);
                BodyDataList data = JsonUtility.FromJson<BodyDataList>(args.Data);
                lastBodyData = data.d;
            } catch (Exception e) {
                Debug.LogError(e);
                connErrCode = 500;
                connErrMessage = "Failed to parse websocket message";
            }
        }
    }
}