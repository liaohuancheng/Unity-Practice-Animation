using UnityEngine;

public class AnimationData : ScriptableObject
{
    [System.Serializable]
    public class FrameData
    {
        public float time;
        public Matrix4x4[] matrix4X4s;
    }
    [SerializeField]
    public string animName;
    [SerializeField]
    public float animLen;
    [SerializeField]
    public int frame;
    [SerializeField]
    public FrameData[] frameDatas;
}