using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// using Cysharp.Threading.Tasks;

public class UIIntro : MonoBehaviour
{
    [SerializeField] private Button _buttonStart;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        Application.targetFrameRate = 120;
        // _buttonStart.onClick.AddListener(() => StartGame().Forget());
    }

    // private async UniTask StartGame()
    // {
    //     await DataManager.Instance.LoadDataAsync();
    //     await DataManager.Instance.LoadConfigTable();
    //     string uid = PlayerPrefs.GetString("uid", "1000");
    //     await UserDataManager.Instance.LoadOrCreateAsync(uid);
    //     PlayerPrefs.SetString("uid", uid);
        
    //     var mainSceneAsync = SceneManager.LoadSceneAsync(GameDefine.MainScene);
    //     await UniTask.WaitUntil(() => mainSceneAsync.isDone);
    // }

}
