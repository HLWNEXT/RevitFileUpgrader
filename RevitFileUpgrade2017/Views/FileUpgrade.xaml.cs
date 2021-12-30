using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using RevitFileUpgrade.ViewModels;

namespace RevitFileUpgrade.Views
{
    /// <summary>
    /// Interaction logic for ParameterManager.xaml
    /// </summary>
    public partial class ParameterManager : Window
    {
        public ParameterManager()
        {
            var viewModel = new ParameterManagerViewModel();
            viewModel.UpgradeFamily += UpgradeFamilyReal;
            DataContext = viewModel;
            InitializeComponent();
            AppWindow = this;
        }
        public OperateFamily UpdateFamily;

        public void UpgradeFamilyReal(FileInfo file, String destPath, ref bool addInfo, ref List<string> fileTypes, ref IList<FileInfo> files, ref StreamWriter writer)
        {
            UpdateFamily(file, destPath, ref addInfo, ref fileTypes, ref files, ref writer);
        }


        public static ParameterManager AppWindow
        {
            get;
            private set;
        }

    }
}
