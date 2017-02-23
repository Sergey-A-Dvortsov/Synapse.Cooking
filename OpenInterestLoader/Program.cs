//Copyright © Сергей Дворцов, 2017,  Все права защищены

// Программа-загрузчик информации о позициях, открытых физическими и юридическими лицами, 
// по финансовым деривативам на Московской бирже
// ======================================================================================
//  

namespace OpenInterestLoader
{
    using StockSharp.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    public enum eClientType
    {
        Private,
        Firm
    }

    [Flags]
    public enum eContractType
    {
        Future = 0,
        Call = 1,
        Put = 2
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

    public class Settings
    {
        public Settings()
        {
            Workdays = new List<DateTime>();
            Holidays = new List<DateTime>();
            Includes = new List<string>();
            Excludes = new List<string>();
        }

        public DateTime From { set; get; }
        public DateTime To { set; get; }
        public DateTime Last { set; get; }
        public string FolderPath { set; get; }
        public List<DateTime> Workdays { set; get; }
        public List<DateTime> Holidays { set; get; }
        public eContractType ContractTypes { set; get; }
        public List<string> Includes { set; get; }
        public List<string> Excludes { set; get; }

    }


    public class Program : BaseLogReceiver
    {
        // "http://moex.com/ru/derivatives/open-positions-csv.aspx?d=20170221&t=1";

        private static string OPEN_POSITIONS_URI = "http://moex.com/ru/derivatives/open-positions-csv.aspx";

        private static List<InterestInfo> _info;
        private static Settings _settings;
        private static LogManager _logManager;
        private static Program _this;

        public Program()
        {
            _this = this;
        }

        static void Main(string[] args)
        {

            _logManager = new LogManager();
            _logManager.Listeners.Add(new FileLogListener("log.txt"));
            _logManager.Sources.Add(_this);
            _settings = LoadSettings();

            DateTime current = _settings.Last > DateTime.MinValue ? _settings.Last.AddDays(1) : _settings.From;

            int t = 1;

            while (true)
            {

                if (current.Date > _settings.To || current.Date == DateTime.Now)
                    break;

                if (((current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday) && !_settings.Workdays.Contains(current)) || _settings.Holidays.Contains(current))
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
                        var list = GetInterestInfos(response);
                    }
                    else
                    {
                        Console.WriteLine("StatusDescription: {0}", response.StatusDescription);

                    }

                    response.Close();
                    current = current.AddDays(1);
                }
                catch (WebException ex)
                {
                    _this.AddErrorLog(string.Format("{0}.{1}", ex.Message, ex.StackTrace));
                }
                catch (Exception ex)
                {
                    _this.AddErrorLog(string.Format("{0}.{1}", ex.Message, ex.StackTrace));
                    break;
                }

            }

            Console.Read();

        }

        private static List<InterestInfo> GetInterestInfos(HttpWebResponse response)
        {
            var infos = new List<InterestInfo>();

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

                    var arr = line.Split(',');

                    var info = new InterestInfo
                    {
                        Date = DateTime.ParseExact(arr[0].Replace(" ", ""), "yyyy-MM-dd", null),
                        Code = arr[1].Trim(),
                        ContractType = arr[3].Trim() == "F" ? eContractType.Future : arr[3].Trim() == "C" ? eContractType.Call : eContractType.Put,
                        ClientType = string.IsNullOrWhiteSpace(arr[4].Trim()) ? eClientType.Firm : eClientType.Private,
                        ClientsInLong = Int32.Parse(arr[5].Trim()),
                        ClientsInShort = Int32.Parse(arr[6].Trim()),
                        ShortPosition = Int32.Parse(arr[7].Trim()),
                        LongPosition = Int32.Parse(arr[8].Trim())
                    };

                    infos.Add(info);
                }

