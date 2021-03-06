﻿namespace Guardian.Features.Commands.Impl
{
    class CommandMute : Command
    {
        public CommandMute() : base("mute", new string[0], "<id>", false) { }

        public override void Execute(InRoomChat irc, string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int id))
            {
                PhotonPlayer player = PhotonPlayer.Find(id);
                if (player != null)
                {
                    if (!InRoomChat.Ignored.Contains(player))
                    {
                        InRoomChat.Ignored.Add(player);
                        irc.AddLine($"Ignoring chat messages from #{id}.");
                    }
                }
            }
        }
    }
}
