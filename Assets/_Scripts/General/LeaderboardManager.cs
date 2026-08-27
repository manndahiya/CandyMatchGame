using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject entryPrefab;

    [Header("Paging")]
    [SerializeField] private int entriesPerPage = 10;
    [SerializeField] private float pageDuration = 5f;

    private List<GameObject> spawnedEntries = new();

    private List<PlayerData> rankedPlayers;
    private PlayerData currentPlayer;

    private bool leavingScene;

    private void Start()
    {
        PlayersList data = LeaderboardSaveSystem.Load();

        if (data == null || data.players.Count == 0)
            return;

        BuildRanks(data.players);

        currentPlayer = FindCurrentPlayer();

        if (rankedPlayers.Count <= entriesPerPage)
        {
            ShowPage(0, rankedPlayers.Count);

            StartCoroutine(ReturnAfterDelay());
        }
        else
        {
            StartCoroutine(PageRoutine());
        }
    }

    private void Update()
    {
        if (leavingScene)
            return;

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            ReturnToStart();
        }
    }

    private void ReturnToStart()
    {
        if (leavingScene)
            return;

        leavingScene = true;

        SceneManager.LoadScene("StartScene");
    }

    private IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(pageDuration);

        if (!leavingScene)
            ReturnToStart();
    }

    private void BuildRanks(List<PlayerData> players)
    {
        rankedPlayers = players
            .OrderByDescending(x => x.score)
            .ToList();

        if (rankedPlayers.Count == 0)
            return;

        int currentRank = 1;

        rankedPlayers[0].rank = currentRank;

        for (int i = 1; i < rankedPlayers.Count; i++)
        {
            if (rankedPlayers[i].score != rankedPlayers[i - 1].score)
            {
                currentRank++;
            }

            rankedPlayers[i].rank = currentRank;
        }

        PlayersList updatedList = new PlayersList();
        updatedList.players = rankedPlayers;

        LeaderboardSaveSystem.SaveAll(updatedList);
    }

    private PlayerData FindCurrentPlayer()
    {
        return rankedPlayers.FirstOrDefault(x =>
            x.submissionId == PlayerSession.Instance.SubmissionId);
    }

    private IEnumerator PageRoutine()
    {
        int playerIndex = rankedPlayers.FindIndex(
            x => x.submissionId == currentPlayer?.submissionId);

        int playerPageStart =
            (playerIndex / entriesPerPage) * entriesPerPage;

        bool playerOnTopPage = playerPageStart == 0;

        // Show player's page first
        ShowPage(
            playerPageStart,
            Mathf.Min(
                entriesPerPage,
                rankedPlayers.Count - playerPageStart));

        yield return new WaitForSeconds(pageDuration);

        if (leavingScene)
            yield break;

        // If player already in Top 10, we're done
        if (playerOnTopPage)
        {
            ReturnToStart();
            yield break;
        }

        // Show Top 10
        ShowPage(
            0,
            Mathf.Min(entriesPerPage, rankedPlayers.Count));

        yield return new WaitForSeconds(pageDuration);

        if (leavingScene)
            yield break;

        ReturnToStart();
    }

    private void ShowPage(int startIndex, int count)
    {
        ClearEntries();

        int endIndex = Mathf.Min(
            startIndex + count,
            rankedPlayers.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            CreateEntry(rankedPlayers[i]);
        }
    }

    private void CreateEntry(PlayerData player)
    {
        GameObject entry =
            Instantiate(entryPrefab, contentParent);

        spawnedEntries.Add(entry);

        TMP_Text rankText =
            entry.transform.GetChild(0).GetComponent<TMP_Text>();

        TMP_Text nameText =
            entry.transform.GetChild(1).GetComponent<TMP_Text>();

        TMP_Text scoreText =
            entry.transform.GetChild(2).GetComponent<TMP_Text>();

        Image background =
            entry.transform.GetChild(3).GetComponent<Image>();

        rankText.text = player.rank.ToString();

        nameText.text =
            $"{player.name}";

        scoreText.text = player.score.ToString();

        if (currentPlayer != null &&
    player.submissionId == currentPlayer.submissionId)
        {
            StartCoroutine(
                HighlightCurrentPlayer(
                    background,
                    entry.transform));
        }
    }

    private IEnumerator HighlightCurrentPlayer(
      Image background,
      Transform root)
    {
        Vector3 baseScale = root.localScale;

        while (background != null && root != null)
        {
            float t =
                (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;

            background.color =
                Color.Lerp(
                    Color.white,
                    Color.yellow,
                    t);

            float pulse =
                1f + Mathf.Sin(Time.time * 4f) * 0.01f;

            root.localScale =
                baseScale * pulse;

            yield return null;
        }
    }

    private void ClearEntries()
    {
        foreach (GameObject entry in spawnedEntries)
        {
            Destroy(entry);
        }

        spawnedEntries.Clear();
    }
}