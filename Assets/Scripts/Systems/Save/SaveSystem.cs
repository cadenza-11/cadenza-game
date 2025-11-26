using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Cadenza;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public static class SaveSystem
{
    private const string ResultsJSONFileName = "results.json";
    private static readonly System.Array scoreClasses = System.Enum.GetValues(typeof(ScoreClass));
    private static readonly System.Array scoreClassNames = System.Enum.GetNames(typeof(ScoreClass));
    private static readonly string ResultsFilePath = Path.Combine(Application.persistentDataPath, ResultsJSONFileName);

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

        // Append this to previous runs.
        JArray previousRuns = GetPreviousRunsAsJSON();
        previousRuns.Add(results.ToJSON());

        // Save to file.
        string json = previousRuns.ToString(Formatting.Indented);
        File.WriteAllText(ResultsFilePath, json);

        Debug.Log($"Saved results to {ResultsFilePath}.");
        return true;
    }

    /// <summary>
    /// Reads from file the stored runs on this device.
    /// </summary>
    /// <param name="results">The list to populate with Results objects.</param>
    /// <returns>Whether the file was read successfully.</returns>
    public static bool GetPreviousRuns(out Results[] results)
    {
        var root = GetPreviousRunsAsJSON();
        if (root == null)
        {
            results = null;
            return false;
        }

        var resultsList = new List<Results>();
        foreach (var result in root)
        {
            var resultJSON = result.FromJSON();

            // Only store results that are successfully converted from JSON.
            if (resultJSON != null)
                resultsList.Add(resultJSON);
        }

        results = resultsList.ToArray();
        return true;
    }

    /// <summary>
    /// Removes the persistent file used to store previous runs.
    /// </summary>
    /// <returns>Whether the file was deleted successfully.</returns>
    public static bool DeletePreviousRuns()
    {
        if (File.Exists(ResultsFilePath))
        {
            File.Delete(ResultsFilePath);
            Debug.Log($"Successfully deleted file at path {ResultsFilePath}.");
        }
        else
        {
            Debug.Log($"No file exists at {ResultsFilePath}. Skipping deletion.");
        }
        return true;
    }

    private static JArray GetPreviousRunsAsJSON()
    {
        // Get file.
        JArray root;
        if (File.Exists(ResultsFilePath))
        {
            try
            {
                string json = File.ReadAllText(ResultsFilePath);
                root = JArray.Parse(json);
            }
            catch (JsonReaderException e)
            {
                Debug.LogWarning($"Failed to get runs from file: {e}");
                root = null;
            }
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultsFilePath));
            root = new JArray();
        }
        return root;
    }

    /// <summary>
    /// Constructs a JSON object from a Results instance.
    /// </summary>
    private static JToken ToJSON(this Results results)
    {
        var playerResults = results.PlayerResults;
        var teamResults = results.TeamResults;

        // Save metadata.
        var metadata = new JObject()
        {
            ["Timestamp"] = DateTime.UtcNow,
        };

        // Save team member information.
        var members = new JArray();
        foreach (var id in playerResults.Keys)
        {
            members.Add(new JObject
            {
                ["ID"] = id
            });
        }
        var team = new JObject
        {
            ["Name"] = results.TeamName,
            ["Members"] = members,
        };

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

        return new JObject
        {
            ["Metadata"] = metadata,
            ["Team"] = team,
            ["TeamScores"] = teamScores,
            ["PlayerScores"] = playerScores,
        };
    }

    /// <summary>
    /// Reconstructs a Results instance from a JSON token. Returns null if JSON parsing fails.
    /// </summary>
    private static Results FromJSON(this JToken root)
    {
        Results results = new();

        // Set metadata.
        var metadata = root["Metadata"];
        results.Timestamp = metadata["Timestamp"].Value<DateTime>();
        results.TeamName = root["Team"]["Name"].Value<string>();

        // Add team scores.
        var teamScores = (JObject)root["TeamScores"];
        {
            // Get score counts.
            for (int i = 0; i < scoreClasses.Length; i++)
            {
                string scoreName = (string)scoreClassNames.GetValue(i);
                ScoreClass scoreClass = (ScoreClass)scoreClasses.GetValue(i);

                if (!teamScores.TryGetValue(scoreName, out JToken count))
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
