using UnityEngine;

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

    // 在ProcessAnimation函数里, 利用IK算法对joint进行了修正
    public void ProcessAnimation(AnimationStream stream)
    {
        // axis是Joint的旋转轴
        Solve(stream, joint, target, axis, minAngle, maxAngle);
    }

    // joint是一个支持读写Transform的Handle, 而target只支持读Transform
    private static void Solve(AnimationStream stream, TransformStreamHandle joint, TransformSceneHandle target, Vector3 jointAxis, float minAngle, float maxAngle)
    {
        // 获取LookAt要调整的唯一Joint, 这里是对应的Chest处的Joint
        var jointPosition = joint.GetPosition(stream);
        var jointRotation = joint.GetRotation(stream);
        var targetPosition = target.GetPosition(stream);

        // joint原本在jointAxis上对应的朝向
        Vector3 fromDir = jointRotation * jointAxis;
        // joint在IK调整后应表现的朝向(就是直接指向target)
        Vector3 toDir = targetPosition - jointPosition;

        // 算出轴和旋转角度, 对角度clamp以后, 算出新的Quaternion, 代表DeltaRot
        var axis = Vector3.Cross(fromDir, toDir).normalized;
        var angle = Vector3.Angle(fromDir, toDir);
        angle = Mathf.Clamp(angle, minAngle, maxAngle);
        var jointToTargetRotation = Quaternion.AngleAxis(angle, axis);

        // 世界的DeltaRot应该乘在左边
        jointRotation = jointToTargetRotation * jointRotation;

        // 设置GlobalRotation
        joint.SetRotation(stream, jointRotation);
    }
}
