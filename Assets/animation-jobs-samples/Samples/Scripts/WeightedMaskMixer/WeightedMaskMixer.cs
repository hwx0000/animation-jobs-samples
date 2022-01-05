using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

using UnityEngine.Experimental.Animations;

// 基于SimpleMixer改进的东西, 加入了AvatarMask, 这里的AvatarMask是以SubTree为单位执行Mask的
// 用户通过在Inspector界面里选择想要执行Mask的joint对应的GameObject, 拖拽到此类的
// boneTransformWeights数组里, 然后在Update函数里, 会改变m_BoneWeights这个Native数组
// 在MixerJob的动态执行过程中, 会去根据这个数组, 在计算LocalTransform时执行Mask功能
// 注意：这里的Mask是应用于Blend之后的Pose上, 所以这里的float weight仍然控制俩动画的权重
// 但boneTransformWeights数组由于动态改变特定Joint对应SubTree的权重
public class WeightedMaskMixer : MonoBehaviour
{
    // 这些是原本就有的
    [Range(0.0f, 1.0f)]
    public float weight = 1.0f;

    NativeArray<TransformStreamHandle> m_Handles;
    NativeArray<float> m_BoneWeights;

    PlayableGraph m_Graph;
    AnimationScriptPlayable m_CustomMixerPlayable;

    // 这些是新加的
    [Serializable]
    public struct BoneTransformWeight
    {
        public Transform transform;

        [Range(0.0f, 1.0f)]
        public float weight;
    }

    // 这个数组会在Inspector界面由用户指定
    public BoneTransformWeight[] boneTransformWeights;

    List<List<int>> m_BoneChildrenIndices;

    void OnEnable()
    {
        // 原本的内容不变
        var idleClip = SampleUtility.LoadAnimationClipFromFbx("DefaultMale/Models/DefaultMale_Generic", "Idle");
        var romClip = SampleUtility.LoadAnimationClipFromFbx("DefaultMale/Models/DefaultMale_Generic", "ROM");
        if (idleClip == null || romClip == null)
            return;

        var animator = GetComponent<Animator>();

        // Get all the transforms in the hierarchy.
        var allTransforms = animator.transform.GetComponentsInChildren<Transform>();
        var numTransforms = allTransforms.Length - 1;

        // 为每个Joint创建一个TransformStreamHandle, 没变
        m_Handles = new NativeArray<TransformStreamHandle>(numTransforms, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (var i = 0; i < numTransforms; ++i)
            m_Handles[i] = animator.BindStreamTransform(allTransforms[i + 1]);


        // SimplerMixer里把m_BoneWeights里的元素全部置为1.0f了, 这里没有
        m_BoneWeights = new NativeArray<float>(numTransforms, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        // Set bone weights for selected transforms and their hierarchy.
        m_BoneChildrenIndices = new List<List<int>>(boneTransformWeights.Length);
        // 遍历每一个用户额外添加Mask的Joint
        foreach (var boneTransform in boneTransformWeights)
        { 
            // 获取Joint的所有子Joints
            var childrenTransforms = boneTransform.transform.GetComponentsInChildren<Transform>();

            // 遍历每个子Joint, 获取该Joint在Skeleton对应数组里的id, 存到childrenIndices数组里
            var childrenIndices = new List<int>(childrenTransforms.Length);
            foreach (var childTransform in childrenTransforms)
            {
                var boneIndex = Array.IndexOf(allTransforms, childTransform);
                Debug.Assert(boneIndex > 0, "Index can't be less or equal to 0");
                // 减一的原因是, allTransforms的0号节点是挂载Animator的GameObject, 不算Skeleton(虽然我觉得没必要)
                childrenIndices.Add(boneIndex - 1);
            }

            m_BoneChildrenIndices.Add(childrenIndices);
        }

        // 后面的代码基本没变
        var job = new MixerJob()
        {
            handles = m_Handles,
            boneWeights = m_BoneWeights,
            weight = 1.0f                       // 初始的Clip选择了第二个
        };

        // Create graph with custom mixer.
        m_Graph = PlayableGraph.Create("CustomMixer");
        m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        m_CustomMixerPlayable = AnimationScriptPlayable.Create(m_Graph, job);
        m_CustomMixerPlayable.SetProcessInputs(false);
        m_CustomMixerPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, idleClip), 0, 1.0f);
        m_CustomMixerPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, romClip), 0, 1.0f);

        var output = AnimationPlayableOutput.Create(m_Graph, "output", animator);
        output.SetSourcePlayable(m_CustomMixerPlayable);

        m_Graph.Play();
    }

    void Update()
    {
        var job = m_CustomMixerPlayable.GetJobData<MixerJob>();

        // 额外的变化就是调用了UpdateWeights函数
        UpdateWeights();

        job.weight = weight;
        job.boneWeights = m_BoneWeights;

        m_CustomMixerPlayable.SetJobData(job);
    }

    void OnDisable()
    {
        m_Graph.Destroy();
        m_Handles.Dispose();
        m_BoneWeights.Dispose();
    }

    void UpdateWeights()
    {
        // 遍历用户指定的需要涉及Mask的Joint
        for (var i = 0; i < boneTransformWeights.Length; ++i)
        {
            var boneWeight = boneTransformWeights[i].weight;
            // 获取该Joint的所有子Joint的id
            List<int> childrenIndices = m_BoneChildrenIndices[i];
            // 遍历子节点, 改变其BoneWeight值
            foreach (var index in childrenIndices)
                m_BoneWeights[index] = boneWeight;
        }
    }
}
