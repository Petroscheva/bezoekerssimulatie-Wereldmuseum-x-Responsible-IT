using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
 
[System.Serializable]
public class ObjectBehaviorRule
{
    // We will use the "#" column from the CSV (1, 2, 3, ...)
    public string objectId;
    public float stopChance;       // 0–1 (e.g. 0.8 for 80%)
    public float minStopSeconds;   // average wait time in seconds
    public float maxStopSeconds;   // same as min (so it's always the average)
}
 
public class BehaviourDataLoader : MonoBehaviour
{
    [Header("CSV File (full observation table)")]
    public TextAsset csvFile;
 
    public List<ObjectBehaviorRule> rules = new List<ObjectBehaviorRule>();
 
    void Awake()
    {
        LoadCsv();
        PrintRulesToConsole();
    }
 
    void LoadCsv()
    {
        if (csvFile == null)
        {
            Debug.LogError("No CSV file assigned to BehaviourDataLoader.");
            return;
        }
 
        rules.Clear();
 
        // Split on both \n and \r, and remove purely empty lines
        string[] lines = csvFile.text.Split(
            new[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries
        );
 
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;
 
            // Stop at totals row
            if (line.StartsWith("TOTALS"))
                break;
 
            string[] values = line.Split(';');
            if (values.Length == 0)
                continue;
 
            // Only parse lines where first column is a numeric ID (1, 2, 3, ...)
            if (!int.TryParse(values[0].Trim(), out int numericId))
            {
                // header or non‑data line; ignore
                continue;
            }
 
            if (values.Length < 12)
            {
                Debug.LogWarning("Skipping data row with too few columns: " + line);
                continue;
            }
 
            string idRaw       = values[0].Trim();   // "#" column
            string stopRateRaw = values[10];         // "Stop Rate (%)"
            string waitRaw     = values[11];         // "Avg Wait per Person"
 
            float stopChance      = ParseStopChance(stopRateRaw);
            float avgWaitSeconds  = ParseSeconds(waitRaw);
 
            // Skip if cannot parse
            if (stopChance <= 0f || avgWaitSeconds <= 0f)
                continue;
 
            ObjectBehaviorRule rule = new ObjectBehaviorRule
            {
                objectId = idRaw,
                stopChance = stopChance,
                minStopSeconds = avgWaitSeconds,
                maxStopSeconds = avgWaitSeconds
            };
 
            rules.Add(rule);
        }
    }
 
    float ParseStopChance(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0f;
 
        // e.g. "80,0%" → "80.0" → 80 / 100 = 0.8
        value = value.Trim()
                     .Replace("%", "")
                     .Replace(" ", "")
                     .Replace(',', '.');
 
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float percent))
        {
            return percent / 100f;
        }
 
        Debug.LogWarning("Could not parse stop chance from: " + value);
        return 0f;
    }
 
    float ParseSeconds(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0f;
 
        // e.g. "234 sec" → "234" → 234
        value = value.Trim()
                     .Replace("sec", "")
                     .Replace("seconds", "")
                     .Replace(" ", "")
                     .Replace(',', '.');
 
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
        {
            return seconds;
        }
 
        Debug.LogWarning("Could not parse seconds from: " + value);
        return 0f;
    }
 
    void PrintRulesToConsole()
    {
        foreach (ObjectBehaviorRule rule in rules)
        {
            Debug.Log(
                "ObjectId(#): " + rule.objectId +
                " | Stop Chance: " + rule.stopChance +
                " | Wait: " + rule.minStopSeconds + "s"
            );
        }
    }
 
    public ObjectBehaviorRule GetRuleForObject(string objectId)
    {
        return rules.Find(rule => rule.objectId == objectId);
    }
 
    /// <summary>
    /// Returns true if NPC should stop, and outputs waitSeconds.
    /// </summary>
    public bool TryGetStopDecision(string objectId, out float waitSeconds)
    {
        waitSeconds = 0f;
 
        ObjectBehaviorRule rule = GetRuleForObject(objectId);
        if (rule == null)
        {
            Debug.LogWarning("No behavior rule found for objectId: " + objectId);
            return false;
        }
 
        if (Random.value <= rule.stopChance)
        {
            waitSeconds = Random.Range(rule.minStopSeconds, rule.maxStopSeconds);
 
            Debug.Log("NPC stopped at objectId " + objectId +
                      " for " + waitSeconds.ToString("F1") + " seconds.");
            return true;
        }
 
        Debug.Log("NPC walked past objectId " + objectId + ".");
        return false;
    }
}