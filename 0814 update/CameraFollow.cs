using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;               

    [Header("1st-Person Offset (Local)")]
    public Vector3 localOffset = new Vector3(0f, 1.6f, 0.1f);

    [Header("Rotation")]
    public Vector3 rotationOffsetEuler = Vector3.zero;        
    public bool matchRotation = true;                         

    [Header("Smoothing")]
    public bool useSmoothing = true;
    public float positionLerp = 12f;                          // 값 올리면 더 빠르게 붙음
    public float rotationLerp = 12f;

    void LateUpdate()
    {
        if (!target) return;


        Vector3 desiredPos = target.TransformPoint(localOffset);

        Quaternion desiredRot = matchRotation
            ? target.rotation * Quaternion.Euler(rotationOffsetEuler)
            : Quaternion.LookRotation(target.forward, Vector3.up);

        if (useSmoothing)
        {

            float pT = 1f - Mathf.Exp(-positionLerp * Time.deltaTime);
            float rT = 1f - Mathf.Exp(-rotationLerp * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPos, pT);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rT);
        }
        else
        {
            transform.position = desiredPos;
            transform.rotation = desiredRot;
        }
    }
}