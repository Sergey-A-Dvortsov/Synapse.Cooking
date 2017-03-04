using Ecng.Serialization;
using MoreLinq;
using StockSharp.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        Future = 0,
        Call = 1,
        Put = 2
    }


    /// <summary>
    /// Параметры загрузки
    /// </summary>
    public class SourceSettings : IPersistable
    {
        /// <summary>
        /// Папка, где хранятся загруженные данные
        /// </summary>
        public string Folder { set; get; }
        /// <summary>
        /// Источник загрузки включен/выключен
        /// </summary>
        public bool OnOff { set; get; }

        private DateTime _startDate = new DateTime(2015, 1, 1);
        /// <summary>
        /// Начальная дата
        /// </summary>
        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }

        private DateTime _endDate = DateTime.Now;

        /// <summary>
        /// Конечная дата
        /// </summary>
        public DateTime EndDate
        {
            get { return _endDate; }
            set { _endDate = value; }
        }

        /// <summary>
        /// Дата, до которой данные уже загружены
        /// </summary>
        public DateTime LoadTo { set; get; }

        private eContractType _contractTypes = eContractType.Future | eContractType.Call | eContractType.Put;
        public eContractType ContractTypes
        {
            get { return _contractTypes; }
            set { _contractTypes = value; }
        }

        public bool SeparateOptionsFile { set; get; }

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

    public struct InterestInfo
    {
        //  0. moment - дата
        public DateTime Date;

        //  1. isin - код
        public string Code;

        //  3. contract_type - тип инструмента: F-фьючерс, C-call, P-put
        public eContractType ContractType;

        //  4. iz_fiz - если физик, то 1, если юрик, то пусто
        public eClientType ClientType;

        //  5. clients_in_long - количество клиентов в лонге 
        public int ClientsInLong;

        //  6. clients_in_lshort - количество клиентов в шорте 
        public int ClientsInShort;

        //  7. short_position - число коротких позиций
        public int ShortPosition;

        //  8. long_position - число длинных позиций
        public int LongPosition;

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
        private static string OPEN_POSITIONS_URI = "http://moex.com/ru/derivatives/open-positions-csv.aspx";

        private static Loader _loader;

        public event Action SettingsLoaded = delegate { };
        private void OnSettingsLoaded()
        {
            SettingsLoaded?.Invoke();
        }

        private Loader()
        {
            Sources = new Dictionary<string, SourceSettings>
            {
                {"Interest", new SourceSettings() { Folder = "Interest" } },
                {"Indexes", new SourceSettings() { Folder = "Indexes" } }
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

        public IDictionary<string, SourceSettings> Sources { private set; get; }

        public InterestSettings InterestSettings { private set; get; }

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
            base.Save(storage);
            storage.SetValue("Sources", Sources.Values.Select(s => s.Save()).ToArray());
        }

        public override void Load(SettingsStorage storage)
        {
            base.Load(storage);
            DataPath = storage.GetValue<string>("DataPath");
            var sourcesStorage = storage.GetValue<SettingsStorage[]>("Sources");
            Sources["Interest"].Load(sourcesStorage[0]);
            Sources["Indexes"].Load(sourcesStorage[0]);
            InterestSettings = LoadSettings();
            OnSettingsLoaded();
        }

        #endregion


        private void OnStart()
        {
            if (Sources["Interest"].OnOff)
                 Task.Factory.StartNew(InterestLoad, TaskCreationOptions.LongRunning);

            if (Sources["Indexes"].OnOff)
                Task.Factory.StartNew(IndexesLoad, TaskCreationOptions.LongRunning);
        }

        private void InterestLoad()
        {
            //TODO
        }

        private void IndexesLoad()
        {
            //TODO
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
                        settings.Workdays.Add(DateTime.ParseExact(element.Value, element.Attribute("Format").Value, null));
                    }
                }

                var holidaysElement = settElement.Element("holidays");

                if (holidaysElement.Elements().Any())
                {
                    foreach (var element in holidaysElement.Elements())
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            continue;
                        settings.Holidays.Add(DateTime.ParseExact(element.Value, element.Attribute("Format").Value, null));

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
