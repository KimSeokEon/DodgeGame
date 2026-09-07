using UnityEngine;

public class Footstepsound : MonoBehaviour
{
    public AudioClip walkLoopClip;      // "Walking On Grass"
    public float runPitch  = 1.15f;
    public float walkPitch = 0.9f;
    [Range(0f, 1f)] public float volume = 0.7f;

    AudioSource src;
    Animator anim;

    void Awake()
    {
        src  = GetComponent<AudioSource>();
        anim = transform.root.GetComponentInChildren<Animator>();

        src.clip        = walkLoopClip;
        src.loop        = true;
        src.playOnAwake = false;
        src.volume      = volume;
    }

    void Update()
    {
        if (anim == null || src == null) return;

        bool moving  = anim.GetBool("isRun");
        bool walking = anim.GetBool("isWalk");

        if (moving)
        {
            src.pitch = walking ? walkPitch : runPitch;

            if (!src.isPlaying)
            {
                // 걸을 때마다 클립의 다른 지점에서 시작 → 매번 다르게 들림
                src.time = Random.Range(0f, Mathf.Max(0.1f, src.clip.length - 1f));
                src.Play();
            }
        }
        else
        {
            if (src.isPlaying) src.Stop();
        }
    }
}