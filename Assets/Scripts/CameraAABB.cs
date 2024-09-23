using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

[Serializable]
public struct Picture
{
    public Texture2D texture;
    public List<GameObject> objects;
}
public class CameraAABB : MonoBehaviour
{
    public Camera mainCamera;    // 사용할 카메라
    GameObject[] _objectsToCheck;  // 확인할 오브젝트들
    public Picture picture;
    public RawImage image;
    public Transform container;
    Tweener _tween1;
    Tweener _tween2;
    Plane[] _planes;
    public Camera targetCamera;
    public RenderTexture renderTexture;

    private int currentWidth;
    private int currentHeight;
    private void Start()
    {
        picture.texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TakePicture();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            StopCoroutine("Realization");
            StartCoroutine("Realization");
        }
    }

    private IEnumerator Realization()
    {
        if (container.childCount <= 0)
            yield break;

        if (_tween1 != null && _tween1.IsPlaying())
        {
            _tween1.Kill();
            _tween2.Kill();
        }
        image.gameObject.SetActive(true);
        _tween1 = image.transform.DOScale(1f, 0.3f);
        _tween2 = image.rectTransform.DOAnchorPos(new Vector2(0f, 0f), 0.3f);
        yield return new WaitForSeconds(0.3f);
        image.gameObject.SetActive(false);

        DeleteBackground();
        foreach(GameObject obj in picture.objects)
        {
            obj.SetActive(true);
            obj.transform.parent = null;
        }
        picture.objects = new List<GameObject>();
    }

    private void DeleteBackground()
    {
        _planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        _objectsToCheck = GameObject.FindGameObjectsWithTag("SliceObj");
        foreach (GameObject obj in _objectsToCheck)
        {
            Renderer objRenderer = obj.GetComponent<Renderer>();

            if (objRenderer != null)
            {
                Bounds bounds = objRenderer.bounds;
                if (GeometryUtility.TestPlanesAABB(_planes, bounds))
                {
                    if (IsObjectOnFrustumSurface(_planes, bounds))
                    {
                        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                        Mesh originalMesh = meshFilter.mesh;
                        Mesh insideMesh;
                        CutMeshWithFrustumOut(_planes, originalMesh, obj.transform, out insideMesh);

                        if (insideMesh != null)
                        {
                            if (insideMesh.triangles.Length > 0)
                            {
                                GameObject insideObject = new GameObject("InsideMesh");
                                insideObject.AddComponent<MeshFilter>().mesh = insideMesh;
                                insideObject.AddComponent<MeshRenderer>().material = obj.GetComponent<MeshRenderer>().material;
                                MeshCollider collider = insideObject.AddComponent<MeshCollider>();
                                collider.sharedMesh = insideMesh;
                                insideObject.SetActive(false);
                                insideObject.transform.parent = container;
                                insideObject.tag = "SliceObj";
                                insideObject.layer = obj.layer;
                                if (obj.TryGetComponent(out Rigidbody rigid))
                                {
                                    collider.convex = true;
                                    Rigidbody r = insideObject.AddComponent<Rigidbody>();
                                    CopyRigidbodyValues(rigid, r);
                                }
                                picture.objects.Add(insideObject);
                            }
                            Destroy(obj);
                        }
                    }
                    else
                    {
                        Destroy(obj);
                    }
                }
            }
        }
    }

    void TakePicture()
    {
        UpdateRenderTexture();
        foreach (GameObject obj in picture.objects)
        {
            Destroy(obj);
        }
        picture.objects = new List<GameObject>();
        StopCoroutine("CapturePhoto");
        StartCoroutine("CapturePhoto");

        _planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        _objectsToCheck = GameObject.FindGameObjectsWithTag("SliceObj");
        // 각 오브젝트를 확인
        foreach (GameObject obj in _objectsToCheck)
        {
            Renderer objRenderer = obj.GetComponent<Renderer>();

            if (objRenderer != null)
            {
                Bounds bounds = objRenderer.bounds;

                // AABB가 절두체에 있는지 먼저 확인
                if (GeometryUtility.TestPlanesAABB(_planes, bounds))
                {
                    // 오브젝트가 절두체 안에 있는 상태에서, 경계에 걸쳐 있는지 확인
                    if (IsObjectOnFrustumSurface(_planes, bounds))
                    {
                        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                        Mesh originalMesh = meshFilter.mesh;
                        // 절두체로 메시를 자름
                        Mesh insideMesh;
                        CutMeshWithFrustum(_planes, originalMesh, obj.transform, out insideMesh);

                        // 새로운 메시 적용 (절두체 내부)
                        if (insideMesh != null)
                        {
                            GameObject insideObject = new GameObject("InsideMesh");
                            insideObject.AddComponent<MeshFilter>().mesh = insideMesh;
                            insideObject.AddComponent<MeshRenderer>().material = obj.GetComponent<MeshRenderer>().material;
                            MeshCollider collider = insideObject.AddComponent<MeshCollider>();
                            collider.sharedMesh = insideMesh;
                            insideObject.SetActive(false);
                            insideObject.transform.parent = container;
                            insideObject.tag = "SliceObj";
                            insideObject.layer = obj.layer;
                            if(obj.TryGetComponent(out Rigidbody rigid))
                            {
                                collider.convex = true;
                                Rigidbody r = insideObject.AddComponent<Rigidbody>();
                                CopyRigidbodyValues(rigid, r);
                            }
                            picture.objects.Add(insideObject);
                        }
                    }
                    else
                    {
                        GameObject copy = Instantiate(obj, obj.transform.position, obj.transform.rotation);
                        copy.SetActive(false);
                        copy.transform.parent = container;
                        copy.tag = "SliceObj";
                        picture.objects.Add(copy);
                    }
                }
            }
        }
    }
    void UpdateRenderTexture()
    {
        // 현재 해상도 가져오기
        currentWidth = Screen.width;
        currentHeight = Screen.height;

        // 기존 RenderTexture를 해제하고 새로 생성
        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        renderTexture = new RenderTexture(currentWidth, currentHeight, 24); // 24는 depthBuffer의 비트
        renderTexture.Create();

        // 카메라의 targetTexture로 설정
        targetCamera.targetTexture = renderTexture;
        image.texture = renderTexture;
    }
    IEnumerator CapturePhoto()
    {
        targetCamera.gameObject.SetActive(true);
        yield return null;
        if (_tween1 != null && _tween1.IsPlaying())
        {
            _tween1.Kill();
            _tween2.Kill();
        }
        image.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        image.transform.localScale = Vector3.one;
        image.gameObject.SetActive(true);
        targetCamera.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.2f);
        _tween1 = image.transform.DOScale(0.2f, 0.3f);
        _tween2 = image.rectTransform.DOAnchorPos(new Vector2(Screen.width * 0.5f - 200f, Screen.height * 0.5f - 200f), 0.3f);
    }

    bool IsObjectOnFrustumSurface(Plane[] planes, Bounds bounds)
    {
        // 바운딩 박스의 8개의 코너 포인트 구하기
        Vector3[] corners = new Vector3[8];
        corners[0] = bounds.min; // 최소점
        corners[1] = bounds.max; // 최대점
        corners[2] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[6] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[7] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);

        // 각 평면을 기준으로 코너 포인트 검사
        foreach (Plane plane in planes)
        {
            int insideCount = 0;
            int outsideCount = 0;

            foreach (Vector3 corner in corners)
            {
                if (plane.GetDistanceToPoint(corner) > 0)
                {
                    insideCount++;
                }
                else
                {
                    outsideCount++;
                }
            }

            if (insideCount > 0 && outsideCount > 0)
            {
                return true;
            }
        }

        return false;
    }
    void CutMeshWithFrustumOut(Plane[] planes, Mesh originalMesh, Transform trm, out Mesh insideMesh)
    {
        // 각 절두체 평면을 사용해 메시를 자름(바깥쪽)

        List<Vector3> insideVertices = new List<Vector3>();
        List<int> insideTriangles = new List<int>();


        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        List<Plane> crossPlane = new List<Plane>();
        // 각 삼각형을 검사
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            Matrix4x4 localToWorld = trm.localToWorldMatrix;
            v0 = localToWorld.MultiplyPoint3x4(v0);
            v1 = localToWorld.MultiplyPoint3x4(v1);
            v2 = localToWorld.MultiplyPoint3x4(v2);
            // 삼각형과 모든 절두체 평면의 교차 여부 확인
            List<Plane> triCrossPlane = new List<Plane>();
            bool isInside = true;
            for (int j = 0; j < planes.Length; j++)
            {
                Plane plane2 = planes[j];
                // 삼각형의 절두체 내부에 있는 버텍스를 추적

                List<Vector3> inside = new List<Vector3>();
                List<Vector3> outside = new List<Vector3>();
                if (plane2.GetDistanceToPoint(v0) > 0f) inside.Add(v0); else outside.Add(v0);
                if (plane2.GetDistanceToPoint(v1) > 0f) inside.Add(v1); else outside.Add(v1);
                if (plane2.GetDistanceToPoint(v2) > 0f) inside.Add(v2); else outside.Add(v2);

                if (inside.Count == 0)
                {
                    isInside = false;
                    break;
                }
                else
                {
                    if (inside.Count < 3)
                    {
                        triCrossPlane.Add(plane2);
                    }
                }
            }
            if (isInside)
            {
                if (triCrossPlane.Count <= 0)
                    continue;

                Vector3[] tri = new Vector3[3];
                tri[0] = v0;
                tri[1] = v1;
                tri[2] = v2;
                ClipTriangleAgainstPlanesOut(tri, triCrossPlane, insideVertices, insideTriangles);//항상 걸침
                foreach(Plane p in triCrossPlane)
                {
                    if (!crossPlane.Contains(p))
                        crossPlane.Add(p);
                }
            }
            else
            {
                Vector3[] tri = new Vector3[3];
                tri[0] = v0;
                tri[1] = v1;
                tri[2] = v2;
                int initialIndex = insideVertices.Count;
                insideVertices.AddRange(tri);
                insideTriangles.Add(initialIndex);
                insideTriangles.Add(initialIndex + 1);
                insideTriangles.Add(initialIndex + 2);
            }
        }

        List<Vector3> v = new List<Vector3>();
        v.AddRange(insideVertices);
        for (int x = 0; x < crossPlane.Count; x++)
        {
            List<Vector3> outs = new List<Vector3>();
            for (int j = 0; j < v.Count; j++)
            {
                if (crossPlane[x].GetDistanceToPoint(v[j]) < 0.001f && crossPlane[x].GetDistanceToPoint(v[j]) > -0.001f)
                {
                    if (!outs.Contains(v[j]))
                        outs.Add(v[j]);
                }
            }
            if (outs.Count >= 3)
            {
                int dir;
                SortPointsClockwise(outs, crossPlane[x], out dir);
                Trianglation(outs, insideVertices, insideTriangles, dir, true);
            }
        }

        insideMesh = new Mesh();
        insideMesh.vertices = insideVertices.ToArray();
        insideMesh.triangles = insideTriangles.ToArray();
        insideMesh.RecalculateNormals();
    }
    void CutMeshWithFrustum(Plane[] planes, Mesh originalMesh, Transform trm, out Mesh insideMesh)
    {
        // 각 절두체 평면을 사용해 메시를 자름
        // 교차점과 절단 알고리즘을 여기서 구현 (복잡한 부분)

        List<Vector3> insideVertices = new List<Vector3>();
        List<int> insideTriangles = new List<int>();


        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        List<Plane> crossPlane = new List<Plane>();
        // 각 삼각형을 검사
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            Matrix4x4 localToWorld = trm.localToWorldMatrix;
            v0 = localToWorld.MultiplyPoint3x4(v0);
            v1 = localToWorld.MultiplyPoint3x4(v1);
            v2 = localToWorld.MultiplyPoint3x4(v2);
            // 삼각형과 모든 절두체 평면의 교차 여부 확인
            List<Plane> triCrossPlane = new List<Plane>();
            bool isInside = true;
            for (int j = 0; j < planes.Length; j++)
            {
                Plane plane2 = planes[j];
                // 삼각형의 절두체 내부에 있는 버텍스를 추적

                List<Vector3> inside = new List<Vector3>();
                List<Vector3> outside = new List<Vector3>();
                if (plane2.GetDistanceToPoint(v0) > 0f) inside.Add(v0); else outside.Add(v0);
                if (plane2.GetDistanceToPoint(v1) > 0f) inside.Add(v1); else outside.Add(v1);
                if (plane2.GetDistanceToPoint(v2) > 0f) inside.Add(v2); else outside.Add(v2);

                if (inside.Count == 0)
                {
                    isInside = false;
                    break;
                }
                else
                {
                    if (inside.Count < 3)
                    {
                        triCrossPlane.Add(plane2);

                        if (!crossPlane.Contains(plane2))
                            crossPlane.Add(plane2);
                    }
                }
            }
            if (isInside)
            {
                Vector3[] tri = new Vector3[3];
                tri[0] = v0;
                tri[1] = v1;
                tri[2] = v2;
                ClipTriangleAgainstPlanes(tri, triCrossPlane, insideVertices, insideTriangles);
            }
        }

        List<Vector3> v = new List<Vector3>();
        v.AddRange(insideVertices);
        for (int x = 0; x < crossPlane.Count; x++)
        {
            List<Vector3> outs = new List<Vector3>();
            for (int j = 0; j < v.Count; j++)
            {
                if (crossPlane[x].GetDistanceToPoint(v[j]) < 0.001f && crossPlane[x].GetDistanceToPoint(v[j]) > -0.001f)
                {
                    if(!outs.Contains(v[j]))
                        outs.Add(v[j]);
                }
            }
            if (outs.Count >= 3)
            {
                int dir;
                SortPointsClockwise(outs, crossPlane[x], out dir);
                Trianglation(outs, insideVertices, insideTriangles, dir);
            }
        }
    
        insideMesh = new Mesh();
        insideMesh.vertices = insideVertices.ToArray();
        insideMesh.triangles = insideTriangles.ToArray();
        insideMesh.RecalculateNormals();
    }

    private void Trianglation(List<Vector3> vertices, List<Vector3> insideVertices, List<int> insideTriangles, int dir, bool reverse = false)
    {
        if (dir == -1)
            return;

        int index = insideVertices.Count;
        insideVertices.AddRange(vertices);
        if (reverse ? (dir != 1) : (dir == 1))
        {
            for (int i = 0; i < vertices.Count - 2; i++)
            {
                insideTriangles.Add(index + i + 2);
                insideTriangles.Add(index + i + 1);
                insideTriangles.Add(index);
            }
        }
        else
        {
            for (int i = 0; i < vertices.Count - 2; i++)
            {
                insideTriangles.Add(index);
                insideTriangles.Add(index + i + 1);
                insideTriangles.Add(index + i + 2);
            }
        }
    }
    void SortPointsClockwise(List<Vector3> points, Plane plane, out int dir)
    {
        Vector3 center = Vector3.zero;
        foreach (var point in points)
            center += point;

        center /= points.Count;
        Vector3 planeNormal = plane.normal;

        points.Sort((a, b) =>
        {
            // 중심점을 기준으로 벡터 계산
            Vector3 dirA = a - center;
            Vector3 dirB = b - center;

            // 벡터를 평면에 투사
            Vector3 projA = Vector3.ProjectOnPlane(dirA, planeNormal);
            Vector3 projB = Vector3.ProjectOnPlane(dirB, planeNormal);

            // 투사된 벡터를 기준으로 atan2를 사용해 각도를 구함
            float angleA = Mathf.Atan2(Vector3.Dot(Vector3.up, projA), Vector3.Dot(Vector3.right, projA));
            float angleB = Mathf.Atan2(Vector3.Dot(Vector3.up, projB), Vector3.Dot(Vector3.right, projB));

            // atan2는 -π에서 π 범위를 반환하므로 이를 0 ~ 2π로 변환
            if (angleA < 0) angleA += Mathf.PI * 2;
            if (angleB < 0) angleB += Mathf.PI * 2;

            // 시계방향 정렬 (반대로 하려면 angleA와 angleB의 비교를 바꾸면 됨)
            return angleA.CompareTo(angleB);
        });

        List<Vector3> nonVec = new List<Vector3>();
        for(int i = 0; i < points.Count; i++)
        {
            bool add = true;
            for (int j = 0; j < nonVec.Count; j++) {
                if ((nonVec[j] - points[i]).sqrMagnitude < 0.01f)
                {
                    add = false;
                    break;
                }
            }
            if (add)
            {
                nonVec.Add(points[i]);
                if (nonVec.Count >= 3)
                    break;
            }
        }
        if(nonVec.Count < 3)
        {
            dir = -1;
            return;
        }
        Vector3 AB = nonVec[1] - nonVec[0];
        Vector3 AC = nonVec[2] - nonVec[0];

        Vector3 normal = Vector3.Cross(AB, AC);
        // 세 축 모두 부호가 같은지 확인
        dir = Vector3.Angle(normal, planeNormal) < 90f ? 1 : 0;
    }

    public void ClipTriangleAgainstPlanes(Vector3[] triangleVertices, List<Plane> planes, List<Vector3> vertices, List<int> triangles)
    {
        List<Vector3> clippedVertices = new List<Vector3>(triangleVertices);

        foreach (Plane plane in planes)
        {
            List<Vector3> inTri = new List<Vector3>();
            ClipPolygonAgainstPlane(clippedVertices, plane, null, inTri, planes);
            clippedVertices = inTri;
        }

        if (clippedVertices.Count >= 3)
        {
            int initialIndex = vertices.Count;

            vertices.AddRange(clippedVertices);

            for (int i = 1; i < clippedVertices.Count - 1; i++)
            {
                triangles.Add(initialIndex);
                triangles.Add(initialIndex + i);
                triangles.Add(initialIndex + i + 1);
            }
        }
    }
    public void ClipTriangleAgainstPlanesOut(Vector3[] triangleVertices, List<Plane> planes, List<Vector3> vertices, List<int> triangles)
    {

        List<Vector3> clippedVertices = new List<Vector3>(triangleVertices);

        List<Plane> planesTemp = new List<Plane>(planes);
        foreach (Plane plane in planesTemp)
        {
            List<Vector3> inTri = new List<Vector3>();
            List<Vector3> outTri = new List<Vector3>();
            if (ClipPolygonAgainstPlane(clippedVertices, plane, outTri, inTri, planes))
            {
                clippedVertices = inTri;
            }
            else
            {
                if (PolygonCore.AreAllPointsInsideAllPlanes(triangleVertices[0], triangleVertices[1], triangleVertices[2], _planes.ToList()))
                {
                    int initialIndex = vertices.Count;
                    vertices.AddRange(triangleVertices);
                    triangles.Add(initialIndex);
                    triangles.Add(initialIndex + 1);
                    triangles.Add(initialIndex + 2);
                }
            }
            if (outTri.Count >= 3)
            {
                int initialIndex = vertices.Count;

                vertices.AddRange(outTri);

                for (int i = 1; i < outTri.Count - 1; i++)
                {
                    triangles.Add(initialIndex);
                    triangles.Add(initialIndex + i);
                    triangles.Add(initialIndex + i + 1);
                }
            }
        }

    }
    private bool ClipPolygonAgainstPlane(List<Vector3> polygon, Plane plane, List<Vector3> outTri, List<Vector3> inTri, List<Plane> planeL)
    {
        List<Vector3> outs = new List<Vector3>();
        List<Vector3> ins = new List<Vector3>();

        int isSafe = 0;
        if (outTri == null)
            isSafe = 6;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3 currentVertex = polygon[i];
            Vector3 previousVertex = polygon[(i + polygon.Count - 1) % polygon.Count];

            float currentDist = plane.GetDistanceToPoint(currentVertex);
            float prevDist = plane.GetDistanceToPoint(previousVertex);

            if (prevDist >= 0)
                ins.Add(previousVertex);
            else
                outs.Add(previousVertex);

            if (currentDist * prevDist < 0)
            {
                Vector3 intersectionPoint = GetPlaneIntersection(plane, previousVertex, currentVertex);

                if (outTri != null && isSafe < 6)
                {
                    foreach (Plane plane2 in _planes)
                    {
                        if (plane2.GetDistanceToPoint(intersectionPoint) > -0.01f)
                        {
                            isSafe++;
                        }
                    }
                }
                if (isSafe < 6)
                    isSafe = 0;

                ins.Add(intersectionPoint);
                outs.Add(intersectionPoint);
            }

        }
        if (isSafe >= 6)
        {
            if (inTri != null)
                inTri.AddRange(ins);
            if (outTri != null)
                outTri.AddRange(outs);

            return true;
        }
        else
        {
            planeL.Remove(plane);
            return false;
        }
    }

    Vector3 GetPlaneIntersection(Plane plane, Vector3 inside, Vector3 outside)
    {
        Ray ray = new Ray(outside, inside - outside);
        float distance;

        if (plane.Raycast(ray, out distance))
            return ray.GetPoint(distance);

        return inside;
    }
    public void CopyRigidbodyValues(Rigidbody source, Rigidbody target)
    {
        if (source == null || target == null)
        {
            Debug.LogError("Source or Target Rigidbody is null!");
            return;
        }

        // Rigidbody의 속성 복사
        target.mass = source.mass;
        target.drag = source.drag;
        target.angularDrag = source.angularDrag;
        target.useGravity = source.useGravity;
        target.isKinematic = source.isKinematic;
        target.interpolation = source.interpolation;
        target.collisionDetectionMode = source.collisionDetectionMode;
        target.constraints = source.constraints;

        // 원한다면 추가적으로 복사할 속성을 여기에 추가할 수 있음
    }
}
