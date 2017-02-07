namespace OptionRecoder
{

    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Linq;

    using MoreLinq;
    using StockSharp.BusinessEntities;
    using StockSharp.Quik;
    using StockSharp.Algo.Storages;
    using StockSharp.Messages;
    using Ecng.Collections;


    public class ChainInfo
    {
        public DateTime Time { set; get; }
        public int Month { set; get; }
        public string Type { set; get; }
        public decimal Strike { set; get; }
        public string ValueType { set; get; }
        public decimal Value { set; get; }
        public decimal Volume { set; get; }
        public Sides Side { set; get; }
    }

    public class OptionChain
    {

        public OptionChain()
        {
            Chain = new List<ChainInfo>();
            Strikes = new List<decimal>();
            Months = new List<int>();
            Options = new List<Security>(); 
        }

        public string UnderId { set; get; }
        public List<ChainInfo> Chain { private set; get; }
        public Security UnderSecurity { set; get; }
        public List<Security> Options { private set; get; }
        public List<decimal> Strikes { private set; get; }
        public List<int> Months { private set; get; }

    }



    //Time Month   Type Strike  ValueType Value   Vol Side




    class Program
    {
        static void Main(string[] args)
        {

            var tradeBufferMax = 200;
            var depthBufferMax = 1000;

            var tradeBuffers = new Dictionary<string, List<Trade>>();
            var depthBuffers = new Dictionary<string, List<MarketDepth>>();

            var tradeStorages = new Dictionary<string, IMarketDataStorage<Trade>>();
            var depthStorages = new Dictionary<string, IMarketDataStorage<MarketDepth>>();

            List<string> securityIds = new List<string>();

            StorageFormats storageFormat;
            string storagePath;

            var settings = XElement.Load("settings.xml");
            storagePath = @settings.Element("storage-path").Value;

            foreach (var item in settings.Element("securities").Elements())
            {
                var baseSecId = item.Element("underling").Value;

                var months = new List<int>();

                foreach (var month in item.Element("expiry-months").Elements())
                {
                    months.Add(Int32.Parse(month.Value));
                }

                var strikes = new List<decimal>();

                foreach (var strike in item.Element("strikes").Elements())
                {
                    strikes.Add(Decimal.Parse(strike.Value));
                }

            }

            var connector = new QuikTrader();

            // Ставим false, чтобы квик при старте не загрузил все 30 тыс инструментов 
            connector.RequestAllSecurities = false;

            // Контролируем соединение в течение работы срочного рынка 
            connector.ReConnectionSettings.WorkingTime = ExchangeBoard.Forts.WorkingTime;

            // Обработчик события успешного соединения
            connector.Connected += () =>
            {
                Console.WriteLine("Соединение установлено!");
                if (securityIds.Any())
                {
                    // Запрашиваем инструменты
                    foreach (var id in securityIds)
                    {
                        connector.LookupSecurities(new Security() { Code = id.Split('@')[0], Board = ExchangeBoard.GetBoard(id.Split('@')[1]) });
                    }
                }
                else
                {
                    Console.WriteLine("Нет инструментов для запроса!");
                }
            };

            connector.LookupSecuritiesResult += (ex, securities) =>
            {
            };

            connector.NewTrades += trades =>
            {
                foreach (var trade in trades)
                {
                    var secId = trade.Security.Id;

                    tradeBuffers[secId].Add(trade);

                    if (tradeBuffers[secId].Count >= tradeBufferMax)
                    {
                        var forsave = tradeBuffers[secId].TakeLast(tradeBufferMax);
                        tradeBuffers[secId] = tradeBuffers[secId].Except(forsave).ToList();
                        var task = Task.Factory.StartNew(() => tradeStorages[secId].Save(forsave));
                    }
                }

            };

            connector.MarketDepthsChanged += depths =>
            {
                foreach (var depth in depths)
                {
                    var secId = depth.Security.Id;
                    depthBuffers[secId].Add(depth);

                    if (depthBuffers[secId].Count >= depthBufferMax)
                    {
                        var forsave = depthBuffers[secId].TakeLast(depthBufferMax);
                        depthBuffers[secId] = depthBuffers[secId].Except(forsave).ToList();
                        var task = Task.Factory.StartNew(() => depthStorages[secId].Save(forsave));
                    }

                }
            };

            connector.ValuesChanged += (security, level, stime, ltime) =>
            {


            };

            // Обработчик события разрыва соединения
            connector.Disconnected += () => Console.WriteLine("Соединение разорвано!");

            // Команда соединения
            connector.Connect();

            Console.Read();

            var ttasks = new List<Task>();
            var dtasks = new List<Task>();

            foreach (var security in connector.Securities)
            {
                // Отменяем регистрацию сделок
                if (connector.RegisteredTrades.Contains(security))
                    connector.UnRegisterTrades(security);

                // Отменяем получение стаканов
                if (connector.RegisteredMarketDepths.Contains(security))
                    connector.UnRegisterMarketDepth(security);

                // Записываем остатки данных
                ttasks.Add(Task.Factory.StartNew(() => tradeStorages[security.Id].Save(tradeBuffers[security.Id])));
                dtasks.Add(Task.Factory.StartNew(() => depthStorages[security.Id].Save(depthBuffers[security.Id])));

            }

            Console.WriteLine("Записываем остатки данных!");

            // Чуток ждем
            Task.WaitAll(ttasks.ToArray());
            Task.WaitAll(dtasks.ToArray());

            // Команда разрыва соединения
            connector.Disconnect();





        }

        private static void OnValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> values, DateTimeOffset serverTime, DateTimeOffset localTime)
        {
            try
            {

                var fields = values.ToDictionary();

                if (fields.Keys.Contains(Level1Fields.TheorPrice))
                { }
                else if (fields.Keys.Contains(Level1Fields.Beta))
                { }
                else if (fields.Keys.Contains(Level1Fields.Delta))
                { }
                else if (fields.Keys.Contains(Level1Fields.Gamma))
                { }
                else if (fields.Keys.Contains(Level1Fields.HistoricalVolatility))
                { }
                else if (fields.Keys.Contains(Level1Fields.ImpliedVolatility))
                { }
                else if (fields.Keys.Contains(Level1Fields.OpenInterest))
                { }
                else if (fields.Keys.Contains(Level1Fields.Rho))
                { }
                else if (fields.Keys.Contains(Level1Fields.Theta))
                { }
                else if (fields.Keys.Contains(Level1Fields.Vega))
                { }



            }
            catch (Exception ex)
            {
                //
            }

        }



    }
}
