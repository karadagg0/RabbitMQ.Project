using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Producer.WinForm
{
    public partial class frmMain : Form
    {
        #region Variables

        private bool _isConnectionOpen;
        private bool isConnectionOpen
        {
            get => _isConnectionOpen;

            set
            {
                _isConnectionOpen = value;
                ConnectionStateChanged();
            }
        }

        private IConnection connection;

        private IModel _channel;
        private IModel channel => _channel ?? (_channel = CreateOrGetChannel());

        #endregion

        public frmMain()
        {
            InitializeComponent();

            Init();
        }



        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!isConnectionOpen || connection == null)
            {
                connection = GetConnection();
            }
            else
                connection.Close();

            isConnectionOpen = connection.IsOpen;
        }

        private void btnPublish_Click(object sender, EventArgs e)
        {
            string message = txtMessage.Text;
            int repeatCount = (int)numericRepeatCount.Value;

            PublishMessage(repeatCount, message);
        }

        private void PublishMessage(int repeatCount, string message)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                string formattedMessage = chUseCounter.Checked
                    ? FormatMessageWithCounter(message, i)
                    : message;

                WriteDataToExchange(txtExchangeName.Text, txtRoutingKey.Text, formattedMessage);
            }
        }

        private string FormatMessageWithCounter(string message, int counter)
        {
            return $"[{counter + 1}] - {message}";
        }


        private void btnDeclareQueue_Click(object sender, EventArgs e)
        {
            channel.QueueDeclare(txtDeclareQueueName.Text, exclusive: false);
            AddLog($"Queue created with Name: {txtDeclareQueueName.Text}");
        }

        private void btnDeclareExchange_Click(object sender, EventArgs e)
        {
            channel.ExchangeDeclare(txtDeclareExchangeName.Text, cbDeclareExchangeType.Text);
            AddLog($"Exchange created with Name: {txtDeclareExchangeName.Text}, Type: {cbDeclareExchangeType.Text}");
        }

        private void btnBindQueue_Click(object sender, EventArgs e)
        {
            channel.QueueBind(txtDeclareQueueName.Text, txtDeclareExchangeName.Text, txtDeclareQueueRoutingKey.Text);
        }


        private IModel CreateOrGetChannel()
        {
            return connection.CreateModel();
        }


        private IConnection GetConnection()
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                Uri = new Uri(txtConnectionString.Text, UriKind.RelativeOrAbsolute)
            };

            return factory.CreateConnection();
        }

        private void WriteDataToExchange(string exchangeName, string routingKey, object data)
        {
            var dataArr = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

            channel.BasicPublish(exchangeName, routingKey, null, dataArr);
        }

        #region App Methods

        private void ConnectionStateChanged()
        {
            btnConnect.Text = isConnectionOpen ? "Disconnect" : "Connect";

            UpdateConnectionStatus();

            pnlMain.Enabled = gbQueueExchange.Enabled = isConnectionOpen;
        }

        private void UpdateConnectionStatus()
        {
            string state = isConnectionOpen ? "Connected" : "Not Connected";
            tsLblConnectionStatus.Text = $"Connection State: {state}";
            AddLog($"Connection state is {state}");
        }

        private void AddLog(string logStr)
        {
            string formattedLog = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] - {logStr}";

            txtLog.AppendText($"{formattedLog}{Environment.NewLine}");

            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }


        private void Init()
        {
            #region Fill exchange types to combos

            cbDeclareExchangeType.Items.AddRange(new[] { "direct", "fanout", "headers", "topic" });

            cbDeclareExchangeType.SelectedIndex = 0;

            #endregion
        }


        #endregion
    }
}
