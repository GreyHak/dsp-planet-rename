using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Security.Cryptography;

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

        public static string PlanetHash(PlanetData planet, GalaxyData galaxy)
        {
            HashAlgorithm algorithm = SHA256.Create();
            byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(GetGalaxyName(galaxy) + planet.name));

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

            __result.name = Config.Bind<string>("Planet Names", PlanetHash(__result, galaxy), __result.name, "[Cluster " + GetGalaxyName(galaxy) + "] Replacement planet name for " + __result.name).Value;
        }
    }
}
