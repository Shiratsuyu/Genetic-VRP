using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace genetic_ui
{
    /// <summary>
    /// CanvasWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CanvasWindow : Window
    {
        private bool import_xml;
        private string import_path;
        private int map;
        private int ashbin;
        private int truck;
        private int capacity;
        private int demand;
        private bool export_xml;
        private string export_path;
        private int population;
        private int iteration;
        private double select_best;
        private double cross;
        private double transform;
        private double new_car;
        private bool operator_choose;
        private bool output_style;

        private MetaData meta;
        private GeneticCore core;
        private CancellationTokenSource kill_task;
        private List<string> information_source;

        public CanvasWindow()
        {
            InitializeComponent();
        }

        public void SendArgument(bool _import_xml, string _import_path, int _map, int _ashbin, int _truck,
            int _capacity, int _demand, bool _export_xml, string _export_path, int _population, int _iteration,
            double _select_best, double _cross, double _transform, double _new_car, bool _operator_choose,
            bool _output_style)
        {
            this.Show();

            import_xml = _import_xml;
            import_path = _import_path;
            map = _map;
            ashbin = _ashbin;
            truck = _truck;
            capacity = _capacity;
            demand = _demand;
            export_xml = _export_xml;
            export_path = _export_path;
            population = _population;
            iteration = _iteration;
            select_best = _select_best;
            cross = _cross;
            transform = _transform;
            new_car = _new_car;
            operator_choose = _operator_choose;
            output_style = _output_style;

            meta = new MetaData();
            core = new GeneticCore();
        }

        private void StartGenetic(bool _import_xml, string _import_path, int _map, int _ashbin, int _truck,
            int _capacity, int _demand, bool _export_xml, string _export_path, int _population, int _iteration,
            double _select_best, double _cross, double _transform, double _new_car, bool _operator_choose,
            bool _output_style)
        {
            try
            {
                //有关于元数据的初始化部分
                if (_import_xml)
                {
                    new Thread(() =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            ConsoleOutputBox.Items.Add("正从XML文件读取预设元数据……");
                        }));
                    }).Start();                    
                    meta.ReadXML(_import_path);
                    new Thread(() =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            ConsoleOutputBox.Items.Add("读取完成！");
                        }));
                    }).Start();
                }
                else
                {
                    new Thread(() =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            ConsoleOutputBox.Items.Add("正由预设信息随机生成元数据……");
                        }));
                    }).Start();
                    meta.RandomCreate(_map, _ashbin, _truck, _capacity, _demand);
                    new Thread(() =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            ConsoleOutputBox.Items.Add("生成结束！");
                        }));
                    }).Start();
                    if (_export_xml)
                    {
                        meta.WriteInXML(_export_path);
                        new Thread(() =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                ConsoleOutputBox.Items.Add("成功写入到：" + _export_path);
                            }));
                        }).Start();
                    }
                }

                //调用算法核心
                core.SetValue(_population, _select_best, _cross, _transform, _new_car);
                core.Meta = meta;
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        ConsoleOutputBox.Items.Add("初始化种群……");
                    }));
                }).Start();
                core.InitializePopulation();
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        IterationBar.Maximum = _iteration;
                    }));
                }).Start();

                double best = core.GlobalShortestDistance;

                for (int i = 0; i < _iteration; i++)
                {
                    //关闭窗口时发送Cancel指令，每轮循环时检查token
                    if (kill_task.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    core.CaculateFitness();

                    if (_output_style == true)
                    {
                        if (best > core.GlobalShortestDistance)
                        {
                            best = core.GlobalShortestDistance;
                            string message = String.Format("迭代至第{0}轮时，发现了新的最优解：目前的最优解是{1}公里。", i + 1, core.GlobalShortestDistance.ToString("F2"));
                            new Thread(() =>
                            {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    ConsoleOutputBox.Items.Add(message);
                                    ConsoleScroll.ScrollToEnd();
                                }));
                            }).Start();
                        }

                        core.SelectChildren();

                        if (_operator_choose == true) core.InverOverTransform();
                        else core.NovelCrossTransform();

                        new Thread(() =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                IterationBar.Value = i + 1;
                            }));
                        }).Start();
                    }
                    else
                    {
                        string message = String.Format("当前是第{0}轮，此轮中适应度最高的后代解为{1}公里，目前的最优解是{2}公里。", i + 1, core.PopulationShortestDistance,
                            core.GlobalShortestDistance);

                        core.SelectChildren();

                        if (_operator_choose == true) core.InverOverTransform();
                        else core.NovelCrossTransform();

                        new Thread(() =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                IterationBar.Value = i + 1;
                                ConsoleOutputBox.Items.Add(message);
                                ConsoleScroll.ScrollToEnd();
                            }));
                        }).Start();
                    }
                }

                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        ConsoleOutputBox.Items.Add("迭代完成！");
                        string message = String.Format("共迭代了{0}轮，最优解为{1}公里", _iteration,
                            core.GlobalShortestDistance.ToString("F2"));
                        ConsoleOutputBox.Items.Add(message);
                        ConsoleOutputBox.Items.Add("最优解的路线为：");
                        List<int> route = core.GlobalShortestRoute;
                        message = string.Format("{0} -> ", route[0]);
                        for (int i = 1; i < route.Count; i++)
                        {
                            if (route[i] == 0)
                            {
                                message += 0;
                                ConsoleOutputBox.Items.Add(message);
                                i++;
                                if (i < route.Count) message = string.Format("{0} -> ", route[i]);
                            }
                            else
                            {
                                message += string.Format("{0} -> ", route[i]);
                            }                        
                        }
                    }));
                }).Start();

            }
            catch (Exception anyone)
            {
                if (anyone.GetType() is System.Threading.Tasks.TaskCanceledException)
                {
                    return;
                }
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        ConsoleOutputBox.Items.Add("致命错误：" + anyone.Message);
                    }));
                }).Start();
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            kill_task = new CancellationTokenSource();
            Task.Run(() => StartGenetic(import_xml, import_path, map, ashbin, truck, capacity, demand,
                    export_xml, export_path, population, iteration, select_best, cross, transform, new_car, operator_choose, output_style),kill_task.Token);            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            kill_task.Cancel();
        }
    }
}
