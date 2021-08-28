using UnityEngine;

using UnityEngine.Events;
using System;
using System.Text;
using System.IO;

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
            byte[] outData = BodyPacket.EncodeBody(data);
            websocket.Send(outData);
            //string serializedString = JsonUtility.ToJson(data);
            //byte[] serialized = UTF8Encoding.UTF8.GetBytes(serializedString);
            //websocket.Send(serialized);
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
                //string msgString = Encoding.UTF8.GetString(msg);
                //FrameData data = JsonUtility.FromJson<FrameData>(msgString);
                FrameData frameData = BodyPacket.DecodeFrame(msg);
                lastBodyData = frameData.d;
                playerCount = frameData.p;
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

    public class FramePacket
    {
        public ushort P;
        public BodyPacket[] D;
    }

    public class BodyPacket
    {
        public static int BodyPacketBits = 16 + 32 + 32 + 32 + 32 + 32 + 32 + 8;
        public static int BodyPacketBytes = BodyPacketBits / 8;

        public ushort I;   // 16
        public float PX;   // 32
        public float PY;   // 32
        public float VX;   // 32
        public float VY;   // 32
        public float M;    // 32
        public float R;    // 32
        public byte T;     // 8

        public static FrameData DecodeFrame(byte[] data) {
            BodyPacket packet = new BodyPacket();
            ushort playerCount = 0;
            // - 4 maybe?
            int bodyCount = (data.Length - 2) / (BodyPacketBytes);
            BodyData[] bodies = new BodyData[bodyCount];
            

            using (MemoryStream stream = new MemoryStream(data)) {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII)) {
                    playerCount = reader.ReadUInt16();

                    for (int i = 0; i < bodyCount; i++) {
                        bodies[i] = new BodyData() {
                            i = reader.ReadUInt16(),
                            p = new float[2] { reader.ReadSingle(), reader.ReadSingle() },
                            v = new float[2] { reader.ReadSingle(), reader.ReadSingle() },
                            m = reader.ReadSingle(),
                            r = reader.ReadSingle(),
                            t = reader.ReadByte(),
                        };
                    }
                }
            }

            return new FrameData() {
                p = playerCount,
                d = bodies,
            };
        }

        public static byte[] EncodeBody(BodyData body) {
            byte[] data = new byte[BodyPacketBytes];
            using (MemoryStream stream = new MemoryStream(data, 0, BodyPacketBytes, true, true)) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(body.i);
                    writer.Write(body.p[0]);
                    writer.Write(body.p[1]);
                    writer.Write(body.v[0]);
                    writer.Write(body.v[1]);
                    writer.Write(body.m);
                    writer.Write(body.r);
                    writer.Write(body.t);

                    // int len = stream.Read(data, 0, BodyPacketBytes);
                }
            }

            return data;
        }
    }
}