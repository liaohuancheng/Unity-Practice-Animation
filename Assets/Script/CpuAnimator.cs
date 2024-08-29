using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CpuAnimator : MonoBehaviour {
    public AnimationData animData;  //保存的SO文件
    private Mesh mesh; //目标mesh
    private Matrix4x4[] bindPoses; //bindPoses 信息
    private List<Vector3> sourcePoints; //原模型的顶点信息
    private List<Vector3> newPoints; //新（进行动画采样后的）顶点信息
    private int frameCount = 0; //帧数，用于控制动画播放
    private BoneWeight[] boneWeights;
    

    public NativeArray<Vector3> sourcePointsArray;

    public NativeArray<BoneWeight> boneWeightsArray;
    public NativeArray<Vector3> result;
    public NativeArray<Matrix4x4> frameMatrix;
    public NativeArray<Matrix4x4> bindMatrix;
    
    // Start is called before the first frame update
    void Start() {
        Application.targetFrameRate = 60;
        mesh = GetComponentInChildren<MeshFilter>().mesh;
        sourcePoints = new List<Vector3>();
        mesh.GetVertices(sourcePoints);
        bindPoses = mesh.bindposes;
        newPoints = new List<Vector3>(sourcePoints);
        boneWeights = mesh.boneWeights;
        
        sourcePointsArray = new NativeArray<Vector3>(sourcePoints.Count, Allocator.Persistent);
        sourcePointsArray.CopyFrom(sourcePoints.ToArray());
        boneWeightsArray = new NativeArray<BoneWeight>(boneWeights.Length, Allocator.Persistent);
        boneWeightsArray.CopyFrom(boneWeights);
        result = new NativeArray<Vector3>(sourcePoints.Count, Allocator.Persistent);
        bindMatrix = new NativeArray<Matrix4x4>(bindPoses.Length, Allocator.Persistent);
        bindMatrix.CopyFrom(bindPoses);
        frameMatrix = new NativeArray<Matrix4x4>(animData.frameDatas[0].matrix4X4s.Length, Allocator.Persistent);
    }

    // Update is called once per frame
    void Update()
    {
        if (frameCount < animData.frame) {
            ApplyFrame();
            frameCount++;
        } else {
            frameCount = 0;
        }
    }
    
    // 将两个浮点值相加的作业
    public struct MyParallelJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> sourcePoints;
        [ReadOnly]
        public NativeArray<BoneWeight> boneWeightsArray;
        public NativeArray<Vector3> result;
        [ReadOnly]
        public NativeArray<Matrix4x4> frameMatrix;
        [ReadOnly]
        public NativeArray<Matrix4x4> bindMatrix;

        public void Execute(int i)
        {
            var point = sourcePoints[i];
            BoneWeight boneWeight = boneWeightsArray[i];
            Matrix4x4 tempMat0 = frameMatrix[boneWeight.boneIndex0] * bindMatrix[boneWeight.boneIndex0];
            Matrix4x4 tempMat1 = frameMatrix[boneWeight.boneIndex1] * bindMatrix[boneWeight.boneIndex1];
            Matrix4x4 tempMat2 = frameMatrix[boneWeight.boneIndex2] * bindMatrix[boneWeight.boneIndex2];
            Matrix4x4 tempMat3 = frameMatrix[boneWeight.boneIndex3] * bindMatrix[boneWeight.boneIndex3];

            Vector3 tmp = tempMat0.MultiplyPoint(point) * boneWeight.weight0 + tempMat1.MultiplyPoint(point) * boneWeight.weight1 + tempMat2.MultiplyPoint(point) * boneWeight.weight2 + tempMat3.MultiplyPoint(point) * boneWeight.weight3;
            result[i] = tmp;
        }
    }
    private void ApplyFrame() {
        AnimationData.FrameData frameData = animData.frameDatas[frameCount];
        frameMatrix.CopyFrom(frameData.matrix4X4s);
        // for (int i = 0; i < sourcePoints.Count; i++) {
        //     var point = sourcePoints[i];
        //     BoneWeight boneWeight = boneWeights[i];
        //     Matrix4x4 tempMat0 = frameData.matrix4X4s[boneWeight.boneIndex0] * bindPoses[boneWeight.boneIndex0];
        //     Matrix4x4 tempMat1 = frameData.matrix4X4s[boneWeight.boneIndex1] * bindPoses[boneWeight.boneIndex1];
        //     Matrix4x4 tempMat2 = frameData.matrix4X4s[boneWeight.boneIndex2] * bindPoses[boneWeight.boneIndex2];
        //     Matrix4x4 tempMat3 = frameData.matrix4X4s[boneWeight.boneIndex3] * bindPoses[boneWeight.boneIndex3];
        //
        //     Vector3 tmp = tempMat0.MultiplyPoint(point) * boneWeight.weight0 + tempMat1.MultiplyPoint(point) * boneWeight.weight1 + tempMat2.MultiplyPoint(point) * boneWeight.weight2 + tempMat3.MultiplyPoint(point) * boneWeight.weight3;
        //     newPoints[i] = tmp;
        // }
        MyParallelJob jobData = new MyParallelJob();
        jobData.sourcePoints = sourcePointsArray;  
        jobData.boneWeightsArray = boneWeightsArray;
        jobData.result = result;
        jobData.frameMatrix = frameMatrix;
        jobData.bindMatrix = bindMatrix;
        // 调度作业，为结果数组中的每个索引执行一个 Execute 方法，且每个处理批次只处理一项
        JobHandle handle = jobData.Schedule(result.Length, 1);
        // 等待作业完成
        handle.Complete();
        mesh.SetVertices(result);
    }
}
