using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HydraMenu.features;
using HydraMenu.routines;
using HydraMenu.ui;

namespace HydraMenu;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInProcess("Among Us.exe")]
public class Hydra : BasePlugin
{
	internal static new ManualLogSource Log;

	internal const string PLUGIN_GUID = "com.mrd.hydramenu";
	internal const string PLUGIN_NAME = "Hydra";
	internal const string PLUGIN_VERSION = "0.1";

    public static RoutineManager routines;
	public static NotificationManager notifications;

	public override void Load()
	{
		Harmony harmony = new Harmony(PLUGIN_GUID);
		harmony.PatchAll();

		AddComponent<MainUI>();
		AddComponent<Roles>();

		notifications = AddComponent<NotificationManager>();
		routines = AddComponent<RoutineManager>();

		Log = base.Log;
		Log.LogInfo($"Plugin {PLUGIN_GUID} has loaded!");
	}

	[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
	class OnGameLoad
	{
		public static void Postfix()
		{
			Log.LogInfo("Adding mod stamp");
			ModManager.Instance.ShowModStamp();
		}
	}
}