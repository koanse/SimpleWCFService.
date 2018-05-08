using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace SService
{
    [ServiceContract]
    public interface ISService
    {
        [OperationContract]
        void Auth(string clId);
        [OperationContract]
        string[] GetChannels();
        [OperationContract]
        void Connect(string chId);
        [OperationContract]
        void Disconnect();
        [OperationContract]
        double[] GetData();
    }
}
