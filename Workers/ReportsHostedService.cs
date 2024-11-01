namespace Report.Server.Workers
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using System.Threading;
    using System.Threading.Tasks;
    using System;
    using Reporting.Server.Services;

    public class ReportsHostedService : IHostedService, IDisposable
    {
        private long _timerInterval;
        private int _maximumOrderWorkers;
        private readonly ILogger<ReportsHostedService> _logger;
        private readonly object Locker = new object();
        private IServiceProvider Services { get; }
        private Timer _timer;
        private bool _running;
        private Task _currentTask;

        public ReportsHostedService(IServiceProvider services, IConfiguration configuration, ILogger<ReportsHostedService>logger
            )
        {
            Services = services;
            _logger = logger;
            _timerInterval = configuration.GetValue<long>("TimerInterval");
            _maximumOrderWorkers = configuration.GetValue<int>("MaximumOrderWorkers");
        }


        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($@"{nameof(ReportsHostedService)} is running.");

            _timer = new Timer(Callback, null, 0, _timerInterval);

            return Task.CompletedTask;
        }

        private void Callback(object state)
        {
            if (_running) return;

            lock (Locker)
            {
                if (_running) return;

                _running = true;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

                _currentTask = RunAsync();
            }
        }

        private async Task RunAsync()
        {
            using var scope = Services.CreateScope();
            var telerikService = scope.ServiceProvider.GetRequiredService<TelerikReportServerClient>();
            var token = await telerikService.GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                var categories = await telerikService.GetReportCategoriesAsync(token);
             

                _logger.LogInformation("Fetched report categories successfully.");
            }
            else
            {
                _logger.LogWarning("Failed to fetch token for Telerik Report Server.");
            }
            lock (Locker)
            {
                _running = false;
                _timer.Change(_timerInterval, _timerInterval);
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($@"{nameof(ReportsHostedService)} is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
