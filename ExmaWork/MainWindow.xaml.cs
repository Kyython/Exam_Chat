using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;

namespace ExmaWork
{
    public partial class MainWindow : Window
    {
        private UdpClient _client;
        private IPAddress _ipAddress;
        private int _localPort;
        private int _remotePort;
        private string _hostAddress;
        private IPEndPoint _ipEndPoint;
        private bool _isConnect;

        public MainWindow()
        {
            InitializeComponent();
            SignOutItemEnabled();

            _isConnect = false;
            _localPort = 12345;
            _remotePort = 12345;
            _hostAddress = "235.5.5.1";

            _ipAddress = IPAddress.Parse(_hostAddress);
            _ipEndPoint = new IPEndPoint(_ipAddress, _remotePort);
            _client = new UdpClient();
        }

        private void SignInItemEnabled()
        {
            signOutButton.IsEnabled = true;
            sendButton.IsEnabled = true;
            chatTextBox.IsEnabled = true;
            userTextBox.IsEnabled = true;

            signInButton.IsEnabled = false;
            userNameTextBox.IsEnabled = false;
        }

        private void SignOutItemEnabled()
        {
            signOutButton.IsEnabled = false;
            sendButton.IsEnabled = false;
            chatTextBox.IsEnabled = false;
            userTextBox.IsEnabled = false;

            signInButton.IsEnabled = true;
            userNameTextBox.IsEnabled = true;
        }

        private void SignInButtonClick(object sender, RoutedEventArgs e)
        {
            if (userNameTextBox.Text == string.Empty)
            {
                MessageBox.Show("Please, enter your name!!!");
                return;
            }
            SignInItemEnabled();

            try
            {
                const int TIME_TO_LIVE = 42;

                _client = new UdpClient(_localPort);
                _client.JoinMulticastGroup(_ipAddress, TIME_TO_LIVE);

                Task task = new Task(SendMessage);
                task.Start();

                byte[] buffer = Encoding.UTF8.GetBytes($"{userNameTextBox.Text} has joined the chat...");

                _client.Send(buffer, buffer.Length, _ipEndPoint);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error: {exception.Message}");
                return;
            }
        }

        private void SendMessage()
        {
            _isConnect = true;
            try
            {
                while (_isConnect)
                {
                    IPEndPoint ipEndPoint = null;
                    byte[] buffer = _client.Receive(ref ipEndPoint);
                    string message = Encoding.UTF8.GetString(buffer);

                    Dispatcher.BeginInvoke(new ThreadStart(() =>
                    {
                        string messageSendTime = DateTime.Now.ToShortTimeString();

                        chatTextBox.Text = $"{messageSendTime} {message}\n{chatTextBox.Text}\n";
                    }));
                }
            }
            catch (ObjectDisposedException)
            {
                if (!_isConnect)
                    return;
            }
            catch (SocketException)
            {
                return;
            }
        }

        private void SignOutButtonClick(object sender, RoutedEventArgs e)
        {
            SignOutItemEnabled();

            byte[] buffer = Encoding.UTF8.GetBytes($"{userNameTextBox.Text} leaves the chat");
            _client.Send(buffer, buffer.Length, _ipEndPoint);
            _client.DropMulticastGroup(_ipAddress);
            _isConnect = false;
            _client.Close();

            chatTextBox.Clear();
        }

        private void SendButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string message = $"{userNameTextBox.Text} - {userTextBox.Text}";
                byte[] buffer = Encoding.UTF8.GetBytes(message);

                _client.Send(buffer, buffer.Length, _ipEndPoint);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error: {exception.Message}");
                return;
            }
        }

        private void CheckExit()
        {
            string messageBoxText = $"{userNameTextBox.Text}, do you really want to leave the chat?";
            string caption = "Warning";
            MessageBoxButton buttonMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imageMessageBox = MessageBoxImage.Question;
            MessageBoxResult resultMessageBox = MessageBox.Show(messageBoxText, caption, buttonMessageBox, imageMessageBox);

            switch (resultMessageBox)
            {
                case MessageBoxResult.Yes:
                    byte[] buffer = Encoding.UTF8.GetBytes($"{userNameTextBox.Text} leaves the chat");
                    _client.Send(buffer, buffer.Length, _ipEndPoint);
                    _client.DropMulticastGroup(_ipAddress);
                    _client.Close();
                    chatTextBox.Clear();
                    Close();
                    break;

                case MessageBoxResult.No:
                    break;
            }
        }
    }
}
