using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace STest
{
    public partial class ParamForm : Form
    {
        public Client client;
        public ParamForm()
        {
            InitializeComponent();
            dgv.Rows.Add("Базовое имя", "client");
            dgv.Rows.Add("Logon: вероятность аутентификации", "0,5");
            dgv.Rows.Add("Logon: вероятность некорректной аутентификации", "0,5");
            dgv.Rows.Add("Logon: минимальное время, мс", "1000");
            dgv.Rows.Add("Logon: максимальное время, мс", "2000");
            dgv.Rows.Add("Channels: вероятность запроса списка каналов", "0,5");
            dgv.Rows.Add("Channels: вероятность подключения к каналу", "0,49");
            dgv.Rows.Add("Channels: вероятность отключения от службы", "0,01");
            dgv.Rows.Add("Channels: минимальное время, мс", "2000");
            dgv.Rows.Add("Channels: максимальное время, мс", "3000");
            dgv.Rows.Add("Data: вероятность получения данных", "0,99");
            dgv.Rows.Add("Data: вероятность отключения от канала", "0,01");
            dgv.Rows.Add("Data: минимальное время, мс", "20");
            dgv.Rows.Add("Data: максимальное время, мс", "60");
            dgv.Rows.Add("End: минимальное время, мс", "2000");
            dgv.Rows.Add("End: максимальное время, мс", "3000");
        }
        void button1_Click(object sender, EventArgs e)
        {
            try
            {
                client = new Client()
                {
                    host = dgv[1, 0].Value.ToString(),
                    arrPLog = new double[] { double.Parse(dgv[1, 1].Value.ToString()),
                    double.Parse(dgv[1, 2].Value.ToString()) },
                    tLogMin = int.Parse(dgv[1, 3].Value.ToString()),
                    tLogMax = int.Parse(dgv[1, 4].Value.ToString()),
                    arrPChan = new double[] { double.Parse(dgv[1, 5].Value.ToString()),
                    double.Parse(dgv[1, 6].Value.ToString()),
                    double.Parse(dgv[1, 7].Value.ToString()) },
                    tChanMin = int.Parse(dgv[1, 8].Value.ToString()),
                    tChanMax = int.Parse(dgv[1, 9].Value.ToString()),
                    arrPData = new double[] { double.Parse(dgv[1, 10].Value.ToString()),
                    double.Parse(dgv[1, 11].Value.ToString()) },
                    tDataMin = int.Parse(dgv[1, 12].Value.ToString()),
                    tDataMax = int.Parse(dgv[1, 13].Value.ToString()),
                    tEndMin = int.Parse(dgv[1, 14].Value.ToString()),
                    tEndMax = int.Parse(dgv[1, 15].Value.ToString())
                };
                DialogResult = DialogResult.OK;
                Close();
            }
            catch
            {
                MessageBox.Show("Ошибка ввода");
            }
        }
        void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
