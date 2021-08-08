using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using TelegramBots.Core;

namespace TelegramBots
{
    public class CSharpBot : ITelegramBot
    {
        private readonly ITelegramApi telegramApi;
        private readonly ICommandParser commandParser;
        private readonly IStateManager<CSharpBotState> stateManager;
        private ScriptOptions scriptOptions;

        public CSharpBot(ITelegramApi telegramApi, ICommandParser commandParser, IStateManager<CSharpBotState> stateManager)
        {
            this.telegramApi = telegramApi;
            this.commandParser = commandParser;
            this.stateManager = stateManager;

            InitializeScriptOptions();
        }

        private void InitializeScriptOptions()
        {
            var mscorlib = typeof(object).GetTypeInfo().Assembly;
            var systemCore = typeof(Enumerable).GetTypeInfo().Assembly;
            var netClient = typeof(WebClient).GetTypeInfo().Assembly;
            scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.AddReferences(mscorlib, systemCore, netClient);
            scriptOptions = scriptOptions.AddImports("System");
            scriptOptions = scriptOptions.AddImports("System.Linq");
            scriptOptions = scriptOptions.AddImports("System.Collections.Generic");
            scriptOptions = scriptOptions.AddImports("System.Threading.Tasks");
        }

        public string Name => nameof(CSharpBot);

        public string ShortName => "cs";

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;
            var botName = ShortName ?? Name;
            var userName = telegramMessage.From;

            var chatState = await stateManager.HandleState(chatId, telegramMessage.ChatTitle, botName, telegramMessage.Text);
            if (chatState == null)
                return;

            var commandValues = commandParser.Parse(botName, message);
            if (commandValues == null && !chatState.DevModes.Contains(userName))
                return;

            var cmd = commandValues?.FirstWord;
            switch (cmd)
            {
                case "import":
                    var importName = commandValues.List.Skip(1).Take(1).SingleOrDefault();
                    await AddImport(chatId, importName);
                    return;

                case "reference":
                    var referenceType = commandValues.List.Skip(1).Take(1).SingleOrDefault();
                    await AddReference(chatId, referenceType);
                    return;

                case "devmode":
                    await HandleDevMode(chatState, userName);
                    await stateManager.SaveStates();
                    return;

                case "reboot":
                    await Reboot(chatState);
                    return;
            }

            var result = await Execute(chatState, commandValues?.Text ?? message);
            if (!string.IsNullOrEmpty(result))
            {
                await telegramApi.SendTextMessageAsync(chatId, result);
            }

            await stateManager.SaveStates();
        }


        private async Task Reboot(CSharpBotState chatState)
        {
            InitializeScriptOptions();

            if (chatState.ScriptState != null)
            {
                chatState.Globals.TokenSource.Cancel();
                chatState.ScriptState = null;
            }

            await telegramApi.SendTextMessageAsync(chatState.ChatId, $"Reboot succeeded");

            await stateManager.SaveStates();
        }

        private async Task HandleDevMode(CSharpBotState chatState, string userName)
        {
            var devModes = chatState.DevModes;
            if (devModes.Contains(userName))
            {
                devModes.Remove(userName);
                await telegramApi.SendTextMessageAsync(chatState.ChatId,$"Okay {userName}, that's enough");
            }
            else
            {
                devModes.Add(userName);
                await telegramApi.SendTextMessageAsync(chatState.ChatId, $"Enabling DevMode for {userName}");
            }
        }

        private async Task AddImport(long chatId, string importName)
        {
            scriptOptions = scriptOptions.AddImports(importName);
            await telegramApi.SendTextMessageAsync(chatId, $"Added import for {importName}");
        }

        private async Task AddReference(long chatId, string referenceType)
        {
            var typeInfo = Type.GetType(referenceType).GetTypeInfo();
            scriptOptions = scriptOptions.AddReferences(typeInfo.Assembly);

            await telegramApi.SendTextMessageAsync(chatId, $"Added reference for {typeInfo}");
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} allows you to execute CSharp code. ");
            stringBuilder.AppendLine($"e.g. /{ShortName ?? Name} Environment.MachineName");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Import: e.g. /{ShortName ?? Name} import System.Text.RegularExpressions");
            stringBuilder.AppendLine($"Adds the namespace to the scripting environment");
            stringBuilder.AppendLine();


            stringBuilder.AppendLine($"Add Reference: e.g. /{ShortName ?? Name} reference System.Net.WebClient");
            stringBuilder.AppendLine($"Adds the namespace to the scripting environment");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"DevMode: e.g. /{ShortName ?? Name} devmode");
            stringBuilder.AppendLine($"Enables devmode for the user. Commands don't require bot prefix for current user");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Reboot: e.g. /{ShortName ?? Name} reboot");
            stringBuilder.AppendLine($"Clears the scripting environment.");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }

        public async Task<string> Execute(CSharpBotState chatState, string code)
        {
            try
            {
                if (chatState.ScriptState == null)
                {
                    var cancellationTokenSource = new CancellationTokenSource();

                    chatState.Globals = new CSharpBotGlobals()
                    {
                        TelegramApi = telegramApi,
                        BotState = chatState,
                        TokenSource = cancellationTokenSource,
                    };

                    var script = CSharpScript.Create(code, scriptOptions, globalsType: typeof(CSharpBotGlobals));
                    chatState.ScriptState = await script.RunAsync(chatState.Globals, cancellationToken: cancellationTokenSource.Token);
                }
                else
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    chatState.ScriptState.ContinueWithAsync(code, scriptOptions, cancellationToken: chatState.Globals.TokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }

                return chatState.ScriptState.ReturnValue?.ToString();
            }
            catch (CompilationErrorException ex)
            {
                return ex.Message.ToString();
            }
        }
    }

    public class CSharpBotGlobals
    {
        public ITelegramApi TelegramApi { get; set; }

        public CSharpBotState BotState { get; set; }

        public CancellationTokenSource TokenSource { get; set; }

        public async Task WriteLine(string message) {

            await TelegramApi.SendTextMessageAsync(BotState.ChatId, message);
        }
    }

    public class CSharpBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public ScriptState<object> ScriptState { get; set; }

        public List<string> DevModes { get; } = new List<string>();

        [JsonIgnore]
        public CSharpBotGlobals Globals { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            return stringBuilder.ToString();
        }
    }
}
