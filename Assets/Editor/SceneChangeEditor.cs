#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;


public class SceneChangeEditor : Editor
{
    // 씬 변환하는 기능 단축키로
    [MenuItem("SceneMove/Scene_Intro &1")]
    private static void IntroScene()
    {
        EditorSceneManager.OpenScene("Assets/01_Scenes/Intro.unity");
        Debug.Log("Move Intro Scene");
    }

    [MenuItem("SceneMove/Scene_Main &2")]
    private static void MainScene()
    {
        EditorSceneManager.OpenScene("Assets/01_Scenes/Main.unity");
        Debug.Log("Move Main Scene");
    }
    [MenuItem("SceneMove/Scene_UITest &3")]

    private static void UITestScene()
    {
        EditorSceneManager.OpenScene("Assets/01_Scenes/UIscene.unity");
        Debug.Log("Move UI Scene");
    }
}
#endif