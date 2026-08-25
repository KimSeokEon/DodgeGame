using UnityEngine;
using UnityEngine.SceneManagement;

// =============================================================================
// MainMenuManager.cs
// -----------------------------------------------------------------------------
// 역할 : 메인 메뉴 화면의 버튼 클릭 이벤트 처리 (게임 시작 / 종료).
// 붙는 곳 : MainMenu.unity의 메뉴 매니저 오브젝트. 버튼의 OnClick()에서
//        OnStartButton() / GameExitButton()을 연결해서 쓴다.
// =============================================================================
public class MainMenuManager : MonoBehaviour
{
    // "시작" 버튼: PlayScene으로 이동
    public void OnStartButton()
    {
        SceneManager.LoadScene("PlayScene");
    }

    // "종료" 버튼: 에디터에서는 플레이 모드 종료, 빌드에서는 앱 종료
    public void GameExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
