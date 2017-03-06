using System.Globalization;
using Ecng.Serialization;
using MoreLinq;
using StockSharp.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.MoexLoader
{
    public class InterestInfoComparer : Comparer<InterestInfo>
    {
        public override int Compare(InterestInfo x, InterestInfo y)
        {
            return string.Compare(x.Code, y.Code);
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
        public double ClientsInLong;

        //  6. clients_in_lshort - количество клиентов в шорте 
        public double ClientsInShort;

        //  7. short_position - число коротких позиций
        public double ShortPosition;

        //  8. long_position - число длинных позиций
        public double LongPosition;

        public override string ToString()
        {
            return string.Format("{0};{1};{2};{3}", ClientsInLong, ClientsInShort, LongPosition, ShortPosition);
        }

    }

    public abstract class BaseSourceLoader : BaseLogReceiver
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

        public decimal ProgressBarValue { private set; get; }

        private eProcessState _state = eProcessState.Stopped;
        public eProcessState State
        {
            get { return _state; }
            set { _state = value; }
        }

        public abstract void LoadingProcess();

        public override void Load(SettingsStorage storage)
        {
            Folder = storage.GetValue<string>("Folder");
            OnOff = storage.GetValue<bool>("OnOff");
            StartDate = storage.GetValue<DateTime>("StartDate");
            EndDate = storage.GetValue<DateTime>("EndDate");
            LoadTo = storage.GetValue<DateTime>("LoadTo");
        }

        public override void Save(SettingsStorage storage)
        {
            storage.SetValue("Folder", Folder);
            storage.SetValue("OnOff", OnOff);
            storage.SetValue("StartDate", StartDate);
            storage.SetValue("EndDate", EndDate);
            storage.SetValue("LoadTo", LoadTo);
        }
    }

    public class InterestSourceLoader : BaseSourceLoader
    {
        const string OPEN_POSITIONS_URI = "http://moex.com/ru/derivatives/open-positions-csv.aspx";

        private eContractType _contractTypes = eContractType.Future | eContractType.Call | eContractType.Put;
        public eContractType ContractTypes
        {
            get { return _contractTypes; }
            set { _contractTypes = value; }
        }

        public bool SeparateOptionsFile { set; get; }

        public InterestSettings Settings { set; get; }

        public override void LoadingProcess()
        {
            State = eProcessState.Started;

            DateTime current = LoadTo > DateTime.MinValue ? LoadTo.AddDays(1) : StartDate;

            int t = 1;

            while (true)
            {

                if (current.Date > EndDate || current.Date == DateTime.Now)
                    break;

                if (((current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday) && !Settings.Workdays.Contains(current)) || Settings.Holidays.Contains(current))
                {
                    current = current.AddDays(1);
                    continue;
                }

                var uri = string.Format("{0}?d={1}&t={2}", OPEN_POSITIONS_URI, current.ToString("yyyyMMdd"), t);

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        List<InterestInfo> list = GetInterestInfos(response);
                        if (list.Any())
                        {
                            list.Sort(new InterestInfoComparer());
                            SaveToFile(list);
                        }
                    }

                    response.Close();
                    current = current.AddDays(1);
                }
                catch (WebException ex)
                {
                    this.AddErrorLog(string.Format("{0}.{1}", ex.Message, ex.StackTrace));
                }
                catch (Exception ex)
                {
                    this.AddErrorLog(string.Format("{0}.{1}", ex.Message, ex.StackTrace));
                    break;
                }

            }

            State = eProcessState.Stopped;
        }

        private List<InterestInfo> GetInterestInfos(HttpWebResponse response)
        {
            var infos = new List<InterestInfo>();

            string[] arr = null;

            try
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    string line = "";
                    bool first = true;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (first)
                        {
                            first = false;
                            continue;
                        }

                        arr = line.Split(',');

                        //var date = DateTime.ParseExact(arr[0].Replace(" ", ""), "yyyy-MM-dd", null);
                        //var contractType = arr[3].Trim() == "F" ? eContractType.Future : arr[3].Trim() == "C" ? eContractType.Call : eContractType.Put;
                        //var clientType = string.IsNullOrWhiteSpace(arr[4].Trim()) ? eClientType.Firm : eClientType.Private;
                        //var clientsInLong = double.Parse(arr[5].Trim(), CultureInfo.InvariantCulture);
                        //var clientsInShort = double.Parse(arr[6].Trim(), CultureInfo.InvariantCulture);
                        //var shortPosition = double.Parse(arr[7].Trim(), CultureInfo.InvariantCulture);
                        //var longPosition = double.Parse(arr[8].Trim(), CultureInfo.InvariantCulture);

                        var info = new InterestInfo
                        {
                            Date = DateTime.ParseExact(arr[0].Replace(" ", ""), "yyyy-MM-dd", null),
                            Code = arr[1].Trim(),
                            ContractType = arr[3].Trim() == "F" ? eContractType.Future : arr[3].Trim() == "C" ? eContractType.Call : eContractType.Put,
                            ClientType = string.IsNullOrWhiteSpace(arr[4].Trim()) ? eClientType.Firm : eClientType.Private,
                            ClientsInLong = double.Parse(arr[5].Trim(), CultureInfo.InvariantCulture),
                            ClientsInShort = double.Parse(arr[6].Trim(), CultureInfo.InvariantCulture),
                            ShortPosition = double.Parse(arr[7].Trim(), CultureInfo.InvariantCulture),
                            LongPosition = double.Parse(arr[8].Trim(), CultureInfo.InvariantCulture)
                        };

                        infos.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                var ar = arr;
                this.AddErrorLog(string.Format("{0}.{1}", ex.Message, ex.StackTrace));
            }

            return infos;
        }

        private void SaveToFile(List<InterestInfo> list)
        {
            List<InterestInfo> temp = list;

            try
            {
                if (Settings.Includes.Any())
                {
                    temp = list.Where(info => Settings.Includes.Contains(info.Code)).ToList();
                }
                else if (Settings.Excludes.Any())
                {
                    var excludes = list.Where(info => Settings.Excludes.Contains(info.Code));
                    temp = list.Except(excludes).ToList();
                }

                var values = new Dictionary<eContractType, Dictionary<eClientType, string>>
                {
                    {eContractType.Future, new Dictionary<eClientType, string> {{eClientType.Private, ""}, {eClientType.Firm, ""}}},
                    {eContractType.Call, new Dictionary<eClientType, string> {{eClientType.Private, ""}, {eClientType.Firm, ""}}},
                    {eContractType.Put, new Dictionary<eClientType, string> {{eClientType.Private, ""}, {eClientType.Firm, ""}}}
                };

                temp = temp.Where(i => (i.ContractType & ContractTypes) == i.ContractType).ToList();

                string code = "";
                DateTime date = DateTime.MinValue;

                foreach (var info in temp)
                {

                    if (!string.IsNullOrWhiteSpace(code) && code != info.Code)
                    {
                        ContractSaveToFile(code, date, values);
                        values.ClearValues();
                    }

                    code = info.Code;
                    date = info.Date;
                    values[info.ContractType][info.ClientType] = info.ToString();
                }

            }
            catch (Exception ex)
            {
                this.AddErrorLog(string.Format("{0}.{1}", ex.Message, ex.StackTrace));
            }

        }


        private void ContractSaveToFile(string code, DateTime date, Dictionary<eContractType, Dictionary<eClientType, string>> values)
        {
            //TODO
        }

        public override void Load(SettingsStorage storage)
        {
            base.Load(storage);
            ContractTypes = storage.GetValue<eContractType>("ContractTypes");
            SeparateOptionsFile = storage.GetValue<bool>("SeparateOptionsFile");
        }

        public override void Save(SettingsStorage storage)
        {
            storage.SetValue("ContractTypes", ContractTypes);
            storage.SetValue("SeparateOptionsFile", SeparateOptionsFile);
            base.Save(storage);
        }

    }

    public class IndexesSourceLoader : BaseSourceLoader
    {
        public override void LoadingProcess()
        {
            State = eProcessState.Started;
            //todo
            State = eProcessState.Stopped;
        }

        public override void Load(SettingsStorage storage)
        {
            base.Load(storage);
        }

        public override void Save(SettingsStorage storage)
        {
            base.Save(storage);
        }

    }

}
