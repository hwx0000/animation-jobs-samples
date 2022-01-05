using UnityEngine;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.Animations;
#else
using UnityEngine.Experimental.Animations;
#endif


public struct TwoBoneIKJob : IAnimationJob
{
    public TransformSceneHandle goal;

    public TransformStreamHandle top;
    public TransformStreamHandle mid;
    public TransformStreamHandle effector;

    // 存储三块Joint和Goal的Transform, 以便在ProcessAnimation里对它们进行读写
    public void Init(Animator animator, Transform topX, Transform midX, Transform lowX, Transform goalX)
    {
        top = animator.BindStreamTransform(topX);
        mid = animator.BindStreamTransform(midX);
        effector = animator.BindStreamTransform(lowX);

        goal = animator.BindSceneTransform(goalX);
    }

    public void ProcessRootMotion(AnimationStream stream)
    {
    }

    public void ProcessAnimation(AnimationStream stream)
    {
        Solve(stream, top, mid, effector, goal);
    }

    /// <summary>
    /// Returns the angle needed between v1 and v2 so that their extremities are
    /// spaced with a specific length.
    /// </summary>
    /// <returns>The angle between v1 and v2.</returns>
    /// <param name="aLen">The desired length between the extremities of v1 and v2.</param>
    /// <param name="v1">First triangle edge.</param>
    /// <param name="v2">Second triangle edge.</param>
    private static float TriangleAngle(float aLen, Vector3 v1, Vector3 v2)
    {
        float aLen1 = v1.magnitude;
        float aLen2 = v2.magnitude;
        float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
        return Mathf.Acos(c);
    }

    // 求解这个TwoBoneIK问题
    private static void Solve(AnimationStream stream, TransformStreamHandle topHandle, TransformStreamHandle midHandle, TransformStreamHandle endHandle, TransformSceneHandle goalHandle)
    {
        // 只有effector的rotation是肯定不会改变的
        Quaternion aRotation = topHandle.GetRotation(stream);
        Quaternion bRotation = midHandle.GetRotation(stream);
        Quaternion gRotation = goalHandle.GetRotation(stream);

        Vector3 aPosition = topHandle.GetPosition(stream);
        Vector3 bPosition = midHandle.GetPosition(stream);
        Vector3 cPosition = endHandle.GetPosition(stream);
        Vector3 gPosition = goalHandle.GetPosition(stream);

        Vector3 ab = bPosition - aPosition;
        Vector3 bc = cPosition - bPosition;
        Vector3 ac = cPosition - aPosition;
        Vector3 ag = gPosition - aPosition;

        float abcAngle = TriangleAngle(ac.magnitude, ab, bc);
        float abeAngle = TriangleAngle(ag.magnitude, ab, bc);
        float angle = (abcAngle - abeAngle) * Mathf.Rad2Deg;
        Vector3 axis = Vector3.Cross(ab, bc).normalized;

        Quaternion fromToRotation = Quaternion.AngleAxis(angle, axis);

        Quaternion worldQ = fromToRotation * bRotation;
        midHandle.SetRotation(stream, worldQ);

        cPosition = endHandle.GetPosition(stream);
        ac = cPosition - aPosition;
        Quaternion fromTo = Quaternion.FromToRotation(ac, ag);
        topHandle.SetRotation(stream, fromTo * aRotation);

        endHandle.SetRotation(stream, gRotation);
    }
}
