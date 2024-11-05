using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using TicketAdminChat.Controllers;
using TicketAdminChat.Data;
using UserChatManagement.Controllers;

namespace TicketAdminChat
{
    public partial class App : Application
    {
        private HubConnection _hubConnection;
        private IServiceProvider _serviceProvider;
        public static IServiceProvider ServiceProvider { get; private set; }
        private ILogger<App> _logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
            _logger = ServiceProvider.GetRequiredService<ILogger<App>>();

            InitializeSignalR();

            var windowLogin = new WindowLogin(
                ServiceProvider.GetRequiredService<ApplicationUserDAO>(),
                _hubConnection,
                ServiceProvider.GetRequiredService<RoomDAO>()
            );

            windowLogin.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<ApplicationUserDAO>();
            services.AddScoped<RoomDAO>();
            services.AddScoped<MainWindow>();
            services.AddScoped<WindowLogin>();
            services.AddLogging();
        }

        private async void InitializeSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://ticket-workshop.azurewebsites.net/chatHub")
                //.WithUrl("https://localhost:50818/chatHub")
                .WithAutomaticReconnect()
                .Build();

            ConfigureSignalRHandlers();

            _hubConnection.Closed += async (error) => await HandleConnectionClosed();

            await StartConnection();
        }

        private void ConfigureSignalRHandlers()
        {
            _hubConnection.On<string>("ReceiveNewUserNotification", OnNewUserNotification);
            _hubConnection.On<string>("ReceiveHelpRequest", OnHelpRequest);
            _hubConnection.On<string, bool>("updateUserStatus", OnUserStatusUpdate);
        }

        private void OnNewUserNotification(string userName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"{userName} has joined!");
            });
        }

        private void OnHelpRequest(string userName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"{userName} needs help!");
            });
        }

        private void OnUserStatusUpdate(string userName, bool isOnline)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string status = isOnline ? "is online" : "has disconnected";
                MessageBox.Show($"{userName} {status}");
            });
        }

        private async Task StartConnection()
        {
            try
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("Connected to the server.");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Connected to the server."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to the server.");
                MessageBox.Show($"Error connecting to the server: {ex.Message}");
            }
        }

        private async Task HandleConnectionClosed()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Disconnected from the server. Attempting to reconnect...");
            });

            while (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await _hubConnection.StartAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Reconnected to the server.");
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Reconnect failed: {ex.Message}");
                    });
                }
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            base.OnExit(e);
        }

    }
}
