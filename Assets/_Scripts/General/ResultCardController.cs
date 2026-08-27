using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultCardController : MonoBehaviour
{
    [Header("Root object to show/hide (this GameObject or a child of it)")]
    [SerializeField] private GameObject resultCardObject;

    [Header("References")]
    [SerializeField] private RectTransform cardRectTransform;
    [SerializeField] private Image backgroundImage; // used for the fade-in
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button continueButton;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private Vector2 offscreenOffset = new Vector2(0f, -1000f);
    [SerializeField] private string formSceneName = "FormScene";

    private Vector2 centerAnchoredPos;

    private void Awake()
    {
        centerAnchoredPos = cardRectTransform.anchoredPosition;

        // Keep it inactive until EndGame calls Show().
        resultCardObject.SetActive(false);

        continueButton.onClick.AddListener(GoToFormScene);
    }

    public void Show(int finalScore)
    {
        finalScoreText.text = finalScore.ToString();

        // Reset to starting state before animating in.
        cardRectTransform.anchoredPosition = centerAnchoredPos + offscreenOffset;

        if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;
        }

        resultCardObject.SetActive(true);

        DOTween.Sequence()
            .Join(cardRectTransform.DOAnchorPos(centerAnchoredPos, animationDuration).SetEase(Ease.OutBack))
            .Join(backgroundImage != null ? backgroundImage.DOFade(1f, animationDuration) : null);
    }

    private void GoToFormScene()
    {
        SceneManager.LoadScene(formSceneName);
    }
}