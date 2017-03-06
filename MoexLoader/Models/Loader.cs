using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Globalization;

using Ecng.Serialization;
using MoreLinq;
using StockSharp.Logging;

namespace Synapse.MoexLoader
{
    /// <summary>
    /// Сотоянние загрузки
    /// </summary>
    public enum eProcessState
    {
        /// <summary>
        /// Загрузка идет
        /// </summary>
        Started,
        /// <summary>
        /// Загрузка останавливается
        /// </summary>
        Stopping,
        /// <summary>
        /// Загрузка не идет
        /// </summary>
        Stopped
    }

    /// <summary>
    /// Физическое/юридическое лцо
    /// </summary>
    public enum eClientType
    {
        Private,
        Firm
    }

    /// <summary>
    /// Тип срочного инструмента
    /// </summary>
    [Flags]
    public enum eContractType
    {
        Future = 1,
        Call = 2,
        Put = 4
    }

    public class InterestSettings
    {
        public InterestSettings()
        {
            Workdays = new List<DateTime>();
            Holidays = new List<DateTime>();
            Includes = new List<string>();
            Excludes = new List<string>();
        }
        public List<DateTime> Workdays { set; get; }
        public List<DateTime> Holidays { set; get; }
        public List<string> Includes { set; get; }
        public List<string> Excludes { set; get; }
    }

    public class Loader : BaseLogReceiver, IPersistable 
    {

        private static Loader _loader;

        public event Action SettingsLoaded = delegate { };
        private void OnSettingsLoaded()
        {
            SettingsLoaded?.Invoke();
        }

        private Loader()
        {
            Sources = new List<BaseSourceLoader>
            {
                {new InterestSourceLoader() { Folder = "Interest" } },
                {new IndexesSourceLoader() { Folder = "Indexes" } }
            }; 
        }

        #region properties

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
        public IList<BaseSourceLoader> Sources { private set; get; }
      

        #endregion

        #region methods
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
            storage.SetValue("Sources", Sources.Select(s => s.Save()).ToArray());
            base.Save(storage);
        }

        public override void Load(SettingsStorage storage)
        {
            if (storage != null)
            {
                base.Load(storage);
                DataPath = storage.GetValue<string>("DataPath");
                var sourcesStorage = storage.GetValue<SettingsStorage[]>("Sources");
                for (var i = 0; i < sourcesStorage.Count(); i++)
                {
                    Sources[i].Load(sourcesStorage[i]);
                }
            }
            ((InterestSourceLoader)Sources[0]).Settings = LoadSettings();
            OnSettingsLoaded();
        }

        #endregion


        private void OnStart()
        {
            Sources.ForEach(s => 
            {
                if (s.OnOff)
                    Task.Factory.StartNew(s.LoadingProcess, TaskCreationOptions.LongRunning);
            });
        }

        private void OnStop()
        {
            //TODO
        }

        private InterestSettings LoadSettings()
        {
            try
            {

                var settElement = XElement.Load("interest_settings.xml");

                if (settElement == null)
                    throw new ArgumentNullException(nameof(settElement));

                var settings = new InterestSettings();

                var workdaysElement = settElement.Element("workdays");

                if (workdaysElement.Elements().Any())
                {
                    foreach (var element in workdaysElement.Elements())
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            continue;
                        settings.Workdays.Add(DateTime.ParseExact(element.Value, element.Attribute("Format").Value, CultureInfo.InvariantCulture).Date);
                    }
                }

                var holidaysElement = settElement.Element("holidays");

                if (holidaysElement.Elements().Any())
                {
                    foreach (var element in holidaysElement.Elements())
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            continue;
                        settings.Holidays.Add(DateTime.ParseExact(element.Value, element.Attribute("Format").Value, CultureInfo.InvariantCulture).Date);

                    }
                }

                var includeElement = settElement.Element("include-codes");

                if (includeElement.Elements().Any())
                {
                    foreach (var element in includeElement.Elements())
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            continue;
                        settings.Includes.Add(element.Value);
                    }
                }

                var excludeElement = settElement.Element("exclude-codes");

                if (excludeElement.Elements().Any())
                {
                    foreach (var element in excludeElement.Elements())
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            continue;
                        settings.Excludes.Add(element.Value);
                    }
                }

                return settings;

            }
            catch (Exception ex)
            {
                this.AddErrorLog(string.Format("{0}.{1}", ex.Message, ex.StackTrace));
            }

            return null;

        }


    }


}
