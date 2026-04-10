using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace ColorList;

public partial class ColorViewer : Window
{
    public ColorViewer()
    {
        InitializeComponent();

        var brushes = typeof(SystemColors)
            .GetProperties(BindingFlags.Static | BindingFlags.Public)
            .Where(p => p.PropertyType == typeof(SolidColorBrush))
            .Select(p => new
            {
                p.Name,
                Brush = (SolidColorBrush?)p.GetValue(null, null)
            })
            .OrderBy(x => x.Name)
            .ToList();

        ColorList.ItemsSource = brushes;
    }
}