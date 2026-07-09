using UnityEditor;
using UnityEngine;

namespace Gamelogic.Extensions.Editor.Internal
{
	/// <summary>
	/// A simple editor window that displays the support email address and allows the user to copy it to the clipboard.
	/// </summary>
	internal class EmailSupportWindow : EditorWindow
	{
		private const string EmailIconName = "Email";
		
		// Serialized to make window work when recompiling scripts
		[SerializeField] private Texture2D emailIcon;
		
		/// <summary>
		/// Opens the email support window with a fixed size and loads the email icon resource.
		/// </summary>
		public static void Open()
		{
			var window = GetWindow<EmailSupportWindow>(true, "Email Support", true);
			window.minSize = new Vector2(280, 100);
			window.maxSize = new Vector2(280, 100);
			window.LoadResources();
		}
		
		private void LoadResources()
		{
			emailIcon = AssetTools.LoadIcon(EmailIconName);
		}

		private void OnGUI()
		{
			GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(16, 16, 16, 16) });
			GUILayout.Label("For support, please email us at:");
			GUILayout.Space(8);
			WelcomeWindow.DrawSupportEmail(emailIcon);
			GUILayout.EndVertical();
		}
	}
}
