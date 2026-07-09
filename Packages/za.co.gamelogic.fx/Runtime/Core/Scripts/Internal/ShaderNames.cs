using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	/// <summary>
	/// Contains names of the shaders used in this package. 
	/// </summary>
	internal static class ShaderNames
	{
		public const string AddTexture = nameof(AddTexture);
		public const string AdjustGamma = nameof(AdjustGamma);
		public const string BlendTexture = nameof(BlendTexture);
		public const string Desaturate = nameof(Desaturate);
		public const string Pixelate = nameof(Pixelate);
		public const string Quantize = nameof(Quantize);
		public const string TriToneMap = nameof(TriToneMap);
		public const string BoxBlur = nameof(BoxBlur);
		public const string GaussianBlur = nameof(GaussianBlur);
		public const string Min = nameof(Min);
		public const string Max = nameof(Max);
		public const string PowerMean = nameof(PowerMean);
		public const string BilateralFilter = nameof(BilateralFilter);
		public const string ConvexHullMap = nameof(ConvexHullMap);
		public const string MixboxConvexHullMap = nameof(MixboxConvexHullMap);
		
		
		public const string Invert = nameof(Invert);
		public const string AdjustSaturation = nameof(AdjustSaturation);
		public const string QuadToneMap = nameof(QuadToneMap);
		
		public const string SimpleOutline = nameof(SimpleOutline);
		public const string ProceduralMask = nameof(ProceduralMask);
		
		public const string UVMap = nameof(UVMap);
		public const string UVBackground = nameof(UVBackground);
		public const string NormalMap = nameof(NormalMap);
		public const string ConstantColor = nameof(ConstantColor);
		public const string DepthMap = nameof(DepthMap);
		
		/// <summary>
		/// Gets a shader by name, logging an error if it cannot be found.
		/// </summary>
		/// <param name="name">The name of the shader.</param>
		/// <returns>The shader, or null if it could not be found.</returns>
		internal static Shader GetShader(string name)
		{
			var shader = Shader.Find(name);
		
			if (shader == null)
			{
				Debug.LogError($"Could not find shader {name}. Make sure it is included in the build.");
			}

			return shader;
		}
	}
}
