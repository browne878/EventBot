using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using EventBot.Events;
using EventBot.Models;
using EventBot.Services;
using DSharpPlus.Interactivity;
using DSharpPlus.EventArgs;


namespace EventBot.Suggestion
{
    public class Channel
    {
        private readonly DiscordClient Bot;
        private readonly Config Config;
        private readonly EventTimer EventTimer;
        private readonly FileService FileManager;

        public Channel(DiscordClient _bot, Config _config, FileService _fileManager, EventTimer _eventTimer)
        {
            Bot = _bot;
            Config = _config;
            FileManager = _fileManager;
            EventTimer = _eventTimer;
            Config = _fileManager.GetConfig();
        }

        public async Task InitializeAsync()
        {
            EventTimer.SuggestionsComplete += async (_source, _suggestionChannel) =>
            {
                await CompleteSuggestionChannel(_suggestionChannel.SuggestionChannel);
            };
            await Task.CompletedTask;

            Bot.MessageCreated += async (_client, _eventMessage) =>
            {
                SuggestionChannelConfig suggestionChannels = FileManager.GetSuggestionConfig();

                IEnumerable<SuggestionChannel> channelExist = suggestionChannels.SuggestionChannel.Where(
                    _x => _x.ChannelId == _eventMessage.Channel.Id && _x.Closed == false);

                if (_eventMessage.Author.IsBot) return;
                if (suggestionChannels.SuggestionChannel.Count == 0) return;
                if (!channelExist.Any()) return;
                if (_eventMessage.Message.Content.ToLower().Trim().Equals("/suggest")) return;

                await IncorrectSuggestionFormat(_eventMessage.Channel, _eventMessage.Author, _eventMessage.Message);
            };
        }

        public async Task CreateEmoji(DiscordMessage _reactMessage, List<DiscordEmoji> _emojiList)
        {
            foreach (DiscordEmoji emoji in _emojiList) await Task.Run(() => _reactMessage.CreateReactionAsync(emoji));
        }

        public async Task CreateSuggestionsChannel(string _channelName, double _time, DiscordChannel _channelCategory, string _season)
        {
            SuggestionChannelConfig existingSuggestionChannels = FileManager.GetSuggestionConfig(); //gets current suggestion channels
            DiscordGuild server = _channelCategory.Guild; //gets server
            var suggestionChannelPermissions = new List<DiscordOverwriteBuilder>(); //list of discord permissions
            string category = null;
            DiscordRole suggestionRole = null;

            //creates permissions for everyone role
            var suggestionChannelEveryonePermissions = new DiscordOverwriteBuilder();
            suggestionChannelEveryonePermissions.For(server.EveryoneRole);
            suggestionChannelEveryonePermissions.Deny(Permissions.AccessChannels);

            //creates permissions for admins
            var suggestionChannelAdminPermissions = new DiscordOverwriteBuilder();
            suggestionChannelAdminPermissions.For(server.GetRole(Config.DiscordOptions.AdminRole));
            suggestionChannelAdminPermissions.Allow(Permissions.Administrator);

            //creates permissions for community supports
            var suggestionChannelSupportPermissions = new DiscordOverwriteBuilder();
            suggestionChannelSupportPermissions.For(server.GetRole(Config.DiscordOptions.CommSupportRole));
            suggestionChannelSupportPermissions.Allow(Permissions.Administrator);

            //creates permissions for suggestion group
            var suggestionChannelGroupPermissions = new DiscordOverwriteBuilder();
            suggestionChannelGroupPermissions.Allow(Permissions.AccessChannels);
            suggestionChannelGroupPermissions.Allow(Permissions.SendMessages);
            suggestionChannelGroupPermissions.Deny(Permissions.AddReactions);

            //gets who group permissions are for
            switch (_channelCategory.Id)
            {
                case var _ when _channelCategory.Id == Config.DiscordOptions.PvpveCategory:
                    suggestionRole = server.GetRole(Config.DiscordOptions.PvpveRole);
                    suggestionChannelGroupPermissions.For(suggestionRole);
                    category = "PVPVE";
                    break;

                case var _ when _channelCategory.Id == Config.DiscordOptions.PvpCategory:
                    suggestionRole = server.GetRole(Config.DiscordOptions.PvpRole);
                    suggestionChannelGroupPermissions.For(suggestionRole);
                    category = "PVP";
                    break;

                case var _ when _channelCategory.Id == Config.DiscordOptions.PveCategory:
                    suggestionRole = server.GetRole(Config.DiscordOptions.PveRole);
                    suggestionChannelGroupPermissions.For(suggestionRole);
                    category = "PVE";
                    break;
            }

            //adds permissions to list
            suggestionChannelPermissions.Add(suggestionChannelGroupPermissions);
            suggestionChannelPermissions.Add(suggestionChannelEveryonePermissions);
            suggestionChannelPermissions.Add(suggestionChannelAdminPermissions);
            suggestionChannelPermissions.Add(suggestionChannelSupportPermissions);

            //creates channel
            DiscordChannel suggestionChannel = await server.CreateChannelAsync(_channelName, ChannelType.Text, _channelCategory,
                default, null, null, suggestionChannelPermissions);

            var suggestionChannelObject = new SuggestionChannel //Creates suggestion channel for config
            {
                ChannelId = suggestionChannel.Id,
                ChannelName = suggestionChannel.Name,
                CreatedAt = suggestionChannel.CreationTimestamp.DateTime,
                ChannelDuration = _time,
                Closed = false
            };

            if (existingSuggestionChannels.SuggestionChannel.Exists(_x =>
                _x.ChannelId == suggestionChannelObject.ChannelId)) return; //returns if channel is suggestion

            existingSuggestionChannels.SuggestionChannel
                .Add(suggestionChannelObject); //adds channel to suggestion channel to suggestion channel list

            FileManager.AddSuggestionChannel(existingSuggestionChannels); //adds suggestion channel to config

            EventTimer.SuggestionsTimerStart(suggestionChannelObject, _time);

            var channelEmbed = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = $"{category} Season{_season} Suggestions",
                Description = "**THIS CHANNEL IS USED FOR COMMUNITY SUGGESTIONS ONLY**\n\nBlacklisted Suggestions:\n- Server Rates\n- Raidtimes\n- Tribesize\n- Troll Suggestions" +
                              $"\n\nAll suggestions that match the blacklist will be deleted.\n{suggestionRole?.Mention}"
            };

