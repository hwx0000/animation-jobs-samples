﻿using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;

// 这个类都没有Update函数, 都交给LookAtJob去其Update里处理了
public class LookAt : MonoBehaviour
{
    public enum Axis
    {
        Forward,
        Back,
        Up,
        Down,
        Left,
        Right
    }

    // LookAt是个非常简单的IK过程, 这里只会调整一个Joint, 这里是角色的Chest对应的Joint
    public Transform joint;
    public Axis axis = Axis.Forward;
    public float minAngle = -60.0f;
    public float maxAngle = 60.0f;

    GameObject m_Target;

    PlayableGraph m_Graph;
    AnimationScriptPlayable m_LookAtPlayable;

    void OnEnable()
    {
        var idleClip = SampleUtility.LoadAnimationClipFromFbx("Chomper/Animations/@ChomperIdle", "Cooldown");
        if (idleClip == null)
            return;

        if (joint == null)
            return;

        var targetPosition = joint.position + gameObject.transform.rotation * Vector3.forward;

        // 创建一个GameObject, 作为LookAt过程的Target
        m_Target = SampleUtility.CreateGoal("Effector_" + joint.name, targetPosition, Quaternion.identity);

        // 创建PlayableGraph
        m_Graph = PlayableGraph.Create("TwoBoneIK");
        m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var output = AnimationPlayableOutput.Create(m_Graph, "ouput", GetComponent<Animator>());

        var animator = GetComponent<Animator>();
        // 禁止animator的AnimationEvents产生(为啥?)
        animator.fireEvents = false;
        var lookAtJob = new LookAtJob()
        {
            // Join是脖子, 它会受IK算法影响, 由Animator修改其值, 使角色一直看向目标点
            joint = animator.BindStreamTransform(joint),
            // target是场景中的一个物体, 不受animator控制, 对于animator来说是只读的
            target = animator.BindSceneTransform(m_Target.transform),
            // 根据不同的轴枚举返回不同的Vector3
            axis = GetAxisVector(axis),
            minAngle = Mathf.Min(minAngle, maxAngle),
            maxAngle = Mathf.Max(minAngle, maxAngle)
        };

        m_LookAtPlayable = AnimationScriptPlayable.Create(m_Graph, lookAtJob);
        m_LookAtPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, idleClip), 0, 1.0f);

        output.SetSourcePlayable(m_LookAtPlayable);
        m_Graph.Play();
    }

    void OnDisable()
    {
        m_Graph.Destroy();
        Object.Destroy(m_Target);
    }

    // 具体这玩意儿怎么用的, 还得仔细研究研究LookAt这个IK算法
    Vector3 GetAxisVector(Axis axis)
    {
        switch (axis)
        {
            case Axis.Forward:
                return Vector3.forward;
            case Axis.Back:
                return Vector3.back;
            case Axis.Up:
                return Vector3.up;
            case Axis.Down:
                return Vector3.down;
            case Axis.Left:
                return Vector3.left;
            case Axis.Right:
                return Vector3.right;
        }

        return Vector3.forward;
    }
}
