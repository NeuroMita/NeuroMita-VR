Shader "NeuroMita/VR Comfort"
{
    Properties
    {
        _Vignette ("Vignette", Range(0,1)) = 0
        _Fade ("Fade", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay+1000" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float _Vignette;
            float _Fade;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = float4(v.vertex.xy, UNITY_NEAR_CLIP_VALUE, 1);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float radius = lerp(1.45, 0.55, saturate(_Vignette));
                float edge = smoothstep(radius, radius + 0.25, length(i.uv * 2 - 1));
                return fixed4(0, 0, 0, max(saturate(_Fade), edge * 0.95));
            }
            ENDCG
        }
    }
}