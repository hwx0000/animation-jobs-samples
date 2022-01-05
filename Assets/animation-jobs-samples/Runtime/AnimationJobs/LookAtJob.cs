﻿using UnityEngine;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.Animations;
#else
using UnityEngine.Experimental.Animations;
#endif

public struct LookAtJob : IAnimationJob
{
    // 注意, 虽然都是TransformHandle, 这俩类型不一样
    public TransformStreamHandle joint;         // 要调整的Joint的Transform引用
    public TransformSceneHandle target;         // IK问题的Goal的Transform引用

    public Vector3 axis;
    public float minAngle;
    public float maxAngle;

    public void ProcessRootMotion(AnimationStream stream)
    {
    }

    public void ProcessAnimation(AnimationStream stream)
    {
        Solve(stream, joint, target, axis, minAngle, maxAngle);
    }

    private static void Solve(AnimationStream stream, TransformStreamHandle joint, TransformSceneHandle target, Vector3 jointAxis, float minAngle, float maxAngle)
    {
        var jointPosition = joint.GetPosition(stream);
        var jointRotation = joint.GetRotation(stream);
        var targetPosition = target.GetPosition(stream);

        var fromDir = jointRotation * jointAxis;
        var toDir = targetPosition - jointPosition;

        var axis = Vector3.Cross(fromDir, toDir).normalized;
        var angle = Vector3.Angle(fromDir, toDir);
        angle = Mathf.Clamp(angle, minAngle, maxAngle);
        var jointToTargetRotation = Quaternion.AngleAxis(angle, axis);

        jointRotation = jointToTargetRotation * jointRotation;

        joint.SetRotation(stream, jointRotation);
    }
}
