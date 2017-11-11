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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace genetic_ui
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            //设置要选择的文件类型
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";

            //显示文件浏览器窗口
            Nullable<bool> result = dlg.ShowDialog();

            //获取选中文件的路径到TextBox中
            if (result == true)
            {
                string filename = dlg.FileName;
                ImportBox.Text = filename;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new FolderBrowserDialog();

            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string path = dlg.SelectedPath + @"\";
                ExportBox.Text = path;
            }
        }

        private void ResetDataButton_Click(object sender, RoutedEventArgs e)
        {
            ImportXml.IsChecked = true;
            ImportBox.Text = @"C:\data.xml";
            AshbinBox.Text = "50";
            DemandBox.Text = "10";
            TruckBox.Text = "3";
            CapacityBox.Text = "50";
            MapBox.Text = "1000";
            ExportData.IsChecked = true;
            ExportBox.Text = @"C:\Users\Public\Desktop\";
        }

        private void ResetGeneticButton_Click(object sender, RoutedEventArgs e)
        {
            PopulationBox.Text = "300";
            IterationBox.Text = "10000";
            SelectBox.Text = "0.25";
            TransformBox.Text = "0.2";
            NewCarBox.Text = "0.5";
        }

        private void StartCompute(object sender, RoutedEventArgs e)
        {
            CanvasWindow canvas_window = new CanvasWindow();
            bool import_xml = (ImportXml.IsChecked == true);
            bool export_xml = (ExportData.IsChecked == true);
            canvas_window.SendArgument(import_xml, ImportBox.Text, int.Parse(MapBox.Text), int.Parse(AshbinBox.Text),
                int.Parse(TruckBox.Text), int.Parse(CapacityBox.Text), int.Parse(DemandBox.Text),
                export_xml, ExportBox.Text, int.Parse(PopulationBox.Text), int.Parse(IterationBox.Text),
                double.Parse(SelectBox.Text), double.Parse(TransformBox.Text), double.Parse(NewCarBox.Text));
        }
    }
}
