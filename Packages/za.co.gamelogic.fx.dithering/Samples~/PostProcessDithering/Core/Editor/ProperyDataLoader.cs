using System.Collections.Generic;
using Gamelogic.Extensions;
using Gamelogic.Fx;
using UnityEditor;

[InitializeOnLoad]
public class PropertyDataLoader
{
	private static readonly IDictionary<string, FloatMatrix> CustomPresets = new Dictionary<string, FloatMatrix>
	{
		["Custom 2x2"] = new FloatMatrix(2, 2, new[] { 0f, 0.5f, 0.75f, 0.25f }),
		["Custom 3x3"] = new FloatMatrix(3, 3, new[] { 0f, 0.7f, 0.4f, 0.5f, 0.2f, 0.9f, 0.3f, 0.8f, 0.1f }),
		["Custom 4x4"] = new FloatMatrix(4, 4, new[]
		{
			0f, 0.8f, 0.2f, 0.6f,
			0.5f, 0.3f, 0.7f, 0.1f,
			0.25f, 0.75f, 0.15f, 0.55f,
			0.65f, 0.05f, 0.45f, 0.35f
		})
	};
	
	static PropertyDataLoader()
	{
		PropertyDrawerData.RegisterValuesRetriever(
			"MyPresets",
			() => CustomPresets
		);

		
	}
}
