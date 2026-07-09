namespace Gamelogic.Extensions.Editor.Internal
{
	// Contains constants used to implement the tools, mostly UI, and locations (URLs, paths). 
	public static class Constants
	{
		public const string Gamelogic = nameof(Gamelogic);
		public const string Extensions = nameof(Extensions);

		public const string GamelogicFolder = Gamelogic + "/";
		public const string ToolsMenuRoot = "Tools/" + GamelogicFolder;
		public const string ComponentsRoot = GamelogicFolder;
		public const string ExtensionsComponentsRoot = ComponentsRoot + Extensions + "/";
		public const string SupportEmail = "support@gamelogic.co.za";
	}
}
