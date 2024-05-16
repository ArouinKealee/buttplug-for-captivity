using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using HarmonyLib;
using Buttplug;

namespace ButtplugForCaptivity
{
    public class ButtplugForCaptivityClass : MelonMod
    {
        private Player player;

        public static ButtplugForCaptivityClass Instance;

        private ButtplugClient client;

        #region From Game Calculations

        private float gameCurrentPleasure = 0.0f;


        #endregion


        #region Vibration Calculations

        private float currentPleasureAddition;  // From 0 -> 1

        private float pleasureDecaySpeed = 1.0f;  // per second.

        private float orgasmTime = 0.0f;

        #endregion

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Initializing ButtPlugForCaptivity");
            Instance = this;

            Task.Run(InitializeButtplug).Wait();

            MelonLogger.Msg("Initialized ButtPlugForCaptivity");
        }

        public override void OnDeinitializeMelon()
        {
            Task.Run(UninitializeButtplug).Wait();
            client.Dispose();
            client = null;
        }

        private async Task InitializeButtplug()
        {
            MelonLogger.Msg("Initializing Buttplug client");

            var connector = new ButtplugWebsocketConnectorOptions(new Uri("ws://localhost:12345/buttplug"));
            client = new ButtplugClient("Captivity");

            try
            {
                MelonLogger.Msg("Connecting to Buttplug...");
                await client.ConnectAsync(connector);
                MelonLogger.Msg("Connected to Buttplug!");
            }
            catch (ButtplugException ex)
            {
                MelonLogger.Error("Cannot connect to Buttplug Server!\n" + $"Message: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Unspecified error connecting with Buttplug Server: " + ex.ToString());
            }

            MelonLogger.Msg("Successfully Connected to Buttplug Server");

            MelonLogger.Msg("Devices:");

            foreach (var device in client.Devices)
            {
                MelonLogger.Msg("- " + device.Name);
            }
        }

        private async Task UninitializeButtplug()
        {
            MelonLogger.Msg("Uninitializing Buttplug");
            if (client != null)
            {
                await client.DisconnectAsync();
            }
            MelonLogger.Msg("Uninitialized Buttplug");
        }

        private void SetVibrationSpeed(float speed)
        {
            var clampedSpeed = Mathf.Clamp(speed, 0.0f, 1.0f);
            foreach (var device in client.Devices)
            {
                device.SendVibrateCmd(clampedSpeed).GetAwaiter().GetResult();
                //MelonLogger.Msg("Set vibration to: " + clampedSpeed);
            }
        }


        public void SetPlayer(Player player)
        {
            this.player = player;

            player.OnOrgasm += Player_OnOrgasm;
            player.OnFetusInsert += Player_OnFetusInsert;
            player.OnBirth += Player_OnBirth;
            player.OnBeingRaped += Player_OnBeingRaped;
        }

        private void Player_OnBeingRaped()
        {
            FullForceTime(1.0f);
        }

        private void Player_OnOrgasm()
        {
            FullForceTime(7.0f);
        }

        private void Player_OnBirth(Actor i_actorChild)
        {
            FullForceTime(3.0f);
        }

        private void Player_OnFetusInsert(Fetus i_fetusInserted)
        {
            FullForceTime(5.0f);
        }

        private void PleasureGained(float amountGained)
        {
            var addition = Mathf.InverseLerp(0.0f, 5.0f, amountGained) * 2.0f;
            currentPleasureAddition += addition;

            MelonLogger.Msg(amountGained + " pleasure gained, resulting in a pleasure addition of " + addition);
            //MelonLogger.Msg("Current pleasure addition: " + currentPleasureAddition);
        }

        private void FullForceTime(float time)
        {
            orgasmTime = Mathf.Max(orgasmTime, time);
            MelonLogger.Msg("Orgasm received");
        }


        private void Reset()
        {
            gameCurrentPleasure = 0.0f;
        }




        public override void OnUpdate()
        {
            if (!player)
            {
                //Debug.Log("Cannot find player");

                Reset();

                return;
            }

            var playerNewPleasure = player.GetPleasureCurrent();

            if (playerNewPleasure > gameCurrentPleasure)
            {
                var diff = playerNewPleasure - gameCurrentPleasure;
                PleasureGained(diff);
                gameCurrentPleasure = playerNewPleasure;
            }
            else if (playerNewPleasure < gameCurrentPleasure)
            {
                gameCurrentPleasure = playerNewPleasure;
            }

            float libidoPercentage = player.GetLibidoCurrent() / player.GetLibidoMax();

            float libidoContribution = libidoPercentage * 0.15f;
            float pleasureContribution = currentPleasureAddition * 0.85f;



            float finalSpeed = 0.0f;
            if (orgasmTime > 0)
            {
                finalSpeed = 1.0f;
            }
            else
            {
                finalSpeed = libidoContribution + pleasureContribution;
            }
            


            //float finalSpeed = currentPleasureAddition;


            SetVibrationSpeed(finalSpeed);



            // Applying over time effects

            currentPleasureAddition -= pleasureDecaySpeed * Time.deltaTime;
            currentPleasureAddition = Mathf.Clamp(currentPleasureAddition, 0.0f, 1.0f);

            orgasmTime -= Time.deltaTime;
            orgasmTime = Mathf.Max(orgasmTime, 0.0f);
        }
    }

    [HarmonyPatch(typeof(Stage), "OpenStage", new Type[] { })]
    static class Patch
    {
        private static void Prefix()
        {

        }

        private static void Postfix(Stage __instance)
        {
            var player = __instance.GetComponentInChildren<Player>();

            if (player == null)
            {
                MelonLogger.Error("No player is a child of this stage!");
                return;
            }

            ButtplugForCaptivityClass.Instance.SetPlayer(player);

            MelonLogger.Msg("Successfully set player on stage load.");
        }
    }
}