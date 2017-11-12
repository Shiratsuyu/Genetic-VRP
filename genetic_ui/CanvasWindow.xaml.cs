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
        private bool import_xml;    //信息是否是从XML文件导入的
        private string import_path; //若导入XML，导入文件的路径
        private int map;    //随机生成时地图的尺寸
        private int ashbin; //随机生成时的垃圾桶数
        private int truck;  //随机生成时的卡车数
        private int capacity;   //随机生成时单车的最大载重
        private int demand; //随机生成时单垃圾桶的最大垃圾数
        private bool export_xml;    //是否保存随机生成的地图数据
        private string export_path; //若保存，导出XML数据的路径
        private int population; //遗传算法的种群数量
        private int iteration;  //遗传算法的迭代代数
        private double select_best; //将上代最优解批量复制到下一代的比例
        private double cross;   //下代个体进行交配的概率（仅对新颖交叉算子有效）
        private double transform;   //下代个体发生变异的概率
        private double new_car; //当前卡车满载时，是否使用新车的概率
        private bool operator_choose;   //选择何种遗传算子
        private bool output_style;  //输出信息的风格

        private MetaData meta;  //用于存储生成或导入的元数据
        private GeneticCore core;   //用于存储遗传运算的核心类的实例化对象
        private CancellationTokenSource kill_task;  //监控并行计算线程是否应该终止的token

        public CanvasWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化画布窗口时的跨窗口传值，用于接收从主界面发送的各项设置
        /// </summary>
        /// <param name="_import_xml">信息是否是从XML文件导入的</param>
        /// <param name="_import_path">若导入XML，导入文件的路径</param>
        /// <param name="_map">随机生成时地图的尺寸</param>
        /// <param name="_ashbin">随机生成时的垃圾桶数</param>
        /// <param name="_truck">随机生成时的卡车数</param>
        /// <param name="_capacity">随机生成时单车的最大载重</param>
        /// <param name="_demand">随机生成时单垃圾桶的最大垃圾数</param>
        /// <param name="_export_xml">是否保存随机生成的地图数据</param>
        /// <param name="_export_path">若保存，导出XML数据的路径</param>
        /// <param name="_population">遗传算法的种群数量</param>
        /// <param name="_iteration">遗传算法的迭代代数</param>
        /// <param name="_select_best">将上代最优解批量复制到下一代的比例</param>
        /// <param name="_cross">下代个体进行交配的概率（仅对新颖交叉算子有效）</param>
        /// <param name="_transform">下代个体发生变异的概率</param>
        /// <param name="_new_car">当前卡车满载时，是否使用新车的概率</param>
        /// <param name="_operator_choose">选择何种遗传算子</param>
        /// <param name="_output_style">输出信息的风格</param>
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

        /// <summary>
        /// 窗口被打开时调用的并行计算线程，通过新开线程访问主线程的方式异步更新UI，防止阻塞
        /// </summary>
        private void StartGenetic()
        {
            try
            {
                //有关于元数据的初始化部分
                if (import_xml)
                {
                    new Thread(() =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            ConsoleOutputBox.Items.Add("正从XML文件读取预设元数据……");
                        }));
                    }).Start();                    
                    meta.ReadXML(import_path);
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
                    meta.RandomCreate(map, ashbin, truck, capacity, demand);
                    new Thread(() =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            ConsoleOutputBox.Items.Add("生成结束！");
                        }));
                    }).Start();
                    if (export_xml)
                    {
                        meta.WriteInXML(export_path);
                        new Thread(() =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                ConsoleOutputBox.Items.Add("成功写入到：" + export_path);
                            }));
                        }).Start();
                    }
                }

                //调用算法核心
                core.SetValue(population, select_best, cross, transform, new_car);
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
                        IterationBar.Maximum = iteration;
                    }));
                }).Start();

                double best = core.GlobalShortestDistance;

                for (int i = 0; i < iteration; i++)
                {
                    //关闭窗口时发送Cancel指令，每轮循环时检查token
                    if (kill_task.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    core.CaculateFitness();

                    if (output_style == true)
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

                        if (operator_choose == true) core.InverOverTransform();
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

                        if (operator_choose == true) core.InverOverTransform();
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
                        string message = String.Format("共迭代了{0}轮，最优解为{1}公里", iteration,
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
                        ConsoleScroll.ScrollToEnd();
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

        /// <summary>
        /// 窗口被打开时，开始遗传运算
        /// </summary>
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            kill_task = new CancellationTokenSource();
            Task.Run(() => StartGenetic());            
        }

        /// <summary>
        /// 关闭窗口时自动终止正在进行的计算
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            kill_task.Cancel();
        }
    }
}
