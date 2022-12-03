Shader "Mobge/Blur"
{
    Properties
    {
        _Radius ("Radius In ", float) = 0.03
        _SampleCount ("SampleCount", int) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        GrabPass
        {
            "_GrabTextureBlur"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _GrabTextureBlur;
            half _Radius;
            int _SampleCount;

            v2f vert (appdata v)
            {
                v2f o;
                float4 hpos = UnityObjectToClipPos(v.vertex);
                o.vertex = hpos;
                o.uv = ComputeGrabScreenPos(hpos / hpos.w);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float step = _Radius / _SampleCount;
                float2 center = i.uv;
                fixed3 color = fixed3(0, 0, 0);
                for(int x = - _SampleCount; x <= _SampleCount; x++) {
                    for(int y = - _SampleCount; y <= _SampleCount; y++) {
                        float2 p = center;
                        p.x += x * step;
                        p.y += y * step;
                        fixed4 bg = tex2D(_GrabTextureBlur, p);
                        color += bg.rgb;
				    }
				}
                fixed rc = _SampleCount * 2 + 1;
                color = color * (1 / (rc * rc));
                color = (i.color - color.rgb) * i.color.a + color.rgb;
                //return tex2D(_GrabTextureBlur, i.uv);
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
