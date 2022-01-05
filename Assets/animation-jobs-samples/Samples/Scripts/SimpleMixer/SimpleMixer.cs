using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

using UnityEngine.Experimental.Animations;

public class SimpleMixer : MonoBehaviour
{
    // 俩Clip的插值权重
    [Range(0.0f, 1.0f)]
    public float weight;

    // NativeArray是一种特殊的数组, C++和C#端都可以访问同一块内存
    NativeArray<TransformStreamHandle> m_Handles;
    NativeArray<float> m_BoneWeights;

    PlayableGraph m_Graph;
    AnimationScriptPlayable m_CustomMixerPlayable;

    void OnEnable()
    {
        // Load动画clip
        var idleClip = SampleUtility.LoadAnimationClipFromFbx("DefaultMale/Models/DefaultMale_Generic", "Idle");
        var romClip = SampleUtility.LoadAnimationClipFromFbx("DefaultMale/Models/DefaultMale_Generic", "ROM");
        if (idleClip == null || romClip == null)
            return;

        var animator = GetComponent<Animator>();

        // Get all the transforms in the hierarchy.
        Transform[] transforms = animator.transform.GetComponentsInChildren<Transform>();
        var numTransforms = transforms.Length - 1;

        // new一个Native数组, 数组的大小为Animator对应模型的所有GameObject的数量
        m_Handles = new NativeArray<TransformStreamHandle>(numTransforms, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        // new一个Bone的权重数组, 默认的初始权重值都为1.0f, 主要是为了配合AvatarMask的, 其实在这里并没有用到
        m_BoneWeights = new NativeArray<float>(numTransforms, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        for (var i = 0; i < numTransforms; ++i)
        {
            // 把Animator对应GameObject的子GameObject的Transform绑定到animator上
            m_Handles[i] = animator.BindStreamTransform(transforms[i + 1]);
            m_BoneWeights[i] = 1.0f;
        }

        // 创建自定义的AnimationJob
        var job = new MixerJob()
        {
            handles = m_Handles,
            boneWeights = m_BoneWeights,
            weight = 0.0f
        };

        // 创建Graph
        m_Graph = PlayableGraph.Create("SimpleMixer");
        m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // 使用刚刚创建的AnimationJob, 创建AnimationScriptPlayable
        m_CustomMixerPlayable = AnimationScriptPlayable.Create(m_Graph, job);
        m_CustomMixerPlayable.SetProcessInputs(false);
        // 连接两个AnimationClipPlayable, 其实这个权重值无所谓, 连起来传了数据就行
        m_CustomMixerPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, idleClip), 0, 0.0f);
        m_CustomMixerPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, romClip), 0, 0.0f);

        var output = AnimationPlayableOutput.Create(m_Graph, "output", animator);
        output.SetSourcePlayable(m_CustomMixerPlayable);

        m_Graph.Play();
    }

    void Update()
    {
        // MixerJob是个Struct, 这里Copy了一份出来
        MixerJob job = m_CustomMixerPlayable.GetJobData<MixerJob>();

        // 注意, 在这个过程中, 俩AnimationClipPlayable的权重都是1
        job.weight = weight;

        // 改了权重值再Set回去
        m_CustomMixerPlayable.SetJobData(job);
    }

    void OnDisable()
    {
        m_Graph.Destroy();
        m_Handles.Dispose();
        m_BoneWeights.Dispose();
    }
}
