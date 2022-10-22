﻿using HarmonyLib;

using System;
using System.Globalization;

using UnityEngine;
using UnityEngine.UI;

using static BetterBattleUI.PluginConfig;

namespace BetterBattleUI {
  [HarmonyPatch(typeof(DamageText))]
  static class DamageTextPatch {
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DamageText.AddInworldText))]
    static bool AddInworldText(
        ref DamageText __instance, DamageText.TextType type, Vector3 pos, float distance, float dmg, bool mySelf) {
      if (!IsModEnabled.Value) {
        return true;
      }

      DamageText.WorldTextInstance worldText = new();
      __instance.m_worldTexts.Add(worldText);

      worldText.m_worldPos = pos;
      worldText.m_timer = 0f;
      worldText.m_gui = UnityEngine.Object.Instantiate(__instance.m_worldTextBase, __instance.transform);

      worldText.m_textField = worldText.m_gui.GetComponent<Text>();
      worldText.m_textField.text = GetWorldTextText(type, dmg);
      worldText.m_textField.color = GetWorldTextColor(type, dmg, mySelf);
      worldText.m_textField.fontSize =
          distance > DamageTextSmallFontDistance.Value ? DamageTextSmallFontSize.Value : DamageTextLargeFontSize.Value;

      return false;
    }

    static Color GetWorldTextColor(DamageText.TextType damageTextType, float damage, bool isPlayerDamage) {
      if (damageTextType == DamageText.TextType.Heal) {
        return DamageTextHealColor.Value;
      }

      if (isPlayerDamage) {
        return damage == 0f ? DamageTextPlayerNoDamageColor.Value : DamageTextPlayerDamageColor.Value;
      }

      return damageTextType switch {
        DamageText.TextType.Normal => DamageTextNormalColor.Value,
        DamageText.TextType.Resistant => DamageTextResistantColor.Value,
        DamageText.TextType.Weak => DamageTextWeakColor.Value,
        DamageText.TextType.Immune => DamageTextImmuneColor.Value,
        DamageText.TextType.Heal => DamageTextHealColor.Value,
        DamageText.TextType.TooHard => DamageTextTooHardColor.Value,
        DamageText.TextType.Blocked => DamageTextBlockedColor.Value,
        _ => Color.white,
      };
    }

    static readonly Lazy<string> _msgTooHard = new(() => Localization.m_instance.Localize("$msg_toohard"));
    static readonly Lazy<string> _msgBlocked = new(() => Localization.m_instance.Localize("$msg_blocked: "));

    static string GetWorldTextText(DamageText.TextType damageTextType, float damage) {
      return damageTextType switch {
        DamageText.TextType.Heal => "+" + damage.ToString("0.#", CultureInfo.InvariantCulture),
        DamageText.TextType.TooHard => _msgTooHard.Value,
        DamageText.TextType.Blocked => _msgBlocked.Value + damage.ToString("0.#", CultureInfo.InvariantCulture),
        _ => damage.ToString("0.#", CultureInfo.InvariantCulture),
      };
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(DamageText.UpdateWorldTexts))]
    static bool UpdateWorldTextsPrefix(ref DamageText __instance, float dt) {
      if (!IsModEnabled.Value) {
        return true;
      }

      Camera camera = Utils.GetMainCamera();

      float width = Screen.width;
      float height = Screen.height;
      
      for (int i = __instance.m_worldTexts.Count - 1; i >= 0; i--) {
        DamageText.WorldTextInstance worldText = __instance.m_worldTexts[i];
        worldText.m_timer += dt;

        if (worldText.m_timer > DamageTextMessageDuration.Value) {
          UnityEngine.Object.Destroy(worldText.m_gui);

          // Source: https://www.vertexfragment.com/ramblings/list-removal-performance/
          __instance.m_worldTexts[i] = __instance.m_worldTexts[__instance.m_worldTexts.Count - 1];
          __instance.m_worldTexts.RemoveAt(__instance.m_worldTexts.Count - 1);

          continue;
        }

        // TODO: this needs to be lerped for max float value; default 1.5f at most in vanilla
        worldText.m_worldPos[1] += dt;

        Vector3 point = camera.WorldToScreenPoint(worldText.m_worldPos);

        if (point.x < 0f || point.x > width || point.y < 0f || point.y > height || point.z < 0f) {
          worldText.m_gui.SetActive(false);
          continue;
        }

        worldText.m_textField.color =
            LerpFadeOutColorAlpha(worldText.m_textField.color, worldText.m_timer / DamageTextMessageDuration.Value);

        worldText.m_gui.SetActive(true);
        worldText.m_gui.transform.position = point;
      }

      return false;
    }

    static Color LerpFadeOutColorAlpha(Color color, float t) {
      if (DamageTextFadeOutUseBezier.Value) {
        // Bezier curve.
        color.a = 1f - (t * t * (3f - (2f * t))); 
      } else {
        color.a = 1f - (t * t * t);
      }

      return color;
    }
  }
}
