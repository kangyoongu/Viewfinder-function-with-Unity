using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Core
{
    public static void CopyRigidbodyValues(Rigidbody source, Rigidbody target)
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
    }
}
