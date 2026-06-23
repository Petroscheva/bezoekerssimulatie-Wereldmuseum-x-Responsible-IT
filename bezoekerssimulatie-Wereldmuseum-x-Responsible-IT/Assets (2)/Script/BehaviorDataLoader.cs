using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[System.Serializable]
public class ObjectBehaviorRule
{
    public string objectId;
    public float stopChance;
    public float minStopSeconds;
    public float maxStopSeconds;
}

public class BehaviorDataLoader : MonoBehaviour
{
    [Header("CSV File")]
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
            Debug.LogError("No CSV file assigned to BehaviorDataLoader.");
            return;
        }

        rules.Clear();

        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] values = line.Split(';');

            if (values.Length < 4)
            {
                Debug.LogWarning("Skipping invalid CSV line: " + line);
                continue;
            }

            ObjectBehaviorRule rule = new ObjectBehaviorRule
            {
                objectId = values[0].Trim(),
                stopChance = ParseFloat(values[1]),
                minStopSeconds = ParseFloat(values[2]),
                maxStopSeconds = ParseFloat(values[3])
            };

            rules.Add(rule);
        }
    }

    float ParseFloat(string value)
    {
        value = value.Trim().Replace(',', '.');

        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    void PrintRulesToConsole()
    {
        foreach (ObjectBehaviorRule rule in rules)
        {
            Debug.Log(
                "Object: " + rule.objectId +
                " | Stop Chance: " + rule.stopChance +
                " | Min Stop: " + rule.minStopSeconds +
                " | Max Stop: " + rule.maxStopSeconds
            );
        }
    }

    public ObjectBehaviorRule GetRuleForObject(string objectId)
    {
        return rules.Find(rule => rule.objectId == objectId);
    }

    public bool ShouldStopAtObject(string objectId)
    {
        ObjectBehaviorRule rule = GetRuleForObject(objectId);

        if (rule == null)
        {
            Debug.LogWarning("No behavior rule found for object: " + objectId);
            return false;
        }

        bool shouldStop = Random.value <= rule.stopChance;

        if (shouldStop)
        {
            float stopTime = Random.Range(rule.minStopSeconds, rule.maxStopSeconds);

            Debug.Log(
                "NPC stopped at " + objectId +
                " for " + stopTime.ToString("F1") +
                " seconds."
            );
        }
        else
        {
            Debug.Log("NPC walked past " + objectId + ".");
        }

        return shouldStop;
    }
}