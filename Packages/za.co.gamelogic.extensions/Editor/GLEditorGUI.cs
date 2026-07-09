// Copyright Gamelogic (c) http://www.gamelogic.co.za

using Gamelogic.Extensions.Internal;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// Functions to supplement Unity EditorGUI functions.
	/// </summary>
	[Version(1, 2, 0)]
	public static class GLEditorGUI
	{
		/// <summary>The <see cref="GUIStyle"/> used to draw horizontal splitter lines.</summary>
		public static readonly GUIStyle SplitterStyle;
		/// <summary>The <see cref="GUIStyle"/> used to draw vertical lines.</summary>
		public static readonly GUIStyle LineStyle;

		private static readonly Color SplitterColor = EditorGUIUtility.isProSkin ? new Color(0.157f, 0.157f, 0.157f) : new Color(0.5f, 0.5f, 0.5f);
		static GLEditorGUI()
		{
			SplitterStyle = new GUIStyle
			{
				normal = {background = EditorGUIUtility.whiteTexture},
				stretchWidth = true,
				margin = new RectOffset(0, 0, 7, 7)
			};

			LineStyle = new GUIStyle
			{
				normal = { background = EditorGUIUtility.whiteTexture },
				stretchWidth = true,
				margin = new RectOffset(0, 0, 0, 0)
			};
		}



		// GUILayout Style
		/// <summary>Draws a horizontal splitter line with the specified color and thickness using GUILayout.</summary>
		/// <param name="rgb">The color of the splitter.</param>
		/// <param name="thickness">The thickness of the splitter in pixels.</param>
		public static void Splitter(Color rgb, float thickness = 1)
		{
			Rect position = GUILayoutUtility.GetRect(GUIContent.none, SplitterStyle, GUILayout.Height(thickness));

			if (Event.current.type == EventType.Repaint)
			{
				Color restoreColor = GUI.color;
				GUI.color = rgb;
				SplitterStyle.Draw(position, false, false, false, false);
				GUI.color = restoreColor;
			}
		}

		/// <summary>Draws a horizontal splitter line with the specified thickness and style using GUILayout.</summary>
		/// <param name="thickness">The thickness of the splitter in pixels.</param>
		/// <param name="splitterStyle">The <see cref="GUIStyle"/> to use for drawing.</param>
		public static void Splitter(float thickness, GUIStyle splitterStyle)
		{
			Rect position = GUILayoutUtility.GetRect(GUIContent.none, splitterStyle, GUILayout.Height(thickness));

			if (Event.current.type == EventType.Repaint)
			{
				Color restoreColor = GUI.color;
				GUI.color = SplitterColor;
				splitterStyle.Draw(position, false, false, false, false);
				GUI.color = restoreColor;
			}
		}

		/// <summary>Draws a vertical line using the default splitter color and a thickness of 2.</summary>
		public static void VerticalLine()
		{
			VerticalLine(SplitterColor, 2);
		}

		/// <summary>Draws a vertical line with the specified color and thickness using GUILayout.</summary>
		/// <param name="color">The color of the line.</param>
		/// <param name="thickness">The thickness of the line in pixels.</param>
		public static void VerticalLine(Color color, float thickness = 1)
		{
			Rect position = GUILayoutUtility.GetRect(
				GUIContent.none,
				LineStyle, 
				GUILayout.Width(thickness),
				GUILayout.ExpandHeight(true));

			if (Event.current.type == EventType.Repaint)
			{
				Color restoreColor = GUI.color;
				GUI.color = color;
				LineStyle.Draw(position, false, false, false, false);
				GUI.color = restoreColor;
			}
		}

		/// <summary>Draws a vertical line with the specified thickness and style using GUILayout.</summary>
		/// <param name="thickness">The thickness of the line in pixels.</param>
		/// <param name="splitterStyle">The <see cref="GUIStyle"/> to use for drawing.</param>
		public static void VerticalLine(float thickness, GUIStyle splitterStyle)
		{
			Rect position = GUILayoutUtility.GetRect(GUIContent.none, splitterStyle, GUILayout.Width(thickness));

			if (Event.current.type == EventType.Repaint)
			{
				Color restoreColor = GUI.color;
				GUI.color = SplitterColor;
				splitterStyle.Draw(position, false, false, false, false);
				GUI.color = restoreColor;
			}
		}

		/// <summary>Draws a horizontal splitter line with the default style and the specified thickness using GUILayout.</summary>
		/// <param name="thickness">The thickness of the splitter in pixels.</param>
		public static void Splitter(float thickness = 1)
		{
			Splitter(thickness, SplitterStyle);
		}

		// GUI Style
		/// <summary>Draws a horizontal splitter line at the specified position.</summary>
		/// <param name="position">The rectangle defining the position and size of the splitter.</param>
		public static void Splitter(Rect position)
		{
			if (Event.current.type == EventType.Repaint)
			{
				Color restoreColor = GUI.color;
				GUI.color = SplitterColor;
				SplitterStyle.Draw(position, false, false, false, false);
				GUI.color = restoreColor;
			}
		}

		/// <summary>
		/// Draws a bold header label in the Unity Editor.
		/// </summary>
		/// <param name="text">The header text to display in bold.</param>
		[Version(4, 5, 0)]
		public static void Header(string text) => EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
	}
}
