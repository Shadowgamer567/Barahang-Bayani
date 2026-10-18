
Shader "UI/TutorialSpotlightCutout"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0, 0, 0, 0.75)
        _Cutout ("Cutout Rectangle", Vector) = (0.3, 0.3, 0.7, 0.7)
        _Cutout2 ("Second Cutout Rectangle", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _Color;
            float4 _Cutout;
            float4 _Cutout2;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = _Color;

                bool insideFirst =
                    i.uv.x >= _Cutout.x &&
                    i.uv.x <= _Cutout.z &&
                    i.uv.y >= _Cutout.y &&
                    i.uv.y <= _Cutout.w;

                bool insideSecond =
                    i.uv.x >= _Cutout2.x &&
                    i.uv.x <= _Cutout2.z &&
                    i.uv.y >= _Cutout2.y &&
                    i.uv.y <= _Cutout2.w;

                if (insideFirst || insideSecond)
                {
                    color.a = 0;
                }

                return color;
            }

            ENDCG
        }
    }
}
