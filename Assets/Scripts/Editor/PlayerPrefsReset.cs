using UnityEditor;
using UnityEngine;

public class PlayerPrefsEditorMenu
{
    // 에디터의 'Tools' 메뉴에 'Reset PlayerPrefs' 항목을 추가합니다.
    [MenuItem("Tools/Reset PlayerPrefs")]
    private static void ResetPlayerPrefs()
    {
        if (EditorUtility.DisplayDialog("PlayerPrefs 초기화", 
            "정말로 모든 PlayerPrefs 데이터를 삭제하시겠습니까? 이 작업은 되돌릴 수 없습니다.", 
            "네, 삭제합니다", "아니요"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("모든 PlayerPrefs 데이터가 초기화되었습니다.");
        }
    }
}