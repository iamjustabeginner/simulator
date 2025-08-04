//same code as control_intergrated(fix2)

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.IO;

[RequireComponent(typeof(Rigidbody))]
public class IntegratedVehicleController : MonoBehaviour
{
    [Header("Input System")]
    public InputActionAsset inputActions;
    [Header("Manual Control Settings")]
    public float accelerationForce = 50f;
    public float brakeForce = 100f;
    public float drag = 1f;
    public float turnTorque = 10f;
    [Header("Waypoint Auto Control Settings")]
    public GameObject[] waypoints;
    public float waypointSpeed = 3.0f;
    public float waypointReachDistance = 0.5f;
    [Header("Mode Switch Settings")]
    public float idleTimeBeforeAuto = 3f; // 자동 모드 전환까지 대기 시간
    public float inputThreshold = 0.1f; // 입력 감지 임계값

    private InputAction steering;
    private InputAction gas;
    private InputAction brake;

    private Rigidbody rb;

    public enum ControlMode
    {
        Manual,
        AutoWaypoint
    }

    [Header("Debug")]
    public ControlMode currentMode = ControlMode.Manual;

    private int currentWP = 0;
    private int waypointDirection = 1;
    private float lastInputTime;
    private bool hasValidInput = false;

    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;
    private bool isTransitioning = false;
    private float transitionDuration = 1f;
    private float transitionStartTime;

    private Vector3 prevVelocity = Vector3.zero;
    private float autoModeEnteredTime = -1f;
    private string lastAutoModeReason = "";

    // 로그 파일용 StreamWriter
    private StreamWriter logWriter;

    void Start()
    {
        SetupInput();
        rb = GetComponent<Rigidbody>();
        rb.drag = drag;
        lastInputTime = Time.time;

        // 로그파일 열기, 이름에 시간 붙여 중복 방지
        string logFileName = $"VehicleLog_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        logWriter = new StreamWriter(logFileName, true);

        if (waypoints != null && waypoints.Length > 0)
        {
            FindNearestWaypoint();
        }
    }

    void OnDisable()
    {
        if (logWriter != null)
        {
            logWriter.Flush();
            logWriter.Close();
        }
    }

    void SetupInput()
    {
        if (inputActions != null)
        {
            var drivingMap = inputActions.FindActionMap("Driving");
            steering = drivingMap.FindAction("Steering");
            gas = drivingMap.FindAction("Throttle");
            brake = drivingMap.FindAction("Brake");
            drivingMap.Enable();
        }
    }

    void Update()
    {
        CheckInput();
        UpdateControlMode();
        if (currentMode == ControlMode.AutoWaypoint && !isTransitioning)
        {
            UpdateWaypointMovement();
        }
    }

    void FixedUpdate()
    {
        LogStatus();

        if (currentMode == ControlMode.Manual || isTransitioning)
        {
            HandleManualControl();
        }
        prevVelocity = rb.velocity;
    }

    void LogStatus()
    {
        Vector3 position = transform.position;
        Vector3 rotation = transform.rotation.eulerAngles;
        Vector3 velocity = rb.velocity;
        Vector3 angularVelocity = rb.angularVelocity;
        Vector3 acceleration = Time.fixedDeltaTime > 0f ? (rb.velocity - prevVelocity) / Time.fixedDeltaTime : Vector3.zero;
        float time = Time.time;

        string logEntry =
            $"{time:F2}, Pos:({position.x:F2},{position.y:F2},{position.z:F2}), Rot:({rotation.x:F1},{rotation.y:F1},{rotation.z:F1}), " +
            $"Vel:({velocity.x:F2},{velocity.y:F2},{velocity.z:F2}), AngVel:({angularVelocity.x:F2},{angularVelocity.y:F2},{angularVelocity.z:F2}), " +
            $"Accel:({acceleration.x:F2},{acceleration.y:F2},{acceleration.z:F2}), Mode:{currentMode}, Waypoint:{currentWP}, " +
            $"LastInput:{(time - lastInputTime):F2}s ago";

        Debug.Log(logEntry);
        if (logWriter != null)
        {
            logWriter.WriteLine(logEntry);

            if (currentMode == ControlMode.AutoWaypoint && autoModeEnteredTime > 0)
            {
                string autoModeLog = $"[AutoMode] Since: {autoModeEnteredTime:F2}, Reason: {lastAutoModeReason}";
                Debug.Log(autoModeLog);
                logWriter.WriteLine(autoModeLog);
            }

            logWriter.Flush();
        }
    }

    void CheckInput()
    {
        hasValidInput = false;
        if (steering != null && gas != null && brake != null)
        {
            float steer = steering.ReadValue<float>();
            float rawGas = gas.ReadValue<float>();
            float rawBrake = brake.ReadValue<float>();

            float accel = Mathf.Clamp01((1f - rawGas) / 2f);
            float brakeVal = Mathf.Clamp01((rawBrake + 1f) / 2f);

            if (Mathf.Abs(steer) > inputThreshold || accel > inputThreshold || brakeVal > inputThreshold)
            {
                hasValidInput = true;
                lastInputTime = Time.time;
            }
        }
    }

