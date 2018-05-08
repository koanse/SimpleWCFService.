using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SClient
{
    public partial class ChannelForm : Form
    {
        SServiceReference.SServiceClient client;
        public string chId;
        public ChannelForm(SServiceReference.SServiceClient client)
        {
            this.client = client;
            InitializeComponent();
            try
            {
                string[] arrS = client.GetChannels();
                listBox1.Items.AddRange(arrS);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string[] arrS = (listBox1.SelectedItem as string).Split(new char[] { ' ' });
                chId = arrS[0];
                client.Connect(chId);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
            Close();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string[] arrS = client.GetChannels();
                listBox1.Items.Clear();
                listBox1.Items.AddRange(arrS);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
