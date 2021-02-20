using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Huebert
{
    public class Hubert : BackgroundService
    {
        private readonly ILogger<Hubert> _logger;
        private IConfiguration _configuration;

        public Hubert(ILogger<Hubert> logger, IConfigurationRoot configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _configuration["LastUpdate"] = DateTimeOffset.Now.ToString();
                await Task.Delay(1000, stoppingToken);

            }
        }
    }
}