                return infos;

            }
        }

        private static Settings LoadSettings()
        {
            try
            {

                var settElement = XElement.Load("settings.xml");

                if (settElement == null)
                    throw new ArgumentNullException(nameof(settElement));

                var settings = new Settings();

                var fromElement = settElement.Element("from-date");

                if (string.IsNullOrWhiteSpace(fromElement.Value))
                    throw new ArgumentException(nameof(fromElement));

                _settings.From = DateTime.ParseExact(fromElement.Value, fromElement.Attribute("Format").Value, null);

                var toElement = settElement.Element("to-date");

                if (string.IsNullOrWhiteSpace(toElement.Value))
                    throw new ArgumentException(nameof(toElement));

                _settings.To = DateTime.ParseExact(toElement.Value, toElement.Attribute("Format").Value, null);

                var lastElement = settElement.Element("last-date");

                if (!string.IsNullOrWhiteSpace(lastElement.Value))
                    _settings.Last = DateTime.ParseExact(lastElement.Value, lastElement.Attribute("Format").Value, null);

                var folderElement = settElement.Element("folder-path");

                if (string.IsNullOrWhiteSpace(folderElement.Value))
                    throw new ArgumentException(nameof(folderElement));

                _settings.FolderPath = folderElement.Value;

                var derivativeTypesElement = settElement.Element("derivative-types");

                if (!derivativeTypesElement.HasElements)
                    throw new ArgumentException(nameof(derivativeTypesElement));

                foreach (var element in derivativeTypesElement.Elements())
                {
                    if (string.IsNullOrWhiteSpace(element.Value))
                        continue;
                    _settings.ContractTypes |= element.Value == "F" ? eContractType.Future : element.Value == "C" ? eContractType.Call : eContractType.Put;

                }

                var workdaysElement = settElement.Element("workdays");

                if (workdaysElement.Elements().Any())
                {
                    foreach (var element in workdaysElement.Elements())
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            continue;
                        _settings.Workdays.Add(DateTime.ParseExact(element.Value, element.Attribute("Format").Value, null));

                    }
                }

                var holidaysElement = settElement.Element("holidays");

                if (holidaysElement.Elements().Any())
                {
                    foreach (var element in holidaysElement.Elements())
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            continue;
                        _settings.Holidays.Add(DateTime.ParseExact(element.Value, element.Attribute("Format").Value, null));

                    }
                }


                return settings;

            }
            catch (Exception ex)
            {
                _this.AddErrorLog(string.Format("{0}.{1}", ex.Message, ex.StackTrace));
            }

            return null;

        }

    }

}

//  0. moment - дата
//  1. isin - код
//  2. name - имя инструмента
//  3. contract_type - тип инструмента: F-фьючерс, C-call, P-put    
//  4. iz_fiz - если физик, то 1, если юрик, то пусто
//  5. clients_in_long - количество клиентов в лонге 
//  6. clients_in_lshort - количество клиентов в шорте 
//  7. short_position - число коротких позиций
//  8. long_position - число длинных позиций
//  9. change_prev_week_short_abs,
// 10. change_prev_week_long_abs,
// 11. change_prev_week_short_perc,
// 12. change_prev_week_long_perc,

// moment,isin,name,contract_type,iz_fiz,clients_in_long,clients_in_lshort,short_position,long_position,change_prev_week_short_abs,change_prev_week_long_abs,change_prev_week_short_perc,change_prev_week_long_perc,
// 2017-02-21,MOEX,Опцион на фьючерс на обыкновенные акции ПАО Московская Биржа,C,,1.0000,,,1.0000,,,,,
// 2017-02-21,MOEX,Опцион на фьючерс на обыкновенные акции ПАО Московская Биржа,C,1.0000,,1.0000,1.0000,,,,,,
// 2017-02-21,MOEX,Опцион на фьючерс на обыкновенные акции ПАО Московская Биржа,P,,1.0000,,,1.0000,,,,,
// 2017-02-21,MOEX,Опцион на фьючерс на обыкновенные акции ПАО Московская Биржа,P,1.0000,,1.0000,1.0000,,,,,,
// 2017-02-21,Si,Фьючерсный контракт на курс доллар США - российский рубль, F,,519.00,253.00,1869344,1273943,-107714,26475,-0.05448,0.02122,
// 2017-02-21,Si,Фьючерсный контракт на курс доллар США - российский рубль, F,1.0000,11469,5695.0,201155,796556,95993,-38196,0.91281,-0.04576,
// 2017-02-21,BR,Фьючерсный контракт на нефть BRENT,F,,74.000,107.00,68875,242870,13071,65262,0.23423,0.36745,
// 2017-02-21,BR,Фьючерсный контракт на нефть BRENT,F,1.0000,3460.0,3470.0,265569,91574,39503,-12688,0.17474,-0.12169,
