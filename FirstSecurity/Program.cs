namespace Synapse.Cooking.FirstSecurity
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Collections.Generic;

    using Ecng.Collections;
    using Ecng.Common;

    using MoreLinq;

    using StockSharp.Algo;
    using StockSharp.Quik;
    using StockSharp.BusinessEntities;
    using StockSharp.Messages;

    class Program
    {
        static Connector _connector;
        static string _portfolioName = "95949";
        static Portfolio _portfolio;

        static string _securityId = "SBER@QJSIM";
        static Security _security;
        static bool _isSecuritiesLoaded;   // флаг, указывает (true), что инструменты загужены
        static AutoResetEvent _handler;


        static void Main(string[] args)
        {
            _connector = new QuikTrader();

            _handler = new AutoResetEvent(false);

            ((QuikTrader)_connector).RequestAllSecurities = true;

            _connector.Connected += () =>
            {
                Console.WriteLine("Соединение установлено!");

                if ( ((QuikTrader)_connector).RequestAllSecurities == false)
                {
                    _connector.LookupSecurities(new Security() { Id = _securityId });
                }

                _connector.LookupPortfolios(new Portfolio() { Name = _portfolioName });

            };

            _connector.Disconnected += () =>
            {
                Console.WriteLine("Соединение разорвано!");
                _connector.Dispose();
                Console.WriteLine("Для выхода нажмите Q и Enter.");
            };

            _connector.NewPortfolios += portfolios =>
            {
                portfolios.ForEach(portfolio =>
                {
                    Console.WriteLine("Портфель. Name: {0}, Board {1}, Begin: {2}, Current: {3}",
                                       portfolio.Name,
                                       portfolio.Board,
                                       portfolio.BeginValue,
                                       portfolio.CurrentValue);
                    if (portfolio.Name == _portfolioName)
                        _portfolio = portfolio;
                });

                if (_connector.Portfolios.Count() >= 2)
                {
                    Console.WriteLine("Все портфели получены.");

                    if (_isSecuritiesLoaded)
                        _handler.Set();
                }

            };

            _connector.NewSecurities += securities =>
            {
                securities.ForEach(security => 
                {
                    if (security.Id == _securityId)
                    {
                        _security = security;
                        Console.WriteLine("Инструмент. Id: {0}, Code {1}, Type: {2}",
                                           security.Id, 
                                           security.Code, 
                                           security.Type.ToString()
                                           );
                    }

                });
            };

            _connector.LookupSecuritiesResult += (ex, securities) =>
             {

                 if (((QuikTrader)_connector).RequestAllSecurities == true)
                 {
                     _isSecuritiesLoaded = true;
                 }
                 else
                 {
                     if (_connector.Securities.Any(s => s.Id == _securityId))
                         _isSecuritiesLoaded = true;
                 }

                 if (_isSecuritiesLoaded)
                 {
                     Console.WriteLine("Все инструменты получены. Количество {0}.", securities.Count());
                     Console.WriteLine("Всего инструментов в коллекции коннектора: {0}.", _connector.Securities.Count());
                     if (_connector.Portfolios.Count() >= 2)
                         _handler.Set();
                 }
             };

            
            _connector.Connect();

            _handler.WaitOne();

            // Получаем инструмент при помощи метода s#
             _security = _connector.LookupById(_securityId);

            // Получаем список инструментов по определенному критерию
            var futures = _connector.Lookup(new Security() { Type = SecurityTypes.Future });

            // Получаем инструмент из списка инструментов коннектора
            _security = _connector.Securities.FirstOrDefault(s => s.Id == _securityId );

            // Получаем портфель из списка портфелей коннектора
            _portfolio = _connector.Portfolios.FirstOrDefault(p => p.Name == _portfolioName);

            Console.Read();

            _connector.Disconnect();

            // Ждет, пока последовательно не будут нажаты клаваши Q и Enter,
            // после чего программа завершит работу
            while (Console.ReadLine().ToUpper() != "Q") ;

        }

    }
}

