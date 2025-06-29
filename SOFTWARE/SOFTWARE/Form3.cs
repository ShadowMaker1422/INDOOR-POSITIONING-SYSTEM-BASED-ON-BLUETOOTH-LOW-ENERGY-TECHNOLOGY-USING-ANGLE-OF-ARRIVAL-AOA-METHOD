using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SOFTWARE
{
    public partial class Form3 : Form
    {
        MQTT Mqttclient = new MQTT();
        private MqttClient mqttClient;
        public Form3()
        {
            Mqttclient.brokerAddress = "192.168.0.230";
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Task.Run(() => { if (Mqttclient.IsConnected) { Mqttclient.Publish("App1/Message", Encoding.UTF8.GetBytes(textBox1.Text)); } });
            mqttClient.Publish("Anchor1/Message",
           Encoding.UTF8.GetBytes("1,1422003,34"),
           MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
           false);

            mqttClient.Publish("Anchor2/Message",
            Encoding.UTF8.GetBytes("1,1422003,34"),
            MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
            false);
        }


        private void Form3_Load(object sender, EventArgs e)
        {
            /* Task.Run(() => {
                 // Thay thế bằng địa chỉ IP thực của máy broker
                 string brokerAddress = "192.168.0.230";
                 Mqttclient = new MqttClient(brokerAddress);
                 Mqttclient.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
                 Mqttclient.Subscribe(new string[] { "App2/Message" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                 Mqttclient.Connect("App2");
             });*/
            Task.Run(() => {
                Mqttclient.Connect("Anchor2");
                Mqttclient.Received("Anchor2");
            });
            Task.Run(() => {
                Mqttclient.Connect("Anchor1");
                Mqttclient.Received("Anchor1");
            });
        }
       
        public class MQTT()
        {

            MqttClient Mqttclient;
            public  string brokerAddress { get; set; }
            public void Connect(string Origin)
            {

                // Thay thế bằng địa chỉ IP thực của máy broker
                Mqttclient = new MqttClient(brokerAddress);
                Mqttclient.Connect(Origin);
            }
            public void Received(string Origin)
            {
                Mqttclient.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
                Mqttclient.Subscribe(new string[] { $"{Origin}/Message" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            }
            private void MqttClient_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
            {
                var message = Encoding.UTF8.GetString(e.Message);
            }
            public void Send(string String, string Destination)
            {
                if (Mqttclient.IsConnected)
                { 
                    Mqttclient.Publish($"{Destination}/Message", Encoding.UTF8.GetBytes(String));
                }
            }
        }
    }
}
