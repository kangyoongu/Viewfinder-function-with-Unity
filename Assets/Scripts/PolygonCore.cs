using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PolygonCore
{
    public static bool AreAllPointsInsideAllPlanes(Vector3 v0, Vector3 v1, Vector3 v2, List<Plane> planes)
    {
        List<Vector3> points = new List<Vector3>{ v0, v1, v2 };
        for (int i = 1; i < 10; i++)
        {
            points.Add(Vector3.Lerp(v0, v1, i * 0.1f));
            points.Add(Vector3.Lerp(v1, v2, i * 0.1f));
            points.Add(Vector3.Lerp(v2, v0, i * 0.1f));
        }

        // 6개의 점 각각이 모든 평면의 안쪽에 있는지 확인
        foreach (var point in points)
        {
            if (IsPointInsideAllPlanes(point, planes))
            {
                // 한 점이라도 모든 평면 안에 없으면 false 반환
                return false;
            }
        }

        // 모든 점이 모든 평면의 안쪽에 있으면 true 반환
        return true;
    }

    // 점이 모든 평면의 안쪽에 있는지 확인하는 함수
    private static bool IsPointInsideAllPlanes(Vector3 point, List<Plane> planes)
    {
        foreach (var plane in planes)
        {
            // 점이 평면의 바깥쪽에 있으면 false 반환
            if (plane.GetDistanceToPoint(point) < 0f)
            {
                return false;
            }
        }
        // 모든 평면의 안쪽에 있으면 true 반환
        return true;
    }
}
