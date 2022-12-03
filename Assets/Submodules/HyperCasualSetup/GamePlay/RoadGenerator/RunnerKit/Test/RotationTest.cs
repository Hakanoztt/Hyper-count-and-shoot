using Mobge;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTest : MonoBehaviour
{
    [OwnComponent] public Rigidbody testObject;
    [OwnComponent] public Transform rotationApplyObject;

    public Vector3 angularVelocity;

    void FixedUpdate()
    {
        var radVel = angularVelocity * Mathf.Deg2Rad;
        testObject.angularVelocity = radVel;

        var r = rotationApplyObject.rotation;
        r.ApplyAngularVelocity(Time.fixedDeltaTime, radVel);
        rotationApplyObject.rotation = r;
    }
}
