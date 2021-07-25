﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OdinSaves {
  [BepInPlugin(OdinSaves.Package, OdinSaves.ModName, OdinSaves.Version)]
  public class OdinSaves : BaseUnityPlugin {
    public const string Package = "redseiko.valheim.odinsaves";
    public const string Version = "1.0.0";
    public const string ModName = "Odin Saves";

    private static ConfigEntry<bool> _isModEnabled;
    private static ConfigEntry<int> savePlayerProfileInterval;
    private static ConfigEntry<bool> setLogoutPointOnSave;
    private static ConfigEntry<bool> showMessageOnModSave;

    private static ManualLogSource _logger;
    private Harmony _harmony;

    private void Awake() {
      _isModEnabled = Config.Bind("Global", "isModEnabled", true, "Whether the mod should be enabled.");

      savePlayerProfileInterval = Config.Bind(
        "Global",
        "savePlayerProfileInterval",
        300,
        "Interval (in seconds) for how often to save the player profile. Game default (and maximum) is 1200s.");

      setLogoutPointOnSave = Config.Bind(
        "Global",
        "setLogoutPointOnSave",
        true,
        "Sets your logout point to your current position when the mod performs a save.");

      showMessageOnModSave = Config.Bind(
        "Global",
        "saveMessageOnModSave",
        true,
        "Show a message (in the middle of your screen) when the mod tries to save.");

      _logger = Logger;
      _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void OnDestroy() {
      if (_harmony != null) {
        _harmony.UnpatchAll(null);
      }
    }

    [HarmonyPatch(typeof(Game))]
    private class GamePatch {
      [HarmonyPostfix]
      [HarmonyPatch(nameof(Game.UpdateSaving))]
      private static void UpdateSavingPostfix(ref Game __instance) {
        if (!_isModEnabled.Value) {
          return;
        }

        if (__instance.m_saveTimer == 0f || __instance.m_saveTimer < savePlayerProfileInterval.Value) {
          return;
        }

        if (showMessageOnModSave.Value) {
          MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Saving player profile...");
        }

        __instance.m_saveTimer = 0f;
        __instance.SavePlayerProfile(setLogoutPoint: setLogoutPointOnSave.Value);

        if (ZNet.instance) {
          ZNet.instance.Save(sync: false);
        }
      }
    }

    [HarmonyPatch(typeof(Player))]
    private class PlayerPatch {
      [HarmonyPostfix]
      [HarmonyPatch(nameof(Player.OnDeath))]
      private static void PlayerOnDeathPostfix(ref Player __instance) {
        Game.instance.m_playerProfile.ClearLoguoutPoint();
      }
    }

    [HarmonyPatch(typeof(PlayerProfile))]
    private class PlayerProfilePatch {
      [HarmonyPostfix]
      [HarmonyPatch(nameof(PlayerProfile.GetMapData))]
      private static void PlayerProfileGetMapDataPostfix(ref PlayerProfile __instance, ref byte[] __result) {
        __result = DecompressMapData(ref __result);
      }

      [HarmonyPrefix]
      [HarmonyPatch(nameof(PlayerProfile.SetMapData))]
      private static void PlayerProfileSetMapDataPrefix(ref PlayerProfile __instance, ref byte[] data) {
        if (!_isModEnabled.Value || HasUncompressedData(__instance)) {
          return;
        }

        data = CompressMapData(ref data);
      }
    }

    private static byte[] CompressMapData(ref byte[] mapData) {
      if (mapData == null || IsGZipData(mapData)) {
        return mapData;
      }

      using (MemoryStream outStream = new MemoryStream(capacity: mapData.Length)) {
        using (GZipStream deflateStream = new GZipStream(outStream, CompressionMode.Compress)) {
          deflateStream.Write(mapData, 0, mapData.Length);
        }

        return outStream.ToArray();
      }
    }

    private static byte[] DecompressMapData(ref byte[] mapData) {
      if (mapData == null || !IsGZipData(mapData)) {
        return mapData;
      }

      using (var inStream = new MemoryStream(mapData))
      using (var inflateStream = new GZipStream(inStream, CompressionMode.Decompress))
      using (var outStream = new MemoryStream(capacity: 1024 * 1024 * 4)) {
        inflateStream.CopyTo(outStream);
        return outStream.ToArray();
      }
    }

    private static bool IsGZipData(byte[] data) {
      return data != null && data.Length >= 3 && data[0] == 0x1f && data[1] == 0x8b && data[2] == 0x08;
    }

    private static bool HasUncompressedData(PlayerProfile profile) {
      return profile.m_worldData.Values.All(value => value.m_mapData == null || !IsGZipData(value.m_mapData));
    }

    [HarmonyPatch(typeof(FejdStartup))]
    private class FejdStartupPatch {
      private static Button _compressDecompressButton;

      [HarmonyPostfix]
      [HarmonyPatch(nameof(FejdStartup.Awake))]
      private static void FejdStartupAwakePostfix(ref FejdStartup __instance) {
        CreateCompressDecompressButton(__instance);
      }

      [HarmonyPostfix]
      [HarmonyPatch(nameof(FejdStartup.UpdateCharacterList))]
      [HarmonyAfter(new string[] { "MK_BetterUI" })]
      private static void FejdStartupUpdateCharacterListPostfix(ref FejdStartup __instance) {
        if (__instance.m_profileIndex >= 0 && __instance.m_profileIndex < __instance.m_profiles.Count) {
          PlayerProfile profile = __instance.m_profiles[__instance.m_profileIndex];

          UpdateNameText(__instance, profile);
          UpdateCompressDecompressButton(__instance, profile);
        }
      }

      private static void UpdateNameText(FejdStartup fejdStartup, PlayerProfile profile) {
        float mapDataBytes =
            profile.m_worldData.Values.Select(value => value.m_mapData == null ? 0 : value.m_mapData.Length).Sum();

        fejdStartup.m_csName.text =
            string.Format(
                "{0}<size=20>{1}Worlds: <color={2}>{3}</color>   MapData: <color={2}>{4}</color> KB</size>",
                fejdStartup.m_csName.text,
                fejdStartup.m_csName.text == profile.GetName() ? "\n" : "   ",
                "orange",
                profile.m_worldData.Count,
                (mapDataBytes / 1024).ToString("N0"));
      }

      private static void CreateCompressDecompressButton(FejdStartup fejdStartup) {
        _compressDecompressButton =
            Instantiate(fejdStartup.m_csNewButton, fejdStartup.m_selectCharacterPanel.transform);

        RectTransform transform = _compressDecompressButton.GetComponent<RectTransform>();
        transform.anchorMin = new Vector2(0.5f, 0f);
        transform.anchorMax = new Vector2(0.5f, 0f);
        transform.pivot = new Vector2(0.5f, 0.5f);
        transform.anchoredPosition = new Vector2(410f, 151f);

        _compressDecompressButton.onClick.RemoveAllListeners();
        _compressDecompressButton.onClick = new Button.ButtonClickedEvent();

        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);

        Text text = _compressDecompressButton.GetComponentInChildren<Text>();
        text.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
        text.text = "Compression";

        _compressDecompressButton.gameObject.name = "Compression";
        _compressDecompressButton.gameObject.SetActive(false);
      }

      private static void UpdateCompressDecompressButton(FejdStartup fejdStartup, PlayerProfile profile) {
        bool hasUncompressedData = HasUncompressedData(profile);

        _compressDecompressButton.GetComponentInChildren<Text>().text =
            hasUncompressedData ? "Compress Profile" : "Decompress Profile";

        _compressDecompressButton.onClick.RemoveAllListeners();
        _compressDecompressButton.onClick.AddListener(
            () => {
              OnCompressDecompressButtonClick(profile, hasUncompressedData);

              profile.Save();
              fejdStartup.UpdateCharacterList();
            });

        _compressDecompressButton.gameObject.SetActive(true);
      }

      private static void OnCompressDecompressButtonClick(PlayerProfile profile, bool hasUncompressedData) {
        foreach (PlayerProfile.WorldPlayerData worldPlayerData in profile.m_worldData.Values) {
          worldPlayerData.m_mapData =
              hasUncompressedData
                  ? CompressMapData(ref worldPlayerData.m_mapData)
                  : DecompressMapData(ref worldPlayerData.m_mapData);
        }
      }

      private static IEnumerator CompressDecompressMapDataCoroutine(PlayerProfile profile, bool hasUncompressedData) {
        foreach (PlayerProfile.WorldPlayerData worldPlayerData in profile.m_worldData.Values) {
          worldPlayerData.m_mapData =
              hasUncompressedData
                  ? CompressMapData(ref worldPlayerData.m_mapData)
                  : DecompressMapData(ref worldPlayerData.m_mapData);

          yield return null;
        }
      }
    }
  }
}
