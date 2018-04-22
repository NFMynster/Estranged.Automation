﻿using System.Threading;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class TranslationResponder : IResponder
    {
        private readonly ILogger<TranslationResponder> logger;
        private readonly TranslationClient translation;
        private int numberOfCharacters;
        private const int MaximumCharacters = 2500;
        private const string InvocationCommand = "!translate";

        public TranslationResponder(ILogger<TranslationResponder> logger, TranslationClient translation)
        {
            this.logger = logger;
            this.translation = translation;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (numberOfCharacters >= MaximumCharacters)
            {
                logger.LogWarning("Rate limiting translation");
                return;
            }

            if (!message.Content.ToLower().StartsWith(InvocationCommand))
            {
                return;
            }

            string messageContent = message.Content.Substring(InvocationCommand.Length).Trim();

            numberOfCharacters += message.Content.Length;

            var detection = await translation.DetectLanguageAsync(messageContent, token);
            if (detection.Language == "en")
            {
                logger.LogInformation("Ignoring message {0} due to it being in English", message.Content);
                return;
            }

            logger.LogInformation("Message is written in {0} with {1} confidence", detection.Language, detection.Confidence);

            using (message.Channel.EnterTypingState(token.ToRequestOptions()))
            {
                var translated = await translation.TranslateTextAsync(messageContent, "en", detection.Language, cancellationToken: token);
                if (translated.TranslatedText == translated.OriginalText)
                {
                    return;
                }

                string responseMessage = $"Translated \"{translated.OriginalText}\" from {translated.SpecifiedSourceLanguage}:\n{translated.TranslatedText}";
                await message.Channel.SendMessageAsync(responseMessage, options: token.ToRequestOptions());
            }
        }
    }
}
