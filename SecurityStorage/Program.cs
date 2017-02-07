
namespace SecurityStorage
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using StockSharp.Algo.Storages;
    using StockSharp.BusinessEntities;


    class Program
    {

        private static StorageRegistry storage;

        static void Main(string[] args)
        {
            var storagePath = "";
            storage = new StorageRegistry() { DefaultDrive = new LocalMarketDataDrive(storagePath) } ;
            var securityStorage = storage.GetSecurityStorage();

        }

        private void SaveSecurity(Security security, ISecurityStorage securityStorage)
        {
            securityStorage.Save(security);
        }

    }
}
