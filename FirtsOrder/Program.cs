namespace Synapse.Cooking.FirstOrder
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

    class Program
    {
        static Connector _connector;
        static string _portfolioName = "97843";
        static Portfolio _portfolio;

        static string _securityId = "SBER@QJSIM";
        static Security _security;
        static bool _isSecuritiesLoaded;   // флаг, указывает (true), что инструменты загужены
        static AutoResetEvent _handler;

        static void Main(string[] args)
        {
            _connector = new QuikTrader();

            _handler = new AutoResetEvent(false);

            var _sendCancelOrders = new List<Order>();

            var _isCancelMode = false;
            var _isReregisterMode = false;

            int k = 0;


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

            _connector.NewPortfolios += portfolios =>
            {
                portfolios.ForEach(portfolio =>
                {
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
                    if (_connector.Portfolios.Count() >= 2)
                        _handler.Set();
                }
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
                    

                    if (k < 0)
                    {
                        if (o.State == OrderStates.Active && !_sendCancelOrders.Contains(o))
                        {
                            Debug.WriteLine(string.Format("Послана команда на снятие заявки. {0}", o));

                            _sendCancelOrders.Add(o);
                            _connector.CancelOrder(o);
                        }
                         
                        
                           
                    }

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

            // Получаем инструмент при помощи метода s#
            _security = _connector.LookupById(_securityId);

            // Получаем портфель из списка портфелей коннектора
            _portfolio = _connector.Portfolios.FirstOrDefault(p => p.Name == _portfolioName);

            // Регистрируем инструмент для получени изменений
            if (!_connector.RegisteredSecurities.Contains(_security))
                _connector.RegisterSecurity(_security);

            var exitCount = 20;

            k = -100;

            while (exitCount > 0 ) 
            {
                if (_security.BestAsk != null)
                {
                    var price = _security.BestAsk.Price + k * _security.PriceStep;

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
    }
}
