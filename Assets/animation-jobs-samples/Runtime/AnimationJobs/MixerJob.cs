using Unity.Collections;
using UnityEngine;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.Animations;
#else
using UnityEngine.Experimental.Animations;
#endif

// 自定义一个MixerJob, 代替AnimationMixerPlayable
// 注意, 这里跟直接使用AnimationMixerPlayable实现机理不同的是
// AnimationMixerPlayable是通过调整多个输入的AnimationClipPlayable的权重, 然后Unity内部去进行Blend的
// 而这里的MixerJob的处理方式更底层, 它是直接去修改Joints的Transform的
// 这里连接MixerJob的AnimationScriptPlayable的多个AnimationClipPlayable, 其权重值无所谓是多少
// 因为内部反正是手动Set每个Joint的Transform的
public struct MixerJob : IAnimationJob
{
    // 这里的Mixer做的是很具体的东西, 它会把所有的模型上的Joints的Transform根据对应的权重进行混合
    public NativeArray<TransformStreamHandle> handles;      // 每个Handle对应一个Joint的Transform
    public NativeArray<float> boneWeights;                  
    public float weight;

    public void ProcessRootMotion(AnimationStream stream)
    {
        Debug.Log("ProcessRootMotion");
        AnimationStream streamA = stream.GetInputStream(0);
        AnimationStream streamB = stream.GetInputStream(1);

        // 把两个动画对应Stream的velocity和angularVelocity进行混合(不过这玩意儿跟RootMotion有何关系)
        // 当weight为0时, 全部采用Input 0对应Stream的数据
        // 当weight为1时, 全部采用Input 1对应Stream的数据
        var velocity = Vector3.Lerp(streamA.velocity, streamB.velocity, weight);
        var angularVelocity = Vector3.Lerp(streamA.angularVelocity, streamB.angularVelocity, weight);
        stream.velocity = velocity;
        stream.angularVelocity = angularVelocity;
    }

    public void ProcessAnimation(AnimationStream stream)
    {
        Debug.Log("ProcessAnimation");
        AnimationStream streamA = stream.GetInputStream(0);
        AnimationStream streamB = stream.GetInputStream(1);

        // 遍历每个Joint对应Transform的Handle
        int numHandles = handles.Length;
        for (var i = 0; i < numHandles; ++i)
        {
            var handle = handles[i];

            // 获取该Transform在不同的动画Stream里的Local数据, 然后根据权重进行Blend
            // 当weight为0时, 全部采用Input 0对应Stream的数据
            // 当weight为1时, 全部采用Input 1对应Stream的数据
            var posA = handle.GetLocalPosition(streamA);
            var posB = handle.GetLocalPosition(streamB);
            handle.SetLocalPosition(stream, Vector3.Lerp(posA, posB, weight * boneWeights[i]));

            var rotA = handle.GetLocalRotation(streamA);
            var rotB = handle.GetLocalRotation(streamB);
            handle.SetLocalRotation(stream, Quaternion.Slerp(rotA, rotB, weight * boneWeights[i]));
        }
    }
}
