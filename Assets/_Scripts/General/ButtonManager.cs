using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [Header("Form References")]
    [SerializeField] private TMP_InputField firstNameInput;
    [SerializeField] private TMP_InputField lastNameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2f;

    private Coroutine messageCoroutine;

    public void StartGame()
    {
        //AudioManager.Instance.PlayButtonClick();
        SceneManager.LoadScene("MainGameScene");
    }

    public void SubmitButton()
    {
        // Run form logic only in FormScene
        if (SceneManager.GetActiveScene().name != "FormScene")
            return;

        string firstName = firstNameInput.text.Trim();
        string lastName = lastNameInput.text.Trim();
        string email = emailInput.text.Trim();

        // Empty fields check
        if (string.IsNullOrEmpty(firstName) ||
            string.IsNullOrEmpty(lastName) ||
            string.IsNullOrEmpty(email))
        {
            ShowMessage("Please fill all fields");
            return;
        }

        // Name validation (letters only, minimum 1 chars)
        if (!Regex.IsMatch(firstName, @"^[A-Za-z]{1,}$"))
        {
            ShowMessage("Please fill first name correctly");
            return;
        }

        if (!Regex.IsMatch(lastName, @"^[A-Za-z]{1,}$"))
        {
            ShowMessage("Please fill last name correctly");
            return;
        }

        // Basic email validation
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ShowMessage("Please fill valid email");
            return;
        }

        // Success
        messageText.text = "";
        messageText.gameObject.SetActive(false);

        SavePlayerData(firstName, lastName, email);

        SceneManager.LoadScene("LeaderboardScene");
    }

    private static void SavePlayerData(string firstName, string lastName, string email)
    {
        PlayerSession.Instance.SetPlayerData(firstName, lastName, email);  // SET DATA TO CURRENT SESSION

        string submissionId = Guid.NewGuid().ToString();

        PlayerSession.Instance.SetSubmissionId(submissionId);

        PlayerData player = new PlayerData
        {
            submissionId = submissionId,
            name = firstName + " " + lastName,
            email = email,
            score = PlayerSession.Instance.Score
        };

        LeaderboardSaveSystem.Save(player);
    }

    public void SkipButton()
    {
        SceneManager.LoadScene("StartScene");
    }

    private void ShowMessage(string message)
    {
        messageText.text = message;

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);

        messageText.text = "";
        messageCoroutine = null;
    }
}