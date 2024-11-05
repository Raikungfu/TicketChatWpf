using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TicketAdminChat;
using TicketAdminChat.Controllers;
using TicketAdminChat.Data;
using TicketApplication.Models;
using UserChatManagement.Controllers;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxIcon = System.Windows.MessageBoxImage;

namespace TicketAdminChat
{
    public partial class WindowLogin : Window
    {
        private readonly ApplicationUserDAO _userDAO;
        private readonly RoomDAO _roomDAO;
        private readonly HubConnection _hubConnection;

        public WindowLogin()
        {
            InitializeComponent();
        }

        public WindowLogin(ApplicationUserDAO userDAO, HubConnection hubConnection, RoomDAO roomDAO)
        {
            InitializeComponent();
            _userDAO = userDAO;
            _hubConnection = hubConnection;
            _roomDAO = roomDAO;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string username = txtId.Text;
            string password = txtPw.Password;
            try
            {
                var result = await _userDAO.LoginAdmin(username, password);
                if (result.SignInResult.Succeeded)
                {
                    var mainWindow = new MainWindow(this, _userDAO, result.Name, result.Email, result.AvatarUrl, _hubConnection, _roomDAO);
                    mainWindow.Show();
                    _hubConnection.InvokeAsync("AdminConnect", result.Email);
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Login Failed! ID/Password wasn't correct!!!", "ERROR", MessageBoxButton.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void txtPwPlaceholder_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPwPlaceholder.Visibility = Visibility.Hidden;
            txtPw.Visibility = Visibility.Visible;
            txtPw.Focus();
        }

        private void txtPw_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPw.Password))
            {
                txtPw.Visibility = Visibility.Hidden;
                txtPwPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void txtPw_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPw.Password))
            {
                txtPwPlaceholder.Visibility = Visibility.Hidden;
            }
            else
            {
                txtPwPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void TxtId_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtId.Text == "Enter your account...")
            {
                txtId.Text = "";
                txtId.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void TxtId_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                txtId.Text = "Enter your account...";
                txtId.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }
    }
}
