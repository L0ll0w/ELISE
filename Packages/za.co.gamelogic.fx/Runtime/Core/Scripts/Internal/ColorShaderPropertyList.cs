using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamelogic.Extensions.Algorithms;
using UnityEngine;

namespace Gamelogic.Fx.Internal
{
	/// <summary>
	/// This class encapsulates a list of colors that are defined as separate shader properties, suffixed with an index.
	/// </summary>
	/// <remarks>
	/// This class is meant for post process components that allows users to configure shader properties in the
	/// inspector. 
	/// </remarks>
	/*	Design note: Internal because this class is tricky to use correctly.
	*/
	[Serializable]
	internal struct ColorShaderPropertyList : IEnumerable<Color>
	{
		private int[] propertyIDs;
		private int countPropertyId;
		
		[SerializeField] private List<Color> colors;
		
		public IEnumerable<Color> Colors
		{
			get => colors;
			set => colors = value?.ToList();
		}

		/// <summary>
		/// Call this method in a component's <c>OnValidate</c> to ensure the list of colors does not exceed the specified maximum count.
		/// </summary>
		/// <param name="maxCount">The maximum number of colors allowed in the list.</param>
		public void ValidateMaxCount(int maxCount)
		{
			if (colors == null)
			{
				return;
			}
			
			if (colors.Count > maxCount)
			{
				colors.ForceCount(maxCount);
			}
		}

		public void SetBaseName(string propertyNameBase, int firstIndex = 0, string countPostFix = "Count")
		{
			if (colors == null)
			{
				return;
			}
			
			propertyIDs = colors
				.WithIndices()
				.Select((_, index) => propertyNameBase + (index + firstIndex))
				.Select( Shader.PropertyToID)
				.ToArray();
			
			countPropertyId = Shader.PropertyToID(propertyNameBase + countPostFix);
		}

		public void SetOn(Material material)
		{
			if (colors == null)
			{
				return;
			}
			
			if (propertyIDs == null || propertyIDs.Length != colors.Count)
			{
				throw new InvalidOperationException(
					$"Property IDs have not been initialized. Call {nameof(SetBaseName)} before calling SetOn, typically in Awake of " +
					$"the component where {nameof(ColorShaderPropertyList)} is used.");
			}

			for (int i = 0; i < colors.Count; i++)
			{
				material.SetColor(propertyIDs[i], colors[i]);
			}
#if UNITY_2021_2_OR_NEWER
			material.SetInteger(countPropertyId, colors.Count);
#else
			material.SetInt(countPropertyId, colors.Count);
#endif
		}

		public IEnumerator<Color> GetEnumerator() => colors.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		
		internal readonly ColorShaderPropertyList Clone()
			=> new()
			{
				colors = colors != null ? new List<Color>(colors) : null
			};
	}
}
