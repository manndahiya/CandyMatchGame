using System.IO;
using UnityEngine;

public static class LeaderboardSaveSystem
{
    private static string Path => Application.persistentDataPath + "/leaderboard.json";

    public static void Save(PlayerData newPlayer)
    {
        PlayersList dataList = Load();

        dataList.players.Add(newPlayer);

        string json = JsonUtility.ToJson(dataList, true);
        File.WriteAllText(Path, json);
    }
    public static void SaveAll(PlayersList playersList)
    {
        string json = JsonUtility.ToJson(playersList, true);
        File.WriteAllText(Path, json);
    }


    public static PlayersList Load()
    {
        if (!File.Exists(Path))
            return new PlayersList();

        string json = File.ReadAllText(Path);

        if (string.IsNullOrWhiteSpace(json))
            return new PlayersList();

        PlayersList data = JsonUtility.FromJson<PlayersList>(json);

        if (data == null)
            data = new PlayersList();

        if (data.players == null)
            data.players = new System.Collections.Generic.List<PlayerData>();

        return data;
    }

    public static void Clear()
    {
        if (File.Exists(Path))
            File.Delete(Path);
    }
}