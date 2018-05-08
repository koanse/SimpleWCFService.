using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SClient
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppState state = AppState.Logon;
            string clId = "", chId = "";
            SServiceReference.SServiceClient client = null;
            while (state != AppState.End)
            {
                switch (state)
                {
                    case AppState.Logon:
                        LogonForm lf = new LogonForm(clId);
                        switch (lf.ShowDialog())
                        {
                            case DialogResult.OK:
                                clId = lf.clId;
                                state = AppState.Channel;
                                client = lf.client;
                                break;
                            case DialogResult.Abort:
                                state = AppState.End;
                                break;
                            default:
                                state = AppState.End;
                                break;
                        }
                        break;
                    case AppState.Channel:
                        ChannelForm cf = new ChannelForm(client);
                        switch (cf.ShowDialog())
                        {
                            case DialogResult.OK:
                                state = AppState.Data;
                                chId = cf.chId;
                                break;
                            case DialogResult.Abort:
                                state = AppState.End;
                                break;
                            default:
                                state = AppState.End;
                                break;
                        }
                        break;
                    case AppState.Data:
                        DataForm df = new DataForm(client, chId);
                        switch (df.ShowDialog())
                        {
                            case DialogResult.Retry:
                                state = AppState.Channel;
                                break;
                            case DialogResult.Cancel:
                                state = AppState.Logon;
                                break;
                            case DialogResult.Abort:
                                state = AppState.End;
                                break;
                            default:
                                state = AppState.End;
                                break;
                        }
                        break;
                }
            }
            try
            {
                client.Close();
            }
            catch { }
        }
    }
    enum AppState
    {
        Logon, Channel, Data, End
    }
}
