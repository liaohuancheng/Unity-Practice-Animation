/*
Created by jiadong chen
http://www.jiadongchen.me
*/

Shader "chenjd/URP/AnimMapShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {} //着色纹理
		_AnimMap ("AnimMap", 2D) ="white" {} //动画纹理
		_AnimLen("Anim Length", Float) = 0 //动画长度，控制播放速率
	}
	
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline"}
        Cull off

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float2 uv : TEXCOORD0;
                float4 pos : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID //GPU Instancing 宏
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID //GPU Instancing 宏
            };


            CBUFFER_START(UnityPerMaterial) 
                float _AnimLen;
                sampler2D _MainTex;
                float4 _MainTex_ST;
                sampler2D _AnimMap;
                float4 _AnimMap_TexelSize;//x == 1/width
            CBUFFER_END 
            
            float4 ObjectToClipPos (float3 pos) //MVP变换
            {
                return mul (UNITY_MATRIX_VP, mul (UNITY_MATRIX_M, float4 (pos,1)));
            }
            
            v2f vert (appdata v, uint vid : SV_VertexID) //重点： SV_VertexID 语义，指明顶点的index
            {
                UNITY_SETUP_INSTANCE_ID(v);

                float f = _Time.y / _AnimLen; //_Time.y = Time.timeSinceLevelLoad，就是场景加载时间

                f = fmod(f,1.0); //fmod(x, y): 返回 x / y 的小数部分. 如: x = i * y + f
                //这里相当于是把f 限制在了0~1，作为 UV中的 V 值
                
                float animMap_x = (vid + 0.5) * _AnimMap_TexelSize.x; 
                //_AnimMap_TexelSize.x  = 1 / Width ，纹素大小
                //vid : 顶点index， 为什么要+0.5?因为一个像素的中心坐标是（x+0.5,y+0.5）；
                //两者相乘，是顶点实际在纹理上的像素中心坐标。

                float animMap_y = f; //相当于UV中的V

                //tex2Dlod，对纹理进行采样
                //ps： 为什么是 tex2Dlod，因为tex2D是无法在Vertex Shader(顶点着色器）中使用的。
                float4 pos = tex2Dlod(_AnimMap, float4(animMap_x, animMap_y, 0, 0)); 
                
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); //顶点原uv的 tiling and offset
                o.vertex = ObjectToClipPos(pos); //把采样Pos 应用MVP变换。
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv); //纹理着色
                return col;
            }
            ENDHLSL
        }
	}
}