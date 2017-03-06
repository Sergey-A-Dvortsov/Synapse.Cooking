using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Synapse.MoexLoader
{
    public class ContractTypeIsCheckedConverter : IValueConverter
    {

        private eContractType _contractType;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = (eContractType)value;

            switch (parameter.ToString())
            {
                case "Futures" :
                    return type == eContractType.Future;
                case "Options" :
                    return type == (eContractType.Call | eContractType.Put);
                case "FuturesOptions" :
                    return type == (eContractType.Future | eContractType.Call | eContractType.Put);
                default:
                    break;
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var isChecked = (bool)value;

            switch (parameter.ToString())
            {
                case "Futures":
                    if (isChecked)
                        _contractType = eContractType.Future;
                    break;
                case "Options":
                    if (isChecked)
                        _contractType = (eContractType.Call | eContractType.Put);
                    break;
                case "FuturesOptions":
                    if (isChecked)
                        _contractType = (eContractType.Future | eContractType.Call | eContractType.Put);
                    break;
                default:
                    break;
            }

            return _contractType;

            //return DependencyProperty.UnsetValue;

        }
    }


}
