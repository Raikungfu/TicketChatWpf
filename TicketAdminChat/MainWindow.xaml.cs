using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualBasic.ApplicationServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TicketAdminChat.Controllers;
using TicketAdminChat.ViewModel;
using TicketApplication.Models;
using UserChatManagement.Controllers;

namespace TicketAdminChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly ApplicationUserDAO _userDao;
        private readonly RoomDAO _roomDao;
        private readonly HubConnection _hubConnection;
        private string _adminUserName;
        private string currentSender;
        private string currentRoomName;

        private WindowLogin windowLogin;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void LoadUsers()
        {
            var users = await _userDao.GetApplicationUsers();
            UserListBox.ItemsSource = users;
        }

        public MainWindow(WindowLogin _windowLogin, ApplicationUserDAO userDao, string adminFullName, string adminUserName, string adminAvatar, HubConnection hubConnection, RoomDAO roomDao)
        {
            InitializeComponent();
            _hubConnection = hubConnection;
            windowLogin = _windowLogin;
            _userDao = userDao;
            AdminNameTextBlock.Text = adminFullName;
            string baseUrl = "https://localhost:5001";
            _roomDao = roomDao;
            string avatarPath = adminAvatar;
            var imageBrush = (ImageBrush)AdminAvatarImage.Fill;
            imageBrush.ImageSource = new BitmapImage(new Uri(avatarPath, UriKind.RelativeOrAbsolute));
            _adminUserName = adminUserName;
            LoadUsers();

        }

        private void LogoutButton_Click(object sender, MouseButtonEventArgs e)
        {
            windowLogin.Show();
            System.Windows.MessageBox.Show("You have logged out.");
            this.Close();
        }

        private void AdminAvatarImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LogoutPopup.IsOpen = !LogoutPopup.IsOpen;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (LogoutPopup.IsOpen)
            {
                LogoutPopup.IsOpen = false;
            }
        }

        private async void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            MessageListBox.Items.Clear();
            var button = sender as System.Windows.Controls.Button;
            currentSender = button?.CommandParameter as string;

            if (string.IsNullOrEmpty(currentSender))
            {
                System.Windows.MessageBox.Show("User not found.");
                return;
            }

            if (ChatPanel.Visibility == Visibility.Collapsed) ChatPanel.Visibility = Visibility.Visible;

            var room = await _roomDao.StartChatAsync(currentSender, _adminUserName);

            if (room == null)
            {
                System.Windows.MessageBox.Show("User or Admin not found.");
                return;
            }

            currentRoomName = room.Name;

            if (room.Messages != null)
            {
                foreach (var message in room.Messages)
                {
                    var messageViewModel = new MessageViewModel
                    {
                        Id = message.Id,
                        Content = message.Content,
                        Timestamp = message.Timestamp,
                        FromUserName = message.FromUser.Email,
                        FromFullName = message.FromUser.Name,
                        Room = room.Name,
                    };

                    AddMessageToChat(messageViewModel);
                }
            }
            try
            {
                await _hubConnection.InvokeAsync("JoinRoom", room.Name, currentSender);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not connect to chat room: {ex.Message}");
            }
            InitializeChatHub();
        }

        private void InitializeChatHub()
        {
            _hubConnection.On<MessageViewModel>("ReceiveMessage", (message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (ChatPanel.Visibility == Visibility.Visible && message.Room == currentRoomName)
                    {
                        AddMessageToChat(message);
                    }
                });
            });
        }

        private void AddMessageToChat(MessageViewModel message)
        {
            var messageContainer = new DockPanel
            {
                LastChildFill = true,
                Margin = new Thickness(5, 2, 5, 2)
            };

            string avatarPath = "https://cdn-icons-png.flaticon.com/512/6596/6596121.png";
            var avatarImage = new Image
            {
                Source = new BitmapImage(new Uri(avatarPath, UriKind.RelativeOrAbsolute)),
                Width = 30,
                Height = 30,
                Margin = new Thickness(5)
            };

            var nameTextBlock = new TextBlock
            {
                Text = message.FromUserName == _adminUserName ? "Bạn" : message.FromUserName,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 0, 5, 0)
            };

            var textBlock = new TextBlock
            {
                Text = message.Content,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10, 2, 10, 2),
                MaxWidth = 300
            };

            var timestampTextBlock = new TextBlock
            {
                FontStyle = FontStyles.Italic,
                FontSize = 12,
                Margin = new Thickness(5, 0, 5, 0)
            };

            var border = new Border
            {
                Background = message.FromUserName == _adminUserName ? new SolidColorBrush(Colors.LightGray) : new SolidColorBrush(Colors.LightBlue),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(5),
                Margin = new Thickness(5, 2, 5, 2)
            };

            var borderContainer = new StackPanel();
            borderContainer.Children.Add(nameTextBlock);
            borderContainer.Children.Add(textBlock);
            borderContainer.Children.Add(timestampTextBlock);

            border.Child = borderContainer;

            DockPanel.SetDock(avatarImage, Dock.Left);

            messageContainer.Children.Add(avatarImage);
            messageContainer.Children.Add(border);

            MessageListBox.Items.Add(messageContainer);
            MessageListBox.ScrollIntoView(messageContainer);
        }


        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string messageContent = ChatInputBox.Text;
            if (!string.IsNullOrWhiteSpace(messageContent))
            {
                SendButton.IsEnabled = false;

                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    try
                    {
                        await _hubConnection.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Could not connect to chat server: {ex.Message}");
                        SendButton.IsEnabled = true;
                        return;
                    }
                }

                try
                {
                    try
                    {
                        var createdMessage = new MessageViewModel
                        {
                            Content = messageContent,
                            FromUserName = _adminUserName,
                            Room = currentRoomName,
                        };

                        await _hubConnection.InvokeAsync("NewMessage", currentRoomName, createdMessage);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Could not send message to chat room: {ex.Message}");
                    }
                    ChatInputBox.Clear();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred: {ex.Message}");
                }
                finally
                {
                    SendButton.IsEnabled = true;
                }
            }
        }


    }
}