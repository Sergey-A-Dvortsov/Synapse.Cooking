//Copyright © Сергей Дворцов, 2017,  Все права защищены

// Пример использования коннектора RealTimeEmulationTrader
// Данный тип коннектора позволяет работь с реальными рыночными данными
// и эмулировать транзакции, т.е. заявки реально не выставляются в торговую систему

namespace Synapse.Cooking.RealTimeEmulation
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Security;
    using System.Text;

    using MoreLinq;

    using Ecng.Common;
    using Ecng.Serialization;

    using StockSharp.Algo;
    using StockSharp.BusinessEntities;
    using StockSharp.Messages;
    
    using StockSharp.Quik.Lua;
    using StockSharp.Algo.Testing;
    using StockSharp.Logging;

    class Program
    {
        static RealTimeEmulationTrader<IMessageAdapter> _connector;
        static string _portfolioName = "test";
        static Portfolio _portfolio;
        static string _securityId = "SBER@TQBR";
        static Security _security;
        static AutoResetEvent _handler;
        static LogManager _logManager;
        const string _settingsFile = "connection.xml";

        static void Main(string[] args)
        {

            _logManager = new LogManager();
            _logManager.Listeners.Add(new FileLogListener("log.log") { LogDirectory = "Logs" }) ;

            BasketMessageAdapter realAdapter = new BasketMessageAdapter(new MillisecondIncrementalIdGenerator());


            // Можно использовать два варианта создания коннектора
            // 1 вариант. Из файла настроек connection.xml. Чтобы создать файл настроек запустите пример
            // SampleRealTimeEmulation из поставочного комплекта s# и создайте соединение для коннектора
            // Quik. Обратите внимание, что нужно добавлять только MarketDataAdapter, т.к. в нашем случае  
            // используется специальный эмуляционный TransactionAdapter, который создает сам RealTimeEmulationTrader.
            // 2 вариант. Самостоятельно создаем адаптер
            // В этом примере показаны два варианта.

            if (File.Exists(_settingsFile))
            {
                // Создаем адаптер из файла настроек
                realAdapter.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));
                realAdapter.InnerAdapters.ForEach(a => a.RemoveTransactionalSupport());
            }
            else
            {
                // Создаем адаптер "вручную
                realAdapter.AssociatedBoardCode = "ALL";
                realAdapter.LogLevel = LogLevels.Inherit;

                realAdapter.InnerAdapters.Add(new LuaFixMarketDataMessageAdapter(realAdapter.TransactionIdGenerator)
                {
                    Dialect = StockSharp.Fix.FixDialects.Default,
                    SenderCompId = "quik",
                    TargetCompId = "StockSharpMD",
                    Login = "quik",
                    Password = "quik".To<SecureString>(),
                    Address = "localhost:5001".To<EndPoint>(),
                    RequestAllPortfolios = false,
                    RequestAllSecurities = true,
                    IsResetCounter = true,
                    ReadTimeout = TimeSpan.Zero,
                    WriteTimeout = TimeSpan.Zero,
                    HeartbeatInterval = TimeSpan.Zero,
                    SupportedMessages = new MessageTypes[] { MessageTypes.MarketData, MessageTypes.SecurityLookup, MessageTypes.ChangePassword },
                    AssociatedBoardCode = "ALL",
                    LogLevel = LogLevels.Inherit
                });
            }

            // Добавляем адаптер к коннектору
            _connector = new RealTimeEmulationTrader<IMessageAdapter>(realAdapter);
            _connector.EmulationAdapter.Emulator.Settings.TimeZone = TimeHelper.Est;
            _connector.EmulationAdapter.Emulator.Settings.ConvertTime = true;

            _logManager.Sources.Add(_connector);

            _logManager.Sources.Add(_connector);

            _handler = new AutoResetEvent(false);

            bool isHandlerSet = false;

            var _sendCancelOrders = new List<Order>();

            _connector.Connected += () =>
            {
                Console.WriteLine("Соединение установлено!");
            };

            _connector.Disconnected += () =>
            {
                Console.WriteLine("Соединение разорвано!");
                Console.WriteLine("Для выхода нажмите Q и Enter.");
            };

            _connector.NewPortfolios += portfolios =>
            {
                portfolios.ForEach(portfolio =>
                {
                    Console.WriteLine("Получен портфель: {0}", portfolio.Name);

                    _connector.RegisterPortfolio(portfolio);

                    if (portfolio.Name == _portfolioName)
                        _portfolio = portfolio;

                });

            };

            _connector.NewSecurities += securities =>
            {

                securities.ForEach(s =>
                {
                    if (s.Id == _securityId)
                    {
                        _security = s;
                        _connector.RegisterSecurity(s);
                        _handler.Set();
                    }
                });
            };

            _connector.ValuesChanged += (security, values, stime, ltime) =>
            {
                //TODO
            };

            _connector.PortfoliosChanged += portfolios =>
            {

                Debug.WriteLine("Изменение состояния портфелей.");

                var sb = new StringBuilder();

                portfolios.ForEach(p =>
                {
                    sb.AppendFormat("Name: {0}{1}", p.Name, Environment.NewLine);
                    sb.AppendFormat("AveragePrice: {0}{1}", p.AveragePrice, Environment.NewLine);
                    sb.AppendFormat("BeginValue: {0}{1}", p.BeginValue, Environment.NewLine);
                    sb.AppendFormat("BlockedValue: {0}{1}", p.BlockedValue, Environment.NewLine);
                    sb.AppendFormat("Board: {0}{1}", p.Board, Environment.NewLine);
                    sb.AppendFormat("Commission: {0}{1}", p.Commission, Environment.NewLine);
                    sb.AppendFormat("CurrentPrice: {0}{1}", p.CurrentPrice, Environment.NewLine);
                    sb.AppendFormat("CurrentValue: {0}{1}", p.CurrentValue, Environment.NewLine);
                    sb.AppendFormat("Leverage: {0}{1}", p.Leverage, Environment.NewLine);
                    sb.AppendFormat("RealizedPnL: {0}{1}", p.RealizedPnL, Environment.NewLine);
                    sb.AppendFormat("UnrealizedPnL: {0}{1}", p.UnrealizedPnL, Environment.NewLine);
                    sb.AppendFormat("VariationMargin: {0}{1}", p.VariationMargin, Environment.NewLine);

                    Debug.WriteLine(sb.ToString());

                    sb.Clear();

                });

            };

            _connector.NewPositions += positions =>
            {
                Debug.WriteLine("Новые позиции.");
                PrintPositions(positions);
            };

            _connector.PositionsChanged += positions =>
            {
                Debug.WriteLine("Изменение позиций.");
                PrintPositions(positions);
            };

            _connector.NewOrders += orders =>
            {
                orders.ForEach(o => Debug.WriteLine(string.Format("NewOrders. {0}", o)));
            };

            _connector.OrdersChanged += orders =>
            {
                orders.ForEach(o =>
                {
                    Debug.WriteLine(string.Format("OrdersChanged. {0}. IsMatched: {1}, IsCanceled : {2}", o, o.IsMatched().ToString(), o.IsCanceled().ToString()));
                });
            };

            _connector.OrdersRegisterFailed += fails =>
            {
                fails.ForEach(f => Debug.WriteLine(string.Format("OrdersRegisterFailed. {0}", f.Error.Message)));
            };

            _connector.OrdersCancelFailed += fails =>
            {
                fails.ForEach(f => Debug.WriteLine(string.Format("OrdersCancelFailed. {0}", f.Error.Message)));
            };

            _connector.NewMyTrades += trades =>
            {
                trades.ForEach(t => Debug.WriteLine(string.Format("NewMyTrades. {0}", t.ToString())));
            };

            _connector.Connect();

            _handler.WaitOne();

            Console.WriteLine("Инструмент и портфель получены");


            var order = new Order()
                {
                    Security = _security,
                    Portfolio = _portfolio,
                    Type = OrderTypes.Market,
                    Direction = Sides.Buy,
                    Volume = 1
                };

            _connector.RegisterOrder(order);

            Console.WriteLine("ПОСЛАНА ЗАЯВКА");

            Console.Read();

            _connector.Disconnect();

            // Ждет, пока последовательно не будут нажаты клаваши Q и Enter,
            // после чего программа завершит работу
            while (Console.ReadLine().ToUpper() != "Q") ;

        }

        static void PrintPositions(IEnumerable<Position> positions)
        {
            var sb = new StringBuilder();

            positions.ForEach(p =>
            {
                sb.AppendFormat("AveragePrice: {0}{1}", p.AveragePrice, Environment.NewLine);
                sb.AppendFormat("BeginValue: {0}{1}", p.BeginValue, Environment.NewLine);
                sb.AppendFormat("BlockedValue: {0}{1}", p.BlockedValue, Environment.NewLine);
                sb.AppendFormat("ClientCode: {0}{1}", p.ClientCode, Environment.NewLine);
                sb.AppendFormat("Commission: {0}{1}", p.Commission, Environment.NewLine);
                sb.AppendFormat("CurrentPrice: {0}{1}", p.CurrentPrice, Environment.NewLine);
                sb.AppendFormat("CurrentValue: {0}{1}", p.CurrentValue, Environment.NewLine);
                sb.AppendFormat("LimitType: {0}{1}", p.LimitType.ToString(), Environment.NewLine);
                sb.AppendFormat("Portfolio: {0}{1}", p.Portfolio.Name, Environment.NewLine);
                sb.AppendFormat("Security: {0}{1}", p.Security.Id, Environment.NewLine);
                sb.AppendFormat("UnrealizedPnL: {0}{1}", p.UnrealizedPnL, Environment.NewLine);
                sb.AppendFormat("VariationMargin: {0}{1}", p.VariationMargin, Environment.NewLine);

                Debug.WriteLine(sb.ToString());

                sb.Clear();

            });
        }

    }
}

