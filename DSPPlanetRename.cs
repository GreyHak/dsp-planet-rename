using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace DSPPlanetRename
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class DSPPlanetRename : BaseUnityPlugin
    {
        public const string pluginGuid = "greyhak.dysonsphereprogram.planetrename";
        public const string pluginName = "DSP Planet Rename";
        public const string pluginVersion = "1.0.2";
        new internal static ManualLogSource Logger;
        new internal static BepInEx.Configuration.ConfigFile Config;
        Harmony harmony;

        public class PlanetNamingData
        {
            public int planetId;
            public string originalName;
            public string customName;
        }
        public static Dictionary<int, PlanetNamingData> nameDictionary = new Dictionary<int, PlanetNamingData>();

        public void Awake()
        {
            Logger = base.Logger;  // "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\LogOutput.log"
            Config = base.Config;
            Config.SaveOnConfigSet = false;

            harmony = new Harmony(pluginGuid);
            harmony.PatchAll(typeof(DSPPlanetRename));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameData), "Export")]
        public static void GameData_Export_Postfix()
        {
            Logger.LogDebug("Saving planet names.");
            Config.Save();
        }

        public static string GetGalaxyName(GalaxyData galaxy)
        {
            return galaxy.seed.ToString("D8") + "-" + galaxy.starCount.ToString();
        }

        public static string PlanetHash(string originalPlanetName, GalaxyData galaxy)
        {
            HashAlgorithm algorithm = SHA256.Create();
            byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(GetGalaxyName(galaxy) + originalPlanetName));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StarGen), "CreateStarPlanets")]
        public static void StarGen_CreateStarPlanets_Postfix(GalaxyData galaxy, StarData star, GameDesc gameDesc)
        {
            foreach (PlanetData planet in star.planets)
            {
                InitializePlanetName(planet, galaxy);
            }
        }

        public static void InitializePlanetName(PlanetData __result, GalaxyData galaxy)
        {
            if (DSPGame.IsMenuDemo)
            {
                return;
            }

            if (__result.name.Length == 0)
            {
                return;
            }

            string customName = Config.Bind<string>("Planet Names", PlanetHash(__result.name, galaxy), __result.name, "[Cluster " + GetGalaxyName(galaxy) + "] Replacement planet name for " + __result.name).Value;
            if (customName != __result.name)
            {
                Logger.LogDebug("Loading planet " + __result.name + "'s name " + customName);
            }

            PlanetNamingData existingPlanetNamingData;
            if (nameDictionary.TryGetValue(__result.id, out existingPlanetNamingData))
            {  // This happens when a new game is created.
                existingPlanetNamingData.originalName = __result.name;
                existingPlanetNamingData.customName = customName;
            }
            else
            {
                nameDictionary.Add(__result.id, new PlanetNamingData
                {
                    planetId = __result.id,
                    originalName = __result.name,
                    customName = customName
                });
            }

            __result.name = customName;
        }

        public static DefaultControls.Resources uiResources;
        public static GameObject nameInputObject = null;
        public static InputField nameInput = null;
        public static PlanetData activePlanet = null;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), "_OnCreate")]
        public static void UIPlanetDetail_OnCreate_Postfix(UIPlanetDetail __instance)
        {
            if (nameInputObject == null)
            {
                uiResources = new DefaultControls.Resources();
                nameInputObject = DefaultControls.CreateInputField(uiResources);
                nameInput = nameInputObject.GetComponent<InputField>();

                nameInputObject.transform.SetParent(__instance.transform.parent);
                nameInputObject.transform.localPosition = new Vector3(-145f, -38f, 0f);  // Moving the shorter one
                nameInputObject.transform.localScale = new Vector3(1.06f, 1.3f, 1f);
                nameInput.image.color = new Color(0, 0, 0, 0);
                nameInput.textComponent.color = new Color(1, 1, 1, 1);

                nameInput.onValueChanged.AddListener(new UnityAction<string>(OnNameInputSubmit));
                nameInput.onEndEdit.AddListener(new UnityAction<string>(OnNameInputSubmit));

                nameInputObject.SetActive(false);

                __instance.nameText.enabled = false;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), "_OnOpen")]
        public static void UIPlanetDetail_OnOpen_Postfix(UIPlanetDetail __instance)
        {
            nameInputObject.SetActive(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), "_OnClose")]
        public static void UIPlanetDetail_OnClose_Postfix(UIPlanetDetail __instance)
        {
            activePlanet = null;
            nameInputObject.SetActive(false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), "OnPlanetDataSet")]
        public static void UIPlanetDetail_OnPlanetDataSet_Postfix(UIPlanetDetail __instance)
        {
            if (__instance.planet != null && ((activePlanet == null) || (__instance.planet.id != activePlanet.id)))
            {
                activePlanet = __instance.planet;
                nameInput.text = __instance.planet.name;  // This results in a call to OnNameInputSubmit
            }
        }

        public static void OnNameInputSubmit(string s)
        {
            if (activePlanet != null)
            {
                if (nameInput == null)
                {
                    Logger.LogError("OnNameInputSubmit called while nameInput is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                if (nameInput.text == null)
                {
                    Logger.LogError("OnNameInputSubmit called while nameInput.text is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                string newPlanetName = nameInput.text.Trim();

                if (activePlanet == null)
                {
                    Logger.LogError("OnNameInputSubmit called while activePlanet is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                PlanetNamingData planetNamingData;
                nameDictionary.TryGetValue(activePlanet.id, out planetNamingData);
                if (planetNamingData == null)
                {
                    Logger.LogError("OnNameInputSubmit called without planet ID (" + activePlanet.id.ToString() + ") in nameDictionary.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                if (newPlanetName.Length == 0)
                {
                    newPlanetName = planetNamingData.originalName;
                }

                if (GameMain.data == null)
                {
                    Logger.LogError("OnNameInputSubmit called while GameMain.data is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                if (GameMain.data.galaxy == null)
                {
                    Logger.LogError("OnNameInputSubmit called while GameMain.data.galaxy is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                Logger.LogDebug("Changing planet " + planetNamingData.originalName + "'s name from " + planetNamingData.customName + " to " + newPlanetName);
                Config.Bind<string>("Planet Names", PlanetHash(planetNamingData.originalName, GameMain.data.galaxy), planetNamingData.originalName, "[Cluster " + GetGalaxyName(GameMain.data.galaxy) + "] Replacement planet name for " + planetNamingData.originalName).Value =
                    planetNamingData.customName = activePlanet.name = newPlanetName;

                if (UIRoot.instance == null)
                {
                    Logger.LogError("OnNameInputSubmit called while UIRoot.instance is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                if (UIRoot.instance.uiGame == null)
                {
                    Logger.LogError("OnNameInputSubmit called while UIRoot.instance.uiGame is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                if (UIRoot.instance.uiGame.planetDetail == null)
                {
                    Logger.LogError("OnNameInputSubmit called while UIRoot.instance.uiGame.planetDetail is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                UIRoot.instance.uiGame.planetDetail.OnPlanetDataSet();

                if (UIRoot.instance.uiGame.planetGlobe != null &&
                    UIRoot.instance.uiGame.planetGlobe.lastStarId == -1 &&
                    UIRoot.instance.uiGame.planetGlobe.lastPlanetId > 0)
                {
                    PlanetData globePlanet = GameMain.data.galaxy.PlanetById(UIRoot.instance.uiGame.planetGlobe.lastPlanetId);
                    if (globePlanet == null)
                    {
                        Logger.LogError("OnNameInputSubmit called while globePlanet is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    }
                    else
                    {
                        UIRoot.instance.uiGame.planetGlobe.planetNameText.text = globePlanet.name;
                    }
                }
                else
                {
                    Logger.LogDebug("Skipping global planet name update.");  // This is expected when not around a planet.
                }

                if (UIRoot.instance.uiGame.starmap == null)
                {
                    Logger.LogError("OnNameInputSubmit called while UIRoot.instance.uiGame.starmap is null.  This isn't expected, but has happened, and isn't handled properly.  If you see this error, please report it at https://github.com/GreyHak/dsp-planet-rename/issues.  Thank you.");
                    return;
                }

                if (UIRoot.instance.uiGame.starmap.planetUIs != null)  // This is null when the planet view is clicked on.
                {
                    foreach (UIStarmapPlanet uistarmapPlanet in UIRoot.instance.uiGame.starmap.planetUIs)
                    {
                        uistarmapPlanet.nameText.text = uistarmapPlanet.planet.name;
                    }
                }
            }
        }
    }
}
