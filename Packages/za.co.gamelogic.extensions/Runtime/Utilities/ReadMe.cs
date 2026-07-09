using UnityEngine;
using JetBrains.Annotations;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// A component that has an editable read-only field that can be used to add documentation to a GameObject. 
	/// </summary>
	/// <seealso cref="InspectorTextAttribute"/>
	[Version(4, 5, 0)]
	public class ReadMe : GLMonoBehaviour
	{
#pragma warning disable CS0414 // Field is assigned but its value is never used
		[Tooltip("Documentation text displayed as a read-only label in the inspector.")]
		[InspectorText, UsedImplicitly] [SerializeField]
		private string text = null;
#pragma warning restore CS0414 // Field is assigned but its value is never used
	}
}
