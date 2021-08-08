using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TelegramBots;

namespace TelegramBotsApp.Controllers
{
    public class BossController : Controller
    {
        private readonly ILogger<BossController> logger;
        private readonly ITemperatureBot temperatureBot;

        public BossController(ILogger<BossController> logger, ITemperatureBot temperatureBot)
        {
            this.logger = logger;
            this.temperatureBot = temperatureBot;
        }

        public Task<IActionResult> Index(float temperature)
        {
            logger.LogInformation("### Temperature " + temperature);

            temperatureBot.SetTemperature(temperature);

            return Task.FromResult<IActionResult>(
                Json(new {
                    status = "OK"
                })
            );
        }
    }
}
