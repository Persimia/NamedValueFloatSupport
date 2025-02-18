using MissionPlanner.Plugin;
using System;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Mavlink;
using System.Text;
using MissionPlanner.Grid;
using MissionPlanner.Utilities;

namespace NamedValueFloatSupport
{
    public class NamedValueFloatSupport : Plugin
    {
        private TextBox textBoxName;
        private TextBox textBoxValue;
        private MyButton sendButton;
        private MyButton clearButton;
        private TextBox logNVFTextBox;
        private TextBox logTextBox;
        private System.Windows.Forms.TabPage tab = new System.Windows.Forms.TabPage();
        private TabControl tabctrl;
        int messagecount;
        System.ComponentModel.Container components;
        private System.Windows.Forms.Timer NVFtabtimer;

        public override string Name => "NamedValueFloatSupport";

        public override string Version => "0.1";

        public override string Author => "Sam Kemp";
        public override bool Loaded()
        {
            components = new System.ComponentModel.Container();

            tab.Text = "NVF";
            tab.Name = "tabNVF";
            Host.MainForm.FlightData.TabListOriginal.Add(tab);

            tabctrl = Host.MainForm.FlightData.tabControlactions;
            tabctrl.TabPages.Insert(2, tab);
            ThemeManager.ApplyThemeTo(tab);
            Settings.Instance["tabcontrolactions"] += ';' + tab.Name;

            // Get the action layout panel from the Flight Data screen
            TableLayoutPanel NVFlayout = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 8,
                ColumnCount = 5
            };
            NVFlayout.RowStyles.Clear();
            for (int i = 0; i < NVFlayout.RowCount; i++)
            {
                NVFlayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f / NVFlayout.RowCount));
            }
            NVFlayout.ColumnStyles.Clear();
            for (int i = 0; i < NVFlayout.ColumnCount; i++)
            {
                NVFlayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / NVFlayout.ColumnCount));
            }


            // Create and add the text boxes and button
            textBoxName = new TextBox { Dock = DockStyle.Fill };
            textBoxValue = new TextBox { Dock = DockStyle.Fill };
            sendButton = new MyButton { Dock = DockStyle.Fill, Text = "Send NVF" };
            clearButton = new MyButton { Dock = DockStyle.Fill, Text = "Clear" };
            logNVFTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Dock = DockStyle.Fill
            };
            logTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Dock = DockStyle.Fill
            };

            sendButton.Click += SendButton_Click;
            clearButton.Click += ClearButton_Click;

            NVFlayout.Controls.Add(new Label { Text = "Name:" }, 0, 0);
            NVFlayout.Controls.Add(textBoxName, 1, 0);
            NVFlayout.SetColumnSpan(textBoxName, 2);

            NVFlayout.Controls.Add(new Label { Text = "Value:" }, 0, 1);
            NVFlayout.Controls.Add(textBoxValue, 1, 1);
            NVFlayout.SetColumnSpan(textBoxValue, 2);

            NVFlayout.Controls.Add(sendButton, 3, 0);
            NVFlayout.SetRowSpan(sendButton, 2);
            NVFlayout.SetColumnSpan(sendButton, 1);

            NVFlayout.Controls.Add(clearButton, 4, 0);
            NVFlayout.SetRowSpan(clearButton, 2);
            NVFlayout.SetColumnSpan(clearButton, 1);

            NVFlayout.Controls.Add(logNVFTextBox, 0, 2);
            NVFlayout.SetColumnSpan(logNVFTextBox, 5);
            NVFlayout.SetRowSpan(logNVFTextBox, 3);

            NVFlayout.Controls.Add(logTextBox, 0, 5);
            NVFlayout.SetColumnSpan(logTextBox, 5);
            NVFlayout.SetRowSpan(logTextBox, 3);

            tab.Controls.Add(NVFlayout);

            // Subscribe to MAVLink message stream
            MainV2.comPort.OnPacketReceived += Mavlink_OnPacketReceived;

            // Timer method
            NVFtabtimer = new System.Windows.Forms.Timer(components);
            NVFtabtimer.Interval = 200;
            NVFtabtimer.Tick += new System.EventHandler(NVFtabtimer_Tick);
            NVFtabtimer.Start();
            return true;
        }
        private void SendButton_Click(object sender, EventArgs e)
        {
            string name = textBoxName.Text;
            if (float.TryParse(textBoxValue.Text, out float value))
            {
                // Create a fixed-length array for name
                byte[] nameBytes = new byte[10]; // Fixed length for MAVLink name field
                byte[] tempBytes = Encoding.ASCII.GetBytes(name);

                // Copy the string bytes into the fixed-length array
                Array.Copy(tempBytes, nameBytes, tempBytes.Length);
                // Create the named_value_float MAVLink message
                var namedValueFloat = new MAVLink.mavlink_named_value_float_t
                {
                    time_boot_ms = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                    name = nameBytes,
                    value = value
                };

                // Send the MAVLink message
                MainV2.comPort.sendPacket(namedValueFloat, 0, 0);
                //MessageBox.Show($"Message sent: {name} = {value}");
            }
            else
            {
                MessageBox.Show("Invalid value. Please enter a valid float value.");
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            logNVFTextBox.Clear();
        }

        private void Mavlink_OnPacketReceived(object sender, MAVLink.MAVLinkMessage message)
        {
            if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.NAMED_VALUE_FLOAT)
            {
                var namedValueFloat = message.ToStructure<MAVLink.mavlink_named_value_float_t>();
                //string name = Encoding.ASCII.GetString(namedValueFloat.name).TrimEnd('\0');
                

                string rawName = Encoding.ASCII.GetString(namedValueFloat.name);
                Console.WriteLine(rawName);
                string name = new string(rawName.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c)).ToArray());

                string logEntry = $"{name} = {namedValueFloat.value}";

                // Update log text box
                Host.MainForm.BeginInvoke((MethodInvoker)delegate
                {
                    logNVFTextBox.AppendText(logEntry + Environment.NewLine);
                    logNVFTextBox.SelectionStart = logNVFTextBox.Text.Length;
                    logNVFTextBox.ScrollToCaret();
                });
            }
        }

        private void NVFtabtimer_Tick(object sender, EventArgs e)
        {
            var messagetime = MainV2.comPort.MAV.cs.messages.LastOrDefault().time;
            if (messagecount != messagetime.toUnixTime())
            {
                try
                {
                    StringBuilder message = new StringBuilder();
                    MainV2.comPort.MAV.cs.messages.ForEach(x =>
                    {
                        message.Append(x.Item1 + " : " + x.Item2 + "\r\n");
                    });
                    logTextBox.Text = message.ToString();
                    logTextBox.SelectionStart = logTextBox.Text.Length; // Move caret to end
                    logTextBox.ScrollToCaret();
                    messagecount = messagetime.toUnixTime();
                }
                catch (Exception ex)
                {
                    //log.Error(ex);
                }
            }
        }

        public override bool Exit()
        {
            return true;
        }

        public override bool Init()
        {
            return true;
        }
    }
}