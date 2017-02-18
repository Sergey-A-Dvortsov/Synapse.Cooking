using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoexIndex
{
    public class SecurityParams
    {
       public string Ticker { set; get; }
       public string Shortname { set; get; }
       public double WaPrice { set; get; }
       public double Issue_Size_Total { set; get; }
       public double Cap_Total { set; get; }
       public double Ff_factor { set; get; }
       public double W_factor { set; get; }
       public double Issue_Size_Index { set; get; }
       public double Cap_Index { set; get; }
       public double Weight { set; get; }
       public double Value { set; get; }
       public long Num_Trades { set; get; }
       public double Volatility { set; get; }
       public double Factora { set; get; }
       public double Factorb { set; get; }
       public double Influence { set; get; }
       public double Determinat { set; get; }

    }
}
