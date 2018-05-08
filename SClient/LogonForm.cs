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
    public partial class LogonForm : Form
    {
        public SServiceReference.SServiceClient client;
        public string clId;
        public LogonForm(string clId)
        {
            InitializeComponent();
            textBox1.Text = clId;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                client = new SClient.SServiceReference.SServiceClient();
                client.Open();
                client.Auth(textBox1.Text);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                try
                {
                    client.Close();
                }
                catch { }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
            Close();
        }
        private void button2_Click_1(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
            Close();
        }
    }
}
