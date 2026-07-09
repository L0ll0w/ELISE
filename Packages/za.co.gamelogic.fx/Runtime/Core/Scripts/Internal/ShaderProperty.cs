using System;
using Gamelogic.Extensions;
using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	/// <summary>
	/// Represents a shader property. 
	/// </summary>
	/// <remarks>
	/// As is often the case in Unity, the class represents each type of variable with a different field, but only the
	/// relevant field is shown in the inspector.  
	/// </remarks>
	[Serializable]
	internal sealed class ShaderProperty
	{
		[SerializeField] private string name = null;
		
		[StringPopup(new []
		{
			ShaderPropertyType.Bool,
			ShaderPropertyType.Int,
			ShaderPropertyType.Integer,
			ShaderPropertyType.Float,

			ShaderPropertyType.Color,
			ShaderPropertyType.Vector,

			ShaderPropertyType.FloatArray,
			ShaderPropertyType.IntegerArray,
			ShaderPropertyType.MatrixArray,
			
			ShaderPropertyType.Texture,
			ShaderPropertyType.FloatArrayCurve,
			
			ShaderPropertyType.Keyword,
			ShaderPropertyType.KeywordSet,

			ShaderPropertyType.ScreenTextureSize,
			ShaderPropertyType.ScreenAspectRatio,
		})]
		[SerializeField] private string shaderPropertyType = null;

		// We do not guarantee to retain the old value oif the type changes. 
		[SerializeField] private bool boolValue = false;
		[SerializeField] private int intValue = 0;
		[SerializeField] private float floatValue = 0f;
		[SerializeField] private Texture textureValue = null;
		[SerializeField] private Keyword keywordValue = null;
		[SerializeField] private Color colorValue = Color.white;
		[SerializeField] private Vector4 vectorValue = Vector4.zero;
		[SerializeField] private float[] floatArrayValue = null;
		[SerializeField] private int[] intArrayValue = null;
		[SerializeField] private KeywordSet keywordSetValue = null;
		[SerializeField] private MatrixArray matrixArrayValue = null;
		[SerializeField] private AnimationCurve curveValue = AnimationCurve.Linear(0, 0, 1, 1);

		/// <summary>
		/// Applies this shader property to the given material that presumably supports it.
		/// </summary>
		/// <param name="material">The material to apply the property to.</param>
		public void SetOn(Material material)
		{
			switch (shaderPropertyType)
			{
				case ShaderPropertyType.Bool:
#if UNITY_2021_3_OR_NEWER
					material.SetInteger(name, boolValue ? 1 : 0);
#else
					material.SetInt(name, boolValue ? 1 : 0);
#endif
					break;
				case ShaderPropertyType.Int:
					material.SetInt(name, intValue);
					break;
				case ShaderPropertyType.Integer:
#if UNITY_2021_3_OR_NEWER
					material.SetInteger(name, intValue);
#else
					material.SetInt(name, intValue);
#endif
					break;
				case ShaderPropertyType.Float:
					material.SetFloat(name, floatValue);
					break;
				case ShaderPropertyType.Texture:
					material.SetTexture(name, textureValue);
					break;
				case ShaderPropertyType.Keyword:
					if (keywordValue.enabled)
					{
						material.EnableKeyword(keywordValue.name);
					}
					else
					{
						material.DisableKeyword(keywordValue.name);
					}

					break;
				case ShaderPropertyType.KeywordSet:
					if (keywordSetValue == null) break;
					material.Set(keywordSetValue.keywords);

					break;
				case ShaderPropertyType.Color:
					material.SetColor(name, colorValue);
					break;
				case ShaderPropertyType.Vector:
					material.SetVector(name, vectorValue);
					break;
				case ShaderPropertyType.FloatArray:
					material.SetFloatArray(name, floatArrayValue);
					break;
				case ShaderPropertyType.IntegerArray:
					
					if(floatArrayValue == null || floatArrayValue.Length != intArrayValue.Length)
					{
						floatArrayValue = new float[intArrayValue.Length];
					}
					
					for (int i = 0; i < intArrayValue.Length; i++)
					{
						floatArrayValue[i] = intArrayValue[i];
					}
					material.SetFloatArray(name, floatArrayValue);
					break;
				case ShaderPropertyType.MatrixArray:
					material.SetFloatArray(name, matrixArrayValue.values);
					break;
				case ShaderPropertyType.ScreenTextureSize:
					float newWidth = Screen.width / vectorValue.x * vectorValue.z;
					float newHeight = Screen.height / vectorValue.y * vectorValue.z;

					material.SetVector(name, new Vector4(newWidth, newHeight, 0, 0));

					break;
				case ShaderPropertyType.ScreenAspectRatio:
					material.SetFloat(name, Screen.width / (float)Screen.height);
					break;
				case ShaderPropertyType.FloatArrayCurve:
					float[] curveValues = new float[64];
					for (int i = 0; i < curveValues.Length; i++)
					{
						float t = i / (float)(curveValues.Length - 1);
						curveValues[i] = curveValue.Evaluate(t);
					}
					material.SetFloatArray(name, curveValues);
					break;
			}
		}
	}
}
