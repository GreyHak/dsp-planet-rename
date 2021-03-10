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
        public const string pluginVersion = "1.0.0";
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

            harmony = new Harmony(pluginGuid);
            harmony.PatchAll(typeof(DSPPlanetRename));
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

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetGen), "CreatePlanet")]
        public static void PlanetGen_CreatePlanet_Postfix(PlanetData __result, GalaxyData galaxy)
        {
            if (DSPGame.IsMenuDemo)
                return;

            string customName = Config.Bind<string>("Planet Names", PlanetHash(__result.name, galaxy), __result.name, "[Cluster " + GetGalaxyName(galaxy) + "] Replacement planet name for " + __result.name).Value;
            if (customName != __result.name)
                Logger.LogDebug("Loading planet " + __result.name + "'s name " + customName);
            nameDictionary.Add(__result.id, new PlanetNamingData
            {
                planetId = __result.id,
                originalName = __result.name,
                customName = customName
            });
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
                nameInput.text = __instance.planet.name;
            }
        }

        public static void OnNameInputSubmit(string s)
        {
            if (activePlanet != null)
            {
                string newPlanetName = nameInput.text.Trim();

                PlanetNamingData planetNamingData = nameDictionary[activePlanet.id];

                if (newPlanetName.Length == 0)
                {
                    newPlanetName = planetNamingData.originalName;
                }

                Logger.LogDebug("Changing planet " + planetNamingData.originalName + "'s name from " + planetNamingData.customName + " to " + newPlanetName);
                Config.Bind<string>("Planet Names", PlanetHash(planetNamingData.originalName, GameMain.data.galaxy), planetNamingData.originalName, "[Cluster " + GetGalaxyName(GameMain.data.galaxy) + "] Replacement planet name for " + planetNamingData.originalName).Value =
                    planetNamingData.customName = activePlanet.name = newPlanetName;

                UIRoot.instance.uiGame.planetDetail.OnPlanetDataSet();
                foreach (UIStarmapPlanet uistarmapPlanet in UIRoot.instance.uiGame.starmap.planetUIs)
                {
                    uistarmapPlanet.nameText.text = uistarmapPlanet.planet.name;
                }
            }
        }
    }
}
