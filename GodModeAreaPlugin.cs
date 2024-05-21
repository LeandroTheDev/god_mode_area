extern alias UnityEngineCoreModule;

using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Provider;
using SDG.Unturned;
using UnityEngine.SocialPlatforms;
using UnityCoreModule = UnityEngineCoreModule.UnityEngine;

namespace GodModeArea
{
    public class GodModeAreaPlugin : RocketPlugin<GodModeAreaConfiguration>
    {
        /// <summary>
        /// Store the players actual status in god mode
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, byte>> TemporaryPlayerValues = new();
        /// <summary>
        /// Stores if player already been notified from exiting god mode
        /// </summary>
        private readonly List<string> TemporaryGodTaskRunning = new();
        /// <summary>
        /// This will store the tasks for delay to lose god mode
        /// </summary>
        private readonly Dictionary<string, CancellationTokenSource> PlayerGodModeTasks = new();

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
                Logger.Log($"Player: {player.DisplayName}, Mode: {GodModeAreaTools.GodModePlayersId[player.Id]}, Position {player.Position}");

            // Check if player is in god mode area
            foreach (GodModeAreas area in Configuration.Instance.GodModeAreas)
            {
                // Verify X position
                if (position.x < area.X1 || position.x > area.X2)
                {
                    // Remove the player from godmode
                    if (!TemporaryGodTaskRunning.Contains(player.Id) && GodModeAreaTools.GodModePlayersId[player.Id])
                        RemovePlayerFromGodMode(player);
                    return;
                }
                // Verify Y position
                else if (position.y < area.Y1 || position.y > area.Y2)
                {
                    // Remove the player from godmode
                    if (!TemporaryGodTaskRunning.Contains(player.Id) && GodModeAreaTools.GodModePlayersId[player.Id])
                        RemovePlayerFromGodMode(player);
                    return;
                }
                // Verifiy Z position
                else if (position.z < area.Z1 || position.z > area.Z2)
                {
                    // Remove the player from godmode
                    if (!TemporaryGodTaskRunning.Contains(player.Id) && GodModeAreaTools.GodModePlayersId[player.Id])
                        RemovePlayerFromGodMode(player);
                    return;
                }
            }
            if (!GodModeAreaTools.GodModePlayersId[player.Id])
            {
                // [Vanilla] Enable the god mode
                if (Configuration.Instance.VanillaGodMode) player.GodMode = true;

                GodModeAreaTools.GodModePlayersId[player.Id] = true;

                // Remove the task from memory
                if (PlayerGodModeTasks.TryGetValue(player.Id, out CancellationTokenSource token))
                {
                    token.Cancel();
                    PlayerGodModeTasks.Remove(player.Id);
                }
                TemporaryGodTaskRunning.Remove(player.Id);
            }
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // [Vanilla] Add the default value for new player
            if (Configuration.Instance.VanillaGodMode) player.GodMode = Configuration.Instance.GodModeDefaultValue;

            // Add temporary values
            GodModeAreaTools.GodModePlayersId.Add(player.Id, Configuration.Instance.GodModeDefaultValue);

            // [Custom] Instanciate the events
            if (!Configuration.Instance.VanillaGodMode)
            {
                TemporaryPlayerValues.Add(player.Id, new());

                TemporaryPlayerValues[player.Id].Add("Health", player.Health);
                player.Events.OnUpdateHealth += PlayerUpdateHealth;

                TemporaryPlayerValues[player.Id].Add("Food", player.Hunger);
                player.Events.OnUpdateFood += PlayerUpdateFood;

                TemporaryPlayerValues[player.Id].Add("Water", player.Thirst);
                player.Events.OnUpdateWater += PlayerUpdateWater;

                TemporaryPlayerValues[player.Id].Add("Virus", player.Infection);
                player.Events.OnUpdateVirus += PlayerUpdateVirus;
            }
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            // Clear Variables
            TemporaryPlayerValues.Remove(player.Id);
            TemporaryGodTaskRunning.Remove(player.Id);
            PlayerGodModeTasks.Remove(player.Id);
            GodModeAreaTools.GodModePlayersId.Remove(player.Id);

            // [Vanilla] Remove player from godmode
            if (Configuration.Instance.VanillaGodMode) player.GodMode = false;
        }

