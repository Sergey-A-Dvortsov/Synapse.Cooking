//Copyright © Сергей Дворцов, 2017,  Все права защищены

/// <summary>
/// Пример FirstCandle к разделу "Моя первая свеча" экспресс курса "Основы StockSharp".
/// В примере: 
/// 1. Cоздается экземпляр CandleManager, в конструктор которого передается коннектор.
/// 2. Выполняется подписка на событие CandleManager.Processing
/// 3. После получения инструмента, создается серия CandleSeries c типом свечи TimeFrameCandle и периодом 5 минут.
/// 4. Выполняется пуск получения свечей этой серии при помощи метода CandleManager.Start(CandleSeries)
/// 5. После нажатия любой клавиши серия останавливается при помощи метода CandleManager.Stop(CandleSeries)
/// </summary>
namespace Synapse.Cooking.FirstCandle
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
    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Candles.Compression;

    class Program
    {
        static Connector _connector;
        static CandleManager _candleManager;
        static CandleSeries _series = null;
        static string _securityId = "SBER@TQBR";
        static Security _security;
        static AutoResetEvent _handler;

        static void Main(string[] args)
        {
            _handler = new AutoResetEvent(false);
            _connector = new QuikTrader();

            // Создается andleManager, который будет использовать данные из коннектора
            _candleManager = new CandleManager(_connector);

            // Также можно создать СandleManager, который использует произвольную коллекцию данных.
            // В контексте этого примера этот способ следует использовать, когда получен инстумент и выполнена подписка на сделки
            // по инструменту
            //_candleManager = CreateCandleManager(_connector, _security);

            _connector.Connected += () =>
            {
                Console.WriteLine("Соединение установлено!");
            };

            _connector.Disconnected += () =>
            {
                Console.WriteLine("Соединение разорвано!");
                Console.WriteLine("Для выхода нажмите Q и Enter.");
            };

            _connector.NewSecurities += securities =>
            {
                securities.ForEach(s =>
                {
                    if (s.Id == _securityId)
                    {
                        _security = s;
                        _handler.Set();
                    }

                });
            };

            _candleManager.Processing += (series, candle) =>
            {

                if (_series == null || _series != series)
                    return;

                // Используем только завершенные свечи
                if (candle.State != CandleStates.Finished)
                    return;
                
                if (candle.OpenTime < (DateTimeOffset.Now - TimeSpan.FromMinutes(6)))
                {
                    // В эту ветку попадает исторические свечи, которые вычленяются при помощи текущего времени.
                    // Обратите внимание, что из текущего времени вычитается тайм-фрейм свечи, увеличенный на 1 минуту 
                    //(чтобы учесть разность между локальным времемем и временем сервера)..
                    // 
                    Debug.WriteLine(string.Format("История. {0}", candle));
                }
                else
                {
                    Debug.WriteLine(string.Format("Текущая. {0}", candle));
                }

            };


            _connector.Connect();

            _handler.WaitOne();

            // создаем серию
            _series = new CandleSeries(typeof(TimeFrameCandle), _security, TimeSpan.FromMinutes(5));

            // стартуем серию
            _candleManager.Start(_series);

            Console.Read();

            // останавливаем серию
            _candleManager.Stop(_series);

            _connector.Disconnect();

            // Ждет, пока последовательно не будут нажаты клаваши Q и Enter,
            // после чего программа завершит работу
            while (Console.ReadLine().ToUpper() != "Q") ;

        }


        // Вот так можно создать CandleManager с произвольной коллекцией данных
        // В нашем случае используются сделки, но это могут быть стаканы или записи ордерлога
        private static CandleManager CreateCandleManager(Connector connector, Security security)
        {
            var trades = connector.Trades.Where(t => t.Security == security);  // некая коллекция сделок
            var source = new RawConvertableCandleBuilderSource<Trade>(security, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, trades);
            CandleManager candleManager = null;
            candleManager = new CandleManager(source);

            // Вот так можно добавить источник к уже существующему CandleManager
            ///candleManager.Sources.Add((ICandleManagerSource)source);

            return candleManager;

        }


    }
}
