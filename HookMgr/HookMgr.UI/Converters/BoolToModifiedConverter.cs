using System.Globalization;
using System.Windows.Data;

namespace HookMgr.UI;

public class BoolToModifiedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isModified)
        {
            return isModified ? "已修改" : "原始";
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
