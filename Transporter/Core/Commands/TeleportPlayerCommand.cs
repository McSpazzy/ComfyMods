﻿using System.Collections.Generic;

using ComfyLib;

using UnityEngine;

namespace Transporter {
  public static class TeleportPlayerCommand {
    [ComfyCommand]
    public static IEnumerable<Terminal.ConsoleCommand> Register() {
      yield return new Terminal.ConsoleCommand(
          "teleport-player",
          "(Transporter) teleport-player",
          args => Run(new ComfyArgs(args)));
    }

    public static bool Run(ComfyArgs comfyArgs) {
      if (!comfyArgs.TryGetListValue("player-id", "pid", out List<long> playerIds) || playerIds.Count <= 0) {
        Transporter.LogError($"Missing or invalid arg: --playerId");
        return false;
      }

      if (!comfyArgs.TryGetValue("destination", "d", out Vector3 destination)) {
        Transporter.LogError($"Missing or invalid arg: --destination");
        return false;
      }

      foreach (long playerId in playerIds) {
        TeleportManager.TeleportPlayer(playerId, destination);
      }
      
      return true;
    }
  }
}