    void UpdateControlMode()
    {
        ControlMode previousMode = currentMode;

        if (hasValidInput)
        {
            if (currentMode == ControlMode.AutoWaypoint)
            {
                StartTransitionToManual();
                currentMode = ControlMode.Manual;
                LogModeChange("입력감지: 수동 전환");
            }
        }
        else if (Time.time - lastInputTime > idleTimeBeforeAuto)
        {
            if (currentMode == ControlMode.Manual)
            {
                StartTransitionToAuto();
                currentMode = ControlMode.AutoWaypoint;
                autoModeEnteredTime = Time.time;
                lastAutoModeReason = $"No input for {idleTimeBeforeAuto}s";
                LogModeChange("입력없음: 자동모드 전환");
            }
        }

        if (previousMode != currentMode)
        {
            string modeChange = $"Control Mode Changed: {previousMode} → {currentMode}";
            Debug.Log(modeChange);
            if (logWriter != null) logWriter.WriteLine(modeChange);
        }
    }

    void LogModeChange(string reason)
    {
        string log = $"[Mode Change] → {currentMode} ({reason}) at {Time.time:F2}";
        Debug.Log(log);
        if (logWriter != null) logWriter.WriteLine(log);
        if (logWriter != null) logWriter.Flush();
    }

    void HandleManualControl()
    {
        if (steering == null || gas == null || brake == null) return;
        float steer = steering.ReadValue<float>();
        float rawGas = gas.ReadValue<float>();
        float rawBrake = brake.ReadValue<float>();

        float accel = Mathf.Clamp01((1f - rawGas) / 2f);
        float brakeVal = Mathf.Clamp01((rawBrake + 1f) / 2f);

        if (accel > 0.01f)
            rb.AddForce(transform.forward * accel * accelerationForce * Time.fixedDeltaTime);

        if (brakeVal > 0.01f)
        {
            Vector3 brakeDir = -rb.velocity.normalized;
            float brakeStrength = brakeVal * brakeForce * Time.fixedDeltaTime;
            rb.AddForce(brakeDir * brakeStrength);
        }

        if (Mathf.Abs(steer) > 0.01f)
            rb.AddTorque(Vector3.up * steer * turnTorque * Time.fixedDeltaTime);
    }

    void UpdateWaypointMovement()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        float distance = Vector3.Distance(transform.position, waypoints[currentWP].transform.position);

        if (distance < waypointReachDistance)
        {
            currentWP += waypointDirection;
            if (currentWP >= waypoints.Length)
            {
                currentWP = waypoints.Length - 2;
                waypointDirection = -1;
            }
            else if (currentWP < 0)
            {
                currentWP = 1;
                waypointDirection = 1;
            }
        }

        Vector3 targetDirection = (waypoints[currentWP].transform.position - transform.position).normalized;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }

        transform.Translate(0, 0, waypointSpeed * Time.deltaTime);
    }

    void StartTransitionToManual()
    {
        Debug.Log("수동 조작 모드로 전환 중...");
        rb.isKinematic = false;
    }

    void StartTransitionToAuto()
    {
        Debug.Log("자동 웨이포인트 모드로 전환 중...");
        FindNearestWaypoint();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void FindNearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        float nearestDistance = float.MaxValue;
        int nearestIndex = 0;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }
        currentWP = nearestIndex;

        if (nearestIndex == 0)
            waypointDirection = 1;
        else if (nearestIndex == waypoints.Length - 1)
            waypointDirection = -1;
        else
        {
            float distanceToNext = Vector3.Distance(transform.position, waypoints[nearestIndex + 1].transform.position);
            float distanceToPrev = Vector3.Distance(transform.position, waypoints[nearestIndex - 1].transform.position);
            waypointDirection = (distanceToNext < distanceToPrev) ? 1 : -1;
        }
        Debug.Log($"가장 가까운 웨이포인트: {nearestIndex}, 방향: {waypointDirection}");
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 10, 300, 30), $"Control Mode: {currentMode}", style);
        if (currentMode == ControlMode.AutoWaypoint && waypoints != null && waypoints.Length > 0)
        {
            GUI.Label(new Rect(10, 40, 300, 30), $"Target Waypoint: {currentWP}", style);
            GUI.Label(new Rect(10, 70, 300, 30), $"Direction: {waypointDirection}", style);
        }
        GUI.Label(new Rect(10, 100, 300, 30), $"Last Input: {Time.time - lastInputTime:F1}s ago", style);
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.color = (i == currentWP && currentMode == ControlMode.AutoWaypoint) ? Color.red : Color.yellow;
            Gizmos.DrawSphere(waypoints[i].transform.position, 0.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(waypoints[i].transform.position + Vector3.up, i.ToString());
#endif
        }
        Gizmos.color = Color.white;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
        }
    }
}

