namespace FinamLoader
{

    using StockSharp.Algo;
    using StockSharp.Algo.Candles.Compression;
    using StockSharp.Algo.History.Russian.Finam;
    using StockSharp.Algo.Storages;
    using StockSharp.BusinessEntities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {

        /// <summary>
        /// Хранилище инструментов Финама
        /// Этот класс сопоставляет инстурменты в формате s# c идентификатормами
        /// инструментов в формате Финам.
        /// Нужные инструменты могут быть добвлены в конструкторе или при помощи
        /// метода Add
        /// </summary>
        private class FinamSecurityStorage : CollectionSecurityProvider, ISecurityStorage
        {
            public FinamSecurityStorage(Security security)
                : base(new[] { security })
            {
            }

            public FinamSecurityStorage(IEnumerable<Security> securities)
                : base(securities)
            {
            }

            void ISecurityStorage.Save(Security security)
            {
            }

            void ISecurityStorage.Delete(Security security)
            {
                throw new NotSupportedException();
            }

            void ISecurityStorage.DeleteBy(Security criteria)
            {
                throw new NotSupportedException();
            }

            IEnumerable<string> ISecurityStorage.GetSecurityIds()
            {
                return Enumerable.Empty<string>();
            }
        }

        static void Main(string[] args)
        {

            Security sber = new Security { Id = "SBER@TQBR", Board = ExchangeBoard.Micex };
            Security gazp = new Security { Id = "GAZP@TQBS", Board = ExchangeBoard.Micex };
            Security gmkn = new Security { Id = "GMKN@TQBS", Board = ExchangeBoard.Micex };

            // создаем хранилище инструментов Финам
            // добавить инструменты можно через конструктор, так
            //var finamSecurityStorage = new FinamSecurityStorage(sber);
            // или так
            var finamSecurityStorage = new FinamSecurityStorage(new List<Security>() { sber, gmkn } );

            // или при помощи метода Add
            finamSecurityStorage.Add(gazp);

            // Создаем экземпляр класса FinamHistorySource. Этот объект управляет получением данных с Финама.
            FinamHistorySource _finamHistorySource = new FinamHistorySource();

            // Создаем жранилище для нативных идентификаторов (родные идентификаторы инструментов Финама)
            var nativeIdStorage = new InMemoryNativeIdStorage();

            bool isCanceled = false;

            // Задаем папку, где будут сохранены запрошенные данные.. Если папку не задавать, то
            // на диске данные сохранены не будут
            _finamHistorySource.DumpFolder = "DataHist";

            // Выполняем обновление хранилища инструментов Финама
            // Перед добавлением каждого инструмента в хранилище вызывается функция (делегат) isCanceled, если функция возвращает false, то обновление
            // хранилища продолжается, если true, то прерывается.
            // При добавлении нового инструмента в хранилище вызывается функция (делегат) newSecurity. В нашем случае этот делегат имеет пустое тело (ничего не делает).
            _finamHistorySource.Refresh(finamSecurityStorage, nativeIdStorage, new Security(), s => {}, () => isCanceled);

            // Задаем таймфрем свечи
            var timeFrame = TimeSpan.FromMinutes(1);

            var now = DateTime.Now;
            var end = new DateTime(now.Year, now.Month, now.Day - 1, 0, 0, 0);
            var start = end.AddDays(-2);

            // Запрашиваем свечи с Финама 
            var candles = _finamHistorySource.GetCandles(gazp, nativeIdStorage, timeFrame, start, end);

            // Запрашиваем тики
            var ticks = _finamHistorySource.GetTicks(gmkn, nativeIdStorage, start, end);

            Console.Read();

        }

    }
}
