using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurityList
{
    class Program
    {

        static void Main(string[] args)
        {
            // Путь к файлу базы данных гидры 
            var dbpath = @"C:\Users\Sergey\Documents\StockSharp\Hydra\StockSharp.db";
            var securities = SQLiteSecurities.GetSecurities(dbpath);
        }

    }
}
