using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : MonoBehaviour
{
    [SerializeField] private Button increaseLevelButton;
    [SerializeField] private Button increaseRandomCoralLevelButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button uidSeed;
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
        itemButton.onClick.AddListener(() =>
        {
            int randomItemId = Random.Range(1, 6);
            if (UserDataManager.Instance.UserData.TryGetItem(randomItemId, out var item))
            {
                UserDataManager.Instance.UpsertItem(randomItemId, item.ItemLevel + 1);
            }
            else
            {
                UserDataManager.Instance.UpsertItem(randomItemId, 1);
            }
            
            UpdateUI();
        });
        skillButton.onClick.AddListener(() =>
        {
            int randomSkillId = Random.Range(1, 6);
            if (UserDataManager.Instance.UserData.TryGetSkill(randomSkillId, out var skill))
            {
                UserDataManager.Instance.UpsertSkill(randomSkillId, skill.SkillLevel.Value + 1);
            }
            else
            {
                UserDataManager.Instance.UpsertSkill(randomSkillId, 1);
            }
            UpdateUI();
        });
        uidSeed.onClick.AddListener(() =>
        {
            UserDataManager.Instance.SetPlayerUid(UserDataManager.Instance.UserData.Player.Uid + 1);
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
        descText.text += "Skills:\n";
        foreach (var skill in UserDataManager.Instance.UserData.Skills.Values)
        {
            descText.text += $" - Skill {skill.SkillID}: Level {skill.SkillLevel}\n";
        }
        descText.text += "Items:\n";
        foreach (var item in UserDataManager.Instance.UserData.Items)
        {
            descText.text += $" - Item {item.ItemID}: Level {item.ItemLevel}\n";
        }
        descText.text += $"Player UID: {UserDataManager.Instance.UserData.Player.Uid}\n";
    }

}
