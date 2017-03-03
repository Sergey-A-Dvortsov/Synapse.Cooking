using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstStrategy
{
    public class SendOrderStrategy : Strategy
    {

        private bool _isSendOrder;

        public Sides Direction { set; get; }

        protected override void OnStarted()
        {
            base.OnStarted();

            _isSendOrder = false;

            Connector.SecuritiesChanged += OnSecuritiesChanged;

            Security.StepPrice = Security.PriceStep;

            if (!Connector.RegisteredSecurities.Contains(Security))
                Connector.RegisterSecurity(Security);

            this.WhenPnLChanged().
                Do(pnl => 
                {
                    this.AddDebugLog(string.Format("PnLChanged. {0}", pnl));
                }).Apply(this);

        }

        protected override void OnStopping()
        {
            Connector.SecuritiesChanged -= OnSecuritiesChanged;
            base.OnStopping();
        }

        protected override void OnStopped()
        {

        }

        private void OnSecuritiesChanged(IEnumerable<Security> securities)
        {
            var security = securities.FirstOrDefault(s => s == Security);

            if (security == null || security.BestAsk == null || security.BestBid == null || _isSendOrder)
                return;
            Security.StepPrice = Security.PriceStep;


            Order order = this.CreateOrder(Direction, security.BestAsk.Price + security.PriceStep, 1);

            _isSendOrder = true;

            order.WhenRegistered(this.Connector).Do(o =>
            {
                this.AddDebugLog(string.Format("Заявка зарегистрирована. {0}", o));
            }).Apply(this).Once();

            order.WhenRegisterFailed(this.Connector).Do(of =>
            {
                this.AddDebugLog(string.Format("Ошибка регистрации заявки. {0}", of.Error));
            }).Apply(this).Once();

            order.WhenChanged(this.Connector).Do(o =>
            {
                this.AddDebugLog(string.Format("Заявка изменилась. {0}", o));
            }).Apply(this);

            order.WhenMatched(this.Connector).Do(o =>
            {
                this.AddDebugLog(string.Format("Заявка исполнена. {0}", o));
            }).Apply(this).Once();

            order.WhenNewTrade(this.Connector).Do(t =>
            {
                this.AddDebugLog(string.Format("Пришла сделка. {0}", t));
            }).Apply(this).Once();

            this.RegisterOrder(order);

        }


    }

}
