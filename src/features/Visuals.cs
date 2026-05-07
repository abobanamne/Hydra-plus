using AmongUs.GameOptions;
using HarmonyLib;

namespace HydraMenu.features
{
    internal class Visuals
    {
        // Is there a better way of implenting fullbright?
        // This current method does not allow you to see through walls due to shadows
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
        public static class Fullbright
        {
            public static bool Enabled { get; set; } = false;

            static bool Prefix(ref float __result)
            {
                if (!Enabled) return true;

                __result = 1000f;
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TurnOnProtection))]
        public static class ShowProtections
        {
            public static bool Enabled { get; set; } = true;

            static void Prefix(ref bool visible)
            {
                if (Enabled) visible = true;
            }
        }

        // The GameData::ShowNotification function by default only handles disconnect reasons of ExitGame, Kicked, or Banned
        // Any other disconnection reasons automatically default to the error disconnection message
        [HarmonyPatch(typeof(GameData), nameof(GameData.ShowNotification))]
        public static class AccurateDisconnectReasons
        {
            public static bool Enabled { get; set; } = true;

            static bool Prefix(string playerName, DisconnectReasons reason)
            {
                if (!Enabled) return true;

                Hydra.Log.LogInfo($"[Disconnect Logger] {playerName} was disconnected with reason {reason}");

                switch (reason)
                {
                    // GameData::ShowNotification already handles these disconnect messages
                    case DisconnectReasons.ExitGame:
                    case DisconnectReasons.Kicked:
                    case DisconnectReasons.Banned:
                    case DisconnectReasons.Error:
                        return true;

                    case DisconnectReasons.Hacking:
                        HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was banned by the Among Us anticheat for hacking.");
                        return false;

                    case DisconnectReasons.DuplicateConnectionDetected:
                        HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to duplicate login.");
                        return false;

                    // This disconnect reason happens when a player does not send the ClientReady message after the game starts in time
                    case DisconnectReasons.ClientTimeout:
                        HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to timeout.");
                        return false;

                    default:
                        HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected due to {reason}.");
                        return false;
                }
            }
        }

        [HarmonyPatch(typeof(ShhhBehaviour), nameof(ShhhBehaviour.PlayAnimation))]
        public static class SkipShhhAnimation
        {
            public static bool Enabled { get; set; } = true;

            static bool Prefix()
            {
                if (Enabled)
                {
                    HudManager.Instance.shhhEmblem.gameObject.SetActive(false);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        // PlayerControl::FixedUpdate sets PlayerControl::set_Visable to false if the player is dead, or true if the player is alive
        // The set_Visible function runs CosmeticsLayer::set_Visible in order to hide or show the player's cosmetics
        // If we want to show ghosts even if we are alive, then we can reimplement PlayerControl::set_Visible and make it so player cosmetics are always visible
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Visible), MethodType.Setter)]
        public static class ShowGhosts
        {
            public static bool Enabled { get; set; } = true;

            static bool Prefix(PlayerControl __instance)
            {
                if (Enabled && __instance.Data.IsDead)
                {
                    __instance.cosmetics.Visible = true;
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        // PlayerControl::FixedUpdate patch to make players visible inside vents
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class ShowPlayersInVent
        {
            public static bool Enabled { get; set; } = false;

            // If current requirements are failed, Postfix won't affect to player for being avoided extra NullReferenceException and visual glitches
            static bool FailedCheck(PlayerControl __instance)
            {
                return GameManager.Instance.IsHideAndSeek() && __instance.Data.RoleType == RoleTypes.Impostor || __instance == null || __instance.Data == null || GameManager.Instance == null
                    || AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started || !PlayerControl.LocalPlayer;
            }

            static void Postfix(PlayerControl __instance)
            {
                if (FailedCheck(__instance))
                    return;
                // try-catch used because of NullReferenceException for few seconds after leaving the server
                try
                {
                    if (!Enabled)
                    {
                        // Reverting values back if they was changed by this patch
                        if (__instance.invisibilityAlpha == 0.3f)
                        {
                            PhantomRole role = __instance.Data.Role as PhantomRole;
                            if (role != null)
                            {
                                __instance.SetInvisibility(role.isInvisible);
                                return;
                            }
                            else
                            {
                                __instance.cosmetics.SetPhantomRoleAlpha(1f);
                                __instance.invisibilityAlpha = 1;
                                if (__instance.inVent)
                                {
                                    __instance.Visible = false;
                                }
                            }
                        }
                        return;
                    }

                    if (__instance.inVent && __instance != PlayerControl.LocalPlayer)
                    {
                        if (GameManager.Instance.IsHideAndSeek() && __instance.Data.Role.IsImpostor) return;
                        __instance.Visible = true;
                        __instance.invisibilityAlpha = 0.3f;
                        __instance.cosmetics.SetPhantomRoleAlpha(0.3f);
                    }
                    else
                    {
                        // Since there is a lack of needed checks, Phantoms always won't be visible inside vents (I'm too lazy rn for trying find the fix)
                        PhantomRole role = __instance.Data.Role as PhantomRole;
                        if (role != null)
                        {
                            __instance.SetInvisibility(role.isInvisible);
                        }
                        __instance.cosmetics.SetPhantomRoleAlpha(1f);
                        __instance.invisibilityAlpha = 1;
                    }
                }
                catch{}
            }
        }
    }
}
