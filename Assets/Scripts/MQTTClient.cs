using UnityEngine;
using UnityEngine.UI;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Newtonsoft.Json.Linq;

public class MQTTVisualizer : MonoBehaviour
{
    private MqttClient client; // MQTT client instance

    [Header("MQTT Settings")]
    public string brokerAddress = "54.79.62.237";
    public int brokerPort = 8883; // TLS port
    public string topic = "inference/result";

    [Header("UI Settings")]
    public Text mqttMessageText; // UI Text to display messages

    void Start()
    {
        Debug.Log("Starting MQTT Secure Connection...");

        // Load CA certificate from Resources folder
        TextAsset certAsset = Resources.Load<TextAsset>("Certs/ca");
        if (certAsset == null)
        {
            Debug.LogError("CA certificate not found in Resources/Certs/ca.bytes!");
            return;
        }

        X509Certificate caCert = new X509Certificate(certAsset.bytes);
        Debug.Log("Loaded CA Certificate: " + caCert.Subject);

        // Optional: bypass SSL errors (self-signed/demo)
        System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
        {
            Debug.LogWarning("Bypassing SSL Error: " + sslPolicyErrors);
            return true;
        };

        // Initialize MQTT client
        client = new MqttClient(
            brokerAddress,
            brokerPort,
            true,
            caCert,
            null,
            MqttSslProtocols.TLSv1_2,
            RemoteCertificateValidationCallback
        );

        client.MqttMsgPublishReceived += OnMessageReceived;

        try
        {
            string clientId = Guid.NewGuid().ToString();
            Debug.Log("Connecting to broker: " + brokerAddress + "...");
            client.Connect(clientId);

            if (client.IsConnected)
            {
                Debug.Log("Successfully connected to broker!");
                client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
                Debug.Log("Subscribed to topic: " + topic);
            }
            else
            {
                Debug.LogError("MQTT connection failed!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("MQTT Connection Exception: " + e.Message);
            if (e.InnerException != null) Debug.LogError("Inner Exception: " + e.InnerException.Message);
        }
    }

    // Custom SSL validation callback
    bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true; // Accept all certs (for self-signed/demo)
    }

    // MQTT message callback — runs on background thread
    void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Message);
        Debug.Log("Topic: " + e.Topic + " | Message: " + message);

        try
        {
            JObject json = JObject.Parse(message);

            string seq = json["seq"]?.ToString() ?? "-1";
            string overallScore = json["scores"]?["overall"]?.ToString() ?? "N/A";

            // Build per-part scores string
            StringBuilder partInfo = new StringBuilder();
            var parts = json["scores"]?["part"];
            if (parts != null)
            {
                foreach (var part in parts.Children<JProperty>())
                {
                    partInfo.AppendLine($"   {part.Name}: {part.Value}");
                }
            }

            string formatted = $"Seq: {seq}\nOverall Score: {overallScore}\nPart Scores:\n{partInfo}";

            // Hand off to main thread — UI can only be touched from main thread
            MainThreadDispatcher.Enqueue(() =>
            {
                if (mqttMessageText != null)
                    mqttMessageText.text = formatted;
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse JSON: " + ex.Message);

            // Fallback: show raw message on main thread
            string raw = message;
            MainThreadDispatcher.Enqueue(() =>
            {
                if (mqttMessageText != null)
                    mqttMessageText.text = raw;
            });
        }
    }

    // Update() removed — MainThreadDispatcher.Update() handles UI dispatch.

    void OnApplicationQuit()
    {
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
            Debug.Log("Disconnected from MQTT broker.");
        }
    }
}