using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public string submissionId;
    public string name;
    public string email;
    public int score;
    public int rank;
}

[Serializable]
public class PlayersList
{
    public List<PlayerData> players = new List<PlayerData>();
}

public class PlayerSession : MonoBehaviour
{
    public static PlayerSession Instance;

    public int Score { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string SubmissionId { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetScore(int score)
    {
        Score = score;
    }

    public void SetPlayerData(string first, string last, string email)
    {
        FirstName = first;
        LastName = last;
        Email = email;
    }

    public void ResetSession()
    {
        Score = 0;
        FirstName = "temp";
        LastName = "T";
        Email = null;
        SubmissionId = null;
    }

    public void SetSubmissionId(string submissionId)
    {
        SubmissionId = submissionId;
    }
}