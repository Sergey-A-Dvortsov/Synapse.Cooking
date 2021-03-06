﻿//Copyright © Сергей Дворцов, 2017,  Все права защищены

namespace Synapse.Cooking.FirstLog
{
    using System;
    using System.Threading;

    using StockSharp.Algo;
    using StockSharp.Quik;
    using StockSharp.Logging;
    using global::FirstLog;

    class Program  
    {
        static Connector _trader;
        static AutoResetEvent _handler;
        static LogManager _logManager;
        static Connection _connection;


        static void Main(string[] args)
        {
            _trader = new QuikTrader();

            // объкт, управляющий логгированием
            _logManager = new LogManager();

            // слушатель - устройство вывода, куда будут записываться сообщения

            
            // слушатели: 
            // консоль
            _logManager.Listeners.Add(new ConsoleLogListener());

            // окно вывода
            _logManager.Listeners.Add(new DebugLogListener());

            // файл
            var fileListener = new FileLogListener
            {
                Append = false,              // true - добавляет сообщения в конец существующего файла, false - перезаписывает существующий файл
                Extension = "log",           // расширение файла лога
                FileName = "FirstLog",       // имя файла лога
                MaxLength = 1000000,         // максимальная длинна файла лога в байтах / в данном случае 1мб
                LogDirectory = "Logs",       // максимальная длинна файла лога 
                SeparateByDates = SeparateByDateModes.FileName  // режим разделения логов по датам
            };

            _logManager.Listeners.Add(fileListener);

            // добавляем источник логов. В данном случае это коннектор.
            // Роль истоника логов может выполнять любой класс, наследующий от BaseLogReceiver, или реализующий интерфейс ILogSource
            _logManager.Sources.Add(_trader);

            _connection = new Connection(_trader, "NL0011100043", "SBER@QJSIM");


            // Уровни логирования
            // Off - логи не выводятся
            // Error - выводятся только сообщения об ошибках
            // Warning - выводятся предупреждения и сообщения об ошибках
            // Info - выводятся информационные сообщения, предупреждения и сообщения об ошибках
            // Debug - выводятся все сообщения

            _connection.LogLevel = LogLevels.Debug;

            // добавляем еще один источник логов. 
            _logManager.Sources.Add(_connection);

            _connection.Connect();
      
            Console.Read();

            _connection.Disconnect();

        }


    }
}
