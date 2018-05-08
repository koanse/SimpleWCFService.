using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.ServiceModel;
using System.IO;
using System.Reflection;
using muWrapper;

namespace SService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SService. Для получения справки введите help");
            try
            {
                Manager.Load("startup.txt");
            }
            catch { }
            while (true)
            {
                string cmd = Console.ReadLine();
                try
                {
                    if (!Manager.DoCmd(cmd))
                        break;
                }
                catch (Exception e)
                {
                    Manager.Log(e.Message);
                }
            }
        }
    }
    public static class Manager
    {
        static List<Channel> lChannel = new List<Channel>();
        static List<Client> lClient = new List<Client>();
        static Semaphore smphChannel;
        static int max, tNum;
        static System.Timers.Timer tmrGen;
        static ServiceHost sh;
        static string log;
        static void Generate()
        {
            smphChannel.WaitOne();
            foreach (Channel ch in lChannel)
            {
                lock (ch.lClient)
                {
                    double[] buf = ch.Generate();
                    foreach (Client cl in ch.lClient)
                        lock (cl)
                        {
                            if (cl.fault)
                                continue;
                            if (cl.buffer == null)
                            {
                                cl.buffer = new double[buf.Length];
                                buf.CopyTo(cl.buffer, 0);
                            }
                            else
                            {
                                double[] newBuf = new double[cl.buffer.Length + buf.Length];
                                cl.buffer.CopyTo(newBuf, 0);
                                buf.CopyTo(newBuf, cl.buffer.Length);
                                cl.buffer = newBuf;
                            }
                            if (cl.buffer.Length > max)
                                DropClient(cl, "Переполнение буфера");
                        }
                }
            }
            smphChannel.Release();
        }
        static void OpenChannel(Channel channel)
        {
            lock (lChannel)
            {
                for (int i = 0; i < tNum; i++)
                    smphChannel.WaitOne();
                int index = lChannel.BinarySearch(channel);
                if (index >= 0)
                {
                    string msg = string.Format("Канал {0} уже существует", channel.id);
                    for (int i = 0; i < tNum; i++)
                        smphChannel.Release();
                    throw new Exception(msg);
                }
                lChannel.Insert(~index, channel);
                for (int i = 0; i < tNum; i++)
                    smphChannel.Release();
                Log(string.Format("Канал {0} открыт", channel.id));
            }
        }
        static void ConfigChannel(Channel channel)
        {
            lock (lChannel)
            {
                for (int i = 0; i < tNum; i++)
                    smphChannel.WaitOne();
                int index = lChannel.BinarySearch(channel);
                if (index < 0)
                {
                    string msg = string.Format("Канал {0} не существует", channel.id);
                    for (int i = 0; i < tNum; i++)
                        smphChannel.Release();
                    throw new Exception(msg);
                }
                lChannel[index].Config(channel);
                for (int i = 0; i < tNum; i++)
                    smphChannel.Release();
                Log(string.Format("Канал {0} сконфигурирован", channel.id));
            }
        }
        static void CloseChannel(Channel channel)
        {
            lock (lChannel)
            {
                for (int i = 0; i < tNum; i++)
                    smphChannel.WaitOne();
                int index = lChannel.BinarySearch(channel);
                if (index < 0)
                {
                    string msg = string.Format("Канал {0} не существует", channel.id);
                    for (int i = 0; i < tNum; i++)
                        smphChannel.Release();
                    throw new Exception(msg);
                }
                foreach (Client cl in lChannel[index].lClient)
                    cl.channel = null;
                lChannel.RemoveAt(index);
                for (int i = 0; i < tNum; i++)
                    smphChannel.Release();
                Log(string.Format("Канал {0} закрыт", channel.id));
            }
        }        
        static void DropClient(string clId, string message)
        {
            lock (lClient)
            {
                Client clNew = new Client(clId);
                int index = lClient.BinarySearch(clNew);
                if (index < 0)
                {
                    string msg = string.Format("Клиент {0} не зарегистрирован", clId);
                    throw new Exception(msg);
                }
                lClient[index].fault = true;
                lClient[index].message = message;
                Log(string.Format("Клиент {0}: {1}", clId, message));
            }
        }
        static void DropClient(object client, string message)
        {
            Client cl = client as Client;
            cl.fault = true;
            cl.message = message;
            Log(string.Format("Клиент {0}: {1}", cl.id, message));
        }
        static void Start(int threadNum, int maxBuf, double interval)
        {
            tNum = threadNum;
            max = maxBuf;
            smphChannel = new Semaphore(tNum, tNum);
            tmrGen = new System.Timers.Timer(interval);
            tmrGen.Elapsed += new ElapsedEventHandler(tmrGen_Elapsed);
            tmrGen.Start();
            sh = new ServiceHost(typeof(SService));
            sh.Open();
            Log("SService запущен");
        }
        static void Stop()
        {
            sh.Close();
            StreamWriter sr = new StreamWriter("log.txt");
            sr.Write(log);
            sr.Flush();
            sr.Close();
            Log("SService остановлен");
        }
        static void tmrGen_Elapsed(object sender, ElapsedEventArgs e)
        {
            Generate();
        }
        public static string[] GetChannels()
        {
            smphChannel.WaitOne();
            string[] arrId = new string[lChannel.Count];
            for (int i = 0; i < arrId.Length; i++)
                arrId[i] = lChannel[i].ToString();
            smphChannel.Release();
            Log("Запрос списка каналов");
            return arrId;
        }
        public static object AuthClient(string clId)
        {
            // вставить проверку допустимости clId
            Client cl = new Client(clId);
            if (cl.id == "wrong id")
                throw new Exception("Неверный идентификатор");
            lock (lClient)
            {                
                int index = lClient.BinarySearch(cl);
                if (index >= 0)
                {
                    string msg = string.Format("Клиент {0} уже зарегистрирован", clId);
                    throw new Exception(msg);
                }
                lClient.Insert(~index, cl);
            }
            Log(string.Format("Клиент {0} зарегистрирован", clId));
            return cl;            
        }
        public static void DeleteClient(object client)
        {
            Client cl = client as Client;
            lock (lClient)
            {
                int index = lClient.BinarySearch(cl);
                if (index < 0)
                {
                    string msg = string.Format("Клиент {0} не зарегистрирован", cl.id);
                    throw new Exception(msg);
                }
                lClient.RemoveAt(index);
            }
            if (cl.channel != null)
                lock (cl.channel.lClient)
                    cl.channel.lClient.Remove(cl);
            cl.channel = null;
            Log(string.Format("Клиент {0} отключен", cl.id));
        }
        public static string[] GetClients()
        {
            lock (lClient)
            {
                string[] arrId = new string[lClient.Count];
                for (int i = 0; i < arrId.Length; i++)
                    arrId[i] = lClient[i].ToString();
                Log("Запрос списка клиентов");
                return arrId;
            }
        }
        public static void CheckClient(object client)
        {
            Client cl = client as Client;
            if (cl.fault)
                throw new FaultException(cl.message);
        }
        public static double[] GetClientData(object client)
        {
            Client cl = client as Client;
            double[] buf;
            lock (cl)
            {
                if (cl.buffer == null && cl.channel != null)
                        return new double[0];
                if (cl.channel == null)
                    buf = null;
                else
                    buf = cl.buffer;
                cl.buffer = null;
            }
            return buf;
        }
        public static void Connect(object client, string chId)
        {
            Client cl = client as Client;
            smphChannel.WaitOne();
            lock (cl)
            {
                if (cl.channel != null)
                {
                    lock (cl.channel.lClient)
                        cl.channel.lClient.Remove(cl);
                    cl.buffer = null;
                }
                Channel ch = new Channel(chId);
                int index = lChannel.BinarySearch(ch);
                if (index < 0)
                {
                    string msg = string.Format("Канал {0} не существует", chId);
                    smphChannel.Release();
                    throw new Exception(msg);
                }
                cl.channel = lChannel[index];
                lock (lChannel[index].lClient)
                    lChannel[index].lClient.Add(cl);
            }
            smphChannel.Release();
            Log(string.Format("Клиент {0} подключен к каналу {1}", cl.id, chId));
        }
        public static void Disconnect(object client)
        {
            smphChannel.WaitOne();
            Client cl = client as Client;
            string chId = cl.channel.id;
            lock (cl.channel.lClient)
                cl.channel.lClient.Remove(cl);
            lock (cl)
            {                
                cl.buffer = null;
                cl.channel = null;
            }
            smphChannel.Release();
            Log(string.Format("Клиент {0} отключен от канала {1}", cl.id, chId));
        }
        public static bool DoCmd(string cmd)
        {
            string[] arrStr = cmd.Split(new char[] { ' ' });
            switch (arrStr[0])
            {
                case "start":
                    Manager.Start(int.Parse(arrStr[1]), int.Parse(arrStr[2]), double.Parse(arrStr[3]));
                    break;
                case "open":
                    switch (arrStr[1])
                    {
                        case "syn":
                            Manager.OpenChannel(new SynChannel(arrStr[2], double.Parse(arrStr[3]),
                                double.Parse(arrStr[4]), new TimeSpan(int.Parse(arrStr[5]))));
                            break;
                        case "squ":
                            Manager.OpenChannel(new SquareChannel(arrStr[2], double.Parse(arrStr[3]),
                                double.Parse(arrStr[4]), new TimeSpan(int.Parse(arrStr[5]))));
                            break;
                        case "tri":
                            Manager.OpenChannel(new TriangleChannel(arrStr[2], double.Parse(arrStr[3]),
                                double.Parse(arrStr[4]), new TimeSpan(int.Parse(arrStr[5]))));
                            break;
                        case "cst":
                            Manager.OpenChannel(new CustomChannel(arrStr[2], arrStr[3],
                                new TimeSpan(int.Parse(arrStr[4]))));
                            break;
                    }
                    break;
                case "config":
                    Manager.ConfigChannel(new SynChannel(arrStr[1], double.Parse(arrStr[2]),
                        double.Parse(arrStr[3]), new TimeSpan(int.Parse(arrStr[4]))));
                    break;
                case "close":
                    Manager.CloseChannel(new Channel(arrStr[1]));
                    break;
                case "channels":
                    string[] arrCh = Manager.GetChannels();
                    foreach (string s in arrCh)
                        Console.WriteLine(s);
                    break;
                case "drop":
                    Manager.DropClient(arrStr[1], arrStr[2]);
                    break;
                case "clients":
                    string[] arrCl = Manager.GetClients();
                    foreach (string s in arrCl)
                        Console.WriteLine(s);
                    break;
                case "load":
                    Load(arrStr[1]);
                    break;
                case "stop":
                    Manager.Stop();
                    return false;
                case "help":
                    StreamReader sr =
                        new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("SService.help.txt"),
                            Encoding.GetEncoding(1251));
                    string help = sr.ReadToEnd();
                    sr.Close();
                    Console.WriteLine(help);
                    break;
                default:
                    Console.WriteLine("Синтаксическая ошибка. Для получения справки введите help");
                    break;
            }
            return true;
        }
        public static void Log(string msg)
        {
            lock ("log")
            {
                msg = DateTime.Now.ToString() + ": " + msg;
                log += msg + "\r\n";
                Console.WriteLine(msg);
            }
        }
        public static void Load(string file)
        {
            StreamReader sr = new StreamReader(file);
            string cmd = sr.ReadToEnd();
            sr.Close();
            Log(string.Format("Файл {0} загружен", file));
            string[] arrStr = cmd.Split(new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (string c in arrStr)
                DoCmd(c);
        }
        class Client : IComparable
        {
            public string id;
            public Channel channel;
            public double[] buffer;
            public bool fault = false;
            public string message;
            public Client(string id)
            {
                this.id = id;
            }
            public int CompareTo(object obj)
            {
                return id.CompareTo((obj as Client).id);
            }
            public override string ToString()
            {
                return string.Format("{0} [fault = {1}, channel = {2}]", id, fault, channel);
            }
        }
        class Channel : IComparable
        {
            public string id;
            public List<Client> lClient;
            public Channel() { }
            public Channel(string id)
            {
                this.id = id;
            }
            public virtual double[] Generate()
            {
                throw new NotImplementedException();
            }
            public virtual void Config(Channel ch)
            {
                throw new NotImplementedException();
            }
            public int CompareTo(object obj)
            {
                return id.CompareTo((obj as Channel).id);
            }
        }
        class SynChannel : Channel
        {
            double freq, amp;
            TimeSpan step, t;
            DateTime dtStart;
            public SynChannel(string id, double freq, double amp, TimeSpan step)
            {
                this.id = id;
                this.freq = freq;
                this.amp = amp;
                this.step = step;
                t = new TimeSpan(0);
                dtStart = DateTime.Now;
                lClient = new List<Client>();
            }
            public override double[] Generate()
            {
                TimeSpan delta = DateTime.Now - dtStart;
                if (delta < t)
                    return new double[0];
                double omega = 2 * Math.PI / 10000000 * freq;
                int count = (int)((delta - t).Ticks / (double)step.Ticks);
                double[] buf = new double[count];
                for (int i = 0; i < count; i++)
                    buf[i] = amp * Math.Sin((t.Ticks + step.Ticks * i) * omega);
                t = new TimeSpan(t.Ticks + step.Ticks * (count + 1));
                return buf;
            }
            public override void Config(Channel ch)
            {
                SynChannel sch = ch as SynChannel;
                freq = sch.freq;
                amp = sch.amp;
                step = sch.step;
            }
            public override string ToString()
            {
                return string.Format("{0} [Syn, freq = {1}, amp = {2}, step = {3}]", id, freq, amp, step);
            }
        }
        class SquareChannel : Channel
        {
            double freq, amp;
            TimeSpan step, t;
            DateTime dtStart;
            public SquareChannel(string id, double freq, double amp, TimeSpan step)
            {
                this.id = id;
                this.freq = freq;
                this.amp = amp;
                this.step = step;
                dtStart = DateTime.Now;
                t = new TimeSpan(0);
                lClient = new List<Client>();
            }
            public override double[] Generate()
            {
                TimeSpan delta = DateTime.Now - dtStart;
                if (delta < t)
                    return new double[0];
                double q = freq / 10000000;
                int count = (int)((delta - t).Ticks / (double)step.Ticks);
                double[] buf = new double[count];
                for (int i = 0; i < count; i++)
                    if ((long)((t.Ticks + step.Ticks * i) * q) % 2 == 0)
                        buf[i] = 0;
                    else
                        buf[i] = amp;
                t = new TimeSpan(t.Ticks + step.Ticks * (count + 1));
                return buf;
            }
            public override void Config(Channel ch)
            {
                SquareChannel sch = ch as SquareChannel;
                freq = sch.freq;
                amp = sch.amp;
                step = sch.step;
            }
            public override string ToString()
            {
                return string.Format("{0} [Square, freq = {1}, amp = {2}, step = {3}]", id, freq, amp, step);
            }
        }
        class TriangleChannel : Channel
        {
            double freq, amp;
            TimeSpan step, t;
            DateTime dtStart;
            public TriangleChannel(string id, double freq, double amp, TimeSpan step)
            {
                this.id = id;
                this.freq = freq;
                this.amp = amp;
                this.step = step;
                dtStart = DateTime.Now;
                t = new TimeSpan(0);
                lClient = new List<Client>();
            }
            public override double[] Generate()
            {
                TimeSpan delta = DateTime.Now - dtStart;
                if (delta < t)
                    return new double[0];
                double q1 = freq / 10000000, q2 = amp * q1;
                int count = (int)((delta - t).Ticks / (double)step.Ticks);
                double[] buf = new double[count];
                for (int i = 0; i < count; i++)
                {
                    double tmp = t.Ticks + step.Ticks * i;
                    buf[i] = (tmp - (long)(tmp * q1) / q1) * q2;
                }
                t = new TimeSpan(t.Ticks + step.Ticks * (count + 1));
                return buf;
            }
            public override void Config(Channel ch)
            {
                TriangleChannel tch = ch as TriangleChannel;
                freq = tch.freq;
                amp = tch.amp;
                step = tch.step;
            }
            public override string ToString()
            {
                return string.Format("{0} [Triangle, freq = {1}, amp = {2}, step = {3}]", id, freq, amp, step);
            }
        }
        class CustomChannel : Channel
        {
            TimeSpan step, t;
            DateTime dtStart;
            Parser prs = new Parser();
            ParserVariable pv = new ParserVariable(0);
            public CustomChannel(string id, string expr, TimeSpan step)
            {
                this.id = id;
                prs.SetExpr(expr);
                prs.DefineVar("t", pv);
                this.step = step;
                dtStart = DateTime.Now;
                t = new TimeSpan(0);
                lClient = new List<Client>();
            }
            public override double[] Generate()
            {
                TimeSpan delta = DateTime.Now - dtStart;
                if (delta < t)
                    return new double[0];
                int count = (int)((delta - t).Ticks / (double)step.Ticks);
                double[] buf = new double[count];
                for (int i = 0; i < count; i++)
                {
                    
                    pv.Value = (t.Ticks + step.Ticks * i) / 10000000.0;
                    buf[i] = prs.Eval();
                }
                t = new TimeSpan(t.Ticks + step.Ticks * (count + 1));
                return buf;
            }
            public override void Config(Channel ch)
            {
                CustomChannel cch = ch as CustomChannel;
                prs.SetExpr(cch.prs.GetExpr());
                step = cch.step;
            }
            public override string ToString()
            {
                return string.Format("{0} [Custom, expr = {1}, step = {2}]", id, prs.GetExpr(), step);
            }
        }
    }
}