            await suggestionChannel.SendMessageAsync(channelEmbed); //sends message in channel
        }

        public async Task CreateSuggestionTicket(DiscordMember _member, DiscordChannel _suggestionChannel)
        {
            DiscordGuild server = _member.Guild; //gets server
            var suggestionTicketPrefix = ""; //defines suggestion ticket cluster
            DiscordChannel suggestionCategory = server.GetChannel(Config.DiscordOptions.SuggestionTicketCategory); //gets suggestion ticket category
            var suggestionResults = new List<string>(); //creates a list of results
            DiscordChannel adminSuggestionChannel = await Bot.GetChannelAsync(Config.DiscordOptions.SuggestionTicketLogCategory);

            //sets ticket cluster
            suggestionTicketPrefix = _suggestionChannel.Parent.Name switch
            {
                "💢Bloody ARK [Main]" => "pvpve",
                "💢Bloody ARK [SMALLTRIBES]" => "pvp",
                "💢Bloody ARK [Survival]" => "pve",
                _ => suggestionTicketPrefix
            };

            //creates permissions for everyone role
            var suggestionTicketPermissionsEveryone = new DiscordOverwriteBuilder();
            suggestionTicketPermissionsEveryone.For(server.EveryoneRole);
            suggestionTicketPermissionsEveryone.Deny(Permissions.AccessChannels);

            //creates permissions for the user
            var suggestionTicketPermissionsUser = new DiscordOverwriteBuilder();
            suggestionTicketPermissionsUser.For(_member);
            suggestionTicketPermissionsUser.Allow(Permissions.AccessChannels);

            //creates permissions for admins
            var suggestionTicketPermissionsAdmin = new DiscordOverwriteBuilder();
            suggestionTicketPermissionsAdmin.For(server.GetRole(Config.DiscordOptions.AdminRole));
            suggestionTicketPermissionsAdmin.Allow(Permissions.Administrator);

            //creates permissions for community supports
            var suggestionTicketPermissionsSupport = new DiscordOverwriteBuilder();
            suggestionTicketPermissionsSupport.For(server.GetRole(Config.DiscordOptions.CommSupportRole));
            suggestionTicketPermissionsAdmin.Allow(Permissions.Administrator);

            //creates the list of permissions to add to the channel
            var suggestionTicketPermissions = new List<DiscordOverwriteBuilder> { suggestionTicketPermissionsEveryone, suggestionTicketPermissionsUser, suggestionTicketPermissionsAdmin, suggestionTicketPermissionsSupport };

            //creates the channel
            DiscordChannel suggestionTicket = await server.CreateChannelAsync(
                $"{suggestionTicketPrefix}-suggestion-{_member.Username}", ChannelType.Text, suggestionCategory,
                default, null, null, suggestionTicketPermissions);

            await SuggestionTicketWelcome(_member, suggestionTicket); //sends welcome message

            string playedClusters = await GetPlayedClusters(_member, suggestionTicket);

            //checks result for errors
            switch (playedClusters)
            {
                case "closed":
                    return;

                case "":
                    await suggestionTicket.SendMessageAsync("There has been an error.");
                    return;
            }

            suggestionResults.Add(playedClusters);

            string lastPlayedCluster = await GetLastClusterPlayed(_member, suggestionTicket);

            //checks result for errors
            switch (lastPlayedCluster)
            {
                case "":
                    await suggestionTicket.SendMessageAsync("There has been an error.");
                    return;

                case "closed":
                    return;
            }

            suggestionResults.Add(lastPlayedCluster);

            string lastTribe = await GetLastTribe(_member, suggestionTicket);

            //checks result for errors
            switch (lastTribe)
            {
                case "":
                    await suggestionTicket.SendMessageAsync("There has been an error.");
                    return;

                case "closed":
                    return;
            }

            suggestionResults.Add(lastPlayedCluster);

            string averageTribeSize = await GetAverageTribeSize(_member, suggestionTicket);

            //checks result for errors
            switch (averageTribeSize)
            {
                case "":
                    await suggestionTicket.SendMessageAsync("There has been an error.");
                    return;

                case "closed":
                    return;
            }

            suggestionResults.Add(averageTribeSize);

            string suggestion = await GetSuggestion(_member, suggestionTicket);

            //checks the result for errors
            switch (suggestion)
            {
                case "":
                    await suggestionTicket.SendMessageAsync("There has been an error.");
                    return;

                case "closed":
                    return;
            }

            suggestionResults.Add(suggestion);

            string reason = await GetReason(_member, suggestionTicket);

            //checks the result for errors
            switch (reason)
            {
                case "":
                    await suggestionTicket.SendMessageAsync("There has been an error.");
                    return;

                case "closed":
                    return;
            }

            suggestionResults.Add(reason);

            bool ticketComplete = await Confirm(_member, suggestionTicket, suggestionResults);

            if (ticketComplete == false) return; //returns if error or closed

            //creates embed for pending suggestion
            var logSuggestionEmbed = new DiscordEmbedBuilder
            {
                Title = $"{suggestionTicketPrefix.ToUpper()} - {_member.Username}'s Suggestion",
                Color = DiscordColor.Red,
            };
            logSuggestionEmbed.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            logSuggestionEmbed.AddField("Played Clusters", playedClusters, true);
            logSuggestionEmbed.AddField("Last Played Cluster", lastPlayedCluster, true);
            logSuggestionEmbed.AddField("Last Tribe", lastTribe, true);
            logSuggestionEmbed.AddField("Average Tribe Size", averageTribeSize, true);
            logSuggestionEmbed.AddField("Suggestion Confirmed Correct", $"{true}", true);
            logSuggestionEmbed.AddField("Suggestion", suggestion);
            logSuggestionEmbed.AddField("Reason", reason);

            var suggestionEmbed = new DiscordEmbedBuilder
            {
                Title = $"{suggestionTicketPrefix.ToUpper()} - {_member.Username}'s Suggestion",
                Color = DiscordColor.Red,
            };
            logSuggestionEmbed.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            suggestionEmbed.AddField("Suggestion", suggestion);
            suggestionEmbed.AddField("Reason", reason);

            await suggestionTicket.DeleteAsync();

            IReadOnlyList<DiscordChannel> serverChannels = await server.GetChannelsAsync();

            List<DiscordChannel> logChannel = serverChannels.Where(_x => _x.Parent == adminSuggestionChannel && _x.Name.Contains(suggestionTicketPrefix + "-")).ToList();

            DiscordMember browne = await server.GetMemberAsync(216557289694429184);
            DiscordDmChannel browneDm = await browne.CreateDmChannelAsync();

            if (logChannel.Count != 1)
            {
                await browneDm.SendMessageAsync("RETARD");
            }

            DiscordMessage logSuggestion = await logChannel[0].SendMessageAsync(logSuggestionEmbed);
            DiscordMessage userSuggestion = await _suggestionChannel.SendMessageAsync(suggestionEmbed);

            logSuggestionEmbed.WithDescription($"Suggestion Link\n{userSuggestion.JumpLink}");
            suggestionEmbed.WithDescription($"Admin Suggestion Link\n||{logSuggestion.JumpLink}||");

            await logSuggestion.ModifyAsync(new Optional<DiscordEmbed>(logSuggestionEmbed));
            await userSuggestion.ModifyAsync(new Optional<DiscordEmbed>(suggestionEmbed));

            var voteEmoji = new List<DiscordEmoji> { DiscordEmoji.FromName(Bot, ":+1:"), DiscordEmoji.FromName(Bot, ":-1:") };
            await CreateEmoji(userSuggestion, voteEmoji);
        }

