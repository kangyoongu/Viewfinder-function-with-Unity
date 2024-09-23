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

        // 6���� �� ������ ��� ����� ���ʿ� �ִ��� Ȯ��
        foreach (var point in points)
        {
            if (IsPointInsideAllPlanes(point, planes))
            {
                // �� ���̶� ��� ��� �ȿ� ������ false ��ȯ
                return false;
            }
        }

        // ��� ���� ��� ����� ���ʿ� ������ true ��ȯ
        return true;
    }

    // ���� ��� ����� ���ʿ� �ִ��� Ȯ���ϴ� �Լ�
    private static bool IsPointInsideAllPlanes(Vector3 point, List<Plane> planes)
    {
        foreach (var plane in planes)
        {
            // ���� ����� �ٱ��ʿ� ������ false ��ȯ
            if (plane.GetDistanceToPoint(point) < 0f)
            {
                return false;
            }
        }
        // ��� ����� ���ʿ� ������ true ��ȯ
        return true;
    }
}
