using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using LobbyCompatibility.Attributes;
using LobbyCompatibility.Enums;
using OIIA.Scripts;
using UnityEngine;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;

namespace OIIA;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
[LobbyCompatibility(CompatibilityLevel.ClientOnly, VersionStrictness.None)]
public class OIIA : BaseUnityPlugin
{
    public static OIIA Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    public static ContentLoader ContentLoader = null!;

    public static AssetBundle MainAssetBundle { get; private set; } = null!;

    private static readonly string ASSETS_PATH = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), MyPluginInfo.PLUGIN_NAME);


    public List<AudioClip> OIIAClips = [];

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(ASSETS_PATH, "oiiaasset"));

        ContentLoader = new ContentLoader(Instance.Info, MainAssetBundle, (content, prefab) => { });

        ContentLoader.Register(new ContentLoader.ScrapItem("OIIA_CAT", "Assets/OIIA/OIIACat.asset", 6, Levels.LevelTypes.All, null, (Item item) =>
        {
            var script = item.spawnPrefab.AddComponent<OIIACatScript>();
            script.itemProperties = item;
        }));

        for (int i = 1; i <= 6; i++)
        {
            OIIAClips.Add(MainAssetBundle.LoadAsset<AudioClip>($"Assets/OIIA/Sounds/OIIA_{i}.wav"));
            OIIAClips.Last().LoadAudioData();
        }

        Patch();

        NetcodePatcher();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }

    private static void NetcodePatcher()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }
}
