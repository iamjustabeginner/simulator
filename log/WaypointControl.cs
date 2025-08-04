using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowWP : MonoBehaviour
{
    public GameObject[] waypoints;   // 路径点数组
    private int currentWP = 0;       // 当前路径点索引
    private int direction = 1;       // 方向：1 表示向前，-1 表示向后

    public float speed = 3.0f;      // 移动速度

    void Update()
    {
        // 如果没有设置路径点，直接返回
        if (waypoints == null || waypoints.Length == 0) return;

        // 检查是否到达当前路径点
        if (Vector3.Distance(transform.position, waypoints[currentWP].transform.position) < 0.5f)
        {
            currentWP += direction;

            // 到达最后一个点，开始反向
            if (currentWP >= waypoints.Length)
            {
                currentWP = waypoints.Length - 2;
                direction = -1;
            }

            // 到达第一个点，开始正向
            else if (currentWP < 0)
            {
                currentWP = 1;
                direction = 1;
            }
        }

        transform.LookAt(waypoints[currentWP].transform);
        transform.Translate(0, 0, speed * Time.deltaTime);
    }
}
