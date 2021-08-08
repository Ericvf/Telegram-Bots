using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramBots;
using TelegramBots.Clients;
using TelegramBots.Core;

namespace TelegramBotsApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();

            // Core
            services.AddSingleton<ITelegramApi, TelegramBotAPI>();
            services.AddSingleton<ITelegramBots, TelegramBots.TelegramBots>();
            services.AddSingleton<ICommandParser, CommandParser>();

            // States
            services.AddSingleton<IStateManager<TodoBotState>, StateManager<TodoBotState>>();
            services.AddSingleton<IStateManager<MemeBotState>, StateManager<MemeBotState>>();
            services.AddSingleton<IStateManager<HappinessBotState>, StateManager<HappinessBotState>>();
            services.AddSingleton<IStateManager<ChuckNorrisBotState>, StateManager<ChuckNorrisBotState>>();
            //services.AddSingleton<IStateManager<BitbucketBotState>, StateManager<BitbucketBotState>>();
            services.AddSingleton<IStateManager<ArtOfWarBotState>, StateManager<ArtOfWarBotState>>();
            services.AddSingleton<IStateManager<ReminderBotState>, StateManager<ReminderBotState>>();
            services.AddSingleton<IStateManager<TemperatureBotState>, StateManager<TemperatureBotState>>();
            services.AddSingleton<IStateManager<DictionaryBotState>, StateManager<DictionaryBotState>>();
            services.AddSingleton<IStateManager<PrinterBotState>, StateManager<PrinterBotState>>();
            services.AddSingleton<IStateManager<TipOfADayBotState>, StateManager<TipOfADayBotState>>();
            services.AddSingleton<IStateManager<CSharpBotState>, StateManager<CSharpBotState>>();
            services.AddSingleton<IStateManager<UnifyEventsBotState>, StateManager<UnifyEventsBotState>>();

            // Clients
            services.AddSingleton<IFavQsClient, FavQsClient>();
            services.AddSingleton<IGiphyClient, GiphyClient>();
            services.AddSingleton<IChuckNorrisClient, ChuckNorrisClient>();
            services.AddSingleton<IArtOfWarClient, ArtOfWarClient>();
            services.AddSingleton<IDictionaryClient, UrbanDictionaryClient>();
            services.AddSingleton<ITipOfADayClient, TipOfADayClient>();
            services.AddSingleton<IUnifyClient, UnifyClient>();

            // Bots
            services.AddSingleton<ITelegramBot, TodoBot>();
            services.AddSingleton<IPeriodicUpdate, TodoBot>();
            services.AddSingleton<ITelegramBot, MemeBot>();
            //services.AddSingleton<ITelegramBot, HappinessBot>();
            services.AddSingleton<ITelegramBot, ChuckNorrisBot>();
            services.AddSingleton<ITelegramBot, ArtOfWarBot>();
            //services.AddSingleton<ITelegramBot, BitbucketBot>();
            //services.AddSingleton<IBitbucketBot, BitbucketBot>();
            services.AddSingleton<ITelegramBot, ReminderBot>();
            services.AddSingleton<IPeriodicUpdate, ReminderBot>();
            services.AddSingleton<ITelegramBot, DictionaryBot>();
            services.AddSingleton<ITelegramBot, PrinterBot>();
            services.AddSingleton<ITelegramBot, TipOfADayBot>();
            services.AddSingleton<ITelegramBot, CSharpBot>();
            services.AddSingleton<ITelegramBot, UnifyEventsBot>();
            services.AddSingleton<IPeriodicUpdate, UnifyEventsBot>();

            services.AddSingleton<ITemperatureBot, TemperatureBot>();
            services.AddSingleton<ITelegramBot>(x => x.GetService<ITemperatureBot>());

            RegisterConfig<ITelegramConfig, TelegramConfig>(services);
            RegisterConfig<IFavQsConfig, FavQsConfig>(services);
            RegisterConfig<IGiphyConfig, GiphyConfig>(services);

            services.AddSingleton<IHostedService, PeriodicUpdateService>();
        }

        private void RegisterConfig<TInterface, T>(IServiceCollection services)
            where T : TInterface, new()
        {
            var config = new T();
            var interfaceType = typeof(TInterface);

            Configuration.GetSection(interfaceType.Name).Bind(config);
            services.AddSingleton(interfaceType, config);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
