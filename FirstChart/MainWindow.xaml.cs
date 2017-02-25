//Copyright © Сергей Дворцов, 2017,  Все права защищены


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Ecng.Xaml;
using StockSharp.Algo.Candles;
using StockSharp.Quik;
using StockSharp.Xaml.Charting;
using StockSharp.Messages;
using StockSharp.Algo.Indicators;
using System.Net;
using Ecng.Common;

namespace FirstChart
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private QuikTrader _connector;
        private CandleManager _candleManager;
        private ChartCandleElement _candleElement;
        private ChartIndicatorElement _indicatorElement;
        private CandleSeries _series;
        private ChartArea _area;
        private SimpleMovingAverage _sma;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectButtom_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;
            if (button.Content.ToString() == "Соединить")
            {
                OnConnect();
            }
            else
            {
                _connector.Disconnect();
            }
            
        }

        private void OnConnect()
        {
            _connector = new QuikTrader() { LuaFixServerAddress = "127.0.0.1:5002".To<EndPoint>() };
            SecurityEditor.SecurityProvider = _connector;
            _candleManager = new CandleManager(_connector);

            _connector.Connected += () =>
            {
                this.GuiAsync(() =>
                {
                    StatusTextBlock.Text = "Соединение установлено!";
                    ConnectButtom.Content = "Разъединить";
                });
                
            };

            _connector.Disconnected += () =>
            {
                this.GuiAsync(() =>
                {
                    StatusTextBlock.Text = "Соединение разорвано!";
                    ConnectButtom.Content = "Соединить";
                });
            };

            // Это обработчик событие получения свечи
            _candleManager.Processing += (series, candle) =>
            {
                if (series != _series)
                    return;

                // Используем только завершенные свечи
                if (candle.State != CandleStates.Finished)
                    return;

                // Рассчитываем значение индикатора
                var smaValue = _sma.Process(candle);

                // Создаем экземпляр класса ChartDrawData - класс, где группируются данные для отрисовки  
                var data = new ChartDrawData();

                // chartItem - набор элементов, привязанных к одной точке на шкале X
                var chartItem = data.Group(candle.OpenTime).Add(_candleElement, candle);
                chartItem.Add(_indicatorElement, smaValue);

                // Безопасно отрисовываем элементы на графике
                this.GuiSync(() =>
                {
                    Chart.Draw(data);
                });

            };

            _connector.Connect();

        }

        private void InitChart(CandleSeries series)
        {
            Chart.ClearAreas();
            _area = new ChartArea();
            var yAxis = _area.YAxises.First();
            var xAxis = _area.XAxises.First();

            yAxis.AutoRange = true;
            Chart.IsAutoRange = true;
            Chart.IsAutoScroll = true;
            Chart.ShowOverview = true;

            _sma = new SimpleMovingAverage() { Length = (int)IntegerUpDown.Value }; 

            _candleElement = new ChartCandleElement() { FullTitle = "Candles" };
            _indicatorElement = new ChartIndicatorElement() { FullTitle = "SMA" };

            Chart.AddArea(_area);
            Chart.AddElement(_area, _candleElement, series);
            Chart.AddElement(_area, _indicatorElement);

        }

        private void DrawButtom_Click(object sender, RoutedEventArgs e)
        {

            if (DrawButtom.Content.ToString() == "Построить")
            {
                _series = new CandleSeries(typeof(TimeFrameCandle), SecurityEditor.SelectedSecurity, TimeSpanEditor.Value);

                InitChart(_series);

                _candleManager.Start(_series);
                DrawButtom.Content = "Остановить";
            }
            else
            {
                _candleManager.Stop(_series);
                DrawButtom.Content = "Построить";
            }

        }

        private void SecurityEditor_SecuritySelected()
        {
            DrawButtom.IsEnabled = SecurityEditor.SelectedSecurity != null;
        }

    }
}
