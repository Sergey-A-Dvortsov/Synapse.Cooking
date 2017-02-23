namespace Synapse.Cooking.RealTimeEmulation
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Security;

    using Ecng.Collections;
    using Ecng.Common;

    using MoreLinq;

    using StockSharp.Algo;
    using StockSharp.Quik;
    using StockSharp.BusinessEntities;
    using StockSharp.Messages;
    using System.Text;
    using StockSharp.Quik.Lua;
    using StockSharp.Algo.Testing;
    using StockSharp.Logging;
    using System.IO;
    using Ecng.Serialization;
    using System.Net;

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


            if (File.Exists(_settingsFile))
            {
                realAdapter.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));
                realAdapter.InnerAdapters.ForEach(a => a.RemoveTransactionalSupport());
            }
            else
            {

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

            _connector.SecuritiesChanged += securities =>
            {
                securities.ForEach(s =>
                {
                    if (s == _security)
                    {
                        if (_security.BestAsk != null)
                        {
                            if (_portfolio != null && _security != null && !isHandlerSet)
                            {
                                isHandlerSet = true;
                                _handler.Set();
                            }
                        }
                    }
                });
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
                //fails.ForEach(f => Debug.WriteLine(string.Format("OrdersRegisterFailed. {0}", f.ToMessage())));
            };

            _connector.OrdersCancelFailed += fails =>
            {
                //fails.ForEach(f => Debug.WriteLine(string.Format("OrdersCancelFailed. {0}", f.ToMessage())));
            };

            _connector.NewMyTrades += trades =>
            {
                trades.ForEach(t => Debug.WriteLine(string.Format("NewMyTrades. {0}", t.ToString())));
            };

            _connector.Connect();

            _handler.WaitOne();

            Console.WriteLine("Инструмент и портфель получены");

            //if (_security.BestAsk != null)
            //{
            //    var price = _security.BestAsk.Price + (10 * _security.PriceStep);

            //    var order = new Order()
            //    {
            //        Security = _security,
            //        Portfolio = _portfolio,
            //        Price = price.Value,
            //        Type = OrderTypes.Limit,
            //        Direction = Sides.Buy,
            //        Volume = 1
            //    };

            //    _connector.RegisterOrder(order);
            //}



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



//<key>SupportedMessages</key>
//    <value>
//      <Type type = "string" > System.String[], mscorlib</Type>
//      <Value type = "System.String[], mscorlib" >
//        < String > MarketData </ String >
//        < String > SecurityLookup </ String >
//        < String > ChangePassword </ String >
//        < String > -1 </ String >
//      </ Value >
//    </ value >




//private void InitConnector()
//{
//    _connector?.Dispose();

//    try
//    {
//        if (File.Exists(_settingsFile))
//            _realAdapter.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));

//        _realAdapter.InnerAdapters.ForEach(a => a.RemoveTransactionalSupport());
//    }
//    catch
//    {
//    }

//    _connector = new RealTimeEmulationTrader<IMessageAdapter>(_realAdapter);
//    _logManager.Sources.Add(_connector);

//    _connector.EmulationAdapter.Emulator.Settings.TimeZone = TimeHelper.Est;
//    _connector.EmulationAdapter.Emulator.Settings.ConvertTime = true;


//protected override void OnClosing(CancelEventArgs e)
//{
//    if (_connector != null)
//        _connector.Dispose();

//    base.OnClosing(e);
//}

//private void SettingsClick(object sender, RoutedEventArgs e)
//{
//    if (_realAdapter.Configure(this))
//        new XmlSerializer<SettingsStorage>().Serialize(_realAdapter.Save(), _settingsFile);

//    InitConnector();
//}


