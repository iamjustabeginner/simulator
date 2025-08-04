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

    public float accelerationForce = 50f;
    public float brakeForce = 100f;
    public float drag = 1f;         // natural slowdown
    public float turnTorque = 10f;

    void Start()
    {
        var drivingMap = inputActions.FindActionMap("Driving");
        steering = drivingMap.FindAction("Steering");
        gas = drivingMap.FindAction("Throttle");  // slider: 1 → -1
        brake = drivingMap.FindAction("Brake");   // stick Y: -1 → 1

        drivingMap.Enable();

        rb = GetComponent<Rigidbody>();
        rb.drag = drag;
    }

    void FixedUpdate()
    {
        float steer = steering.ReadValue<float>();

        // GAS normalization (slider): 1 → -1 → map to 0 → 1
        float rawGas = gas.ReadValue<float>();
        float accel = Mathf.Clamp01((1f - rawGas) / 2f);

        // BRAKE normalization (stick Y): -1 → 1 → map to 0 → 1
        float rawBrake = brake.ReadValue<float>();
        float brakeVal = Mathf.Clamp01((rawBrake + 1f) / 2f);

        // 🚗 Apply forward acceleration
        if (accel > 0.01f)
        {
            rb.AddForce(transform.forward * accel * accelerationForce * Time.fixedDeltaTime);
        }

        // 🛑 Apply braking force (only if moving)
        if (brakeVal > 0.01f)
        {
            Vector3 brakeDir = -rb.velocity.normalized;
            float brakeStrength = brakeVal * brakeForce * Time.fixedDeltaTime;
            rb.AddForce(brakeDir * brakeStrength);
        }

        // 🔁 Apply steering torque
        if (Mathf.Abs(steer) > 0.01f)
        {
            rb.AddTorque(Vector3.up * steer * turnTorque * Time.fixedDeltaTime);
        }
    }
}
