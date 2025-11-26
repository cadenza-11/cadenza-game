using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Cadenza;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public static class SaveSystem
{
    private const string ResultsJSONFileName = "results.json";
    private static readonly System.Array scoreClasses = System.Enum.GetValues(typeof(ScoreClass));
    private static readonly System.Array scoreClassNames = System.Enum.GetNames(typeof(ScoreClass));

    /// <summary>
    /// Serializes and saves an instance of Results to the persistent JSON file.
    /// </summary>
    /// <returns>Whether the file was successfully saved to file.</return>
    public static bool SaveRunToFile(Results results)
    {
        if (results == null)
        {
            Debug.LogWarning("Attempting to save an empty Results instance to file.");
            return true;
        }

        // Save to file.
        string json = results.ToJSON();
        var path = Path.Combine(Application.persistentDataPath, ResultsJSONFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);

        Debug.Log($"Saved results to {path}.");
        return true;
    }

    public static bool GetPreviousRuns(out Results[] results)
    {
        results = null;

        // Read from path.
        string path = Path.Combine(Application.persistentDataPath, ResultsJSONFileName);
        if (!Directory.Exists(path))
            return false;

        string json = File.ReadAllText(path);
        results = FromJSON(json);
        return results != null;
    }

    private static string ToJSON(this Results results)
    {
        var playerResults = results.PlayerResults;
        var teamResults = results.TeamResults;

        // Save team member information.
        var team = new JArray();
        {
            foreach (var id in playerResults.Keys)
            {
                team.Add(new JObject
                {
                    ["ID"] = id
                });
            }
        }

        // Save team scores.
        var teamScores = new JObject();
        {
            for (int i = 0; i < scoreClasses.Length; i++)
            {
                string scoreName = (string)scoreClassNames.GetValue(i);
                ScoreClass scoreClass = (ScoreClass)scoreClasses.GetValue(i);
                teamScores[scoreName] = teamResults.GetCount(scoreClass);
            }
            teamScores["Total"] = teamResults.Hits;
        }

        // Save player scores.
        var playerScores = new JArray();
        {
            foreach ((var id, var result) in playerResults)
            {
                // Save counts for each score class.
                var counts = new JObject();
                for (int i = 0; i < scoreClasses.Length; i++)
                {
                    string scoreName = (string)scoreClassNames.GetValue(i);
                    ScoreClass scoreClass = (ScoreClass)scoreClasses.GetValue(i);
                    counts[scoreName] = result.GetCount(scoreClass);
                }
                counts["Total"] = result.Hits;

                // Create score object.
                var scoreObj = new JObject
                {
                    ["ID"] = id,
                    ["Counts"] = counts,
                    ["Score"] = result.ScoreTotal,
                };
                playerScores.Add(scoreObj);
            }
        }

        var root = new JObject
        {
            ["Timestamp"] = DateTime.UtcNow,
            ["Team"] = team,
            ["TeamScores"] = teamScores,
            ["PlayerScores"] = playerScores,
        };

        string json = root.ToString(Formatting.Indented);
        return json;
    }

    private static Results[] FromJSON(string jsonString)
    {
        List<Results> resultsList;
        try
        {
            JArray root = JArray.Parse(jsonString);
            resultsList = new List<Results>();
            foreach (var result in root)
                resultsList.Add(GetResults(result));
        }
        catch (JsonReaderException e)
        {
            Debug.LogWarning(e);
            return null;
        }

        return resultsList.ToArray();
    }

    private static Results GetResults(JToken root)
    {
        Results results = new();

        // Get metadata.
        DateTime dt = root["Timestamp"].Value<DateTime>();

        // Add team scores.
        var teamScores = root["TeamScores"];
        {
            JObject counts = (JObject)teamScores["Counts"];

            // Get score counts.
            for (int i = 0; i < scoreClasses.Length; i++)
            {
                string scoreName = (string)scoreClassNames.GetValue(i);
                ScoreClass scoreClass = (ScoreClass)scoreClasses.GetValue(i);

                if (!counts.TryGetValue(scoreName, out JToken count))
                    continue;

                for (int j = 0; j < count.Value<int>(); j++)
                    results.AddTeamScore(scoreClass);
            }
        }

        // Add player scores.
        foreach (var scoreToken in root["PlayerScores"])
        {
            // Get score fields.
            int playerID = scoreToken["ID"].Value<int>();
            JObject counts = (JObject)scoreToken["Counts"];

            // Get score counts.
            for (int i = 0; i < scoreClasses.Length; i++)
            {
                string scoreName = (string)scoreClassNames.GetValue(i);
                ScoreClass scoreClass = (ScoreClass)scoreClasses.GetValue(i);

                if (!counts.TryGetValue(scoreName, out JToken count))
                    continue;

                for (int j = 0; j < count.Value<int>(); j++)
                    results.AddPlayerScore(playerID, scoreClass);
            }
        }

        return results;
    }
}
