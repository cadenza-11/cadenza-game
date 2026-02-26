using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Cadenza;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public static class SaveSystem
{
    // Save
    public static bool SaveFileExists => File.Exists(TeamFilePath);
    private const string TeamJSONFileName = "team.json";
    private static readonly string TeamFilePath = Path.Combine(Application.persistentDataPath, TeamJSONFileName);
    public static event Action TeamFileDeleted;
    public static event Action TeamFileCreated;

    // Results
    private const string ResultsJSONFileName = "results.json";
    private static readonly System.Array scoreClasses = System.Enum.GetValues(typeof(ScoreClass));
    private static readonly System.Array scoreClassNames = System.Enum.GetNames(typeof(ScoreClass));
    private static readonly string ResultsFilePath = Path.Combine(Application.persistentDataPath, ResultsJSONFileName);
    public static event Action ResultsFileDeleted;
    public static event Action ResultsFileCreated;

    #region Team

    /// <summary>
    /// Serializes and saves an instance of Team to the persistent JSON file.
    /// </summary>
    /// <returns>Whether the file was successfully saved to file.</return>
    public static bool SaveTeamToFile(Team team)
    {
        if (team == null)
        {
            Debug.LogWarning("Attempting to save a null Team instance to file.");
            return true;
        }

        JObject root = new()
        {
            ["Name"] = team.Name,
            ["ID"] = team.Guid
        };

        // Save to file.
        string json = root.ToString(Formatting.Indented);
        File.WriteAllText(TeamFilePath, json);

        if (SaveFileExists)
        {
            Debug.Log($"Saved team to {TeamFilePath}.");
            TeamFileCreated?.Invoke();
            return true;
        }
        else
        {
            Debug.LogWarning("Failed to save team to file.");
            return false;
        }
    }


    /// <summary>
    /// Reads from file the stored teams on this device.
    /// </summary>
    /// <returns>A Team instance if successfully read from file, otherwise null.</returns>
    public static Team GetTeamFromFile()
    {
        if (!SaveFileExists)
            return null;

        try
        {
            string json = File.ReadAllText(TeamFilePath);
            var root = JObject.Parse(json);

            return new Team()
            {
                Name = root["Name"].Value<string>(),
                Guid = Guid.Parse(root["ID"].Value<string>())
            };
        }
        catch (JsonReaderException e)
        {
            Debug.LogWarning($"Failed to get team from file: {e}");
            return null;
        }
    }

    /// <summary>
    /// Removes the persistent file used to store saved teams.
    /// </summary>
    /// <returns>Whether the file was deleted successfully.</returns>
    public static bool DeleteTeamFile()
    {
        if (SaveFileExists)
        {
            File.Delete(TeamFilePath);
            Debug.Log($"Successfully deleted file at path {TeamFilePath}.");
            TeamFileDeleted?.Invoke();
        }
        else
        {
            Debug.Log($"No file exists at {TeamFilePath}. Skipping deletion.");
        }
        return true;
    }

    #endregion
    #region Results

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
        ResultsFileCreated?.Invoke();
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
            ResultsFileDeleted?.Invoke();
        }
        else
        {
            Debug.Log($"No file exists at {ResultsFilePath}. Skipping deletion.");
        }
        return true;
    }

    #endregion
    #region Private methods

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
            ["HighestStreak"] = results.HighestStreak,
            ["TeamScores"] = teamScores,
            ["PlayerScores"] = playerScores,
        };
    }

    /// <summary>
    /// Reconstructs a Results instance from a JSON token. Returns null if JSON parsing fails.
    /// </summary>
    private static Results FromJSON(this JToken root)
    {
        Results results = new()
        {
            // Set metadata.
            Timestamp = root["Metadata"]["Timestamp"]?.Value<DateTime>() ?? DateTime.MinValue,
            TeamName = root["Team"]["Name"]?.Value<string>() ?? string.Empty,
            HighestStreak = root["HighestStreak"]?.Value<int>() ?? 0
        };

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
            string playerName = scoreToken["Name"].Value<string>();
            JObject counts = (JObject)scoreToken["Counts"];

            // Get score counts.
            for (int i = 0; i < scoreClasses.Length; i++)
            {
                string scoreName = (string)scoreClassNames.GetValue(i);
                ScoreClass scoreClass = (ScoreClass)scoreClasses.GetValue(i);

                if (!counts.TryGetValue(scoreName, out JToken count))
                    continue;

                for (int j = 0; j < count.Value<int>(); j++)
                    results.AddPlayerScore(playerName, scoreClass);
            }
        }

        return results;
    }

    #endregion
}
