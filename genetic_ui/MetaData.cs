using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace genetic_ui
{

    /// <summary>用于描述单个节点定义的结构体。</summary>
    struct Vertex
    {
        public int x;
        public int y;
        public int demand;
    }

    /// <summary>用于存放从XML文件中读取的元数据，或存放来自自动生成的数据。原则上禁止修改</summary>
    class MetaData
    {

        private int map_size;   //地图的分辨率
        private int ashbin_num; //垃圾桶数量
        private int truck_num;  //拥有的垃圾车数量
        private int capacity;   //单车最大负载
        private bool defined = false;   //是否已经装载好数据
        private Vertex origin;  //起点
        private Vertex target;  //终点
        private Vertex[] vertexs;   //用来存储代表垃圾桶的点的数据

        /// <summary>描述地图的尺寸。地图为长宽相等的正方形，该变量为地图的边长。</summary>
        public int MapSize { get => map_size; }
        /// <summary>垃圾桶节点的数量。</summary>
        public int AshbinNum { get => ashbin_num; }
        /// <summary>卡车数量。</summary>
        public int TruckNum { get => truck_num; }
        /// <summary>单辆卡车的最大有效负载。</summary>
        public int Capacity { get => capacity; }
        /// <summary>卡车出发的起点。</summary>
        public Vertex Origin { get => origin; }
        /// <summary>卡车的终点（垃圾中转站）。</summary>
        public Vertex Target { get => target; }

        /// <summary>节点列表数组（已经废弃）。</summary>
        //public Vertex[] Vertexs { get => vertexs; }

        //添加了可用下标-1访问的索引器，使得在访问MetaData类的对所存储的点时可以有类似数组的行为，并可以通过-1索引起点、0索引终点
        public Vertex this[int index]
        {
            get
            {
                if ((index > ashbin_num) || (index < -1)) throw new Exception("数组访问越界！");
                else if (index == -1) return origin;
                else return vertexs[index];
            }
        }


        /// <summary>
        /// 从XML文件中读入想要导入的预设好的元数据。
        /// </summary>
        /// <param name="path">XML文件的路径。</param>
        public void ReadXML(string path)
        {
            XmlDocument xml = new XmlDocument();
            XmlReaderSettings setting = new XmlReaderSettings();
            setting.IgnoreComments = true;  //忽略文件中的注释
            XmlReader reader = XmlReader.Create(path, setting);
            xml.Load(reader);

            //读入基本常量
            XmlNode root = xml.SelectSingleNode("data");
            map_size = int.Parse(root.SelectSingleNode("MapSize").InnerXml);
            ashbin_num = int.Parse(root.SelectSingleNode("AshbinNum").InnerXml);
            truck_num = int.Parse(root.SelectSingleNode("TruckNum").InnerXml);
            capacity = int.Parse(root.SelectSingleNode("Capacity").InnerXml);
            origin.x = int.Parse(root.SelectSingleNode("Origin").SelectSingleNode("x").InnerXml);
            origin.y = int.Parse(root.SelectSingleNode("Origin").SelectSingleNode("y").InnerXml);
            target.x = int.Parse(root.SelectSingleNode("Target").SelectSingleNode("x").InnerXml);
            target.y = int.Parse(root.SelectSingleNode("Target").SelectSingleNode("y").InnerXml);

            //读入垃圾桶的节点列表
            vertexs = new Vertex[ashbin_num + 1];
            vertexs[0].x = target.x;
            vertexs[0].y = target.y;
            for (int i = 0; i <= ashbin_num; i++)    //清空节点
            {
                vertexs[i].demand = 0;
            }
            XmlNodeList nodes = root.SelectSingleNode("Vertexs").SelectNodes("Ashbin");
            if (nodes.Count != ashbin_num) throw new Exception("提供的节点数量和预设不一致！");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlElement contain = (XmlElement)nodes.Item(i);
                int id = int.Parse(contain.GetAttribute("id"));
                if ((vertexs[id].demand != 0) && (id == 0)) throw new Exception("有重复节点！");
                vertexs[id].x = int.Parse(contain.SelectSingleNode("x").InnerXml);
                vertexs[id].y = int.Parse(contain.SelectSingleNode("y").InnerXml);
                vertexs[id].demand = int.Parse(contain.SelectSingleNode("demand").InnerXml);
            }
        }

        /// <summary>
        /// 随机生成一个测试用例
        /// </summary>
        /// <param name="map_s">地图尺寸</param>
        /// <param name="ashbin">垃圾桶数量</param>
        /// <param name="truck">卡车数量</param>
        /// <param name="cap">单车载重</param>
        /// <param name="dem_m">每个垃圾桶最大垃圾数量</param>
        public void RandomCreate(int map_s, int ashbin, int truck, int cap, int dem_m)
        {
            Random random = new Random(DateTime.Now.GetHashCode());
            map_size = map_s;
            ashbin_num = ashbin;
            truck_num = truck;
            capacity = cap;
            int demand_max = dem_m;
            origin = RandomVertex(random, map_size, demand_max);
            target = RandomVertex(random, map_size, demand_max);
            origin.demand = target.demand = 0;
            vertexs = new Vertex[AshbinNum + 1];
            vertexs[0] = target;
            for (int i = 1; i <= ashbin_num; i++) vertexs[i] = RandomVertex(random, map_size, demand_max);
            defined = true;
        }

        /// <summary>
        /// 使用默认参数的生成测试样例重载
        /// </summary>
        public void RandomCreate()
        {
            Random random = new Random(DateTime.Now.GetHashCode());
            int demand_max = 10;
            origin = RandomVertex(random, map_size, demand_max);
            target = RandomVertex(random, map_size, demand_max);
            origin.demand = target.demand = 0;
            vertexs = new Vertex[AshbinNum + 1];
            vertexs[0] = target;
            for (int i = 1; i <= ashbin_num; i++) vertexs[i] = RandomVertex(random, map_size, demand_max);
            defined = true;
        }

        /// <summary>
        /// 为随机生成节点的lambda表达式提供委托
        /// </summary>
        /// <param name="random">随机数生成器</param>
        /// <param name="edge_limit">地图边界极限</param>
        /// <param name="demand_limit">垃圾数量极限</param>
        /// <returns>返回一个节点</returns>
        delegate Vertex CreateVertex(Random random, int edge_limit, int demand_limit);
        /// <summary>
        /// 随机生成一个节点的lambda表达式
        /// </summary>
        private CreateVertex RandomVertex = (Random random, int edge_limit, int demand_limit) =>
        {
            return new Vertex()
            {
                x = random.Next(edge_limit + 1),
                y = random.Next(edge_limit + 1),
                demand = random.Next(1, demand_limit + 1)
            };
        };

        /// <summary>
        /// 将随机生成的测试样例写入XML文件，并存放到指定位置
        /// </summary>
        /// <param name="path">存储XML的路径（不能包含文件名！）</param>
        public void WriteInXML(string path)
        {
            //设置存储的文件路径
            if (defined == false) RandomCreate();
            string time = DateTime.Now.ToString();
            time = time.Replace(' ', '_');
            time = time.Replace('/', '-');
            time = time.Replace(':', '-');
            string file_name = path + @"\" + time + "data.xml";
            file_name = file_name.Replace(@"\\", @"\");

            //全局变量的写入IO操作
            XmlDocument xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement root_element = xml.CreateElement("data");
            xml.AppendChild(root_element);
            root_element.AppendChild(xml.CreateElement("MapSize"));
            root_element.SelectSingleNode("MapSize").InnerXml = map_size.ToString();
            root_element.AppendChild(xml.CreateElement("AshbinNum"));
            root_element.SelectSingleNode("AshbinNum").InnerXml = ashbin_num.ToString();
            root_element.AppendChild(xml.CreateElement("TruckNum"));
            root_element.SelectSingleNode("TruckNum").InnerXml = truck_num.ToString();
            root_element.AppendChild(xml.CreateElement("Capacity"));
            root_element.SelectSingleNode("Capacity").InnerXml = capacity.ToString();
            root_element.AppendChild(xml.CreateElement("Origin"));
            root_element.SelectSingleNode("Origin").AppendChild(xml.CreateElement("x"));
            root_element.SelectSingleNode("Origin").SelectSingleNode("x").InnerXml = origin.x.ToString();
            root_element.SelectSingleNode("Origin").AppendChild(xml.CreateElement("y"));
            root_element.SelectSingleNode("Origin").SelectSingleNode("y").InnerXml = origin.y.ToString();
            root_element.AppendChild(xml.CreateElement("Target"));
            root_element.SelectSingleNode("Target").AppendChild(xml.CreateElement("x"));
            root_element.SelectSingleNode("Target").SelectSingleNode("x").InnerXml = target.x.ToString();
            root_element.SelectSingleNode("Target").AppendChild(xml.CreateElement("y"));
            root_element.SelectSingleNode("Target").SelectSingleNode("y").InnerXml = target.y.ToString();

            //写入随机生成的垃圾桶节点
            XmlElement vertexs_element = xml.CreateElement("Vertexs");
            for (int i = 1; i <= ashbin_num; i++)
            {
                XmlElement new_vertex = xml.CreateElement("Ashbin");
                new_vertex.SetAttribute("id", i.ToString());
                new_vertex.AppendChild(xml.CreateElement("x"));
                new_vertex.SelectSingleNode("x").InnerXml = vertexs[i].x.ToString();
                new_vertex.AppendChild(xml.CreateElement("y"));
                new_vertex.SelectSingleNode("y").InnerXml = vertexs[i].y.ToString();
                new_vertex.AppendChild(xml.CreateElement("demand"));
                new_vertex.SelectSingleNode("demand").InnerXml = vertexs[i].demand.ToString();
                vertexs_element.AppendChild(new_vertex);
            }
            root_element.AppendChild(vertexs_element);

            //保存文件到指定路径
            xml.Save(file_name);
        }

    }
}
