using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameBoard gameBoard;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ResultCardController resultCard;

    [Header("Scoring")]
    [SerializeField] private int simpleMatchScore = 5;
    [SerializeField] private int specialMatchScore = 10;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        gameBoard.OnMatchScored += HandleMatchScored;
        gameTimer.OnTimerExpired += HandleTimerExpired;
    }

    private void OnDisable()
    {
        gameBoard.OnMatchScored -= HandleMatchScored;
        gameTimer.OnTimerExpired -= HandleTimerExpired;
    }

    private void Start()
    {
        IsGameOver = false;
        scoreManager.ResetScore();
        gameBoard.SetInputLocked(false);
        gameTimer.StartTimer();
    }

    private void HandleMatchScored(bool isSpecial)
    {
        if (IsGameOver) return;
        scoreManager.AddScore(isSpecial ? specialMatchScore : simpleMatchScore);
    }

    private void HandleTimerExpired()
    {
        if (IsGameOver) return;
        EndGame();
    }

    private void EndGame()
    {
        IsGameOver = true;

        gameBoard.SetInputLocked(true);
        gameTimer.StopTimer();

        int finalScore = scoreManager.CurrentScore;

        if (PlayerSession.Instance != null)
            PlayerSession.Instance.SetScore(finalScore);
        else
            Debug.LogWarning("PlayerSession.Instance is null — final score won't carry over to the form scene.");

        resultCard.Show(finalScore);
    }
}