//Copyright © Сергей Дворцов, 2016,  Все права защищены

namespace Synapse.Cooking.MarketDataRecorder
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
            storageFormat = (StorageFormats)Enum.Parse(typeof(StorageFormats), settings.Element("storage-format").Value.ToUpper());

            foreach (var item in settings.Element("securities").Elements())
            {
                var secId = item.Element("security").Value;
                securityIds.Add(secId);
                depthBuffers.Add(secId, new List<MarketDepth>());
                tradeBuffers.Add(secId, new List<Trade>());
            }

            var storage = new StorageRegistry() { DefaultDrive = new LocalMarketDataDrive { Path = storagePath } };

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
                foreach (var security in securities)
                {
                    if (tradeStorages.ContainsKey(security.Id) || depthStorages.ContainsKey(security.Id))
                        continue;

                    // Инициализируем специализированные хранилища
                    tradeStorages.Add(security.Id, storage.GetTradeStorage(security, null, storageFormat));
                    depthStorages.Add(security.Id, storage.GetMarketDepthStorage(security, null, storageFormat));

                    // Региструем получение сделок
                    if (!connector.RegisteredTrades.Contains(security))
                        connector.RegisterTrades(security);

                    // Региструем получение стаканов
                    if (!connector.RegisteredMarketDepths.Contains(security))
                        connector.RegisterMarketDepth(security);

                }
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
    }
}
