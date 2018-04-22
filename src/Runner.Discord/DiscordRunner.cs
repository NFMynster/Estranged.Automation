﻿using Estranged.Automation.Shared;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using Discord;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Estranged.Automation.Runner.Discord.Responders;
using Estranged.Automation.Runner.Discord;
using System.Diagnostics;

namespace Estranged.Automation.Runner.Syndication
{
    public class DiscordRunner : IRunner
    {
        private readonly ILogger<DiscordRunner> logger;
        private readonly ILoggerFactory loggerFactory;
        private IDiscordClient client;
        private IServiceProvider responderProvider;

        public DiscordRunner(ILogger<DiscordRunner> logger, ILoggerFactory loggerFactory)
        {
            this.logger = logger;
            this.loggerFactory = loggerFactory;

            client = new DiscordSocketClient();
            responderProvider = new ServiceCollection()
                .AddSingleton(loggerFactory)
                .AddLogging()
                .AddSingleton(client)
                .AddSingleton<IResponder, TextResponder>()
                .AddSingleton<IResponder, HoistedRoleResponder>()
                .AddSingleton<IResponder, DadJokeResponder>()
                .BuildServiceProvider();
        }

        public async Task Run(CancellationToken token)
        {
            var socketClient = (DiscordSocketClient)client;

            socketClient.Log += ClientLog;
            socketClient.MessageReceived += ClientMessageReceived;

            await socketClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await socketClient.StartAsync();
        }

        private async Task ClientMessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook)
            {
                return;
            }

            var responders = responderProvider.GetServices<IResponder>().ToArray();
            logger.LogInformation("Starting to invoke {0} responders", responders.Length);
            await Task.WhenAll(responders.Select(x => RunResponder(x, socketMessage, CancellationToken.None)));
            logger.LogInformation("Completed invoking {0} responders", responders.Length);
        }

        private async Task RunResponder(IResponder responder, IMessage message, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            logger.LogInformation("Running responder {0} for message {1}", responder.GetType().Name, message);
            stopwatch.Start();
            await responder.ProcessMessage(message, token);
            logger.LogInformation("Completed responder {0} in {1} for message: {2}", responder.GetType().Name, stopwatch.Elapsed, message);
        }

        private Task ClientLog(LogMessage logMessage)
        {
            logger.Log(GetLogLevel(logMessage.Severity), -1, logMessage.Message, logMessage.Exception, null);
            return null;
        }

        private LogLevel GetLogLevel(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                    return LogLevel.Critical;
                case LogSeverity.Debug:
                    return LogLevel.Debug;
                case LogSeverity.Error:
                    return LogLevel.Error;
                case LogSeverity.Info:
                    return LogLevel.Information;
                case LogSeverity.Verbose:
                    return LogLevel.Trace;
                case LogSeverity.Warning:
                    return LogLevel.Warning;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
