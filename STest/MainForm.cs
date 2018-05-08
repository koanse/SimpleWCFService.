using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using STest.SServiceReference;

namespace STest
{
    public partial class MainForm : Form
    {
        List<Thread> lThread = new List<Thread>();
        Random rnd = new Random();
        public MainForm()
        {
            InitializeComponent();
        }
        void button1_Click(object sender, EventArgs e)
        {
            ParamForm pf = new ParamForm();
            if (pf.ShowDialog() != DialogResult.OK)
                return;
            foreach (Thread t in lThread)
                t.Abort();
            lvClient.Items.Clear();
            lThread = new List<Thread>();
            for (int i = 0; i < (int)nudN.Value; i++)
            {
                Client c = pf.client;
                c.id = i;
                string clId = string.Format("{0}{1}", c.host, c.id);
                lThread.Add(new Thread(new ParameterizedThreadStart(clientThreadProc)));
                lvClient.Items.Add(new ListViewItem(new string[] { clId, "", "Инициализация" }));
                lThread[lThread.Count - 1].Start(c);
            }
        }
        void clientThreadProc(object client)
        {
            Client cl = (Client)client;
            AppState state = AppState.End;
            string chId = "", clId = string.Format("{0}{1}", cl.host, cl.id);
            SServiceClient sc = null;
            while (true)
            {
                try
                {
                    switch (state)
                    {
                        case AppState.End:
                            chId = "";
                            Report(cl, chId, "Offline");
                            Thread.Sleep(Rnd(cl.tEndMin, cl.tEndMax));
                            Report(cl, chId, "Offline");
                            state = AppState.Logon;
                            break;
                        case AppState.Logon:
                            Report(cl, chId, "Ввод логина");
                            Thread.Sleep(Rnd(cl.tLogMin, cl.tLogMax));
                            try
                            {
                                sc.Close();
                            }
                            catch { }
                            sc = new SServiceClient();
                            sc.Open();
                            switch (Select(cl.arrPLog))
                            {
                                case 0:
                                    Report(cl, chId, "Аутентификация");
                                    sc.Auth(clId);
                                    state = AppState.Channel;
                                    break;
                                case 1:
                                    Report(cl, chId, "Некорректная аутентификация");
                                    try
                                    {
                                        sc.Auth("wrong id");
                                    }
                                    catch { }
                                    break;
                            }
                            break;
                        case AppState.Channel:
                            Report(cl, chId, "Список каналов");
                            Thread.Sleep(Rnd(cl.tChanMin, cl.tChanMax));
                            switch (Select(cl.arrPChan))
                            {
                                case 0:
                                    string[] arrCh = sc.GetChannels();
                                    int i = Rnd(0, arrCh.Length - 1);
                                    chId = arrCh[i].Split(new char[] { ' ' })[0];
                                    Report(cl, chId, "Подключение к каналу");
                                    sc.Connect(chId);
                                    state = AppState.Data;
                                    break;
                                case 1:
                                    Report(cl, chId, "Список каналов");
                                    sc.GetChannels();
                                    break;
                                case 2:
                                    Report(cl, chId, "Offline");
                                    state = AppState.End;
                                    break;
                            }
                            break;
                        case AppState.Data:
                            Thread.Sleep(Rnd(cl.tDataMin, cl.tDataMax));
                            switch (Select(cl.arrPData))
                            {
                                case 0:
                                    double[] arrX = sc.GetData();
                                    if (arrX == null)
                                    {
                                        Report(cl, chId, "Канал закрыт, отключение от канала");
                                        sc.Disconnect();
                                        state = AppState.Channel;
                                    }
                                    else
                                        Report(cl, chId, string.Format("Получение данных ({0})", arrX.Length));
                                    break;
                                case 1:
                                    Report(cl, chId, "Отключение от канала");
                                    sc.Disconnect();
                                    state = AppState.Channel;
                                    chId = "";
                                    break;
                            }
                            break;
                    }
                }
                catch (ThreadAbortException tae)
                {
                    try
                    {
                        sc.Close();
                    }
                    catch { }
                    return;
                }
                catch (Exception e)
                {
                    Log(string.Format("{0} [{1}] - {2}", clId, chId, e.Message));
                    state = AppState.End;
                }
            }
        }
        int Select(double[] arrP)
        {
            double x = rnd.NextDouble();
            double sum = 0;
            int i;
            for (i = 0; i < arrP.Length - 1; i++)
            {
                sum += arrP[i];
                if (x < sum)
                    return i;
            }
            return i;
        }
        int Rnd(int min, int max)
        {
            return rnd.Next(min, max + 1);
        }
        void Report(Client cl, string chId, string state)
        {
            BeginInvoke(new ReportDelegate(ReportProc), cl, chId, state);
        }
        void ReportProc(Client cl, string chId, string state)
        {
            lvClient.Items[cl.id].SubItems[1].Text = chId;
            string s = lvClient.Items[cl.id].SubItems[2].Text, p;
            switch (s[s.Length - 1])
            {
                case '\\':
                    p = "|";
                    break;
                case '|':
                    p = "/";
                    break;
                case '/':
                    p = "\\";
                    break;
                default:
                    p = "\\";
                    break;
            }
            lvClient.Items[cl.id].SubItems[2].Text = string.Format("{0} {1}", state, p);
        }
        void Log(string msg)
        {
            Invoke(new LogDelegate(LogProc), msg);
        }
        void LogProc(string msg)
        {
            lock (lbErr)
                lbErr.Items.Add(string.Format("{0} : {1}", DateTime.Now, msg));
        }
        enum AppState
        {
            Logon, Channel, Data, End
        }
        delegate void ReportDelegate(Client cl, string chId, string state);
        delegate void LogDelegate(string msg);
        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Thread t in lThread)
                t.Abort();
            foreach (Thread t in lThread)
                while (t.ThreadState != ThreadState.Stopped)
                    Thread.Sleep(100);
        }
    }
    public struct Client
    {
        public int id;
        public string host;
        public double[] arrPLog, arrPChan, arrPData;
        public int tLogMin, tLogMax, tChanMin, tChanMax;
        public int tDataMin, tDataMax, tEndMin, tEndMax;
    }
}
