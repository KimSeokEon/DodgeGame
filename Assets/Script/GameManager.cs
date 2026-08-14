using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    // Update is called once per frame
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
