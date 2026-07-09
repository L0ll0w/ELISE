using System;
using Gamelogic.Extensions;
using Gamelogic.Fx.Internal;
using UnityEngine;

namespace Gamelogic.Fx.Mapping.Maps
{
	/// <summary>
	/// Properties for rendering out an ID map.
	/// See <see href="../common/docs/map-renderers-reference-common.html#id-map"/>.
	/// </summary>
	[Serializable]
	public sealed class IDMapProperties : MapProperties
	{
		private const float HashK = 43758.5453f;
	
		/// <inheritdoc/>
		public override string ShaderName => Constants.MapsRoot + ShaderNames.ConstantColor;

		/// <inheritdoc/>
		public override void SetRendererProperties(MaterialPropertyBlock block, Renderer renderer)
		{
			int id = renderer.GetInstanceID();
			var color = ColorFromId(id);
		
			block.SetColor("_Color", color);
		}
		
		private Color ColorFromId(int id)
		{
			float r = Mangle(12.9898f);
			float g = Mangle(73.1563f);
			float b = Mangle(37.719f);

			return new Color(r, g, b);

			float Mangle(float coefficient) => GLMathf.Frac(Mathf.Sin(coefficient * id * HashK));
		}
	}
}
