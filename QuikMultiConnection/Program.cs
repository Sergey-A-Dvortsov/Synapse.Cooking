namespace Synapse.Cooking.QuikMultiConnection
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
    using System.Net;

    class Program
    {
        static Connector _connector;
        static AutoResetEvent _handler;

        static void Main(string[] args)
        {

            if (!args.Any())
                return;

            _connector = new QuikTrader
            {
                LuaFixServerAddress = args[0].To<EndPoint>()    
            };

            _handler = new AutoResetEvent(false);

            var _sendCancelOrders = new List<Order>();

            _connector.Connected += () =>
            {
                Console.WriteLine("Соединение установлено!");
            };

            _connector.NewPortfolios += portfolios =>
            {
                portfolios.ForEach(portfolio =>
                {
                    Console.WriteLine("Получен портфель: {0}", portfolio.Name);
                });
            };

            _connector.Connect();


            Console.Read();

            _connector.Disconnect();

        }

    }
}
