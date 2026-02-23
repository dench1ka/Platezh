using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Platezh.Views
{
    /// <summary>
    /// Логика взаимодействия для MoreContractInfoWindow.xaml
    /// </summary>
    public partial class MoreContractInfoWindow : Window
    {
        public MoreContractInfoWindow(Casher casher, TableMode mode, object selectedItem)
        {
            InitializeComponent();
        }
    }
}
