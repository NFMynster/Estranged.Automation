﻿using Discord;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class RepeatPhraseResponder : IResponder
    {
        private sealed class LastPhrase
        {
            public string Message { get; set; }
            public ulong Author { get; set; }
        }

        private LastPhrase _lastMessage = new LastPhrase();

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(message.Content) || message.Embeds.Count > 0)
            {
                return;
            }

            if (message.Content == _lastMessage.Message && message.Author.Id != _lastMessage.Author)
            {
                if (RandomNumberGenerator.GetInt32(100) > 50)
                {
                    await message.Channel.SendMessageAsync(message.Content, options: token.ToRequestOptions());
                }
            }

            _lastMessage = new LastPhrase{Author = message.Author.Id,Message = message.Content};
        }
    }
}
