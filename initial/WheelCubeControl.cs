using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class WheelCubeControl : MonoBehaviour
{
    public InputActionAsset inputActions;

    private InputAction steering;
    private InputAction gas;
    private InputAction brake;

    private Rigidbody rb;

    public float accelerationForce = 500f;
    public float brakeForce = 1000f;
    public float drag = 1f;
    public float turnTorque = 100f;

    void Start()
    {
        var drivingMap = inputActions.FindActionMap("Driving");
        steering = drivingMap.FindAction("Steering");
        gas = drivingMap.FindAction("Throttle"); 
        brake = drivingMap.FindAction("Brake");

        drivingMap.Enable();

        rb = GetComponent<Rigidbody>();
        rb.drag = drag;
    }

    void FixedUpdate()
    {
        float steer = steering.ReadValue<float>();

        //slider
        float rawGas = gas.ReadValue<float>();
        float accel = Mathf.Clamp01((1f - rawGas) / 2f);

        //stick Y
        float rawBrake = brake.ReadValue<float>();
        float brakeVal = Mathf.Clamp01((rawBrake + 1f) / 2f);

        //전진
        if (accel > 0.01f)
        {
            rb.AddForce(transform.forward * accel * accelerationForce * Time.fixedDeltaTime);
        }

        //움직일때 브레이크
        if (brakeVal > 0.01f)
        {
            Vector3 brakeDir = -rb.velocity.normalized;
            float brakeStrength = brakeVal * brakeForce * Time.fixedDeltaTime;
            rb.AddForce(brakeDir * brakeStrength);
        }

        //전환
        if (Mathf.Abs(steer) > 0.01f)
        {
            rb.AddTorque(Vector3.up * steer * turnTorque * Time.fixedDeltaTime);
        }
    }
}
