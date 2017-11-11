using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace genetic_ui
{
    /// <summary>
    /// 遗传算法的核心类。
    /// </summary>
    class GeneticCore
    {

        private int population_size;
        private double cross_probability;
        private double transform_probability;
        private double new_car_probability;
        private Random random;
        private MetaData meta;
        private double population_fitness;
        private List<double> individuals_fitness;
        private List<List<int>> population;
        private double global_shortest_distance;
        private List<int> global_shortest_route;
        private List<int> global_best_individual;
        private double population_shortest_distance;
        private List<int> population_shortest_route;
        private List<int> population_best_individual;

        /// <summary>只写访问器，给核心类添加用于生成种群的元数据引用</summary>
        public MetaData Meta { set => meta = value; }

        /// <summary>获取所有代中的最优解</summary>
        public double GlobalShortestDistance { get => global_shortest_distance; }

        /// <summary>获取当前代中的最优解</summary>
        public double PopulationShortestDistance { get => population_shortest_distance; }

        /// <summary>获取所有代中的最短路径</summary>
        public List<int> GlobalShortestRoute { get => global_shortest_route; }

        /// <summary>获取所有代中的最短路径</summary>
        public List<int> PopulationShortestRoute { get => population_shortest_route; }

        public GeneticCore()
        {
            global_shortest_distance = double.MaxValue;
            random = new Random(DateTime.Now.GetHashCode()); //根据当前时间生成随机数种子
        }

        public void SetValue(int pop_size, double cross_prob, double trans_prob, double new_car_prob)
        {
            population_size = pop_size;
            cross_probability = cross_prob;
            transform_probability = trans_prob;
            new_car_probability = new_car_prob;
        }

        /// <summary>
        /// 初始化种群
        /// </summary>
        public void InitializePopulation()
        {
            population = new List<List<int>>();
            List<int> meta_individual = new List<int>();    //用于生成其他个体的模板——元个体
            for (int i = 1; i <= meta.AshbinNum; i++) meta_individual.Add(i);
            for (int i = 0; i < population_size; i++)
            {
                List<int> individual = meta_individual;
                int exchange_count = 0;
                while (exchange_count++ < meta.AshbinNum * 2)   //打乱代表路径的编码数组
                {
                    int first = random.Next(meta.AshbinNum);
                    int second = random.Next(meta.AshbinNum);
                    while (first == second) second = random.Next(meta.AshbinNum);
                    int exchange = individual[first];
                    individual[first] = individual[second];
                    individual[second] = exchange;
                }
                //population.Add(individual);
                population.Add(new List<int>());
                for (int j = 0; j < individual.Count; j++) population[i].Add(individual[j]);
            }
        }

        /// <summary>
        /// 为种群中的个体添加“起点B”和“终点A”，构成完整的总规划路径。
        /// “起点B”和“终点A”分别用-1和0来表示。
        /// 单趟路线由-1节点或0节点出发，最终结束后到达0。
        /// 由哪个节点出发取决于卡车的总数量和发新车的概率。由类中的相关private变量定义。
        /// </summary>
        /// <param name="individual">种群中的一个个体。</param>
        /// <returns>返回的是已经分割好单趟路线的个体。</returns>
        private List<int> AddOriginTarget(List<int> individual)
        {
            List<int> route = new List<int>();
            int truck_used = 1;
            int capacity_used = 0;

            route.Add(-1);

            for (int i = 0; i < individual.Count; i++)
            {
                if (capacity_used + meta[individual[i]].demand <= meta.Capacity)
                {
                    route.Add(individual[i]);
                    capacity_used += meta[individual[i]].demand;
                }
                else
                {
                    route.Add(0);
                    /*  判定：
                     *  若已用卡车数量未达到上限，则有预设的发车概率（50%？）从起点B派出一辆新的卡车。
                     *  若派出一辆新卡车，则在节点编码0后面再添加一个-1.视作开辟一条新线路。
                     *  否则则在节点编码0后面再添加一个0,视为已使用过的一辆卡车再从终点A折回产生新线路。
                    */
                    if ((truck_used < meta.TruckNum) && (random.NextDouble() < new_car_probability)) //发一辆新车
                    {
                        route.Add(-1);
                        truck_used++;
                    }
                    else
                    {
                        route.Add(0);
                    }
                    route.Add(individual[i]);
                    capacity_used = meta[individual[i]].demand;
                }
            }
            route.Add(0);

            return route;
        }

        /// <summary>
        /// 计算单个个体的路程总长。
        /// </summary>
        /// <param name="route">传入一个已经分割好每趟单程的个体。</param>
        /// <returns>返回该个体总的路程长度。</returns>
        private double CaculateRouteDistance(List<int> route)
        {
            //List<int> route = AddOriginTarget(individual);
            Double total_distance = 0;
            for (int i = 1; i < route.Count; i++)
            {
                total_distance += Distance(meta[route[i]], meta[route[i - 1]]);
                if (route[i] == 0) i++; //遇0截断，算作是单独的一趟
            }

            return total_distance;
        }

        /// <summary>
        /// 为计算距离的lambda表达式提供委托
        /// </summary>
        /// <param name="a">第一个点</param>
        /// <param name="b">第二个点</param>
        /// <returns>两点间距离</returns>
        delegate double TwoPoints(Vertex a, Vertex b);
        /// <summary>计算两点间距离的lambda表达式，用Delegate封装。</summary>
        private TwoPoints Distance = (Vertex a, Vertex b) =>
            Math.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));

        public void CaculateFitness()
        {
            List<int> route = new List<int>();
            population_fitness = 0;
            individuals_fitness = new List<double>();
            population_shortest_distance = double.MaxValue;

            for (int i = 0; i < population_size; i++)
            {
                route = AddOriginTarget(population[i]);
                double individual_distance = CaculateRouteDistance(route);
                if (individual_distance < population_shortest_distance)
                {
                    population_shortest_distance = individual_distance;
                    population_best_individual = population[i];
                    population_shortest_route = route;
                }
                if (individual_distance < global_shortest_distance)
                {
                    global_shortest_distance = individual_distance;
                    global_best_individual = population[i];
                    global_shortest_route = route;
                }
                double individual_fitness = 1 / individual_distance;    //适应度越高越好，因此为最短距离的倒数
                individuals_fitness.Add(individual_fitness);
                population_fitness += individual_fitness;
            }
        }

        public void SelectChildren()
        {
            //计算累计概率
            List<double> accumulate_probability = new List<double>();
            double probability = individuals_fitness[0] / population_fitness;
            accumulate_probability.Add(probability);
            for (int i = 1; i < population_size; i++)
            {
                probability = individuals_fitness[i] / population_fitness;
                accumulate_probability.Add(probability + accumulate_probability[i - 1]);
            }

            //赌轮盘决定下代个体
            List<List<int>> next_population = new List<List<int>>();
            double roll;
            for (int i = 0; i < population_size - 1; i++)
            {
                roll = random.NextDouble();
                for (int j = 0; j < population_size; j++)
                {
                    if (roll <= accumulate_probability[j])
                    {
                        //next_population.Add(population[j]);
                        next_population.Add(new List<int>());
                        for (int k = 0; k < population[j].Count; k++) next_population[i].Add(population[j][k]);
                        break;
                    }
                }
            }

            //复制对应概率的最优个体进行选种
            for (int i = 0; i < population_size; i++)
            {
                if (random.NextDouble() < 0.25)
                {
                    List<int> best = new List<int>();
                    for (int j = 0; j < population_best_individual.Count; j++) best.Add(population_best_individual[j]);
                    next_population[i] = best;
                }
            }

            //解引用前先清空旧对象，减少动态堆的内存占用，降低GC频率
            population.Clear();
            population = next_population;
            individuals_fitness.Clear();
        }

        //NovelCross
        public void IndividualsTransform()
        {
            //交配
            for (int i = 0; i < population_size - 2; i += 2)
            {
                if (random.NextDouble() < cross_probability)
                {
                    int first_cross = random.Next(meta.AshbinNum);
                    int second_cross = random.Next(meta.AshbinNum);
                    while (first_cross == second_cross) second_cross = random.Next(meta.AshbinNum);
                    if (first_cross > second_cross)
                    {
                        int exchange = first_cross;
                        first_cross = second_cross;
                        second_cross = exchange;
                    }
                    List<int> first_segment = new List<int>();
                    List<int> second_segment = new List<int>();
                    for (int j = first_cross; j < second_cross; j++)
                    {
                        first_segment.Add(population[i + 1][j]);
                        second_segment.Add(population[i][j]);
                    }
                    for (int j = 0; j < first_segment.Count; j++)
                    {
                        population[i].Remove(first_segment[j]);
                        population[i + 1].Remove(second_segment[j]);
                    }
                    first_segment.AddRange(population[i]);
                    second_segment.AddRange(population[i + 1]);
                    population[i] = first_segment;
                    population[i + 1] = second_segment;
                }
            }
            //变异
            for (int i = 0; i < population_size - 1; i++)
            {
                if (random.NextDouble() < transform_probability)
                {
                    int first_node = random.Next(meta.AshbinNum);
                    int second_node = random.Next(meta.AshbinNum);
                    while (first_node == second_node) second_node = random.Next(meta.AshbinNum);
                    if (first_node > second_node)
                    {
                        int exchange = first_node;
                        first_node = second_node;
                        second_node = exchange;
                    }
                    population[i].Reverse(first_node, second_node - first_node + 1);
                }
            }

        }

        [Obsolete("我可……去你妈的吧")]
        //InsvreOver
        public void OidIndividualsTransform()
        {
            for (int i = 0; i < population_size; i++)
            {
                int first_vertex = random.Next(1, meta.AshbinNum + 1);
                int first_index = population[i].IndexOf(first_vertex);
                int second_vertex;
                int second_index;

                if (random.NextDouble() < transform_probability)
                {
                    do second_vertex = random.Next(1, meta.AshbinNum + 1); while (first_vertex == second_vertex);
                }
                else
                {
                    int another_individual;
                    do another_individual = random.Next(0, population_size); while (i == another_individual);
                    int another_index = population[another_individual].IndexOf(first_vertex);
                    if (another_index == meta.AshbinNum - 1)
                        second_vertex = population[another_individual][another_index - 1];
                    else second_vertex = population[another_individual][another_index + 1];

                }
                second_index = population[i].IndexOf(second_vertex);

                if (first_index > second_index)
                {
                    int exchange = first_index;
                    first_index = second_index;
                    second_index = exchange;
                }

                population[i].Reverse(first_index + 1, second_index - first_index);
            }
        }

    }
}
