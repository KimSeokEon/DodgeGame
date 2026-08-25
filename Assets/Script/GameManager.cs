using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// =============================================================================
// GameManager.cs   [사용 안 함 / 레거시]
// -----------------------------------------------------------------------------
// 역할 : 아주 초기 버전의 싱글플레이 게임 매니저 (생존 시간 표시 + 게임오버 + 최고기록).
// 상태 : 지금 실제로 쓰이는 건 이 스크립트가 아니라 GameManager2.cs (Assets/Script/Dodge2)다.
//        PlayScene.unity 안에 이 스크립트가 붙은 오브젝트가 남아있긴 하지만,
//        비활성화된 옛날 계층(DODGE1) 소속이라 지금은 실행되지 않는다.
//        이름이 새 버전과 똑같이 "GameManager"라서 헷갈리기 쉬우니 참고할 것.
// =============================================================================
public class GameManager : MonoBehaviour
{
    public GameObject GameoverText;

    public TMP_Text timeText;

    public TMP_Text RecordText;

    private float surviveTime;

    private bool isGameOver;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        surviveTime = 0;
        isGameOver = false;
    }

    // 살아있는 동안 시간을 카운트업해서 표시하고, 게임오버 후에는 R키로 재시작
    void Update()
    {
        if (!isGameOver)
        {
            surviveTime += Time.deltaTime;
            timeText.text = "Time: " + (int)surviveTime;

        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene("SampleScene");
            }
        }
    }

    // 플레이어가 죽었을 때 호출: 게임오버 텍스트를 켜고 최고기록을 갱신
    public void Endgame()
    {
        isGameOver = true;
        GameoverText.SetActive(true);

        float besttime = PlayerPrefs.GetFloat("BestTime");

        if (surviveTime > besttime)
        {
            besttime = surviveTime;
            PlayerPrefs.SetFloat("BestTime",besttime);

        }

        RecordText.text = "BestTime : " + (int)besttime;
    }
}
