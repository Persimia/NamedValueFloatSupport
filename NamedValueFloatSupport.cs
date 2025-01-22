//using MissionPlanner;
using MissionPlanner.Plugin;
//using MissionPlanner.Utilities;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Windows.Forms;
//using System.Diagnostics;
//using MissionPlanner.Controls.PreFlight;
//using MissionPlanner.Controls;
//using System.Linq;

//namespace NmedValueFloatSupport
//{
//    public class NmedValueFloatSupport : Plugin
//    {
//        public override string Name
//        {
//            get { return "NmedValueFloatSupport"; }
//        }

//        public override string Version
//        {
//            get { return "0.1"; }
//        }

//        public override string Author
//        {
//            get { return "Add your name here"; }
//        }

//        //[DebuggerHidden]
//        public override bool Init()
//		//Init called when the plugin dll is loaded
//        {
//            loopratehz = 1;  //Loop runs every second (The value is in Hertz, so 2 means every 500ms, 0.1f means every 10 second...) 

//            return true;	 // If it is false then plugin will not load
//        }

//        public override bool Loaded()
//		//Loaded called after the plugin dll successfully loaded
//        {
//            return true;     //If it is false plugin will not start (loop will not called)
//        }

//        public override bool Loop()
//		//Loop is called in regular intervalls (set by loopratehz)
//        {
//            return true;	//Return value is not used
//        }

//        public override bool Exit()
//		//Exit called when plugin is terminated (usually when Mission Planner is exiting)
//        {
//            return true;	//Return value is not used
//        }
//    }
//}

using System;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Mavlink;
using System.Text;

namespace NamedValueFloatSupport
{
    public class NamedValueFloatSupport : Plugin
    {
        private TextBox textBoxName;
        private TextBox textBoxValue;
        private MyButton sendButton;
        private TextBox logTextBox;

        public override string Name => "NamedValueFloatSupport";

        public override string Version => "1.0";

        public override string Author => "Your Name";

        public override bool Loaded()
        {
            // Get the action layout panel from the Flight Data screen
            TableLayoutPanel actionLayout = Host.MainForm.FlightData.Controls.Find("tableLayoutPanel1", true).FirstOrDefault() as TableLayoutPanel;

            if (actionLayout == null)
            {
                MessageBox.Show("Action layout not found!");
                return false;
            }

            // Create and add the text boxes and button
            textBoxName = new TextBox();
            textBoxName.Dock = DockStyle.Fill;
            textBoxValue = new TextBox();
            textBoxValue.Dock = DockStyle.Fill;
            sendButton = new MyButton();
            sendButton.Dock = DockStyle.Fill;
            logTextBox = new TextBox();

            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.ReadOnly = true;
            logTextBox.Height = 100;
            logTextBox.Dock = DockStyle.Fill;

            sendButton.Text = "Send NamedValueFloat";
            sendButton.Click += SendButton_Click;

            actionLayout.Controls.Add(new Label { Text = "Name:" }, 0, 5);
            actionLayout.Controls.Add(textBoxName, 1, 5);
            actionLayout.SetColumnSpan(textBoxName, 2);

            actionLayout.Controls.Add(new Label { Text = "Value:" }, 0, 6);
            actionLayout.Controls.Add(textBoxValue, 1, 6);
            actionLayout.SetColumnSpan(textBoxValue, 2);

            actionLayout.Controls.Add(sendButton, 3, 5);
            actionLayout.SetRowSpan(sendButton, 2);
            actionLayout.SetColumnSpan(sendButton, 2);

            actionLayout.Controls.Add(logTextBox, 0, 7);
            actionLayout.SetColumnSpan(logTextBox, 5);
            actionLayout.SetRowSpan(logTextBox, 5);

            // Subscribe to MAVLink message stream
            MainV2.comPort.OnPacketReceived += Mavlink_OnPacketReceived;
            return true;
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            string name = textBoxName.Text;
            if (float.TryParse(textBoxValue.Text, out float value))
            {
                // Create the named_value_float MAVLink message
                var namedValueFloat = new MAVLink.mavlink_named_value_float_t
                {
                    time_boot_ms = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds,
                    name = Encoding.ASCII.GetBytes(name),
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

        private void Mavlink_OnPacketReceived(object sender, MAVLink.MAVLinkMessage message)
        {   
            if (message.msgid == (byte)MAVLink.MAVLINK_MSG_ID.NAMED_VALUE_FLOAT)
            {
                MessageBox.Show("Message recieved");
                var namedValueFloat = (MAVLink.mavlink_named_value_float_t)message.ToStructure<MAVLink.mavlink_named_value_float_t>();
                string name = Encoding.ASCII.GetString(namedValueFloat.name).TrimEnd('\0');
                string logEntry = $"{name} = {namedValueFloat.value}";

                // Update log text box
                Host.MainForm.BeginInvoke((MethodInvoker)delegate
                {
                    logTextBox.AppendText(logEntry + Environment.NewLine);
                    logTextBox.SelectionStart = logTextBox.Text.Length;
                    logTextBox.ScrollToCaret();
                });
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