extern alias UnityEngineCoreModule;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityCoreModule = UnityEngineCoreModule.UnityEngine;

namespace GodModeArea
{
    public class GodModeAreaPlugin : RocketPlugin<GodModeAreaConfiguration>
    {
        public static readonly Dictionary<string, bool> PlayerOnGodMode = new();
        public override void LoadPlugin()
        {
            base.LoadPlugin();
            // Instanciating the player events
            Rocket.Unturned.U.Events.OnPlayerConnected += OnPlayerConnected;
            Rocket.Unturned.U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdatePosition += PositionUpdate;
            Logger.Log("GodModeArea by LeandroTheDev");
        }

        private void PositionUpdate(UnturnedPlayer player, UnityCoreModule.Vector3 position)
        {
            // Debug
            if (Configuration.Instance.DebugExtended)
                Logger.Log($"Player: {player.DisplayName}, Mode: {player.GodMode}, Position {player.Position}");
            // Execute the tickrate for the player only if his exist in god mode context
            if (PlayerOnGodMode.TryGetValue(player.Id, out _))
            {
                foreach (GodModeAreas area in Configuration.Instance.GodModeAreas)
                {
                    // Verify X position
                    if (position.x < area.X1 || position.x > area.X2)
                    {
                        // Remove the player from godmode
                        if (PlayerOnGodMode[player.Id]) RemovePlayerFromGodMode(player);
                        return;
                    }
                    // Verify Y position
                    else if (position.y < area.Y1 || position.y > area.Y2)
                    {
                        // Remove the player from godmode
                        if (PlayerOnGodMode[player.Id]) RemovePlayerFromGodMode(player);
                        return;
                    }
                    // Verifiy Z position
                    else if (position.z < area.Z1 || position.z > area.Z2)
                    {
                        // Remove the player from godmode
                        if (PlayerOnGodMode[player.Id]) RemovePlayerFromGodMode(player);
                        return;
                    }
                }
                if (!player.GodMode)
                {
                    player.GodMode = true;
                    PlayerOnGodMode[player.Id] = true;
                }
            }
        }


        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // Add the default value for new player
            player.GodMode = Configuration.Instance.GodModeDefaultValue;
            PlayerOnGodMode.Add(player.Id, Configuration.Instance.GodModeDefaultValue);
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            // Clear Variables
            PlayerOnGodMode.Remove(player.Id);
        }

        private void RemovePlayerFromGodMode(UnturnedPlayer player)
        {
            // Informate the player the are exiting the zone
            UnturnedChat.Say(player, Translate("Exiting_God_Mode", Math.Round(Configuration.Instance.GodModeMillisecondsExitDelay / 1000.0)), Palette.COLOR_R);

            // Remove the player from the list
            PlayerOnGodMode[player.Id] = false;

            // After delay disable the god mode
            Task.Delay(Configuration.Instance.GodModeMillisecondsExitDelay).ContinueWith((_) =>
            {
                // If player back to the god mode again just cancel
                if (PlayerOnGodMode[player.Id]) return;
                // Inform the player
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    UnturnedChat.Say(player, Translate("No_Longer_God_Mode"), Palette.COLOR_R);
                    // Remove god mod
                    player.GodMode = false;
                });
            });
        }

        public override TranslationList DefaultTranslations => new()
        {
            { "Exiting_God_Mode", "Exiting zone, you have {0} seconds of invulnerability" },
            { "No_Longer_God_Mode", "You are now vulnerable!" },
        };
    }
}
