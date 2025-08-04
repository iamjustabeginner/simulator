using UnityEngine;
using System.IO;

public class SimulationLogger : MonoBehaviour
{
    public IntegratedVehicleController controller;  // Drag & drop in Inspector
    private Rigidbody rb;
    private StreamWriter writer;
    private float startTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        string filePath = Application.dataPath + "/simulation_log.csv";
        writer = new StreamWriter(filePath);
        writer.WriteLine("Time,PosX,PosY,PosZ,RotY,Velocity,Steering,Throttle,Brake,Mode");

        startTime = Time.time;
    }

    void Update()
    {
        if (controller == null || rb == null) return;

        float time = Time.time - startTime;
        Vector3 pos = transform.position;
        float rotY = transform.eulerAngles.y;
        float velocity = rb.velocity.magnitude;

        float steer = controller.GetSteering();
        float throttle = controller.GetThrottle();
        float brake = controller.GetBrake();
        string mode = controller.currentMode.ToString();

        writer.WriteLine($"{time:F2},{pos.x:F2},{pos.y:F2},{pos.z:F2},{rotY:F2},{velocity:F2},{steer:F2},{throttle:F2},{brake:F2},{mode}");
    }

    void OnDestroy()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
        }
    }
}