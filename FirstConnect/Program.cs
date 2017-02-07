namespace Synapse.Cooking.FirstConnect
{
    using System;
    using StockSharp.Algo;
    using StockSharp.Quik;

    class Program
    {
        static Connector connector;
        static void Main(string[] args)
        {
            connector = new QuikTrader();

            connector.Connected += () => Console.WriteLine("Соединение установлено!");

            connector.Disconnected += () =>
            {
                Console.WriteLine("Соединение разорвано!");
                connector.Dispose();
                Console.WriteLine("Для выхода нажмите Q и Enter.");
            };

            //connector.Connected += OnConnected;
            //connector.Disconnected += OnDisconnected;

            connector.Connect();

            Console.Read();

            connector.Disconnect();

            // Ждет, пока последовательно не будут нажаты клаваши Q и Enter,
            // после чего программа завершит работу
            while (Console.ReadLine().ToUpper() != "Q") ;

        }

        private static void OnConnected()
        {
            Console.WriteLine("Соединение установлено!");
        }

        private static void OnDisconnected()
        {
            Console.WriteLine("Соединение разорвано!");
            connector.Dispose();
            Console.WriteLine("Для выхода нажмите Q и Enter.");
        }

    }
}
