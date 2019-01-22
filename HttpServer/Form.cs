using System;
using System.Configuration;
using System.Threading;
using System.Windows.Forms;

namespace HttpServer
{
    public partial class Form : System.Windows.Forms.Form
    {
        private Thread serverThread;
        private int maxPendingConns;

        public Form() { InitializeComponent(); }
        private void Form_Load(object sender, EventArgs e)
        {
            // App.config settings
            if(!int.TryParse(ConfigurationManager.AppSettings["maxPendingConnections"], out maxPendingConns))
                throw new ConfigurationErrorsException();
        }

        // Start HTTP server
        private void startBtn_Click(object sender, EventArgs e)
        {
            int port;
            string root = rootTextBox.Text;

            if (root == string.Empty)
            {
                MessageBox.Show("Root is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (!int.TryParse(portTextBox.Text, out port))
            {
                MessageBox.Show("Invalid port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            switchFormState();
            serverThread = new Thread(() => { WebServerAsync.StartListening(port, root, maxPendingConns); });
            serverThread.Start();
        }

        // Block unnecessary GUI buttons
        private void switchFormState()
        {
            browseBtn.Enabled = !browseBtn.Enabled;
            stopBtn.Enabled = !stopBtn.Enabled;
            startBtn.Enabled = !startBtn.Enabled;
            portTextBox.ReadOnly = !portTextBox.ReadOnly;
        }

        // Stop HTTP server
        private void stopBtn_Click(object sender, EventArgs e)
        {
            switchFormState();
            serverThread.Abort();
        }

        // Add root dir
        private void browseBtn_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    rootTextBox.Text = fbd.SelectedPath;
            }
        }
    }
}
