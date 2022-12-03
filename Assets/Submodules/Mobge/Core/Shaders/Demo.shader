Shader "Unlit/Demo"
{
    Properties
    {
        _MyFloat ("My float", Float) = 1.5
        _MyColor ("My Color", Vector) = (1, 0, 0, 1) // (x, y, z, w)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members color)
#pragma exclude_renderers d3d11

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float3 color : COLOR;
            };
            
            float _MyFloat;
            fixed4 _MyColor;
            
            struct v2f
            {
                float3 color : COLOR;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = _MyColor.rgb;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _MyColor;
            }
            ENDCG
        }
    }
}
