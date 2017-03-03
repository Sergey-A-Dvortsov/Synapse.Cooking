namespace Synapse.Cooking.FirstStrategy
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
    using General;
    using global::FirstStrategy;
    using StockSharp.Logging;

    class Program
    {
        static Connector _connector;
        static string _portfolioName = "NL0011100043";
        static Portfolio _portfolio;
        static string _securityId = "SBER@QJSIM";
        static Security _security;
        static LogManager _logManager;
        static AutoResetEvent _handler;

        static void Main(string[] args)
        {
            _logManager = new LogManager();
            _logManager.Listeners.Add(new DebugLogListener());
            _connector = new QuikTrader();
            _logManager.Sources.Add(_connector);

            _handler = new AutoResetEvent(false);

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

            _connector.Connect();

            _handler.WaitOne();

            var strategy = new SendOrderStrategy
            {
                Connector = _connector,
                Security = _security,
                Portfolio = _portfolio,
                Volume = 1
            };

            _logManager.Sources.Add(strategy);

            strategy.Start();

            Console.Read();

            strategy.Stop();
            _connector.Disconnect();


            // Ждет, пока последовательно не будут нажаты клаваши Q и Enter,
            // после чего программа завершит работу
            while (Console.ReadLine().ToUpper() != "Q") ;



        }


    }
}
