using UnityEngine;
using DG.Tweening;

public class GameoverAnim : MonoBehaviour
{
    private Vector3 targetPos = new Vector3(0, 5, 0);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.DOMove(targetPos, 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
