using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TelegramBots;

namespace TelegramBotsApp
{
    public class PeriodicUpdateService : BackgroundService
    {
        private readonly IEnumerable<IPeriodicUpdate> periodicUpdates;

        public PeriodicUpdateService(IEnumerable<IPeriodicUpdate> periodicUpdates)
        {
            this.periodicUpdates = periodicUpdates;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Run(cancellationToken);
        }

        private async Task Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"PeriodicUpdateService.ExecuteAsync");

                foreach (var periodicUpdate in periodicUpdates)
                {
                    Console.WriteLine($"## {periodicUpdate.GetType().ToString()}");
                    await periodicUpdate.PeriodicUpdate();
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }
}
