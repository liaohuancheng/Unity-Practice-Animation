using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class MatrixExtractorToTexture : EditorWindow
{
    [MenuItem("MyWindows/MatrixExtractorToTexture")]
    public static void OpenWindow()
    {
        EditorWindow.GetWindow<MatrixExtractorToTexture>().Show();
    }

    private Animator m_animator;
    private AnimationClip animationClip;
    private GameObject m_obj;
    private Transform[] m_bones;
    private Mesh mesh;
    private bool doExtract;
    private string m_dir;
    private List<AnimationData.FrameData> m_frameList = new List<AnimationData.FrameData>();
    private PlayableGraph m_graph;
    private AnimationClipPlayable clipPlayable;
    private float m_sampleTime;
    private int m_frameCount;
    private float m_perFrameTime;
    private int frameCounter = 0;
    private SkinnedMeshRenderer smr;
    private Texture2D texture;

    private void OnEnable()
    {
        m_graph = PlayableGraph.Create();
        EditorApplication.update += Extract;
    }
    private void OnDisable()
    {
        m_graph.Destroy();
        EditorApplication.update -= Extract;
    }
    private void OnGUI()
    {
        m_animator = EditorGUILayout.ObjectField(m_animator, typeof(Animator), true) as Animator;
        animationClip = EditorGUILayout.ObjectField(animationClip, typeof(AnimationClip), false) as AnimationClip;
        if (GUILayout.Button("解析矩阵"))
        {
            var dir = EditorUtility.SaveFolderPanel("导出动画数据", "", "");
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace("\\", "/");
                if (!dir.StartsWith(Application.dataPath))
                {
                    Debug.LogError("请选择以【Assets/...】开头的文件夹路径");
                    return;
                }
                dir = dir.Replace(Application.dataPath, "Assets");
                ExportAnim(dir);
            }
        }
    }
    private AnimationData.FrameData GetFrameData()
    {
        AnimationData.FrameData frameData = new AnimationData.FrameData();
        frameData.time = m_sampleTime;
        List<Matrix4x4> matrix4X4s = new List<Matrix4x4>();
        foreach (var bone in m_bones)
        {
            matrix4X4s.Add(bone.localToWorldMatrix);
        }
        frameData.matrix4X4s = matrix4X4s.ToArray();
        return frameData;
    }
    private void Extract()
    {
         if (doExtract)
        {
            if (Application.isPlaying)
            {
                if (frameCounter < m_frameCount)
                {
                    clipPlayable.SetTime(m_sampleTime);
                    smr.BakeMesh(mesh);
                    for (int i = 0; i < mesh.vertexCount; i++) {
                        var vertex = mesh.vertices[i];
                        texture.SetPixel(i,frameCounter,new Color(vertex.x, vertex.y, vertex.z));
                    }
                    m_sampleTime += m_perFrameTime;
                    frameCounter++;
                }
                else {
                    texture.Apply();
                    SaveAssets();
                    doExtract = false;
                }
            }
            else
            {
                Debug.LogError("Playable 必须在 runtime下进行采样，请先Play后再采样");
                doExtract = false;
            }
        }
    }
    private void SaveAssets() {
        

        string path = m_dir + "/" + $"AnimationExtract{animationClip.name}" + ".asset";
        AssetDatabase.CreateAsset(texture, path);
        AssetDatabase.Refresh();
    }
    private void ExportAnim(string dir)
    {
        if (m_animator != null)
        {
            m_obj = m_animator.gameObject;
        }
        m_obj.transform.position = Vector3.zero;
        m_obj.transform.rotation = Quaternion.identity;
        m_obj.transform.localScale = Vector3.one;
        smr = m_obj.GetComponentInChildren<SkinnedMeshRenderer>();
        mesh = new Mesh();
        m_bones = m_obj.GetComponentInChildren<SkinnedMeshRenderer>().bones;
        doExtract = true;
        frameCounter = 0;
        m_dir = dir;
        m_frameCount = (int)(animationClip.frameRate * animationClip.length);
        m_perFrameTime = animationClip.length / m_frameCount; ;
        texture = new Texture2D(smr.sharedMesh.vertexCount, m_frameCount, TextureFormat.RGBAHalf, true);
        SetPlayableGraph();
    }
    private void SetPlayableGraph()
    {
        clipPlayable = AnimationClipPlayable.Create(m_graph, animationClip);
        AnimationPlayableUtilities.Play(m_animator, clipPlayable, m_graph);
        clipPlayable.Pause();
    }
}