//Copyright © Сергей Дворцов, 2016,  Все права защищены

namespace Synapse.Cooking.FirstPortfolio
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using MoreLinq;

    using StockSharp.Algo;
    using StockSharp.Quik;
    using StockSharp.BusinessEntities;

    class Program
    {
        static Connector _connector;
        static string _portfolioName = "Ваш портфель";
        static Portfolio _portfolio;
        static void Main(string[] args)
        {
            _connector = new QuikTrader();

            _connector.Connected += () => Console.WriteLine("Соединение установлено!");

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

                if ( ((IList<Portfolio>)_connector.Portfolios).Count >= 2)
                {
                    _portfolio = _connector.Portfolios.FirstOrDefault(p => p.Name == _portfolioName);
                    Console.WriteLine("Все портфели получены.");

                }

            };

            _connector.Connect();

            Console.Read();

            _connector.Disconnect();

            // Ждет, пока последовательно не будут нажаты клаваши Q и Enter,
            // после чего программа завершит работу
            while (Console.ReadLine().ToUpper() != "Q") ;

        }


    }
}
