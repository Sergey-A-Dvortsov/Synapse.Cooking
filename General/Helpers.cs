using StockSharp.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Cooking.General
{
    public static class Helpers
    {

        public static string ParametersToString(this Position p)
        {
            if (p == null)
                return String.Empty;
            var sb = new StringBuilder();

            sb.AppendFormat("AveragePrice: {0}{1}", p.AveragePrice, Environment.NewLine);
            sb.AppendFormat("BeginValue: {0}{1}", p.BeginValue, Environment.NewLine);
            sb.AppendFormat("BlockedValue: {0}{1}", p.BlockedValue, Environment.NewLine);
            sb.AppendFormat("ClientCode: {0}{1}", p.ClientCode, Environment.NewLine);
            sb.AppendFormat("Commission: {0}{1}", p.Commission, Environment.NewLine);
            sb.AppendFormat("CurrentPrice: {0}{1}", p.CurrentPrice, Environment.NewLine);
            sb.AppendFormat("CurrentValue: {0}{1}", p.CurrentValue, Environment.NewLine);
            sb.AppendFormat("LimitType: {0}{1}", p.LimitType.ToString(), Environment.NewLine);
            sb.AppendFormat("Portfolio: {0}{1}", p.Portfolio.Name, Environment.NewLine);
            sb.AppendFormat("Security: {0}{1}", p.Security.Id, Environment.NewLine);
            sb.AppendFormat("UnrealizedPnL: {0}{1}", p.UnrealizedPnL, Environment.NewLine);
            sb.AppendFormat("VariationMargin: {0}{1}", p.VariationMargin, Environment.NewLine);
            return sb.ToString();
        }

    }

}
