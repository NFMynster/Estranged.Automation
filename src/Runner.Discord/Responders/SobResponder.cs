﻿using Discord;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class SobResponder : IResponder
    {
        private const string SOB = "😭";

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.Contains(SOB))
            {
                return;
            }

            if (!(RandomNumberGenerator.GetInt32(0, 101) >= 90))
            {
                return;
            }

            using (message.Channel.EnterTypingState())
            {
                await Task.Delay(TimeSpan.FromSeconds(1), token);
                await message.Channel.SendMessageAsync(SOB, options: token.ToRequestOptions());
            }
        }
    }
}
