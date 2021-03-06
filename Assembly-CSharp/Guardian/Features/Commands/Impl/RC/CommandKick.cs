﻿using Guardian.Utilities;

namespace Guardian.Features.Commands.Impl.RC
{
    class CommandKick : Command
    {
        public CommandKick() : base("kick", new string[0], "<id> [reason]", false) { }

        public override void Execute(InRoomChat irc, string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int id))
            {
                if (id != PhotonNetwork.player.Id)
                {
                    if (!(FengGameManagerMKII.OnPrivateServer || PhotonNetwork.isMasterClient))
                    {
                        string name = GExtensions.AsString(PhotonNetwork.player.customProperties[PhotonPlayerProperty.Name]).Colored();
                        if (name == string.Empty)
                        {
                            name = GExtensions.AsString(PhotonNetwork.player.customProperties[PhotonPlayerProperty.Name]);
                        }
                        FengGameManagerMKII.Instance.photonView.RPC("Chat", PhotonTargets.All, "/kick #" + id, name);
                    }
                    else
                    {
                        PhotonPlayer player = PhotonPlayer.Find(id);
                        if (player != null)
                        {
                            string reason = args.Length > 1 ? string.Join(" ", args.CopyOfRange(1, args.Length)) : "Kicked.";
                            FengGameManagerMKII.Instance.KickPlayer(player, false, reason);

                            if (!FengGameManagerMKII.OnPrivateServer)
                            {
                                GameHelper.Broadcast(GExtensions.AsString(player.customProperties[PhotonPlayerProperty.Name]).Colored() + " has been kicked!");
                                GameHelper.Broadcast($"Reason: \"{reason}\"");
                            }
                        }
                    }
                }
            }
        }
    }
}
