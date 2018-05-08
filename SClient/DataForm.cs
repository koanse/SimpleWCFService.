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
    public partial class DataForm : Form
    {
        SServiceReference.SServiceClient client;
        Timer tmr;
        int smp = 100, interval = 50, num = 0;
        List<double> lData = new List<double>();
        public DataForm(SServiceReference.SServiceClient client, string chId)
        {
            this.client = client;
            InitializeComponent();
            nudSmp.Value = smp;
            nudInterval.Value = interval;
            zgc.GraphPane.Title.Text = chId;
            zgc.GraphPane.XAxis.Title.Text = "Номер отсчета";
            zgc.GraphPane.YAxis.Title.Text = "Сигнал";
            zgc.GraphPane.AddCurve("", new double[] { }, new double[] { }, Color.Green);
            tmr = new Timer();
            tmr.Interval = interval;
            tmr.Tick += new EventHandler(tmr_Tick);
            tmr.Start();
        }
        void tmr_Tick(object sender, EventArgs e)
        {
            try
            {
                double[] arrY = client.GetData();
                if (arrY == null)
                {
                    tmr.Stop();
                    DialogResult = DialogResult.Retry;
                    MessageBox.Show("Канал отключен");
                    Close();
                }
                foreach (double y in arrY)
                {
                    zgc.GraphPane.CurveList[0].AddPoint(num++, y);
                    if (zgc.GraphPane.CurveList[0].Points.Count > smp)
                        zgc.GraphPane.CurveList[0].RemovePoint(0);
                }
                while (zgc.GraphPane.CurveList[0].Points.Count > smp)
                    zgc.GraphPane.CurveList[0].RemovePoint(0);
                zgc.GraphPane.AxisChange();
                if (checkBox1.Checked)
                    zgc.Refresh();
            }
            catch (Exception ex)
            {
                tmr.Stop();
                if (client.State == System.ServiceModel.CommunicationState.Opened)
                    DialogResult = DialogResult.Retry;
                else
                    DialogResult = DialogResult.Cancel;
                MessageBox.Show(ex.Message);
                Close();
            }

        }
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                interval = (int)nudInterval.Value;
                smp = (int)nudSmp.Value;
                tmr.Interval = interval;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Retry;
            tmr.Stop();
            try
            {
                client.Disconnect();
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.Cancel;
                MessageBox.Show(ex.Message);
                Close();
            }            
            Close();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
            tmr.Stop();
            Close();
        }
    }
}
