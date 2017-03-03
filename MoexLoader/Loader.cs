using Ecng.Serialization;
using MoreLinq;
using StockSharp.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.MoexLoader
{
    public enum eProcessState
    {
        Started,
        Stopping,
        Stopped
    }

    public class SourceSettings : IPersistable
    {
        public string Folder { set; get; }
        public bool OnOff { set; get; }
        public DateTime StartDate { set; get; }
        public DateTime EndDate { set; get; }
        public DateTime LoadTo { set; get; }

        public void Load(SettingsStorage storage)
        {
            Folder = storage.GetValue<string>("Folder");
            OnOff = storage.GetValue<bool>("OnOff");
            StartDate = storage.GetValue<DateTime>("StartDate");
            EndDate = storage.GetValue<DateTime>("EndDate");
            LoadTo = storage.GetValue<DateTime>("LoadTo");
        }

        public void Save(SettingsStorage storage)
        {
            storage.SetValue("Folder", Folder);
            storage.SetValue("OnOff", OnOff);
            storage.SetValue("StartDate", StartDate);
            storage.SetValue("EndDate", EndDate);
            storage.SetValue("LoadTo", LoadTo);
        }
    }


    public class Loader : BaseLogReceiver, IPersistable 
    {
        private static Loader _loader;

        private Loader()
        {
            Sources = new Dictionary<string, SourceSettings>
            {
                {"Interest", new SourceSettings() { Folder = "Interest" } },
                {"Indexes", new SourceSettings() { Folder = "Indexes" } }
            }; 
        }

        /// <summary>
        /// Возвращает экземпляр(singleton) класса
        /// </summary>
        public static Loader GetInstance()
        {
            if (_loader == null)
            {
                lock (typeof(Loader))
                {
                    _loader = new Loader();
                }
            }
            return _loader;
        }

        private eProcessState _state = eProcessState.Stopped;
        public eProcessState State
        {
            get { return _state; }
            set { _state = value; }
        }
        public string DataPath { set; get; }

        public IDictionary<string, SourceSettings> Sources { private set; get; }

        public void Start()
        {
            if (State == eProcessState.Stopped)
            {
                State = eProcessState.Started;
                OnStart();
            }
        }

        public void Stop()
        {
            if (State == eProcessState.Started)
            {
                State = eProcessState.Stopping;
                OnStop();
                State = eProcessState.Stopped;
            }
        }

        public override void Save(SettingsStorage storage)
        {
            storage.SetValue("DataPath", DataPath);
            storage.SetValue("Sources", Sources.Values.Select(s => s.Save()).ToArray());
        }

        public override void Load(SettingsStorage storage)
        {
            DataPath = storage.GetValue<string>("DataPath");
            var sourcesStorage = storage.GetValue<SettingsStorage[]>("Sources");
            Sources["Interest"].Load(sourcesStorage[0]);
            Sources["Indexes"].Load(sourcesStorage[0]);
        }

        private void OnStart()
        {
            //TODO
        }

        private void OnStop()
        {
            //TODO
        }




    }


}