        private async Task SuggestionTicketWelcome(DiscordMember _member, DiscordChannel _suggestionTicket)
        {
            var welcomeReactions = new List<DiscordEmoji>();

            //creates welcome embed
            var welcomeEmbed = new DiscordEmbedBuilder
            {
                Title = "Welcome to our New Suggestion System!",
                Color = DiscordColor.Red,
                Description =
                    "We have developed this new suggestion system to hopefully get a better idea of what you guys want. With this we will be able to understand why you want the change," +
                    " what category of player you fall into and allow us to make a more informed final decision.\n\n" +
                    "In order for this to work please read each stage carefully and follow the instructions. Be sure to answer honestly, if you lie during this process your suggestion " +
                    "will be deleted without warning.\n" +
                    "You can cancel your suggestion at any time by reacting to this message\n\n" +
                    "We hope you all like this new system! Lets get started..."
            };
            welcomeEmbed.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            welcomeReactions.Add(DiscordEmoji.FromName(Bot, ":-1:"));

            DiscordMessage welcomeMessage = await _suggestionTicket.SendMessageAsync(welcomeEmbed);

            await CreateEmoji(welcomeMessage, welcomeReactions);
        }

        private async Task<string> GetPlayedClusters(DiscordMember _member, DiscordChannel _suggestionTicket)
        {
            DiscordUser memberAsUser = await Bot.GetUserAsync(_member.Id); //gets member as a user
            var clusterSelectReactions = new List<DiscordEmoji>(); //creates emoji list
            InteractivityExtension interactivity = Bot.GetInteractivity(); //gets interactivity module
            var selectedClusterResult = ""; //defines selected cluster

            //creates cluster selection embed
            var clusterSelect = new DiscordEmbedBuilder
            {
                Title = "Please select the clusters you play",
                Color = DiscordColor.Red,
                Description = "Please react with one of the following to select the clusters that you play\n\n" +
                              ":one: - PVPVE\n:two: - PVP\n:three: - PVE\n:four: - PVPVE and PVP and PVE\n:five: - PVPVE and PVP\n:six: - PVPVE and PVE\n:seven: - PVP and PVE"
            };
            clusterSelect.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            //adds emojis to list
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":one:"));
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":two:"));
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":three:"));
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":four:"));
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":five:"));
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":six:"));
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":seven:"));

            DiscordMessage clusterMessage = await _suggestionTicket.SendMessageAsync(clusterSelect); //sends embed
            await CreateEmoji(clusterMessage, clusterSelectReactions); //adds reactions to message

            InteractivityResult<MessageReactionAddEventArgs> selectedCluster = await interactivity.WaitForReactionAsync(_x => _x.Message == clusterMessage &&
                                                                                                                              _x.User == memberAsUser && _x.Channel == _suggestionTicket &&
                                                                                                                              (_x.Emoji == clusterSelectReactions[0] || _x.Emoji == clusterSelectReactions[1] ||
                                                                                                                               _x.Emoji == clusterSelectReactions[2] ||
                                                                                                                               _x.Emoji == clusterSelectReactions[3] || _x.Emoji == clusterSelectReactions[4] ||
                                                                                                                               _x.Emoji == clusterSelectReactions[5] ||
                                                                                                                               _x.Emoji == clusterSelectReactions[6]));

            if (selectedCluster.TimedOut)
            {
                await _suggestionTicket.DeleteAsync();
                selectedClusterResult = "closed";
                return selectedClusterResult;
            }

            selectedClusterResult = selectedCluster.Result.Emoji switch
            {
                _ when selectedCluster.Result.Emoji == clusterSelectReactions[0] => "PVPVE",
                _ when selectedCluster.Result.Emoji == clusterSelectReactions[1] => "PVP",
                _ when selectedCluster.Result.Emoji == clusterSelectReactions[2] => "PVE",
                _ when selectedCluster.Result.Emoji == clusterSelectReactions[3] => "PVPVE and PVP and PVE",
                _ when selectedCluster.Result.Emoji == clusterSelectReactions[4] => "PVPVE and PVP",
                _ when selectedCluster.Result.Emoji == clusterSelectReactions[5] => "PVPVE and PVE",
                _ when selectedCluster.Result.Emoji == clusterSelectReactions[6] => "PVP and PVE",
                _ => selectedClusterResult
            };

            await _suggestionTicket.SendMessageAsync("Thanks! We will move onto the next step.");
            return selectedClusterResult;
        }

        private async Task<string> GetLastClusterPlayed(DiscordMember _member, DiscordChannel _suggestionTicket)
        {
            DiscordUser memberAsUser = await Bot.GetUserAsync(_member.Id); //gets member as a user
            var clusterSelectReactions = new List<DiscordEmoji>(); //creates emoji list
            InteractivityExtension interactivity = Bot.GetInteractivity(); //gets interactivity module
            var selectedClusterResult = ""; //defines selected cluster

            //creates cluster selection embed
            var clusterSelect = new DiscordEmbedBuilder
            {
                Title = "Please select the last clusters you played",
                Color = DiscordColor.Red,
                Description = "Please react with one of the following to select the last clusters that you played\n\n" +
                              ":one: - PVPVE\n:two: - PVP\n:three: - PVE"
            };
            clusterSelect.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            //adds emojis to list
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":one:"));
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":two:"));
            clusterSelectReactions.Add(DiscordEmoji.FromName(Bot, ":three:"));

            DiscordMessage clusterMessage = await _suggestionTicket.SendMessageAsync(clusterSelect); //sends embed
            await CreateEmoji(clusterMessage, clusterSelectReactions); //adds reactions to message

            InteractivityResult<MessageReactionAddEventArgs> selectedCluster = await interactivity.WaitForReactionAsync(_x => _x.Message == clusterMessage &&
                                                                                                                              _x.User == memberAsUser && _x.Channel == _suggestionTicket &&
                                                                                                                              (_x.Emoji == clusterSelectReactions[0] || _x.Emoji == clusterSelectReactions[1] ||
                                                                                                                               _x.Emoji == clusterSelectReactions[2]));

            if (selectedCluster.TimedOut)
            {
                await _suggestionTicket.DeleteAsync();
                selectedClusterResult = "closed";
                return selectedClusterResult;
            }

            switch (selectedCluster.Result.Emoji)
            {
                case var _ when selectedCluster.Result.Emoji == clusterSelectReactions[0]:
                    selectedClusterResult = "PVPVE";
                    break;

                case var _ when selectedCluster.Result.Emoji == clusterSelectReactions[1]:
                    selectedClusterResult = "PVP";
                    break;

                case var _ when selectedCluster.Result.Emoji == clusterSelectReactions[2]:
                    selectedClusterResult = "PVE";
                    break;
            }

            await _suggestionTicket.SendMessageAsync("Thanks! We will move onto the next step.");
            return selectedClusterResult;
        }

        private async Task<string> GetLastTribe(DiscordMember _member, DiscordChannel _suggestionTicket)
        {
            DiscordUser memberAsUser = await Bot.GetUserAsync(_member.Id); //gets member as a user
            InteractivityExtension interactivity = Bot.GetInteractivity(); //gets interactivity module
            string lastTribeResult; //defines last tribe result

            var lastTribeEmbed = new DiscordEmbedBuilder
            {
                Title = "What was your tribe name last time you played?",
                Color = DiscordColor.Red,
                Description = "Please type the name of the tribe you last played in. The bot will then do its magic!"
            };
            lastTribeEmbed.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            await _suggestionTicket.SendMessageAsync(lastTribeEmbed);

            InteractivityResult<DiscordMessage> lastTribe =
                await interactivity.WaitForMessageAsync(_x =>
                    _x.Channel == _suggestionTicket && _x.Author == memberAsUser);

            if (lastTribe.TimedOut)
            {
                await _suggestionTicket.DeleteAsync();
                lastTribeResult = "closed";
                return lastTribeResult;
            }

            lastTribeResult = lastTribe.Result.Content;

            await _suggestionTicket.SendMessageAsync("Thanks! Onto the next step.");
            return lastTribeResult;
        }

        private async Task<string> GetAverageTribeSize(DiscordMember _member, DiscordChannel _suggestionTicket)
        {
            DiscordUser memberAsUser = await Bot.GetUserAsync(_member.Id); //gets member as a user
            InteractivityExtension interactivity = Bot.GetInteractivity(); //gets interactivity module
            var averageTribeSizeResult = ""; //defines average tribe size result

            var tribeSizeEmbed = new DiscordEmbedBuilder
            {
                Title = "Please tell us the tribe size you normally play with",
                Color = DiscordColor.Red,
                Description =
                    "Please type the size of the tribe you normally play with. Make sure you just type it as a number like 12 so the bot can understand it."
            };
            tribeSizeEmbed.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            await _suggestionTicket.SendMessageAsync(tribeSizeEmbed);

            var loopComplete = false;
            while (loopComplete == false)
            {
                InteractivityResult<DiscordMessage> tribeSizeAnswer =
                    await interactivity.WaitForMessageAsync(_x =>
                        _x.Channel == _suggestionTicket && _x.Author == memberAsUser);

                if (tribeSizeAnswer.TimedOut)
                {
                    await _suggestionTicket.DeleteAsync();
                    averageTribeSizeResult = "closed";
                    return averageTribeSizeResult;
                }

                if (Regex.IsMatch(tribeSizeAnswer.Result.Content, @"^\d+$"))
                    switch (tribeSizeAnswer.Result.Content)
                    {
                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "1":
                            averageTribeSizeResult = "1";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "2":
                            averageTribeSizeResult = "2";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "3":
                            averageTribeSizeResult = "3";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "4":
                            averageTribeSizeResult = "4";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "5":
                            averageTribeSizeResult = "5";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "6":
                            averageTribeSizeResult = "6";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "7":
                            averageTribeSizeResult = "7";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "8":
                            averageTribeSizeResult = "8";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "9":
                            averageTribeSizeResult = "9";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "10":
                            averageTribeSizeResult = "10";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "11":
                            averageTribeSizeResult = "11";
                            loopComplete = true;
                            break;

                        case var _ when tribeSizeAnswer.Result.Content.Trim() == "12":
                            averageTribeSizeResult = "12";
                            loopComplete = true;
                            break;
                    }
                else
                    await _suggestionTicket.SendMessageAsync("You did not enter a number. Please try again");
            }

            await _suggestionTicket.SendMessageAsync("Great! On to the next step");
            return averageTribeSizeResult;
        }

        private async Task<string> GetSuggestion(DiscordMember _member, DiscordChannel _suggestionTicket)
        {
            DiscordUser memberAsUser = await Bot.GetUserAsync(_member.Id); //gets member as a user
            InteractivityExtension interactivity = Bot.GetInteractivity(); //gets interactivity module
            var suggestionResult = ""; //defines suggestion result

            var suggestionEmbed = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Please provide us with your suggestion for the next wipe.",
                Description =
                    "Please keep your suggestion short and sweet.\n\nFor example \"Reduce bola timer\".\n\nYou will be asked to give a reason and description in the next step."
            };
            suggestionEmbed.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            await _suggestionTicket.SendMessageAsync(suggestionEmbed);

            var loopComplete = false;
            while (loopComplete == false)
            {
                InteractivityResult<DiscordMessage> userSuggestion =
                    await interactivity.WaitForMessageAsync(_x =>
                        _x.Channel == _suggestionTicket && _x.Author == memberAsUser);

                if (userSuggestion.TimedOut)
                {
                    await _suggestionTicket.DeleteAsync();
                    suggestionResult = "closed";
                    return suggestionResult;
                }

                if (userSuggestion.Result.Content.Length > 150)
                {
                    await _suggestionTicket.SendMessageAsync(
                        "Your suggestion is too long. You will be asked for more detail next. DON'T PANIC EVERYTHING IS FINE!");
                }
                else
                {
                    suggestionResult = userSuggestion.Result.Content;
                    loopComplete = true;
                }
            }

            await _suggestionTicket.SendMessageAsync("Awesome! Onto the final step.");
            return suggestionResult;
        }

        private async Task<string> GetReason(DiscordMember _member, DiscordChannel _suggestionTicket)
        {
            DiscordUser memberAsUser = await Bot.GetUserAsync(_member.Id); //gets member as a user
            InteractivityExtension interactivity = Bot.GetInteractivity(); //gets interactivity module
            var reasonResult = ""; //defines suggestion result

            var reasonEmbed = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Please give us an explanation to why you would like to see this change in the next season",
                Description =
                    "Give us a good explanation to why you would like to see this change. Even if this is not voted for, admins may decide to approve this suggestion based off your" +
                    " description.\n\nThere is a character limit of 350. Make sure to have your reason in 1 message!"
            };
            reasonEmbed.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

            await _suggestionTicket.SendMessageAsync(reasonEmbed);

            var loopComplete = false;
            while (loopComplete == false)
            {
                var userReason =
                    await interactivity.WaitForMessageAsync(_x =>
                        _x.Channel == _suggestionTicket && _x.Author == memberAsUser);

                if (userReason.TimedOut)
                {
                    reasonResult = "closed";
                    await _suggestionTicket.DeleteAsync();
                    return reasonResult;
                }

                if (userReason.Result.Content.Length > 350)
                {
                    await _suggestionTicket.SendMessageAsync(
                        "Your reason is too long! Please make it a bit shorter and easier to read");
                }
                else
                {
                    loopComplete = true;
                    reasonResult = userReason.Result.Content;
                }
            }

            await _suggestionTicket.SendMessageAsync("Great! Thanks for your suggestion!");
            return reasonResult;
        }

        private async Task<bool> Confirm(DiscordMember _member, DiscordChannel _suggestionTicket,
                                         IReadOnlyList<string> _suggestionAnswers)
        {
            var memberAsUser = await Bot.GetUserAsync(_member.Id); //gets member as a user
            var interactivity = Bot.GetInteractivity(); //gets interactivity module
            var addReactions = new List<DiscordEmoji>(); //creates list of emojis to add
            var confirmSuggestion = new DiscordEmbedBuilder(); //creates embed variable

            var loopComplete = false;
            while (loopComplete == false)
            {
                //builds embed
                confirmSuggestion.WithTitle("Please read and confirm your suggestion");
                confirmSuggestion.WithColor(DiscordColor.Red);
                confirmSuggestion.WithDescription(
                    "Please react with the relevant reaction to change any part of your suggestion. Once you are happy react with :+1: and we will complete the suggestion");
                confirmSuggestion.WithFooter("https://bloody-ark.com/ - " + DateTime.Now,
                    "https://cdn.discordapp.com/attachments/752642174801281214/825190264053563432/favicon.png");

                confirmSuggestion.AddField(":one: - Played Clusters", _suggestionAnswers[0]);
                confirmSuggestion.AddField(":two: - Last Played Cluster", _suggestionAnswers[1]);
                confirmSuggestion.AddField(":three: - Last Tribe", _suggestionAnswers[2]);
                confirmSuggestion.AddField(":four: - Average Tribe Size", _suggestionAnswers[3]);
                confirmSuggestion.AddField(":five: - Suggestion", _suggestionAnswers[4]);
                confirmSuggestion.AddField(":six: - Reason", _suggestionAnswers[5]);

                addReactions.Add(DiscordEmoji.FromName(Bot, ":one:"));
                addReactions.Add(DiscordEmoji.FromName(Bot, ":two:"));
                addReactions.Add(DiscordEmoji.FromName(Bot, ":three:"));
                addReactions.Add(DiscordEmoji.FromName(Bot, ":four:"));
                addReactions.Add(DiscordEmoji.FromName(Bot, ":five:"));
                addReactions.Add(DiscordEmoji.FromName(Bot, ":six:"));
                addReactions.Add(DiscordEmoji.FromName(Bot, ":+1:"));

                DiscordMessage confirmMessage = await _suggestionTicket.SendMessageAsync(confirmSuggestion);

                await CreateEmoji(confirmMessage, addReactions);

                InteractivityResult<MessageReactionAddEventArgs> confirmResult = await interactivity.WaitForReactionAsync(_x => _x.Message == confirmMessage &&
                                                                                                                                _x.Channel == _suggestionTicket &&
                                                                                                                                _x.User == memberAsUser && (_x.Emoji == addReactions[0] ||
                                                                                                                                                            _x.Emoji == addReactions[1] || _x.Emoji == addReactions[2] ||
                                                                                                                                                            _x.Emoji == addReactions[3] || _x.Emoji == addReactions[4] ||
                                                                                                                                                            _x.Emoji == addReactions[5] || _x.Emoji == addReactions[6]));

                if (confirmResult.TimedOut)
                {
                    await _suggestionTicket.DeleteAsync();
                    return false;
                }

                switch (confirmResult.Result.Emoji)
                {
                    case var _ when confirmResult.Result.Emoji == addReactions[0]:
                        await GetPlayedClusters(_member, _suggestionTicket);
                        break;

                    case var _ when confirmResult.Result.Emoji == addReactions[1]:
                        await GetLastClusterPlayed(_member, _suggestionTicket);
                        break;

                    case var _ when confirmResult.Result.Emoji == addReactions[2]:
                        await GetLastTribe(_member, _suggestionTicket);
                        break;

                    case var _ when confirmResult.Result.Emoji == addReactions[3]:
                        await GetAverageTribeSize(_member, _suggestionTicket);
                        break;

                    case var _ when confirmResult.Result.Emoji == addReactions[4]:
                        await GetSuggestion(_member, _suggestionTicket);
                        break;

                    case var _ when confirmResult.Result.Emoji == addReactions[5]:
                        await GetReason(_member, _suggestionTicket);
                        break;

                    case var _ when confirmResult.Result.Emoji == addReactions[6]:
                        loopComplete = true;
                        break;
                }
            }

            return true;
        }

        private async Task IncorrectSuggestionFormat(DiscordChannel _suggestionChannel, DiscordUser _user,
                                                     DiscordMessage _message) //needs changing
        {
            await _message.DeleteAsync();

            var wrongFormat = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = $"{_user.Username} - Incorrect format suggestion",
                Description =
                    "Your suggestion was not formatted correctly. Please follow the instructions below to provide a suggestion\n\n" +
                    "Command - /suggest \"Suggestion\" \"Reason\"\n" +
                    "Example - /suggest \"Decrease turret limit\" \"our lines are not as good as official so makes big bases harder to raid with more turrets\"\n\n" +
                    "We have used this new style of suggestions to get better feedback from the community so we can make more informed decisions!"
            };

            var wrongFormatMessage = new DiscordMessageBuilder
            {
                Content = _user.Mention,
                Embed = wrongFormat
            };

            DiscordMessage formatMessage = await _suggestionChannel.SendMessageAsync(wrongFormatMessage);

            await formatMessage.DeleteAsync();
        }

        private async Task CompleteSuggestionChannel(SuggestionChannel _suggestionChannel)
        {
            DiscordChannel actualChannel =
                await Bot.GetChannelAsync(_suggestionChannel.ChannelId); //gets the channel of the suggestionchannel
            DiscordGuild server = await Bot.GetGuildAsync(actualChannel.Guild.Id);
            DiscordRole suggestionRole = server.EveryoneRole; //defines role to see suggestion channel
            SuggestionChannelConfig suggestionChannels = FileManager.GetSuggestionConfig();

            switch (actualChannel.Parent.Id)
            {
                case var _ when actualChannel.Parent.Id == Config.DiscordOptions.PvpCategory:
                    suggestionRole = server.GetRole(Config.DiscordOptions.PvpRole);
                    break;

                case var _ when actualChannel.Parent.Id == Config.DiscordOptions.PvpveCategory:
                    suggestionRole = server.GetRole(Config.DiscordOptions.PvpveRole);
                    break;

                case var _ when actualChannel.Parent.Id == Config.DiscordOptions.PveCategory:
                    suggestionRole = server.GetRole(Config.DiscordOptions.PveRole);
                    break;
            }

            await actualChannel.AddOverwriteAsync(suggestionRole, Permissions.None, Permissions.AccessChannels);

            //suggestionChannels.SuggestionChannel.Remove(_suggestionChannel);

            int test = suggestionChannels.SuggestionChannel.IndexOf(
                suggestionChannels.SuggestionChannel.Find(_x => _x.ChannelId == _suggestionChannel.ChannelId));

            _suggestionChannel.Closed = true;

            suggestionChannels.SuggestionChannel[test].Closed = true;

            FileManager.AddSuggestionChannel(suggestionChannels);

            List<MemberSuggestion> memberSuggestions = await CalculateVoteResults(_suggestionChannel);
        }

        private async Task<List<MemberSuggestion>> CalculateVoteResults(SuggestionChannel _suggestionChannel)
        {
            DiscordChannel actualChannel =
                await Bot.GetChannelAsync(_suggestionChannel.ChannelId); //gets the actual suggestion channel
            IReadOnlyList<DiscordMessage> channelSuggestions = await actualChannel.GetMessagesAsync(); //gets the messages in the channel
            bool suggestionsOverflow = channelSuggestions.Count == 100; //suggestion overflow check
            var suggestionsList = new List<MemberSuggestion>(); //list of formatted suggestions

            var loopBool = false;
            while (loopBool == false)
            {
                //creates suggestion object for each suggestion in suggestion channel
                foreach (DiscordMessage message in channelSuggestions)
                {
                    //int yesVotes = message.Reactions.Count(x => x.Emoji == DiscordEmoji.FromGuildEmote(Bot, 820669038589247518));
                    //int noVotes = message.Reactions.Count(x => x.Emoji == DiscordEmoji.FromGuildEmote(Bot, 820669063625310238));

                    int yesVotes = message.Reactions.Count(_x => _x.Emoji == DiscordEmoji.FromName(Bot, ":+1:"));
                    int noVotes = message.Reactions.Count(_x => _x.Emoji == DiscordEmoji.FromName(Bot, ":-1:"));

                    var suggestion = new MemberSuggestion
                    {
                        NoVotes = noVotes,
                        YesVotes = yesVotes,
                        Suggestion = message
                    };
                }

                //checks if there are more suggestions to convert
                if (suggestionsOverflow)
                {
                    channelSuggestions = await actualChannel.GetMessagesBeforeAsync(channelSuggestions[99].Id);
                    suggestionsOverflow = channelSuggestions.Count == 100;
                }
                else
                {
                    loopBool = true;
                }
            }

            return suggestionsList;
        }
    }
}