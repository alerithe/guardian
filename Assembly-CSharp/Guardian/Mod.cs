﻿using Guardian.AntiAbuse;
using Guardian.Features.Commands;
using Guardian.Features.Properties;
using Guardian.Features.Gamemodes;
using Guardian.Networking;
using Guardian.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Guardian
{
    class Mod : MonoBehaviour
    {
        public static string Build = "07112021";
        public static string RootDir = Application.dataPath + "\\..";
        public static string HostWhitelistPath = RootDir + "\\Hosts.txt";

        public static GamemodeManager Gamemodes = new GamemodeManager();
        public static CommandManager Commands = new CommandManager();
        public static PropertyManager Properties = new PropertyManager();
        public static UI.UIManager Menus;
        public static List<string> HostWhitelist = new List<string>();
        public static Regex BlacklistedTags = new Regex("<(\\/?)(size|material|quad)(.*)>", RegexOptions.IgnoreCase);
        public static Logger Logger = new Logger();
        public static bool IsProgramQuitting = false;

        private static bool Initialized = false;
        private static bool FirstJoin = true;

        public static bool IsMultiMap;

        void Start()
        {
            if (!Initialized)
            {
                // Check for an update before doing anything
                StartCoroutine(CoCheckForUpdate());

                // Host whitelist (for skins)
                if (!File.Exists(HostWhitelistPath))
                {
                    HostWhitelist.Add("i.imgur.com");
                    HostWhitelist.Add("imgur.com");
                    HostWhitelist.Add("cdn.discordapp.com");
                    HostWhitelist.Add("cdn.discord.com");
                    HostWhitelist.Add("media.discordapp.net");
                    HostWhitelist.Add("i.gyazo.com");
                    File.WriteAllLines(HostWhitelistPath, HostWhitelist.ToArray());
                }
                LoadSkinHostWhitelist();

                // Auto-load name and guild (if possible)
                FengGameManagerMKII.NameField = PlayerPrefs.GetString("name", string.Empty);
                if (FengGameManagerMKII.NameField.Uncolored().Length == 0)
                {
                    FengGameManagerMKII.NameField = LoginFengKAI.Player.Name;
                }
                LoginFengKAI.Player.Guild = PlayerPrefs.GetString("guildname", string.Empty);

                // Load various features
                Gamemodes.Load();
                Commands.Load();
                Properties.Load();

                UI.WindowManager.IsExclusiveFullscreen = Properties.ExclusiveFullscreen.Value;

                // Property whitelist
                foreach (FieldInfo field in typeof(PhotonPlayerProperty).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    NetworkPatches.PropertyWhitelist.Add((string)field.GetValue(null));
                }

                Initialized = true;

                DiscordHelper.StartTime = GameHelper.CurrentTimeMillis();
            }

            Menus = base.gameObject.AddComponent<UI.UIManager>();
            base.gameObject.AddComponent<MicEF>();

            DiscordHelper.SetPresence(new Discord.Activity
            {
                Details = $"Staring at the main menu...",
            });
        }

        private IEnumerator CoCheckForUpdate()
        {
            Logger.Info("Checking for update...");
            Logger.Info($"Installed: {Build}");

            using (WWW www = new WWW("https://summie.tk/GUARDIAN_BUILD.TXT?t=" + GameHelper.CurrentTimeMillis())) // Random long to try and avoid cache issues
            {
                yield return www;

                if (www.error != null)
                {
                    Logger.Error(www.error);

                    Logger.Error($"\nIf errors persist, contact me on Discord!");
                    Logger.Info("Discord:");
                    Logger.Info($"\t- {"https://cb.run/FFT".WithColor("0099FF")}");

                    try
                    {
                        GameObject.Find("VERSION").GetComponent<UILabel>().text = "Could not verify version. If errors persists, contact me @ [0099FF]https://cb.run/FFT[-]!";
                    }
                    catch { }
                }
                else
                {
                    string latestVersion = www.text.Split('\n')[0];
                    Logger.Info("Latest: " + latestVersion);

                    if (!latestVersion.Equals(Build))
                    {
                        Logger.Info($"You are {"OUTDATED".AsBold().AsItalic().WithColor("FF0000")}, please update!");
                        Logger.Info("Download:");
                        Logger.Info($"\t- {"https://cb.run/GuardianAoT".WithColor("0099FF")}");

                        try
                        {
                            GameObject.Find("VERSION").GetComponent<UILabel>().text = "[FF0000]Outdated![-] Download the latest build @ [0099FF]https://cb.run/GuardianAoT[-]!";
                        }
                        catch { }
                    }
                    else
                    {
                        Logger.Info($"You are {"UP TO DATE".AsBold().AsItalic().WithColor("AAFF00")}, yay!");
                    }
                }
            }
        }

        public static void LoadSkinHostWhitelist()
        {
            HostWhitelist = new List<string>(File.ReadAllLines(HostWhitelistPath));
        }

        void Update()
        {
            if (PhotonNetwork.isMasterClient)
            {
                Gamemodes.Current.OnUpdate();
            }

            DiscordHelper.RunCallbacks();
        }

        void OnLevelWasLoaded(int level)
        {
            if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Singleplayer || PhotonNetwork.offlineMode)
            {
                string difficulty = "Training";
                switch (IN_GAME_MAIN_CAMERA.Difficulty)
                {
                    case 0:
                        difficulty = "Normal";
                        break;
                    case 1:
                        difficulty = "Hard";
                        break;
                    case 2:
                        difficulty = "Abnormal";
                        break;
                }

                DiscordHelper.SetPresence(new Discord.Activity
                {
                    Details = $"Playing offline.",
                    State = $"{FengGameManagerMKII.Level.Name} / {difficulty}"
                });
            }

            if (PhotonNetwork.isMasterClient)
            {
                Gamemodes.Current.OnReset();
            }

            if (FirstJoin)
            {
                FirstJoin = false;
                string joinMessage = Properties.JoinMessage.Value.Colored();
                if (joinMessage.Uncolored().Length <= 0)
                {
                    joinMessage = Properties.JoinMessage.Value;
                }
                if (joinMessage.Length > 0)
                {
                    Commands.Find("say").Execute(InRoomChat.Instance, joinMessage.Split(' '));
                }
            }
        }

        void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            if (PhotonNetwork.isMasterClient)
            {
                Gamemodes.Current.OnPlayerJoin(player);
            }

            Logger.Info($"[{player.Id}] " + GExtensions.AsString(player.customProperties[PhotonPlayerProperty.Name]).Colored() + " connected.".WithColor("00FF00"));
        }

        void OnPhotonPlayerDisconnected(PhotonPlayer player)
        {
            if (PhotonNetwork.isMasterClient)
            {
                Gamemodes.Current.OnPlayerLeave(player);
            }

            Logger.Info($"[{player.Id}] " + GExtensions.AsString(player.customProperties[PhotonPlayerProperty.Name]).Colored() + " disconnected.".WithColor("FF0000"));
        }

        void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
        {
            NetworkPatches.OnPlayerPropertyModification(playerAndUpdatedProps);

            ModDetector.OnPlayerPropertyModification(playerAndUpdatedProps);
        }

        void OnPhotonCustomRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            NetworkPatches.OnRoomPropertyModification(propertiesThatChanged);

            if (!FirstJoin)
            {
                PhotonPlayer sender = null;
                if (propertiesThatChanged.ContainsKey("sender") && propertiesThatChanged["sender"] is PhotonPlayer)
                {
                    sender = (PhotonPlayer)propertiesThatChanged["sender"];
                }

                if (sender == null || sender.isMasterClient)
                {
                    if (propertiesThatChanged.ContainsKey("Map") && propertiesThatChanged["Map"] is string && IsMultiMap)
                    {
                        LevelInfo levelInfo = LevelInfo.GetInfo((string)propertiesThatChanged["Map"]);
                        if (levelInfo != null)
                        {
                            FengGameManagerMKII.Level = levelInfo;
                        }
                    }

                    if (propertiesThatChanged.ContainsKey("Lighting") && propertiesThatChanged["Lighting"] is string)
                    {
                        if (GExtensions.TryParseEnum((string)propertiesThatChanged["Lighting"], out DayLight time))
                        {
                            Camera.main.GetComponent<IN_GAME_MAIN_CAMERA>().SetLighting(time);
                        }
                    }
                }
            }
        }

        void OnJoinedLobby()
        {
            // TODO: Begin testing with Photon Friends API
            PhotonNetwork.playerName = SystemInfo.deviceUniqueIdentifier;

            DiscordHelper.SetPresence(new Discord.Activity
            {
                Details = "Searching for a room...",
                State = $"Region: {NetworkHelper.GetRegionCode()}"
            });
        }

        void OnJoinedRoom()
        {
            IsMultiMap = PhotonNetwork.room.name.Split('`')[1].StartsWith("Multi-Map");
            FirstJoin = true;

            PhotonNetwork.player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                 { "GuardianMod", Build + "-M" }
            });

            string[] roomInfo = PhotonNetwork.room.name.Split('`');
            if (roomInfo.Length > 6)
            {
                DiscordHelper.SetPresence(new Discord.Activity
                {
                    Details = $"Playing in {(roomInfo[5].Length == 0 ? string.Empty : "[PWD]")} {roomInfo[0].Uncolored()}",
                    State = $"({NetworkHelper.GetRegionCode()}) {roomInfo[1]} / {roomInfo[2].ToUpper()}"
                });
            }
        }

        void OnLeftRoom()
        {
            Gamemodes.Current.CleanUp();

            PhotonNetwork.SetPlayerCustomProperties(null);

            DiscordHelper.SetPresence(new Discord.Activity
            {
                Details = "Idle..."
            });
        }

        void OnConnectionFail(DisconnectCause cause)
        {
            Logger.Warn($"OnConnectionFail ({cause})");
        }

        void OnPhotonRoomJoinFailed(object[] codeAndMsg)
        {
            Logger.Error($"OnPhotonRoomJoinFailed ({codeAndMsg[0]}: {codeAndMsg[1]})");
        }

        // Attempts to fix some dumb bugs that occur when you alt-tab
        void OnApplicationFocus(bool hasFocus)
        {
            UI.WindowManager.HandleWindowFocusEvent(hasFocus);

            if (hasFocus)
            {
                if (IN_GAME_MAIN_CAMERA.Gametype != GameType.Stop)
                {
                    // Minimap turning white
                    if (Minimap.Instance != null)
                    {
                        Minimap.WaitAndTryRecaptureInstance(0.1f);
                    }

                    // TPS crosshair ending up where it shouldn't
                    if (IN_GAME_MAIN_CAMERA.CameraMode == CameraType.TPS)
                    {
                        Screen.lockCursor = false;
                        Screen.lockCursor = true;
                    }
                }
            }
        }

        void OnApplicationQuit()
        {
            IsProgramQuitting = true;

            Properties.Save();

            DiscordHelper.Dispose();
        }
    }
}