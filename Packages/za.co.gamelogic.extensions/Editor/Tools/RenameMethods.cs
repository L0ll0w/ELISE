using UnityEngine;
using UnityEditor;
using Gamelogic.Extensions.Editor.Internal;
using Gamelogic.Extensions.Internal;
using System.Collections.Generic;

namespace Gamelogic.Extensions.Editor
{	
	/// <summary>Provides menu commands and utility methods for renaming selected objects sequentially in the Unity Editor.</summary>
	[Version(4, 5, 0)]
	public static class RenameMethods
	{
		/// <summary>
		/// Renames the selected objects sequentially starting from 0.
		/// </summary>
		[MenuItem(Constants.ToolsMenuRoot + "/Rename/Sequential From 0")]
		public static void RenameSequentiallyFrom0() => RenameSequentially(0);

		/// <summary>
		/// Renames the selected objects sequentially starting from 1.
		/// </summary>
		[MenuItem(Constants.ToolsMenuRoot + "/Rename/Sequential From 1")]
		public static void RenameSequentiallyFrom1() => RenameSequentially(1);

		/// <summary>
		/// Renames the currently selected objects using their first object's name
		/// as the base name, starting from the given index.
		/// </summary>
		/// <param name="start">The starting number for the sequence.</param>
		public static void RenameSequentially(int start)
		{
			var objects = Selection.objects;
			string baseName = objects[0].name;

			RenameSequentially(objects, baseName, start);
		}

		/// <summary>
		/// Renames the given objects sequentially using the provided base name
		/// and starting index.
		/// </summary>
		/// <param name="objects">The objects to rename.</param>
		/// <param name="baseName">The name prefix for all renamed objects.</param>
		/// <param name="start">The starting number for the sequence.</param>
		public static void RenameSequentially(IEnumerable<Object> objects, string baseName, int start)
		{
			int index = start;

			foreach (var obj in objects)
			{
				string newName = $"{baseName} {index}";
				obj.name = newName;
				index++;
			}
		}
	}
}