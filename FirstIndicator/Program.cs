namespace Synapse.Cooking.FirstIndicator
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Ecng.Collections;
    using Ecng.Common;

    using MoreLinq;

    using StockSharp.Algo;
    using StockSharp.Quik;
    using StockSharp.BusinessEntities;
    using StockSharp.Messages;
    using System.Text;

    class Program
    {
        static Connector _connector;
        static string _portfolioName = "NL0011100043";
        static Portfolio _portfolio;
        static string _securityId = "SBER@QJSIM";
        static Security _security;
        static AutoResetEvent _handler;

        static void Main(string[] args)
        {
            _connector = new QuikTrader();

            _handler = new AutoResetEvent(false);

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

                    if (portfolio.Name == _portfolioName)
                        _portfolio = portfolio;

                    _connector.RegisterPortfolio(portfolio);



                });

                if (_connector.Portfolios.Count() >= 3)
                {
                    Console.WriteLine("Все портфели получены.");
                    if (_portfolio != null && _security != null)
                        _handler.Set();
                }

            };

            _connector.NewSecurities += securities =>
            {
                securities.ForEach(s =>
                {
                    if (s.Id == _securityId)
                    {
                        _security = s;
                        _connector.RegisterSecurity(s);
                    }

                    if (_portfolio != null && _security != null)
                        _handler.Set();

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
                fails.ForEach(f => Debug.WriteLine(string.Format("OrdersRegisterFailed. {0}", f.ToMessage())));
            };

            _connector.OrdersCancelFailed += fails =>
            {
                fails.ForEach(f => Debug.WriteLine(string.Format("OrdersCancelFailed. {0}", f.ToMessage())));
            };

            _connector.NewMyTrades += trades =>
            {
                trades.ForEach(t => Debug.WriteLine(string.Format("NewMyTrades. {0}", t.ToString())));
            };

            _connector.Connect();

            _handler.WaitOne();

            var exitCount = 20;

            while (exitCount > 0)
            {
                if (_security.BestAsk != null)
                {
                    var price = _security.BestAsk.Price + (10 * _security.PriceStep);

                    var order = new Order()
                    {
                        Security = _security,
                        Portfolio = _portfolio,
                        Price = price.Value,
                        Type = OrderTypes.Limit,
                        Direction = Sides.Buy,
                        Volume = 1
                    };

                    _connector.RegisterOrder(order);
                    exitCount = 0;
                }
                else
                {

                    Thread.Sleep(200);
                }
                exitCount--;
            }

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
