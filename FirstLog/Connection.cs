using MoreLinq;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FirstLog
{
    public class Connection : BaseLogReceiver
    {

        private Connector _connector;
        static string _portfolioName;
        static Portfolio _portfolio;
        static string _securityId;
        static Security _security;

        public Connection(Connector connector, string portfolioName, string securityId)
        {
            _connector = connector;
            _portfolioName = portfolioName;
            _securityId = securityId;
        }

        /// <summary>
        /// Устанавливает соединение
        /// </summary>
        public void Connect()
        {
            _connector.Connected += OnConnected;
            _connector.Disconnected += OnDisconnected;
            _connector.NewPortfolios += OnNewPortfolios;
            _connector.NewSecurities += OnNewSecurities;
            _connector.Connect();
        }

        /// <summary>
        /// Разрывает соединение
        /// </summary>
        public void Disconnect()
        {
            _connector.Connected -= OnConnected;
            _connector.Disconnected -= OnDisconnected;
            _connector.NewPortfolios -= OnNewPortfolios;
            _connector.NewSecurities -= OnNewSecurities;
            _connector.Disconnect();
        }

        private void OnConnected()
        {
            //  Добавляем в журнал информационное сообщение 
            this.AddInfoLog("Соединение установлено!");
        }

        private void OnDisconnected()
        {
            //  Добавляем в журнал информационное сообщение 
            this.AddInfoLog("Соединение разорвано!");
        }

        private void OnNewPortfolios(IEnumerable<Portfolio> portfolios)
        {
            portfolios.ForEach(portfolio =>
            {
                //  Добавляем в журнал отладочное сообщение 
                this.AddDebugLog("Получен портфель: {0}", portfolio.Name);
            });
        }

        private void OnNewSecurities(IEnumerable<Security> securities)
        {
            try
            {
                securities.ForEach(s =>
                {
                    if (s.Id == _securityId)
                    {
                        _security = s;
                        //  Добавляем в журнал отладочное сообщение 
                        this.AddDebugLog("Получен инструмент: {0}", s.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                //  Добавляем в журнал сообщение об ошибке
                this.AddErrorLog(ex);
            }

        }

    }

}
