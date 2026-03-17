using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Newtonsoft.Json.Linq;
using TaiChi;

/// <summary>
/// Connects to the same MQTT broker as MQTTVisualizer
/// and routes part scores to OrbManager and ScoreReader.
/// Does NOT touch groupmate's MQTTVisualizer code.
/// </summary>
public class MQTTDataRouter : MonoBehaviour
{
    [Header("MQTT Settings — must match MQTTVisualizer")]
    public string brokerAddress = "54.79.62.237";
    public int brokerPort = 8883;
    public string topic = "inference/result";

    private MqttClient _client;

    public bool useFakeData = false; // Toggle this in the Inspector

    void Start()
    {
        TextAsset certAsset = Resources.Load<TextAsset>("Certs/ca");
        if (certAsset == null)
        {
            Debug.LogError("[MQTTDataRouter] CA cert not found at Resources/Certs/ca!");
            return;
        }

        X509Certificate caCert = new X509Certificate(certAsset.bytes);

        System.Net.ServicePointManager.ServerCertificateValidationCallback =
            (sender, cert, chain, errors) => true;

        _client = new MqttClient(
            brokerAddress, brokerPort, true,
            caCert, null,
            MqttSslProtocols.TLSv1_2,
            (sender, cert, chain, errors) => true
        );

        _client.MqttMsgPublishReceived += OnMessageReceived;

        try
        {
            // Use different clientId from MQTTVisualizer so both can connect
            string clientId = "UnityDataRouter_" + Guid.NewGuid().ToString().Substring(0, 8);
            _client.Connect(clientId);

            if (_client.IsConnected)
            {
                _client.Subscribe(
                    new string[] { topic },
                    new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE }
                );
                Debug.Log("[MQTTDataRouter] Connected and subscribed to: " + topic);
            }
            else
            {
                Debug.LogError("[MQTTDataRouter] Connection failed!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[MQTTDataRouter] Exception: " + e.Message);
        }
    }

    // Background thread
    void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Message);
        if (useFakeData) return;

        try
        {
            JObject json = JObject.Parse(message);

            // Inside OnMessageReceived in MQTTDataRouter.cs

            // 1. Navigate into the nested JSON structure
            var scoresObj = json["scores"];
            if (scoresObj == null) return;

            var parts = scoresObj["part"]; // Look for "part" inside "scores"
            if (parts == null)
            {
                Debug.LogWarning("[MQTTDataRouter] 'part' key not found under 'scores'.");
                return;
            }

            // 2. Clear and fill the dictionary
            var partScores = new Dictionary<string, float>();
            foreach (var part in parts.Children<JProperty>())
            {
                if (float.TryParse(part.Value.ToString(), out float score))
                {
                    partScores[part.Name] = score;
                }
            }

            // 3. Dispatch to your EventBus (Main Thread)
            MainThreadDispatcher.Enqueue(() =>
            {
                ScoreEventBus.Publish(partScores);
                // Add this debug log to confirm the orbs SHOULD be receiving data
                Debug.Log($"[MQTTDataRouter] Successfully published {partScores.Count} scores to EventBus.");
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("[MQTTDataRouter] JSON parse error: " + ex.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (_client != null && _client.IsConnected)
        {
            _client.Disconnect();
            Debug.Log("[MQTTDataRouter] Disconnected.");
        }
    }
}