        private void RemovePlayerFromGodMode(UnturnedPlayer player)
        {
            // Check if player already been notified
            if (!TemporaryGodTaskRunning.Contains(player.Id))
            {
                // Informate the player the are exiting the zone
                UnturnedChat.Say(player, Translate("Exiting_God_Mode", Math.Round(Configuration.Instance.GodModeMillisecondsExitDelay / 1000.0)), Palette.COLOR_R);
                TemporaryGodTaskRunning.Add(player.Id);
            }

            // Cancel previous tasks if exist
            if (PlayerGodModeTasks.TryGetValue(player.Id, out CancellationTokenSource token))
            {
                token.Cancel();
                PlayerGodModeTasks.Remove(player.Id);
            }

            // Create new cancellation token
            PlayerGodModeTasks.Add(player.Id, new());

            // After delay disable the god mode
            Task.Delay(Configuration.Instance.GodModeMillisecondsExitDelay, PlayerGodModeTasks[player.Id].Token).ContinueWith((task) =>
            {
                // If is cancelled just return
                if (task.IsCanceled) return;
                // Inform the player
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    UnturnedChat.Say(player, Translate("No_Longer_God_Mode"), Palette.COLOR_R);

                    GodModeAreaTools.GodModePlayersId[player.Id] = false;
                    TemporaryGodTaskRunning.Remove(player.Id);

                    // [Vanilla] Remove god mode
                    if (Configuration.Instance.VanillaGodMode) player.GodMode = false;

                    // Remove the task from memory
                    PlayerGodModeTasks.Remove(player.Id);
                });
            });
        }

        #region custom god mode
        private void PlayerUpdateHealth(UnturnedPlayer player, byte health)
        {
            // If player is on God Mode
            if (GodModeAreaTools.GodModePlayersId[player.Id])
            {
                // Check the existence of health value from previous attribute
                if (TemporaryPlayerValues[player.Id].TryGetValue("Health", out byte previousHealth))
                {
                    if (previousHealth > health)
                        player.Heal((byte)(previousHealth - health));
                }
                // If dont exist create one
                else TemporaryPlayerValues[player.Id].Add("Health", health);
            }
            else TemporaryPlayerValues[player.Id]["Health"] = player.Health;
        }
        private void PlayerUpdateFood(UnturnedPlayer player, byte food)
        {
            // If player is on God Mode
            if (GodModeAreaTools.GodModePlayersId[player.Id])
            {
                // Check the existence of Food value from previous attribute
                if (TemporaryPlayerValues[player.Id].TryGetValue("Food", out byte previousFood))
                {
                    if (previousFood > food + 1)
                    {
                        // Bro, why? just why, i take a long time to understand
                        // how this works
                        byte total = (byte)(100 - previousFood);
                        if (total <= 0) player.Hunger = 1;
                        if (total >= 100) player.Hunger = 99;
                        else player.Hunger = total;
                    }
                    // The variable receives the actual value but
                    // for the setter hes receive the percentage in 1 to 99
                    // and for be better the function update is called again
                    // and finally will return the value correctly
                }
                // If dont exist create one
                else TemporaryPlayerValues[player.Id].Add("Food", food);
            }
            else TemporaryPlayerValues[player.Id]["Food"] = player.Hunger;
        }
        private void PlayerUpdateWater(UnturnedPlayer player, byte water)
        {
            // If player is on God Mode
            if (GodModeAreaTools.GodModePlayersId[player.Id])
            {
                // Check the existence of Water value from previous attribute
                if (TemporaryPlayerValues[player.Id].TryGetValue("Water", out byte previousWater))
                {
                    if (previousWater > water + 1)
                    {
                        // Bro, why? just why, i take a long time to understand
                        // how this works
                        byte total = (byte)(100 - previousWater);
                        if (total <= 0) player.Thirst = 1;
                        if (total >= 100) player.Thirst = 99;
                        else player.Thirst = total;
                    }
                    // The variable receives the actual value but
                    // for the setter hes receive the percentage in 1 to 99
                    // and for be better the function update is called again
                    // and finally will return the value correctly,
                    // now i understand why the vanilla god mode is so bugged
                    // with player hunger and thirst
                }
                // If dont exist create one
                else TemporaryPlayerValues[player.Id].Add("Water", water);
            }
            else TemporaryPlayerValues[player.Id]["Water"] = player.Thirst;
        }
        private void PlayerUpdateVirus(UnturnedPlayer player, byte virus)
        {
            // If player is on God Mode
            if (GodModeAreaTools.GodModePlayersId[player.Id])
            {
                // Check the existence of Virus value from previous attribute
                if (TemporaryPlayerValues[player.Id].TryGetValue("Virus", out byte previousVirus))
                {
                    if (previousVirus > virus + 1)
                    {
                        // Bro, why? just why, i take a long time to understand
                        // how this works
                        byte total = (byte)(100 - previousVirus);
                        if (total <= 0) player.Infection = 1;
                        if (total >= 100) player.Infection = 99;
                        else player.Infection = total;
                    }
                    // The variable receives the actual value but
                    // for the setter hes receive the percentage in 1 to 99
                    // and for be better the function update is called again
                    // and finally will return the value correctly,
                    // now i understand why the vanilla god mode is so bugged
                    // with player hunger and thirst
                }
                // If dont exist create one
                else TemporaryPlayerValues[player.Id].Add("Virus", virus);
            }
            else TemporaryPlayerValues[player.Id]["Virus"] = player.Infection;
        }
        #endregion
        public override TranslationList DefaultTranslations => new()
        {
            { "Exiting_God_Mode", "Exiting zone, you have {0} seconds of invulnerability" },
            { "No_Longer_God_Mode", "You are now vulnerable!" },
        };
    }

    static public class GodModeAreaTools
    {
        /// <summary>
        /// Contains a boolean if player is on god mode or not
        /// </summary>
        static public Dictionary<string, bool> GodModePlayersId = new();
    }
}
