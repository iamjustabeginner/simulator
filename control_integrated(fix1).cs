using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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

    // Input Actions
    private InputAction steering;
    private InputAction gas;
    private InputAction brake;

    // Components
    private Rigidbody rb;

    // Control Mode
    public enum ControlMode
    {
        Manual,
        AutoWaypoint
    }

    [Header("Debug")]
    public ControlMode currentMode = ControlMode.Manual;

    // Auto Mode Variables
    private int currentWP = 0;
    private int waypointDirection = 1;
    private float lastInputTime;
    private bool hasValidInput = false;

    // Transition Variables
    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;
    private bool isTransitioning = false;
    private float transitionDuration = 1f;
    private float transitionStartTime;

    // 로그용 변수
    private Vector3 prevVelocity = Vector3.zero;
    private float autoModeEnteredTime = -1f;
    private string lastAutoModeReason = "";

    void Start()
    {
        SetupInput();
        rb = GetComponent<Rigidbody>();
        rb.drag = drag;
        lastInputTime = Time.time;

        // 가장 가까운 웨이포인트를 찾아서 시작점으로 설정
        if (waypoints != null && waypoints.Length > 0)
        {
            FindNearestWaypoint();
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
        // 로그 기록
        LogStatus();

        if (currentMode == ControlMode.Manual || isTransitioning)
        {
            HandleManualControl();
        }
        prevVelocity = rb.velocity; // 가속도 계산을 위한 이전 속도 저장
    }

    void LogStatus()
    {
        Vector3 position = transform.position;
        Vector3 rotation = transform.rotation.eulerAngles;
        Vector3 velocity = rb.velocity;
        Vector3 angularVelocity = rb.angularVelocity;
        Vector3 acceleration = (Time.fixedDeltaTime > 0f) ? (rb.velocity - prevVelocity) / Time.fixedDeltaTime : Vector3.zero;

        Debug.Log($"[Vehicle] Pos:{position}, Rot:{rotation}, Vel:{velocity}, AngularVel:{angularVelocity}, Accel:{acceleration}");
        Debug.Log($"[Vehicle] Mode:{currentMode}, Waypoint:{currentWP}, LastInput:{Time.time - lastInputTime:F2}s ago");

        if (currentMode == ControlMode.AutoWaypoint && autoModeEnteredTime > 0)
        {
            Debug.Log($"[AutoMode] Since: {autoModeEnteredTime:F2}, Reason: {lastAutoModeReason}");
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

            // 가스 정규화 (slider): 1 → -1 → map to 0 → 1
            float accel = Mathf.Clamp01((1f - rawGas) / 2f);
            // 브레이크 정규화 (stick Y): -1 → 1 → map to 0 → 1
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
                Debug.Log($"[Mode Change] → MANUAL (입력감지: 수동 전환) at {Time.time:F2}");
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
                Debug.Log($"[Mode Change] → AUTO_WAYPOINT (입력없음: 자동모드 전환) at {autoModeEnteredTime:F2}");
            }
        }

        // 모드 변경 시 로그
        if (previousMode != currentMode)
        {
            Debug.Log($"Control Mode Changed: {previousMode} → {currentMode}");
        }
    }

    void HandleManualControl()
    {
        if (steering == null || gas == null || brake == null) return;
        float steer = steering.ReadValue<float>();
        float rawGas = gas.ReadValue<float>();
        float rawBrake = brake.ReadValue<float>();

        // 가스 정규화
        float accel = Mathf.Clamp01((1f - rawGas) / 2f);
        // 브레이크 정규화
        float brakeVal = Mathf.Clamp01((rawBrake + 1f) / 2f);

        // 🚗 전진 가속
        if (accel > 0.01f)
        {
            rb.AddForce(transform.forward * accel * accelerationForce * Time.fixedDeltaTime);
        }

        // 🛑 브레이크 적용
        if (brakeVal > 0.01f)
        {
            Vector3 brakeDir = -rb.velocity.normalized;
            float brakeStrength = brakeVal * brakeForce * Time.fixedDeltaTime;
            rb.AddForce(brakeDir * brakeStrength);
        }

        // 🔁 조향 토크 적용
        if (Mathf.Abs(steer) > 0.01f)
        {
            rb.AddTorque(Vector3.up * steer * turnTorque * Time.fixedDeltaTime);
        }
    }

    void UpdateWaypointMovement()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // 현재 웨이포인트까지의 거리 확인
        float distance = Vector3.Distance(transform.position, waypoints[currentWP].transform.position);

        if (distance < waypointReachDistance)
        {
            // 다음 웨이포인트로 이동
            currentWP += waypointDirection;

            // 마지막 포인트에 도달했을 때 방향 반전
            if (currentWP >= waypoints.Length)
            {
                currentWP = waypoints.Length - 2;
                waypointDirection = -1;
            }
            // 첫 번째 포인트에 도달했을 때 방향 반전
            else if (currentWP < 0)
            {
                currentWP = 1;
                waypointDirection = 1;
            }
        }

        // 목표 지점을 향해 회전
        Vector3 targetDirection = (waypoints[currentWP].transform.position - transform.position).normalized;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }

        // 웨이포인트를 향해 이동
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

        // 방향 설정 (다음 웨이포인트가 있는 방향으로)
        if (nearestIndex == 0)
        {
            waypointDirection = 1;
        }
        else if (nearestIndex == waypoints.Length - 1)
        {
            waypointDirection = -1;
        }
        else
        {
            // 중간에 있을 때는 더 가까운 다음 웨이포인트 방향으로
            float distanceToNext = Vector3.Distance(transform.position, waypoints[nearestIndex + 1].transform.position);
            float distanceToPrev = Vector3.Distance(transform.position, waypoints[nearestIndex - 1].transform.position);
            waypointDirection = (distanceToNext < distanceToPrev) ? 1 : -1;
        }

        Debug.Log($"가장 가까운 웨이포인트: {nearestIndex}, 방향: {waypointDirection}");
    }

    // 디버그용 GUI
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

    // 에디터에서 웨이포인트 시각화
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        // 웨이포인트들 그리기
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.color = (i == currentWP && currentMode == ControlMode.AutoWaypoint) ? Color.red : Color.yellow;
            Gizmos.DrawSphere(waypoints[i].transform.position, 0.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(waypoints[i].transform.position + Vector3.up, i.ToString());
#endif
        }
        // 웨이포인트 간 연결선 그리기
        Gizmos.color = Color.white;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
        }
    }
}
