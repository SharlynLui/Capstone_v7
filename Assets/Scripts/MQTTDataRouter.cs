using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;
using TaiChi;

public class MQTTDataRouter : MonoBehaviour
{
    [Header("MQTT Settings — must match MQTTVisualizer")]
    public string brokerAddress = "54.79.62.237";
    public int brokerPort = 8883;
    public string topic = "inference/result";

    private MqttClient _client;

    public bool useFakeData = false;

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

    void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Message);
        if (useFakeData) return;

        try
        {
            JObject json = JObject.Parse(message);

            // 1. Navigate into the nested JSON structure
            var scoresObj = json["scores"];
            if (scoresObj == null) return;

            var parts = scoresObj["part"];
            if (parts == null)
            {
                Debug.LogWarning("[MQTTDataRouter] 'part' key not found under 'scores'.");
                return;
            }

            // 2. Fill the dictionary
            var partScores = new Dictionary<string, float>();

            // Read overall directly from JSON
            var overallToken = scoresObj["overall"];
            if (overallToken != null && float.TryParse(overallToken.ToString(), out float overallScore))
            {
                partScores["overall"] = overallScore;
            }
            else
            {
                Debug.LogWarning("[MQTTDataRouter] 'overall' key not found under 'scores'.");
            }

            // Read part scores
            foreach (var part in parts.Children<JProperty>())
            {
                if (float.TryParse(part.Value.ToString(), out float score))
                {
                    partScores[part.Name] = score;
                }
            }

            // 3. Dispatch to EventBus (Main Thread)
            MainThreadDispatcher.Enqueue(() =>
            {
                ScoreEventBus.Publish(partScores);
                Debug.Log($"[MQTTDataRouter] Published {partScores.Count} scores. Overall: {partScores.GetValueOrDefault("overall", -1f):F3}");
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