using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("State Panels")]
    public GameObject titlePanel;
    public GameObject levelSelectPanel;
    public GameObject inLevelPanel;
    public GameObject levelCompletePanel;
    public GameObject levelFailedPanel;

    [Header("Level Select")]
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;

    [Header("Text References")]
    public TMP_Text titleText;
    public TMP_Text inLevelNameText;
    public TMP_Text completeLevelNameText;
    public TMP_Text failedLevelNameText;

    [Header("Status")]
    public TMP_Text statusText;

    [Header("Buttons")]
    public Button startButton;
    public Button leaveButton;
    public Button nextButton;
    public Button completeLeaveButton;
    public Button failRetryButton;
    public Button failLeaveButton;

    private LevelManager levelManager;
    private int pendingLevelIndex;

    private void Start()
    {
        Instance = this;
        levelManager = FindObjectOfType<LevelManager>();
        BindButtons();
        ShowTitle();
    }

    public void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    public void ClearStatus()
    {
        if (statusText != null) statusText.text = "";
    }

    private void BindButtons()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (leaveButton != null) leaveButton.onClick.AddListener(OnLeaveClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (completeLeaveButton != null) completeLeaveButton.onClick.AddListener(OnLeaveClicked);
        if (failRetryButton != null) failRetryButton.onClick.AddListener(OnRetryClicked);
        if (failLeaveButton != null) failLeaveButton.onClick.AddListener(OnLeaveClicked);
    }

    private void HideAll()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (inLevelPanel != null) inLevelPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (levelFailedPanel != null) levelFailedPanel.SetActive(false);
    }

    public void ShowTitle()
    {
        HideAll();
        if (titlePanel != null) titlePanel.SetActive(true);
        if (titleText != null) titleText.text = "Coding Blocks";
    }

    public void ShowLevelSelect()
    {
        HideAll();
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);

        foreach (Transform child in levelButtonContainer)
            Destroy(child.gameObject);

        if (levelButtonPrefab != null)
        {
            foreach (var level in levelManager.levels)
            {
                var btnObj = Instantiate(levelButtonPrefab, levelButtonContainer);
                var text = btnObj.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = $"Level {level.levelNumber}";
                var btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    int index = levelManager.levels.IndexOf(level);
                    btn.onClick.AddListener(() => OnLevelSelected(index));
                }
            }
        }
    }

    public void ShowInLevel()
    {
        HideAll();
        if (inLevelPanel != null) inLevelPanel.SetActive(true);
        UpdateLevelName();
    }

    public void ShowLevelComplete()
    {
        HideAll();
        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);
        if (completeLevelNameText != null)
            completeLevelNameText.text = $"Level {levelManager.currentLevelData.levelNumber} Complete!";
    }

    public void ShowLevelFailed()
    {
        HideAll();
        if (levelFailedPanel != null) levelFailedPanel.SetActive(true);
        if (failedLevelNameText != null)
            failedLevelNameText.text = $"Wrong Order!";
    }

    private void UpdateLevelName()
    {
        var name = $"Level {levelManager.currentLevelData.levelNumber}";
        if (inLevelNameText != null) inLevelNameText.text = name;
    }

    private void OnStartClicked()
    {
        ShowLevelSelect();
    }

    private void OnLeaveClicked()
    {
        levelManager.StopLevel();
        ShowLevelSelect();
    }

    private void OnNextClicked()
    {
        pendingLevelIndex = levelManager.currentLevelIndex + 1;
        if (pendingLevelIndex >= levelManager.levels.Count)
        {
            ShowLevelSelect();
            return;
        }
        levelManager.LoadLevelByIndex(pendingLevelIndex);
        ShowInLevel();
    }

    private void OnRetryClicked()
    {
        levelManager.ReloadLevel();
        ShowInLevel();
    }

    private void OnLevelSelected(int index)
    {
        pendingLevelIndex = index;
        levelManager.LoadLevelByIndex(index);
        ShowInLevel();
        inLevelNameText.text = $"Level {levelManager.currentLevelData.levelNumber}";
    }
}
