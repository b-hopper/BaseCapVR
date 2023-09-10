// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/CelEffectsMarchingAnts"
{
	Properties
	{
		_OutlineExtrusion("Outline Extrusion", float) = 0
		_OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
		_OutlineDot("Outline Dot", float) = 0.25
		_OutlineDot2("Outline Dot Distance", float) = 0.5
		_OutlineSpeed("Outline Dot Speed", float) = 50.0 
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

		// Background pass
		Pass {
			
			// alpha channel
			ColorMask A
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			float4 vert(float4 vertex : POSITION) : SV_POSITION {
				return UnityObjectToClipPos(vertex);
			}

			fixed4 frag() : SV_Target {
				return 0,0,0,0;
			}

			ENDCG
		}
		
		// Outline pass
		Pass
		{
			Cull OFF
			ZWrite OFF
			ZTest ON
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			// Properties
			float4 _OutlineColor;
			float  _OutlineSize;
			float  _OutlineExtrusion;
			float  _OutlineDot;
			float  _OutlineDot2;
			float  _OutlineSpeed;

			struct vertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID 
			};

			struct vertexOutput
			{
				float4 pos : SV_POSITION;
				float4 screenCoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;

				UNITY_SETUP_INSTANCE_ID(input); 
				UNITY_INITIALIZE_OUTPUT(vertexOutput, output); 
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				
				float4 newPos = input.vertex;

				float3 normal = normalize(input.normal);
				newPos += float4(normal, 0.0) * _OutlineExtrusion;

				output.pos = UnityObjectToClipPos(newPos);

				output.screenCoord = ComputeScreenPos(output.pos);

				return output;
			}
			
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			
			float4 frag(vertexOutput input) : COLOR
			{
			    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

			    // dotted line with animation
			    float2 pos = input.pos.xy + _Time * _OutlineSpeed;

			    // Convert the source position to clip space
			    float4 worldPos = mul(unity_ObjectToWorld, input.pos);
			    // Then convert to screen space
			    float2 sourceScreenPos = ComputeScreenPos(worldPos).xy;

				// Horizontal marching ants
				float skip = cos(sourceScreenPos.x * _OutlineDot + _Time * _OutlineSpeed);
				
			    clip(skip); // stops rendering a pixel if 'skip' is negative

			    float4 color = _OutlineColor;
			    return color;
			}

			ENDCG
		}
	}
}