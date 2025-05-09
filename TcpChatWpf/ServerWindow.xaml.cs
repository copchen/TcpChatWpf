using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SocketLab
{
    public partial class ServerWindow : Window
    {
        private Socket _serverSocket;
        private readonly List<Socket> _clientSockets = new List<Socket>();
        private bool _isRunning;

        public ServerWindow()
        {
            InitializeComponent();
            AppendLog("Сервер готов к запуску");
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PortTextBox.Text, out int port) || port < 1 || port > 65535)
            {
                AppendLog("Неверный порт. Должен быть от 1 до 65535");
                return;
            }

            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                _serverSocket.Listen(10);
                _isRunning = true;

                AppendLog($"Сервер запущен на порту {port}");
                StartButton.IsEnabled = false;
                PortTextBox.IsEnabled = false;

                await Task.Run(AcceptClients);
            }
            catch (SocketException ex)
            {
                AppendLog($"Ошибка: {ex.Message}");
                _serverSocket?.Close();
            }
        }

        private async Task AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _serverSocket.AcceptAsync();
                    _clientSockets.Add(client);
                    AppendLog($"Клиент подключен: {client.RemoteEndPoint}");
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (Exception ex)
                {
                    AppendLog($"Ошибка приема подключения: {ex.Message}");
                }
            }
        }

        private async Task HandleClient(Socket client)
        {
            var buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int received = await client.ReceiveAsync(buffer, SocketFlags.None);
                    if (received == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, received);
                    AppendLog($"Получено: {message}");
                    BroadcastMessage(message, client);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка клиента: {ex.Message}");
            }
            finally
            {
                _clientSockets.Remove(client);
                client.Close();
                AppendLog("Клиент отключен");
            }
        }

        private void BroadcastMessage(string message, Socket sender)
        {
            foreach (var client in _clientSockets)
            {
                if (client != sender && client.Connected)
                {
                    try
                    {
                        client.Send(Encoding.UTF8.GetBytes(message));
                    }
                    catch {}
                }
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}\n");
                LogTextBox.ScrollToEnd();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _isRunning = false;
            _serverSocket?.Close();
            base.OnClosed(e);
        }
    }
}