namespace Synapse.Cooking.FirstMarketData
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
        static string _portfolioName = "97843";
        static Portfolio _portfolio;

        static string _securityId = "SBER@QJSIM";
        static Security _security;
        static AutoResetEvent _handler;


        static void Main(string[] args)
        {
            _connector = new QuikTrader();

            _handler = new AutoResetEvent(false);

            ((QuikTrader)_connector).RequestAllSecurities = false;

            _connector.Connected += () =>
            {
                Console.WriteLine("Соединение установлено!");

                if (((QuikTrader)_connector).RequestAllSecurities == false)
                {
                    _connector.LookupSecurities(new Security() { Id = _securityId });
                }
            };

            _connector.Disconnected += () =>
            {
                Console.WriteLine("Соединение разорвано!");
                _connector.Dispose();
                Console.WriteLine("Для выхода нажмите Q и Enter.");
            };

            _connector.LookupSecuritiesResult += (ex, securities) =>
            {
                if (_connector.Securities.Any(s => s.Id == _securityId))
                    _handler.Set();
            };

            _connector.SecuritiesChanged += securities =>
            {
                securities.ForEach(s => 
                {
                    if (s == _security)
                        Console.WriteLine(s.ToString());
                });
            };

            _connector.NewTrades += trades =>
            {
                trades.ForEach(t =>
                {
                    if (t.Security == _security)
                        Console.WriteLine(t.ToString());
                });
            };

            _connector.NewMarketDepths += depths =>
            {
                depths.ForEach(d =>
                {
                    if (d.Security == _security)
                        Console.WriteLine(d.ToString());
                });
            };

            _connector.MarketDepthsChanged += depths =>
            {
                depths.ForEach(d =>
                {
                    if (d.Security == _security)
                        Console.WriteLine(d.ToString());
                });
            };

            _connector.Connect();

            _handler.WaitOne();

            _security = _connector.LookupById(_securityId);

            // Проверяем зарегистрирован ли инструмент на получение определенного
            // типа рыночных данных и выполняем регистрацию
             
            if (!_connector.RegisteredSecurities.Contains(_security))
                _connector.RegisterSecurity(_security);

            if (!_connector.RegisteredTrades.Contains(_security))
                _connector.RegisterTrades(_security);

            if (!_connector.RegisteredMarketDepths.Contains(_security))
                _connector.RegisterMarketDepth(_security);

            Console.Read();

            // Проверяем зарегистрирован ли инструмент на получение определенного
            // типа рыночных данных и отменяем регистрацию

            if (_connector.RegisteredSecurities.Contains(_security))
                _connector.UnRegisterSecurity(_security);

            if (_connector.RegisteredTrades.Contains(_security))
                _connector.UnRegisterTrades(_security);

            if (_connector.RegisteredMarketDepths.Contains(_security))
                _connector.UnRegisterMarketDepth(_security);

            _connector.Disconnect();

            //_connector.TradesKeepCount

            // Ждет, пока последовательно не будут нажаты клаваши Q и Enter,
            // после чего программа завершит работу
            while (Console.ReadLine().ToUpper() != "Q") ;

        }
    }

}