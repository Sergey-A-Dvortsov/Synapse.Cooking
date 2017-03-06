using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.MoexLoader
{
    public static class Helpers
    {

        public static void ClearValues(this Dictionary<eContractType, Dictionary<eClientType, string>> values)
        {
            values.ForEach(kvp =>
            {
                foreach (var k in kvp.Value.ToList())
                {
                    kvp.Value[k.Key] = "";
                }
            });
        } 

    }
}
