using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gamelogic.Extensions.Editor
{
	/// <summary>
	/// A lightweight auxiliary window that displays a scrollable list of options
	/// and invokes a callback when the user selects an item.  
	/// Its behavior is similar to the color selector window.
	/// </summary>
	/// <remarks>
	/// The popup is displayed using <see cref="EditorWindow.ShowAuxWindow"/>.
	///
	/// <see cref="EditorWindow.ShowAuxWindow"/>
	/// </remarks>

	public sealed class ListSelectionPopup : EditorWindow
	{
		private const float MinWidth = 100f;
		private const float DefaultWidth = 200f;
		private string[] labels;
		private Action<int> onSelected;
		private Vector2 scroll;

		/// <summary>
		/// Shows a selector as a auxiliary window.
		/// </summary>
		/// <param name="title">Title of the window.</param>
		/// <param name="labels">List of labels to show.</param>
		/// <param name="onSelected">Called with index of the chosen option.</param>
		/// <seealso cref="EditorWindow.ShowAuxWindow"/>
		public static EditorWindow Show(string title, string[] labels, Action<int> onSelected)
		{
			labels.ThrowIfNull(nameof(labels));

			var window = CreateInstance<ListSelectionPopup>();
			
			window.titleContent = new GUIContent(title);
			window.labels = labels;
			window.onSelected = onSelected;

			float width = DefaultWidth;
			float buttonHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			float height = (labels.Length + 1) * buttonHeight - EditorGUIUtility.standardVerticalSpacing;
			
			window.minSize = new Vector2(MinWidth, buttonHeight);
			window.maxSize = new Vector2(Screen.width, height);
			
			window.position = new Rect(
				Screen.width / 2f - width / 2,
				Screen.height / 2f - height / 2,
				width,
				height
			);
			
			window.ShowAuxWindow();
			window.Focus();

			return window;
		}

		private void OnGUI()
		{
			scroll = GUILayout.BeginScrollView(scroll);

			for (int i = 0; i < labels.Length; i++)
			{
				if (GUILayout.Button(labels[i]))
				{
					onSelected?.Invoke(i);
					Close();
					return;
				}
			}

			GUILayout.EndScrollView();
		}
	}
}
