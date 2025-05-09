using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SocketLab
{
    public partial class ClientWindow : Window
    {
        private Socket _clientSocket;
        private bool _isConnected;
        private readonly string _bindIp;

        public ClientWindow(string bindIp = null)
        {
            InitializeComponent();
            _bindIp = bindIp;
            AppendLog("Клиент готов к подключению");
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
           
            if (!IPAddress.TryParse(IpTextBox.Text, out IPAddress ip))
            {
                AppendLog("Неверный IP-адрес");
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out int port) || port < 1 || port > 65535)
            {
                AppendLog("Неверный порт. Должен быть от 1 до 65535");
                return;
            }

            try
            {
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                if (!string.IsNullOrWhiteSpace(_bindIp))
                {
                    if (!IPAddress.TryParse(_bindIp, out IPAddress localIp))
                    {
                        AppendLog("Неверный локальный IP (bind)");
                        return;
                    }

                    try
                    {
                        _clientSocket.Bind(new IPEndPoint(localIp, 0));
                        AppendLog($"Сокет привязан к {_bindIp}");
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Ошибка привязки сокета: {ex.Message}");
                        return;
                    }
                }

                await _clientSocket.ConnectAsync(new IPEndPoint(ip, port));
                _isConnected = true;

                AppendLog($"Подключено к {ip}:{port}");
                ConnectButton.IsEnabled = false;
                IpTextBox.IsEnabled = false;
                PortTextBox.IsEnabled = false;
                NameTextBox.IsEnabled = false;

                _ = Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024];
            try
            {
                while (_isConnected)
                {
                    int received = await _clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                    if (received == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, received);
                    AppendLog(message);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка получения данных: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private void Disconnect()
        {
            _isConnected = false;
            _clientSocket?.Close();

            Dispatcher.Invoke(() =>
            {
                AppendLog("Отключено от сервера");
                ConnectButton.IsEnabled = true;
                IpTextBox.IsEnabled = true;
                PortTextBox.IsEnabled = true;
                NameTextBox.IsEnabled = true;
            });
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected || string.IsNullOrWhiteSpace(MessageTextBox.Text))
                return;

            try
            {
                string message = $"{NameTextBox.Text}: {MessageTextBox.Text}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _clientSocket.SendAsync(data, SocketFlags.None);
                MessageTextBox.Clear();
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка отправки: {ex.Message}");
                Disconnect();
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ChatTextBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}\n");
                ChatTextBox.ScrollToEnd();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _isConnected = false;
            _clientSocket?.Close();
            base.OnClosed(e);
        }
    }
}
