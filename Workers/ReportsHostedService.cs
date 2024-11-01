namespace Report.Server.Workers
{
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

        public ReportsHostedService(
            IServiceProvider services
        )
        {
            Services = services;
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

            var semaphore = new SemaphoreSlim(_maximumOrderWorkers); // Maximum 7 concurrent tasks
            
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
