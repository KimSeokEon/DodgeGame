using UnityEngine;

public class Heart : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2f, 0);
    private Transform cam;
    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    
    void LateUpdate()
    {
        // transform.rotation = cam.rotation;
        transform.LookAt(cam);
    }
}
