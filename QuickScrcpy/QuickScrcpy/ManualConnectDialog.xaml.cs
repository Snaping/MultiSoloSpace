
using System.Windows;

namespace QuickScrcpy
{
    public partial class ManualConnectDialog : Window
    {
        public string? IPAddress { get; private set; }
        public int Port { get; private set; }

        public ManualConnectDialog()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var ip = IPTextBox.Text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("请输入IP地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out var port) || port < 1 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号 (1-65535)", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IPAddress = ip;
            Port = port;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
