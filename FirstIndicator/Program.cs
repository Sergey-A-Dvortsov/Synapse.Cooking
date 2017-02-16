namespace Synapse.Cooking.FirstIndicator
{
    using System;
    using StockSharp.BusinessEntities;
    using StockSharp.Messages;
    using StockSharp.Algo.Storages;
    using StockSharp.Algo.Indicators;
    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Candles.Compression;

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

            // Создаем простой индикатор - простая скользяшая средняя. Простой индикатор - индикатор, которые состоит из одного индикатора.
            var ma = new SimpleMovingAverage() { Length = 11 };

            // Событие генерируется при изменении текущего значения индикатора.. Событие имеет два параметра: входное и выходное значение.
            ma.Changed += (input, output) =>
            {
                //TODO
            };

            // Создаем комплексный индикатор - полосы Боллинджера. Комплексный индикатор - индикатор, которые состоит из нескольких простых или комплексных
            // индикаторов. В состав полос Боллинджера входят три простых индикатора: верхняя и нижняя полоса BollingerBand, а также простая скользящая средняя.
            var bb = new BollingerBands() { Length = 11, Width = 1.5m };

            // Событие генерируется при изменении текущего значения индикатора.. Событие имеет два параметра: входное и выходное значение.
            bb.Changed += (input, output) =>
            {
                //TODO
            };

            // Создаем кандлеменеджер, который в качестве источника свечек использует сделки из хранилища.
            CandleManager candleManager = new CandleManager(new TradeStorageCandleBuilderSource() { StorageRegistry = storage });

            candleManager.Processing += (series, candle) =>
            {
                if (candle.State != CandleStates.Finished)
                    return;

                // передаем в метод Process значение для обработки... 
                // Передаваемый параметр должен реализовывать интерфейс IIndicatorValue. 
                // В нашем случае используется метод расширения, который преобразует
                // свечу в класс CandleIndicatorValue, который реализует требуемый интерфейс. 
                // Функция возвращается тип, который также реализует интерфейс IIndicatorValue
                // Такой подход обладает следующими преимуществами:
                // 1. IIndicatorValue используется при построении графиков
                // 2. Значение одного индикторы, можно сразу передавать на вход другого индикатра.  
                var maValue = ma.Process(candle);


                // Свойство IsFormed становится true, когда текущее значение индикатора становится валидным
                // Наример, в скользящих средних, если число значений поступивших на вход больше или равно
                // периоду индикатора 
                if (ma.IsFormed)
                {
                    // Так можно вернуть "нормальное" значение из IIndicatorValue 
                    var maCur = ma.GetCurrentValue();

                    // Это другой способ получения "нормальное" текущего значения индикатора
                    maCur = maValue.GetValue<decimal>();
                }


                // Здесь мы используем комплексный индикатор. Также передаем в метод Process значение для обработки...
                // На выходе мы получаем тип ComplexIndicatorValue (тоже реализует IIndicatorValue).
                // Главная особенность типа ComplexIndicatorValue в наличии свойства InnerValues, где
                // хранятся текущие значения всех простых индикаторов, входящих в состав комплексного индикатора.
                var bbValue = (ComplexIndicatorValue)bb.Process(candle);

                if (bb.IsFormed)
                {
                    // Так можно получить значения из InnerValues 
                    var upBandValue = bbValue.InnerValues[bb.UpBand];
                    var lowBandValue = bbValue.InnerValues[bb.LowBand];

                    var upBand = bb.UpBand.GetCurrentValue();
                    var lowBand = bb.LowBand.GetCurrentValue();

                    upBand = upBandValue.GetValue<decimal>();
                    lowBand = lowBandValue.GetValue<decimal>();
                }

            };

            var srs = new CandleSeries(typeof(TimeFrameCandle), security, TimeSpan.FromMinutes(1));

            candleManager.Start(srs, from, to);

            Console.Read();

            candleManager.Stop(srs);

        }

    }
}
