using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CpuAnimator : MonoBehaviour {
    public AnimationData animData;  //保存的SO文件
    private Mesh mesh; //目标mesh
    private Matrix4x4[] bindPoses; //bindPoses 信息
    private List<Vector3> sourcePoints; //原模型的顶点信息
    private List<Vector3> newPoints; //新（进行动画采样后的）顶点信息
    private int frameCount = 0; //帧数，用于控制动画播放
    private BoneWeight[] boneWeights;
    
    // Start is called before the first frame update
    void Start() {
        mesh = GetComponentInChildren<MeshFilter>().mesh;
        sourcePoints = new List<Vector3>();
        mesh.GetVertices(sourcePoints);
        bindPoses = mesh.bindposes;
        newPoints = new List<Vector3>(sourcePoints);
        boneWeights = mesh.boneWeights;
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

    private void ApplyFrame() {
        AnimationData.FrameData frameData = animData.frameDatas[frameCount];
        for (int i = 0; i < sourcePoints.Count; i++) {
            var point = sourcePoints[i];
            BoneWeight boneWeight = boneWeights[i];
            Matrix4x4 tempMat0 = frameData.matrix4X4s[boneWeight.boneIndex0] * bindPoses[boneWeight.boneIndex0];
            Matrix4x4 tempMat1 = frameData.matrix4X4s[boneWeight.boneIndex1] * bindPoses[boneWeight.boneIndex1];
            Matrix4x4 tempMat2 = frameData.matrix4X4s[boneWeight.boneIndex2] * bindPoses[boneWeight.boneIndex2];
            Matrix4x4 tempMat3 = frameData.matrix4X4s[boneWeight.boneIndex3] * bindPoses[boneWeight.boneIndex3];

            Vector3 tmp = tempMat0.MultiplyPoint(point) * boneWeight.weight0 + tempMat1.MultiplyPoint(point) * boneWeight.weight1 + tempMat2.MultiplyPoint(point) * boneWeight.weight2 + tempMat3.MultiplyPoint(point) * boneWeight.weight3;
            newPoints[i] = tmp;
        }
        mesh.SetVertices(newPoints);
    }
}
