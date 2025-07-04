using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;     // The object the camera follows (your cube)
    public Vector3 offset = new Vector3(0, 5, -10); // Camera position relative to the cube

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target);
        }
    }
}