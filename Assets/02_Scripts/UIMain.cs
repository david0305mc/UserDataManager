using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : MonoBehaviour
{
    [SerializeField] private Button increaseLevelButton;
    [SerializeField] private Button increaseRandomCoralLevelButton;
    [SerializeField] private TextMeshProUGUI descText;

    void Awake()
    {
        increaseLevelButton.onClick.AddListener(() =>
        {
            UserDataManager.Instance.SetStoneLevel(UserDataManager.Instance.UserData.StoneLevel.Value + 1);
            UpdateUI();
        });

        increaseRandomCoralLevelButton.onClick.AddListener(() =>
        {
            int randomCoralId = Random.Range(1, 6);
            if (UserDataManager.Instance.UserData.TryGetCoral(randomCoralId, out var coral))
            {
                UserDataManager.Instance.UpsertCoral(randomCoralId, coral.CoralLevel + 1);
            }
            else
            {
                UserDataManager.Instance.UpsertCoral(randomCoralId, 1);
            }
            UpdateUI();
        });
        UpdateUI();
    }
    private void UpdateUI()
    {
        descText.SetText($"Stone Level: {UserDataManager.Instance.UserData.StoneLevel.Value}\nCorals:\n");
        foreach (var coral in UserDataManager.Instance.UserData.Corals.Values)
        {
            descText.text += $" - Coral {coral.CoralId}: Level {coral.CoralLevel}\n";
        }
    }

}
