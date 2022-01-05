using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;

// TwoBoneIK其实有三个Joint, m_TopJoint、m_MidJoint和endJoint
public class TwoBoneIK : MonoBehaviour
{
    public Transform endJoint;      // EndJoint就是Effector

    Transform m_TopJoint;           
    Transform m_MidJoint;
    GameObject m_Goal;          

    PlayableGraph m_Graph;
    AnimationScriptPlayable m_IKPlayable;

    void OnEnable()
    {
        var idleClip = SampleUtility.LoadAnimationClipFromFbx("DefaultMale/Models/DefaultMale_Generic", "Idle");
        if (idleClip == null)
            return;

        if (endJoint == null)
            return;

        // 根据传入的endJoint, 找到其parent和parent的parent, 三个Joint就找好了
        m_MidJoint = endJoint.parent;
        if (m_MidJoint == null)
            return;

        m_TopJoint = m_MidJoint.parent;
        if (m_TopJoint == null)
            return;

        // 一开始Goal会出现在EndJoint的位置
        m_Goal = SampleUtility.CreateGoal("Goal_" + endJoint.name, endJoint.position, endJoint.rotation);

        m_Graph = PlayableGraph.Create("TwoBoneIK");
        m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var output = AnimationPlayableOutput.Create(m_Graph, "ouput", GetComponent<Animator>());


        var twoBoneIKJob = new TwoBoneIKJob();
        twoBoneIKJob.Init(GetComponent<Animator>(), m_TopJoint, m_MidJoint, endJoint, m_Goal.transform);

        m_IKPlayable = AnimationScriptPlayable.Create(m_Graph, twoBoneIKJob);
        m_IKPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, idleClip), 0, 1.0f);

        output.SetSourcePlayable(m_IKPlayable);
        m_Graph.Play();
    }

    void OnDisable()
    {
        m_Graph.Destroy();
        Object.Destroy(m_Goal);
    }
}
