using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using muWrapper;

namespace SService
{
    public class SService : ISService
    {
        public object client;
        public void Auth(string clId)
        {
            client = Manager.AuthClient(clId);
            OperationContext.Current.Channel.Closed += new EventHandler(Channel_Closed);
        }
        public string[] GetChannels()
        {
            Manager.CheckClient(client);
            return Manager.GetChannels();
        }
        public void Connect(string chId)
        {
            Manager.CheckClient(client);
            Manager.Connect(client, chId);
        }
        public void Disconnect()
        {
            Manager.CheckClient(client);
            Manager.Disconnect(client);
        }
        public double[] GetData()
        {
            Manager.CheckClient(client);
            return Manager.GetClientData(client);
        }
        void Channel_Closed(object sender, EventArgs e)
        {
            Manager.DeleteClient(client);
        }
    }
}
