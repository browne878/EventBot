namespace EventBot
{
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.Interactivity;
    using DSharpPlus.Interactivity.Extensions;
    using EventBot.Commands;
    using EventBot.Events;
    using EventBot.Models;
    using EventBot.Services;
    using EventBot.Suggestion;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using DSharpPlus.SlashCommands;
    using EventBot.EmbedBuilder;

    public class Program
    {
        private DiscordClient Bot;
        private CommandsNextExtension Commands;
        private FileService FileManager;
        private Config Config;
        private SuggestionChannelConfig SuggestionConfig;

        private static void Main()
        {
            Program program = new Program();
            program.MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            FileManager = new FileService();
            Config = FileManager.GetConfig();
            SuggestionConfig = FileManager.GetSuggestionConfig();


            //bot setup configuration
            Bot = new DiscordClient(new DiscordConfiguration()
            {
                Token = Config.DiscordOptions.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            });

            IServiceProvider services = ConfigureServices();

            SlashCommandsExtension slash = Bot.UseSlashCommands(new SlashCommandsConfiguration
                                                                {
                                                                    Services = services
                                                                });

            slash.SlashCommandErrored += Slash_SlashCommandErrored;

            

            //commands setup configuration
            CommandsNextConfiguration ccfg = new CommandsNextConfiguration
            {
                StringPrefixes = new[] { Config.DiscordOptions.Prefix },
                EnableDms = true,
                EnableMentionPrefix = true,
                Services = services,
                EnableDefaultHelp = false,
                IgnoreExtraArguments = true,
                CaseSensitive = false
            };

            //Set up interactivity
            Bot.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(5)
            });

            Commands = Bot.UseCommandsNext(ccfg);

            //call event handler
            await services.GetRequiredService<Channel>().InitializeAsync();

            //register commands
            Commands.RegisterCommands<SuggestionCommands>();
            Commands.RegisterCommands<EmbedBuilderCommands>();

            //login
            await Bot.ConnectAsync();
            await Task.Delay(Timeout.Infinite);


        }

        private Task Slash_SlashCommandErrored(SlashCommandsExtension _sender, DSharpPlus.SlashCommands.EventArgs.SlashCommandErrorEventArgs _e)
        {
            Console.WriteLine("Error");

            return Task.CompletedTask;
        }

        //dependency injection
        public IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<FileService>()
                .AddSingleton<ArkCommandManager>()
                .AddSingleton<RconManager>()
                .AddSingleton<DatabaseManager>()
                .AddSingleton<Channel>()
                .AddSingleton<EventTimer>()
                .AddSingleton<EmbedCreate>()
                .AddSingleton(Bot)
                .AddSingleton(Config)
                .AddSingleton(SuggestionConfig)
                .BuildServiceProvider();
        }
    }
}