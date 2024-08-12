#if UNITY_IOS

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerformanceMonitorController : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void StartTracking();

    [DllImport("__Internal")]
    private static extern IntPtr StopTracking();

    public Button startButton; // Reference to the Start button
    public Button stopButton; // Reference to the Stop button
    public Button spawnEntitiesButton; // Reference to the Spawn Entities button
    public TMP_Text resultText;

    private bool trackingStarted = false; // Flag to track if StartTracking has been called

    void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        stopButton.onClick.AddListener(OnStopButtonClicked);
        spawnEntitiesButton.gameObject.SetActive(false); // Hide the SpawnEntitiesButton initially
    }

    void OnStartButtonClicked()
    {
        Debug.Log("OnStartButtonClicked: Calling StartTracking");
        resultText.text = "Tracking, press stop to show results...";
        StartTracking();
        trackingStarted = true; // Set the flag to true when tracking starts

        // Hide the Start button and show the Spawn Entities button
        startButton.gameObject.SetActive(false);
        spawnEntitiesButton.gameObject.SetActive(true);
    }

    void OnStopButtonClicked()
    {
        if (!trackingStarted)
        {
            Debug.LogWarning("OnStopButtonClicked: Tracking has not been started yet.");
            resultText.text = "You must start tracking before stopping.";
            return; // Exit if tracking has not been started
        }

        Debug.Log("OnStopButtonClicked: Calling StopTracking");
        IntPtr ptr = StopTracking();
        if (ptr == IntPtr.Zero)
        {
            Debug.LogError("Received NULL pointer from StopTracking");
        }
        else
        {
            string usageData = Marshal.PtrToStringAnsi(ptr);

            // Parse and format the usage data
            usageData = FormatUsageData(usageData);

            Debug.Log($"Received usage data: {usageData}");
            resultText.text = usageData;
        }

        trackingStarted = false;

        // Hide the Spawn Entities button and show the Start button
        spawnEntitiesButton.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);
    }

    private string FormatUsageData(string data)
    {
        string[] lines = data.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("CPU:"))
            {
                lines[i] = FormatLine(lines[i], "CPU");
            }
            else if (lines[i].Contains("Memory:"))
            {
                lines[i] = FormatLine(lines[i], "Memory");
            }
            else if (lines[i].Contains("GPU:"))
            {
                lines[i] = FormatLine(lines[i], "GPU");
            }
        }
        return string.Join("\n", lines);
    }

    private string FormatLine(string line, string label)
{
    string[] parts = line.Split(' ');
    if (parts.Length >= 2 && double.TryParse(parts[1].TrimEnd('%', 'M', 'B'), out double value))
    {
        string unit = "";
        if (parts[1].EndsWith("%"))
        {
            unit = "%";
        }
        else if (parts[1].EndsWith("MB"))
        {
            unit = " MB";
        }

        parts[1] = value.ToString("F2") + unit; // Reattach the unit (e.g., %, MB) after formatting
    }
    return string.Join(" ", parts);
}

}

#endif
