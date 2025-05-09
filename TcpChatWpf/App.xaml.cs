using System.Windows;

namespace SocketLab
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0 && e.Args[0].ToLower() == "server")
            {
                new ServerWindow().Show();
            }
            else if (e.Args.Length > 0 && e.Args[0].ToLower() == "client")
            {
                string bindIp = e.Args.Length > 1 ? e.Args[1] : null;
                new ClientWindow(bindIp).Show();
            }
            else
            {
                MessageBox.Show("Использование:\nTcpChatWpf.exe server\nили\nTcpChatWpf.exe client [bind_ip]");
                Shutdown();
            }
        }
    }
}
