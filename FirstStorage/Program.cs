namespace Synapse.Cooking.FirstStorage
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
    using StockSharp.Algo.Storages;

    class Program
    {

        static void Main(string[] args)
        {
            var security = new Security() { Id = "RIH7@FORTS", Board = ExchangeBoard.Forts };
            StorageRegistry storage = new StorageRegistry();

            string path = @"../../../Data/Quik";

            //Для работы будем использовать готовое локальное файловое хранилище, в которое данные были предварительно записаны при помощи Гидры.
            //Создаем экземпляр LocalMarketDataDrive.
            LocalMarketDataDrive drive = new LocalMarketDataDrive() { Path = path };

            //Передаем в StorageRegistry ссылку на локальное файловое хранилище
            storage.DefaultDrive = drive;

            DateTime from = new DateTime(2017, 02, 14, 10, 0, 0);
            DateTime to = new DateTime(2017, 02, 15, 23, 50, 0);

            // Получаем хранилище сделок для RIH7
            IMarketDataStorage<Trade> tradeStorage = storage.GetTradeStorage(security);

            // Загружаем сделки из хранилища
            var trades = tradeStorage.Load(from, to);

            // Отображаем в окне вывода информацию
            foreach (var trade in trades)
            {
                Debug.WriteLine("{0} {1}", trade, trade.OpenInterest);
            }

            Console.Read();

        }

    }
}
