﻿using Ae.Steam.Client;
using Amazon;
using Amazon.DynamoDBv2;
using Estranged.Automation.Runner.Syndication;
using Estranged.Automation.Shared;
using Google.Cloud.Language.V1;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Bootstrapping");

            var httpClient = new HttpClient();

            var productHeader = new ProductInfoHeaderValue("Estranged-Automation", "1.0.0");
            httpClient.DefaultRequestHeaders.UserAgent.Add(productHeader);

            var gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue(productHeader.Product.Name, productHeader.Product.Version))
            {
                Credentials = new Credentials("estranged-automation", Environment.GetEnvironmentVariable("GITHUB_PASSWORD"))
            };

            var services = new ServiceCollection()
                .AddLogging(options => options.AddConsole().SetMinimumLevel(LogLevel.Warning))
                .AddTransient<RunnerManager>()
                .AddTransient<IRunner, DiscordRunner>()
                .AddSingleton<IGitHubClient>(gitHubClient)
                .AddTransient<IAmazonDynamoDB>(x => new AmazonDynamoDBClient(RegionEndpoint.EUWest1))
                .AddTransient<ISeenItemRepository, SeenItemRepository>()
                .AddSingleton(TranslationClient.Create())
                .AddSingleton(LanguageServiceClient.Create());

            services.AddHttpClient();
            services.AddHttpClient<ISteamClient, SteamClient>();

            var provider = services.BuildServiceProvider();

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            var source = new CancellationTokenSource();

            AppDomain.CurrentDomain.ProcessExit += (sender, ev) => source.Cancel();

            try
            {
                Console.WriteLine("Starting manager");
                provider.GetRequiredService<RunnerManager>()
                        .Run(source.Token)
                        .GetAwaiter()
                        .GetResult();
            }
            catch (TaskCanceledException e)
            {
                loggerFactory.CreateLogger(nameof(Program)).LogInformation(e, "Task cancelled.");
            }

            return 0;
        }
    }
}
