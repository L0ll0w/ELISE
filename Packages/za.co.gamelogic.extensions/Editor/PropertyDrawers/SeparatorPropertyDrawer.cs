// Copyright Gamelogic Pty Ltd (c) http://www.gamelogic.co.za

using Gamelogic.Extensions.Internal;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>Property drawer for <see cref="SeparatorAttribute"/> that draws a colored horizontal line as a decorator.</summary>
	[CustomPropertyDrawer(typeof(SeparatorAttribute))]
	[Version(4, 3, 0)]
	public class SeparatorDrawer : DecoratorDrawer
	{
		private SeparatorAttribute Attribute => (SeparatorAttribute) attribute;

		/// <inheritdoc />
		public override float GetHeight()
		{
			return Attribute.Height;
		}

		/// <inheritdoc />
		public override void OnGUI(Rect position)
		{
			EditorGUI.DrawRect(position, Attribute.Color);
		}
	}
}
