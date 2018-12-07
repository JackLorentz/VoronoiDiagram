using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoronoiDiagram
{
    class Voronoi
    {
        //存檔用
        private List<Vertex> vertices = new List<Vertex>();
        //以下才是計算voronoi diagram用到的DS
        public List<Edge> HPs = new List<Edge>();
        private int convex_hull_hps_cnt = 0;
        private int hp_cnt = 0;
        private int prev_hp_cnt = 0;//計數每一個step要畫的HP,避免上個step的直線畫進該step範圍
        //step by step需要
        public bool pause = true;
        public List<Color> palette = new List<Color>();
        private int palette_cnt = 0;
        //
        private int num;
        public int Num
        {
            get
            {
                return num;
            }
            set
            {
                this.num = value;
            }
        }

        public Voronoi(int num)
        {
            this.num = num;
        }

        public void setVertex(int x, int y)
        {
            Vertex vertex = new Vertex();
            vertex.x = x;
            vertex.y = y;
            vertices.Add(vertex);
        }

        public Vertex getVertex(int index)
        {
            return vertices[index];
        }

        public void sort()
        {
            this.vertices = vertices.OrderBy(o => o.x).ToList();
            this.vertices = y_sorting(vertices);
        }

        public void step_by_step(List<Vertex> points, int left, int right, Graphics canvas, Pen pen, int left_boundary, int right_boundary)
        {
            //若只有一個點
            if (left == right)
            {
                List<Point> shadePoints = new List<Point>();
                shadePoints.Add(new Point(left_boundary, 0));
                shadePoints.Add(new Point(left_boundary, 600));
                shadePoints.Add(new Point(right_boundary, 600));
                shadePoints.Add(new Point(right_boundary, 0));
                canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                palette_cnt++;
                //畫點
                for (int i = 0; i < points.Count; i++)
                {
                    if (i <= right && i >= left)
                    {
                        canvas.FillRectangle(new SolidBrush(Color.White), points[i].x, points[i].y, 2, 2);
                    }
                    else
                    {
                        canvas.FillRectangle(new SolidBrush(Color.Black), points[i].x, points[i].y, 2, 2);
                    }
                }
                while (pause) { }
                pause = true;

                return ;
            }
            //考慮所有點(兩點以上)共線
            else if (is_all_on_a_line(points) || is_all_on_horizontal_line(points) || is_all_on_vertical_line(points))
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Edge edge = find_mid_line(points[i], points[i + 1]);
                    edge.left_polygon = left;
                    edge.right_polygon = right;
                    edge.start_vertex.infinity = true;
                    edge.end_vertex.infinity = true;
                    edge.is_line = true;
                    edge.index = hp_cnt;
                    this.HPs.Add(edge);
                    this.hp_cnt++;
                }
                //
                if(points.Count == 2)
                {
                    draw(canvas, pen, points, left_boundary, right_boundary, left, right);
                }
                else
                {
                    for(int i=0; i<HPs.Count; i++)
                    {
                        if (HPs[i].vector_x == 0)
                        {
                            int x1 = get_x_of_line(HPs[i], 0);
                            int x2 = get_x_of_line(HPs[i], 600);
                            canvas.DrawLine(pen, x1, 0, x2, 600);
                            HPs[i].start_vertex.x = x1;
                            HPs[i].start_vertex.y = 0;
                            HPs[i].end_vertex.x = x2;
                            HPs[i].end_vertex.y = 600;
                        }
                        else
                        {
                            int y1 = get_y_of_line(HPs[i], left_boundary);
                            int y2 = get_y_of_line(HPs[i], right_boundary);
                            canvas.DrawLine(pen, left_boundary, y1, right_boundary, y2);
                            HPs[i].start_vertex.x = left_boundary;
                            HPs[i].start_vertex.y = y1;
                            HPs[i].end_vertex.x = right_boundary;
                            HPs[i].end_vertex.y = y2;
                        }
                    }
                    
                    List<Point> shadePoints = new List<Point>();
                    if (is_all_on_horizontal_line(points))
                    {
                        //最左邊
                        shadePoints.Add(new Point(left_boundary, 0));
                        shadePoints.Add(new Point(left_boundary, 600));
                        shadePoints.Add(new Point(HPs[0].end_vertex.x, HPs[0].end_vertex.y));
                        shadePoints.Add(new Point(HPs[0].start_vertex.x, HPs[0].start_vertex.y));
                        canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                        palette_cnt++;
                        shadePoints.Clear();
                        for (int i = 1; i < HPs.Count; i++)
                        {
                            shadePoints.Add(new Point(HPs[i - 1].start_vertex.x, HPs[i - 1].start_vertex.y));
                            shadePoints.Add(new Point(HPs[i - 1].end_vertex.x, HPs[i - 1].end_vertex.y));
                            shadePoints.Add(new Point(HPs[i].end_vertex.x, HPs[i].end_vertex.y));
                            shadePoints.Add(new Point(HPs[i].start_vertex.x, HPs[i].start_vertex.y));
                            canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                            palette_cnt++;
                            shadePoints.Clear();
                        }
                        //最右邊
                        shadePoints.Add(new Point(right_boundary, 0));
                        shadePoints.Add(new Point(right_boundary, 600));
                        shadePoints.Add(new Point(HPs[HPs.Count-1].end_vertex.x, HPs[HPs.Count-1].end_vertex.y));
                        shadePoints.Add(new Point(HPs[HPs.Count-1].start_vertex.x, HPs[HPs.Count-1].start_vertex.y));
                        canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                        palette_cnt++;
                    }
                    else
                    {
                        //下半邊
                        shadePoints.Add(new Point(left_boundary, 0));
                        shadePoints.Add(new Point(right_boundary, 0));
                        shadePoints.Add(new Point(HPs[0].end_vertex.x, HPs[0].end_vertex.y));
                        shadePoints.Add(new Point(HPs[0].start_vertex.x, HPs[0].start_vertex.y));
                        canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                        palette_cnt++;
                        shadePoints.Clear();
                        for (int i = 1; i < HPs.Count; i++)
                        {
                            shadePoints.Add(new Point(HPs[i - 1].start_vertex.x, HPs[i - 1].start_vertex.y));
                            shadePoints.Add(new Point(HPs[i - 1].end_vertex.x, HPs[i - 1].end_vertex.y));
                            shadePoints.Add(new Point(HPs[i].end_vertex.x, HPs[i].end_vertex.y));
                            shadePoints.Add(new Point(HPs[i].start_vertex.x, HPs[i].start_vertex.y));
                            canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                            palette_cnt++;
                            shadePoints.Clear();
                        }
                        //上半邊
                        shadePoints.Add(new Point(left_boundary, 600));
                        shadePoints.Add(new Point(right_boundary, 600));
                        shadePoints.Add(new Point(HPs[HPs.Count - 1].end_vertex.x, HPs[HPs.Count - 1].end_vertex.y));
                        shadePoints.Add(new Point(HPs[HPs.Count - 1].start_vertex.x, HPs[HPs.Count - 1].start_vertex.y));
                        canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                        palette_cnt++;
                    }
                    //畫點
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (i <= right && i >= left)
                        {
                            canvas.FillRectangle(new SolidBrush(Color.White), points[i].x, points[i].y, 2, 2);
                        }
                        else
                        {
                            canvas.FillRectangle(new SolidBrush(Color.Black), points[i].x, points[i].y, 2, 2);
                        }
                    }
                    //畫邊
                    for (int i = 0; i < HPs.Count; i++)
                    {
                        canvas.DrawLine(pen, HPs[i].start_vertex.x, HPs[i].start_vertex.y, HPs[i].end_vertex.x, HPs[i].end_vertex.y);
                    }
                }
                while (pause) { }
                pause = true;
                prev_hp_cnt += hp_cnt;

                return ;
            }
            else
            {
                //Spilt & Recursive
                int x_mid = 0;
                int mid = 0;
                //若兩點不在同一垂直線上
                if (points[left].x != points[right].x)
                {
                    x_mid = (int)((double)(points[left].x + points[right].x) / (double)2);
                    mid = (int)((double)(left + right) / (double)2);
                    step_by_step(points, left, mid, canvas, pen, left_boundary, x_mid);
                    step_by_step(points, mid + 1, right, canvas, pen, x_mid, right_boundary);
                }
                //Merge兩個Convex hull(left和right各代表一個convex hull)
                if (right - left == 1)
                {
                    Edge edge = find_mid_line(points[left], points[right]);
                    edge.left_polygon = left;
                    edge.right_polygon = right;
                    edge.index = hp_cnt;
                    edge.is_line = true;
                    this.HPs.Add(edge);
                    this.hp_cnt++;
                    //
                    draw(canvas, pen, points, left_boundary, right_boundary, left, right);
                    while (pause) { }
                    pause = true;
                    prev_hp_cnt ++;
                }
                else
                {
                    //因為外公切邊不一定只有一個(要找最短的)
                    List<Edge> upper_edge = new List<Edge>();
                    List<Edge> lower_edge = new List<Edge>();
                    //找出左右兩邊的convex hull
                    int[] l_convex_hull = find_convex_hull(points, left, mid);
                    int[] r_convex_hull = find_convex_hull(points, mid + 1, right);
                    //找出兩個convex hull的外公切線 
                    //上面的公切線: 若所有點都低於某兩點(左右Convex hull各一點)相連的直線,則該線為最上面的外公切線
                    for (int i = 0; i < l_convex_hull.Length; i++)
                    {
                        for (int j = 0; j < r_convex_hull.Length; j++)
                        {
                            Edge edge = new Edge();
                            edge.start_vertex = points[l_convex_hull[i]];
                            edge.end_vertex = points[r_convex_hull[j]];
                            edge.vector_x = edge.end_vertex.x - edge.start_vertex.x;
                            edge.vector_y = edge.end_vertex.y - edge.start_vertex.y;

                            if (find_uppest_tangent(points, edge, left, right))
                            {
                                upper_edge.Add(edge);
                            }
                        }
                    }
                    int shortest_upper_edge = find_shortest_edge(upper_edge);
                    //下面的公切線
                    for (int i = 0; i < l_convex_hull.Length; i++)
                    {
                        for (int j = 0; j < r_convex_hull.Length; j++)
                        {
                            Edge edge = new Edge();
                            edge.start_vertex = points[l_convex_hull[i]];
                            edge.end_vertex = points[r_convex_hull[j]];
                            edge.vector_x = edge.end_vertex.x - edge.start_vertex.x;
                            edge.vector_y = edge.end_vertex.y - edge.start_vertex.y;

                            if (find_lowest_tangent(points, edge, left, right))
                            {
                                lower_edge.Add(edge);
                            }
                        }
                    }
                    int shortest_lower_edge = find_shortest_edge(lower_edge);
                    //開始Merge
                    //暫存HPs
                    List<Edge> tmp_HPs = new List<Edge>();
                    //SG = xy(最上面且最短的外公切線)
                    Edge SG = upper_edge[shortest_upper_edge];
                    //建立候選點
                    List<Vertex> candidates = new List<Vertex>();
                    //暫存點
                    Vertex tmp_p = new Vertex();
                    //最後的HP要保存
                    Edge final = new Edge();
                    //畫voronoi diagram用
                    bool is_cw_assigned = false;
                    int next_cw_predecessor = -1, cw = 0;
                    while (!is_edge_equal(SG, lower_edge[shortest_lower_edge]))
                    {
                        if (SG.start_vertex.x > SG.end_vertex.x)
                        {
                            Vertex t = assign_vertex(SG.end_vertex);
                            SG.end_vertex = assign_vertex(SG.start_vertex);
                            SG.start_vertex = assign_vertex(t);
                        }
                        else if (SG.start_vertex.x == SG.end_vertex.x && SG.start_vertex.y > SG.end_vertex.y)
                        {
                            Vertex t = assign_vertex(SG.end_vertex);
                            SG.end_vertex = assign_vertex(SG.start_vertex);
                            SG.start_vertex = assign_vertex(t);
                        }
                        candidates.Clear();
                        for (int i = left; i <= right; i++)
                        {
                            Vertex t = new Vertex();
                            t.x = points[i].x;
                            t.y = points[i].y;
                            candidates.Add(t);
                        }
                        //上一個三角形的點都不能找
                        pop(candidates, SG.start_vertex);
                        pop(candidates, SG.end_vertex);
                        pop(candidates, tmp_p);
                        //找出SG中線(BS)
                        Edge BS = find_mid_line(SG.start_vertex, SG.end_vertex);
                        BS.left_polygon = find_points(points, SG.start_vertex);
                        BS.right_polygon = find_points(points, SG.end_vertex);
                        //畫voronoi diagram用
                        BS.index = hp_cnt;
                        BS.cw_predecessor = next_cw_predecessor;
                        //選出z點: 最近的外心
                        Vertex z = new Vertex();
                        for (int i = 0; i < candidates.Count; i++)
                        {
                            //如果候選點在SG上(三點共線)則不行
                            if (is_point_on_line(SG, candidates[i]))
                            {
                                continue;
                            }
                            Edge candidate_edge = new Edge();
                            candidate_edge.start_vertex = SG.end_vertex;
                            candidate_edge.end_vertex = candidates[i];
                            candidate_edge.vector_x = SG.end_vertex.x - candidates[i].x;
                            candidate_edge.vector_y = SG.end_vertex.y - candidates[i].y;
                            if (find_z_point(candidates, BS, candidates[i], find_mid_line(candidate_edge.start_vertex, candidate_edge.end_vertex)))
                            {
                                z = candidates[i];
                                break;
                            }
                        }
                        //第三邊: 由HP的位置決定
                        Edge e = new Edge();
                        //HP必須跨左右convex hull
                        //若z為左convex hull, 則必須跟右convex hull的點連線
                        Edge HP = find_mid_line(SG.end_vertex, z);
                        //邊方向: X軸負方向為左向, 正方向為右向
                        HP.left_polygon = find_points(points, SG.end_vertex);
                        HP.right_polygon = find_points(points, z);
                        e.start_vertex = assign_vertex(z);
                        e.end_vertex = assign_vertex(SG.start_vertex);
                        //下個迭代不能用
                        tmp_p = assign_vertex(SG.start_vertex);
                        //若z為右convex hull, 則必須跟左convex hull的點連線
                        if (is_in_convex_hull(points, r_convex_hull, z))
                        {
                            HP = find_mid_line(SG.start_vertex, z);
                            HP.left_polygon = find_points(points, z);
                            HP.right_polygon = find_points(points, SG.start_vertex);
                            e.start_vertex = assign_vertex(SG.end_vertex);
                            e.end_vertex = assign_vertex(z);
                            //畫voronoi diagram用
                            BS.cw_successor = hp_cnt + 1;
                            is_cw_assigned = true;
                            cw = hp_cnt;
                            //下個迭代不能用
                            tmp_p = assign_vertex(SG.end_vertex);
                        }
                        //找出BS和yz(或xz)的HP(以下簡稱HP)的交點
                        Vertex intersect = find_intersect(BS, HP);
                        //設定2個邊: HP, BS
                        BS.end_vertex = assign_vertex(BS.start_vertex);
                        BS.start_vertex = assign_vertex(intersect);
                        if (!is_vertex_equal(BS.end_vertex, BS.start_vertex))
                        {
                            BS.vector_x = BS.end_vertex.x - BS.start_vertex.x;
                            BS.vector_y = BS.end_vertex.y - BS.start_vertex.y;
                        }
                        BS.start_vertex.infinity = false;
                        BS.end_vertex.infinity = true;
                        //
                        HP.end_vertex = assign_vertex(HP.start_vertex);
                        HP.start_vertex = assign_vertex(intersect);
                        if (!is_vertex_equal(HP.start_vertex, HP.end_vertex))
                        {
                            HP.vector_x = HP.end_vertex.x - HP.start_vertex.x;
                            HP.vector_y = HP.end_vertex.y - HP.start_vertex.y;
                        }
                        HP.start_vertex.infinity = false;
                        HP.end_vertex.infinity = true;
                        //若是直角三角形, 檢查校正向量方向
                        check_if_right_triangle(BS, HP);
                        tmp_HPs.Add(BS);
                        //tmp_HPs.Add(HP);
                        //算第三個邊, 並檢查是否需要跟前一次迭代結果的邊合併
                        Edge e_HP = find_mid_line(e.start_vertex, e.end_vertex);
                        e_HP.left_polygon = find_points(points, e.start_vertex);
                        e_HP.right_polygon = find_points(points, e.end_vertex);
                        e_HP.end_vertex = assign_vertex(e_HP.start_vertex);
                        e_HP.start_vertex = assign_vertex(intersect);
                        if (!is_vertex_equal(e_HP.start_vertex, e_HP.end_vertex))
                        {
                            e_HP.vector_x = e_HP.end_vertex.x - e_HP.start_vertex.x;
                            e_HP.vector_y = e_HP.end_vertex.y - e_HP.start_vertex.y;
                        }
                        //若是直角三角形, 檢查校正向量方向
                        check_if_right_triangle(BS, e_HP);
                        //若是鈍角三角形, 檢查校正向量方向
                        if (kind_of_triangle(SG.start_vertex, SG.end_vertex, z) == 3)
                        {
                            //用第三邊中線來判斷是否鈍角
                            check_if_obtuse_triangle(BS, HP, e_HP);
                        }
                        if (!is_cw_assigned)
                        {
                            cw = hp_cnt + 1;
                        }
                        int prev = merge_previous(e_HP, cw);
                        //畫voronoi diagram用
                        if (!is_cw_assigned)
                        {
                            BS.cw_successor = prev;
                            next_cw_predecessor = hp_cnt;
                        }
                        else
                        {
                            next_cw_predecessor = prev;
                        }
                        //HP左右兩點即為下個迭代的SG
                        Edge next = new Edge();
                        next.start_vertex = assign_vertex(points[HP.left_polygon]);
                        next.end_vertex = assign_vertex(points[HP.right_polygon]);
                        next.vector_x = next.end_vertex.x - next.start_vertex.x;
                        next.vector_y = next.end_vertex.y - next.start_vertex.y;
                        SG = next;
                        final = HP;
                        if (!is_cw_assigned)
                        {
                            final.cw_successor = hp_cnt;
                        }
                        else
                        {
                            final.cw_successor = prev;
                        }
                        hp_cnt++;
                        is_cw_assigned = false;
                    }
                    //從最上面的交點開始去連到最近的交點,並一路連到最下面的交點
                    for (int i = 1; i < tmp_HPs.Count; i++)
                    {
                        tmp_HPs[i].end_vertex = assign_vertex(tmp_HPs[i - 1].start_vertex);
                        tmp_HPs[i].end_vertex.infinity = false;
                    }
                    //
                    for (int i = 0; i < tmp_HPs.Count; i++)
                    {
                        //tmp_HPs[i].index = hp_cnt;
                        HPs.Add(tmp_HPs[i]);
                        //hp_cnt++;
                    }
                    final.index = hp_cnt;
                    HPs.Add(final);
                    hp_cnt++;
                    //
                    canvas.Clear(Color.White);
                    int tmp_hp_cnt = prev_hp_cnt;
                    prev_hp_cnt = 0;
                    draw(canvas, pen, points, left_boundary, right_boundary, left, right);
                    while (pause) { }
                    pause = true;
                    prev_hp_cnt = tmp_hp_cnt + tmp_HPs.Count + 1;
                }
            }
        }

        //points一定要lexical order
        //left: 第0個點 ; right: 最後一個點(不是List的長度喔)
        public bool run(List<Vertex> points, int left, int right)
        {
            //若只有一個點
            if (left == right)
            {
                return false;
            }
            //先考慮三點共線
            else if (is_all_on_a_line(points) || is_all_on_horizontal_line(points) || is_all_on_vertical_line(points))
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Edge edge = find_mid_line(points[i], points[i + 1]);
                    edge.left_polygon = left;
                    edge.right_polygon = right;
                    edge.start_vertex.infinity = true;
                    edge.end_vertex.infinity = true;
                    edge.is_line = true;
                    edge.index = hp_cnt;
                    this.HPs.Add(edge);
                    this.hp_cnt++;
                }
                return true;
            }
            else
            {
                //Spilt & Recursive
                //找中線做分割
                //int x_mid = 0;
                int mid = 0;
                //若兩點不在同一垂直線上
                if (points[left].x != points[right].x)
                {
                    //x_mid = (int)((double)(points[left].x + points[right].x) / (double)2);
                    mid = (int)((double)(left + right) / (double)2);
                    run(points, left, mid);
                    run(points, mid + 1, right);
                }
                //Merge兩個Convex hull(left和right各代表一個convex hull)
                if (right - left == 1)
                {
                    Edge edge = find_mid_line(points[left], points[right]);
                    edge.left_polygon = left;
                    edge.right_polygon = right;
                    edge.index = hp_cnt;
                    this.HPs.Add(edge);
                    this.hp_cnt++;
                }
                else
                {
                    //因為外公切邊不一定只有一個(要找最短的)
                    List<Edge> upper_edge = new List<Edge>();
                    List<Edge> lower_edge = new List<Edge>();
                    //找出左右兩邊的convex hull
                    int[] l_convex_hull = find_convex_hull(points, left, mid);
                    int[] r_convex_hull = find_convex_hull(points, mid + 1, right);
                    //找出兩個convex hull的外公切線 
                    //上面的公切線: 若所有點都低於某兩點(左右Convex hull各一點)相連的直線,則該線為最上面的外公切線
                    for (int i = 0; i < l_convex_hull.Length; i++)
                    {
                        for (int j = 0; j < r_convex_hull.Length; j++)
                        {
                            Edge edge = new Edge();
                            edge.start_vertex = points[l_convex_hull[i]];
                            edge.end_vertex = points[r_convex_hull[j]];
                            edge.vector_x = edge.end_vertex.x - edge.start_vertex.x;
                            edge.vector_y = edge.end_vertex.y - edge.start_vertex.y;

                            if (find_uppest_tangent(points, edge, left, right))
                            {
                                upper_edge.Add(edge);
                            }
                        }
                    }
                    int shortest_upper_edge = find_shortest_edge(upper_edge);
                    //下面的公切線
                    for (int i = 0; i < l_convex_hull.Length; i++)
                    {
                        for (int j = 0; j < r_convex_hull.Length; j++)
                        {
                            Edge edge = new Edge();
                            edge.start_vertex = points[l_convex_hull[i]];
                            edge.end_vertex = points[r_convex_hull[j]];
                            edge.vector_x = edge.end_vertex.x - edge.start_vertex.x;
                            edge.vector_y = edge.end_vertex.y - edge.start_vertex.y;

                            if (find_lowest_tangent(points, edge, left, right))
                            {
                                lower_edge.Add(edge);
                            }
                        }
                    }
                    int shortest_lower_edge = find_shortest_edge(lower_edge);
                    //開始Merge
                    //暫存HPs
                    List<Edge> tmp_HPs = new List<Edge>();
                    //SG = xy(最上面且最短的外公切線)
                    Edge SG = upper_edge[shortest_upper_edge];
                    //建立候選點
                    List<Vertex> candidates = new List<Vertex>();
                    //暫存點
                    Vertex tmp_p = new Vertex();
                    //最後的HP要保存
                    Edge final = new Edge();
                    while (!is_edge_equal(SG, lower_edge[shortest_lower_edge]))
                    {
                        if (SG.start_vertex.x > SG.end_vertex.x)
                        {
                            Vertex t = assign_vertex(SG.end_vertex);
                            SG.end_vertex = assign_vertex(SG.start_vertex);
                            SG.start_vertex = assign_vertex(t);
                        }
                        else if (SG.start_vertex.x == SG.end_vertex.x && SG.start_vertex.y > SG.end_vertex.y)
                        {
                            Vertex t = assign_vertex(SG.end_vertex);
                            SG.end_vertex = assign_vertex(SG.start_vertex);
                            SG.start_vertex = assign_vertex(t);
                        }
                        candidates.Clear();
                        for (int i = left; i <= right; i++)
                        {
                            Vertex t = new Vertex();
                            t.x = points[i].x;
                            t.y = points[i].y;
                            candidates.Add(t);
                        }
                        //上一個三角形的點都不能找
                        pop(candidates, SG.start_vertex);
                        pop(candidates, SG.end_vertex);
                        pop(candidates, tmp_p);
                        //找出SG中線(BS)
                        Edge BS = find_mid_line(SG.start_vertex, SG.end_vertex);
                        BS.left_polygon = find_points(points, SG.start_vertex);
                        BS.right_polygon = find_points(points, SG.end_vertex);
                        //選出z點: 最近的外心
                        Vertex z = new Vertex();
                        for (int i = 0; i < candidates.Count; i++)
                        {
                            //如果候選點在SG上(三點共線)則不行
                            if (is_point_on_line(SG, candidates[i]))
                            {
                                continue;
                            }
                            Edge candidate_edge = new Edge();
                            candidate_edge.start_vertex = SG.end_vertex;
                            candidate_edge.end_vertex = candidates[i];
                            candidate_edge.vector_x = SG.end_vertex.x - candidates[i].x;
                            candidate_edge.vector_y = SG.end_vertex.y - candidates[i].y;
                            if (find_z_point(candidates, BS, candidates[i], find_mid_line(candidate_edge.start_vertex, candidate_edge.end_vertex)))
                            {
                                z = candidates[i];
                                break;
                            }
                        }
                        //第三邊: 由HP的位置決定
                        Edge e = new Edge();
                        //HP必須跨左右convex hull
                        //若z為左convex hull, 則必須跟右convex hull的點連線
                        Edge HP = find_mid_line(SG.end_vertex, z);
                        //邊方向: X軸負方向為左向, 正方向為右向
                        HP.left_polygon = find_points(points, SG.end_vertex);
                        HP.right_polygon = find_points(points, z);
                        e.start_vertex = assign_vertex(z);
                        e.end_vertex = assign_vertex(SG.start_vertex);
                        //下個迭代不能用
                        tmp_p = assign_vertex(SG.start_vertex);
                        //若z為右convex hull, 則必須跟左convex hull的點連線
                        if (is_in_convex_hull(points, r_convex_hull, z))
                        {
                            HP = find_mid_line(SG.start_vertex, z);
                            HP.left_polygon = find_points(points, z);
                            HP.right_polygon = find_points(points, SG.start_vertex);
                            e.start_vertex = assign_vertex(SG.end_vertex);
                            e.end_vertex = assign_vertex(z);
                            //下個迭代不能用
                            tmp_p = assign_vertex(SG.end_vertex);
                        }
                        //找出BS和yz(或xz)的HP(以下簡稱HP)的交點
                        Vertex intersect = find_intersect(BS, HP);
                        //設定2個邊: HP, BS
                        BS.end_vertex = assign_vertex(BS.start_vertex);
                        BS.start_vertex = assign_vertex(intersect);
                        if (!is_vertex_equal(BS.end_vertex, BS.start_vertex))
                        {
                            BS.vector_x = BS.end_vertex.x - BS.start_vertex.x;
                            BS.vector_y = BS.end_vertex.y - BS.start_vertex.y;
                        }
                        BS.start_vertex.infinity = false;
                        BS.end_vertex.infinity = true;
                        //
                        HP.end_vertex = assign_vertex(HP.start_vertex);
                        HP.start_vertex = assign_vertex(intersect);
                        if (!is_vertex_equal(HP.start_vertex, HP.end_vertex))
                        {
                            HP.vector_x = HP.end_vertex.x - HP.start_vertex.x;
                            HP.vector_y = HP.end_vertex.y - HP.start_vertex.y;
                        }
                        HP.start_vertex.infinity = false;
                        HP.end_vertex.infinity = true;
                        //若是直角三角形, 檢查校正向量方向
                        check_if_right_triangle(BS, HP);
                        tmp_HPs.Add(BS);
                        //tmp_HPs.Add(HP);
                        //算第三個邊, 並檢查是否需要跟前一次迭代結果的邊合併
                        Edge e_HP = find_mid_line(e.start_vertex, e.end_vertex);
                        e_HP.left_polygon = find_points(points, e.start_vertex);
                        e_HP.right_polygon = find_points(points, e.end_vertex);
                        e_HP.end_vertex = assign_vertex(e_HP.start_vertex);
                        e_HP.start_vertex = assign_vertex(intersect);
                        if (!is_vertex_equal(e_HP.start_vertex, e_HP.end_vertex))
                        {
                            e_HP.vector_x = e_HP.end_vertex.x - e_HP.start_vertex.x;
                            e_HP.vector_y = e_HP.end_vertex.y - e_HP.start_vertex.y;
                        }
                        //若是直角三角形, 檢查校正向量方向
                        check_if_right_triangle(BS, e_HP);
                        //若是鈍角三角形, 檢查校正向量方向
                        if (kind_of_triangle(SG.start_vertex, SG.end_vertex, z) == 3)
                        {
                            //用第三邊中線來判斷是否鈍角
                            check_if_obtuse_triangle(BS, HP, e_HP);
                        }
                        merge_previous(e_HP, -1);
                        //HP左右兩點即為下個迭代的SG
                        Edge next = new Edge();
                        next.start_vertex = assign_vertex(points[HP.left_polygon]);
                        next.end_vertex = assign_vertex(points[HP.right_polygon]);
                        next.vector_x = next.end_vertex.x - next.start_vertex.x;
                        next.vector_y = next.end_vertex.y - next.start_vertex.y;
                        SG = next;
                        final = HP;
                    }
                    //從最上面的交點開始去連到最近的交點,並一路連到最下面的交點
                    for (int i = 1; i < tmp_HPs.Count; i++)
                    {
                        tmp_HPs[i].end_vertex = assign_vertex(tmp_HPs[i - 1].start_vertex);
                        tmp_HPs[i].end_vertex.infinity = false;
                    }
                    //
                    for (int i = 0; i < tmp_HPs.Count; i++)
                    {
                        tmp_HPs[i].index = hp_cnt;
                        HPs.Add(tmp_HPs[i]);
                        hp_cnt++;
                    }
                    final.index = hp_cnt;
                    HPs.Add(final);
                    hp_cnt++;
                }
            }
            return true;
        }
        //畫出現階段的convex hull
        public void draw(Graphics canvas, Pen pen, List<Vertex> points, int left_boundary, int right_boundary, int left, int right)
        {
            List<Edge> drawEdges = new List<Edge>();
            for(int i=prev_hp_cnt; i<HPs.Count; i++)
            {
                Edge e = new Edge();
                e.start_vertex = assign_vertex(HPs[i].start_vertex);
                e.end_vertex = assign_vertex(HPs[i].end_vertex);
                e.vector_x = HPs[i].vector_x;
                e.vector_y = HPs[i].vector_y;
                e.left_polygon = HPs[i].left_polygon;
                e.right_polygon = HPs[i].right_polygon;
                e.index = HPs[i].index;
                e.is_line = HPs[i].is_line;
                e.cw_successor = HPs[i].cw_successor;
                e.cw_predecessor = HPs[i].cw_predecessor;
                drawEdges.Add(e);
            }

            //先把邊畫出來
            //只有一個邊
            if (drawEdges.Count == 1)
            {
                if (drawEdges[0].vector_x == 0)
                {
                    int x1 = get_x_of_line(drawEdges[0], 0);
                    int x2 = get_x_of_line(drawEdges[0], 600);
                    canvas.DrawLine(pen, x1, 0, x2, 600);
                    drawEdges[0].start_vertex.x = x1;
                    drawEdges[0].start_vertex.y = 0;
                    drawEdges[0].end_vertex.x = x2;
                    drawEdges[0].end_vertex.y = 600;
                }
                else
                {
                    int y1 = get_y_of_line(drawEdges[0], left_boundary);
                    int y2 = get_y_of_line(drawEdges[0], right_boundary);
                    canvas.DrawLine(pen, left_boundary, y1, right_boundary, y2);
                    drawEdges[0].start_vertex.x = left_boundary;
                    drawEdges[0].start_vertex.y = y1;
                    drawEdges[0].end_vertex.x = right_boundary;
                    drawEdges[0].end_vertex.y = y2;
                }
            }
            //有多個邊
            else
            {
                //確認是否為矩形
                if (check_rectangle(drawEdges) && points.Count == 4)
                {
                    //把水平射線改成直線
                    for (int i = 0; i < drawEdges.Count; i++)
                    {
                        if (drawEdges[i].vector_x != 0 && drawEdges[i].vector_y == 0)
                        {
                            drawEdges[i].end_vertex.infinity = true;
                            drawEdges[i].start_vertex.infinity = true;
                            drawEdges[i].is_line = true;
                        }
                    }
                    //把非水平和垂直線去掉
                    bool is_finish = false;
                    //由於刪除元素後串列會縮短以至於可能會有該刪的沒刪到, 所以要一直檢查
                    while (!is_finish)
                    {
                        for (int j = 0; j < drawEdges.Count; j++)
                        {
                            if (drawEdges[j].vector_x != 0 && drawEdges[j].vector_y != 0)
                            {
                                is_finish = false;
                                drawEdges.RemoveAt(j);
                                break;
                            }
                            else
                            {
                                is_finish = true;
                            }
                        }
                    }
                }

                for (int i = 0; i < drawEdges.Count; i++)
                {
                    //射線畫線
                    if (drawEdges[i].end_vertex.infinity && !drawEdges[i].start_vertex.infinity)
                    {
                        if (drawEdges[i].vector_x == 0 && drawEdges[i].vector_y != 0)
                        {
                            if (drawEdges[i].end_vertex.y < drawEdges[i].start_vertex.y)
                            {
                                canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, drawEdges[i].start_vertex.x, 0);
                                drawEdges[i].end_vertex.x = drawEdges[i].start_vertex.x;
                                drawEdges[i].end_vertex.y = 0;
                            }
                            else
                            {
                                canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, drawEdges[i].start_vertex.x, 600);
                                drawEdges[i].end_vertex.x = drawEdges[i].start_vertex.x;
                                drawEdges[i].end_vertex.y = 600;
                            }
                        }
                        else if (drawEdges[i].vector_x != 0 && drawEdges[i].vector_y == 0)
                        {
                            if (drawEdges[i].end_vertex.x < drawEdges[i].start_vertex.x)
                            {
                                canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, left_boundary, drawEdges[i].start_vertex.y);
                                drawEdges[i].end_vertex.x = left_boundary;
                                drawEdges[i].end_vertex.y = drawEdges[i].start_vertex.y;
                            }
                            else
                            {
                                canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, right_boundary, drawEdges[i].start_vertex.y);
                                drawEdges[i].end_vertex.x = right_boundary;
                                drawEdges[i].end_vertex.y = drawEdges[i].start_vertex.y;
                            }
                        }
                        else
                        {
                            if (drawEdges[i].end_vertex.x < drawEdges[i].start_vertex.x && drawEdges[i].end_vertex.y < drawEdges[i].start_vertex.y)
                            {
                                int x = 0, y = 0;
                                if ((x = get_x_of_line(drawEdges[i], 0)) > left_boundary)
                                {
                                    canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, x, 0);
                                    drawEdges[i].end_vertex.x = x;
                                    drawEdges[i].end_vertex.y = 0;
                                }
                                else
                                {
                                    y = get_y_of_line(drawEdges[i], left_boundary);
                                    canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, left_boundary, y);
                                    drawEdges[i].end_vertex.x = left_boundary;
                                    drawEdges[i].end_vertex.y = y;
                                }
                            }
                            else if (drawEdges[i].end_vertex.x > drawEdges[i].start_vertex.x && drawEdges[i].end_vertex.y < drawEdges[i].start_vertex.y)
                            {
                                int x = 0, y = 0;
                                if ((x = get_x_of_line(drawEdges[i], 0)) < right_boundary)
                                {
                                    canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, x, 0);
                                    drawEdges[i].end_vertex.x = x;
                                    drawEdges[i].end_vertex.y = 0;
                                }
                                else
                                {
                                    y = get_y_of_line(drawEdges[i], right_boundary);
                                    canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, right_boundary, y);
                                    drawEdges[i].end_vertex.x = right_boundary;
                                    drawEdges[i].end_vertex.y = y;
                                }
                            }
                            else if (drawEdges[i].end_vertex.x < drawEdges[i].start_vertex.x && drawEdges[i].end_vertex.y > drawEdges[i].start_vertex.y)
                            {
                                int x = 0, y = 0;
                                if ((x = get_x_of_line(drawEdges[i], 600)) > left_boundary)
                                {
                                    canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, x, 600);
                                    drawEdges[i].end_vertex.x = x;
                                    drawEdges[i].end_vertex.y = 600;
                                }
                                else
                                {
                                    y = get_y_of_line(drawEdges[i], 0);
                                    canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, left_boundary, y);
                                    drawEdges[i].end_vertex.x = left_boundary;
                                    drawEdges[i].end_vertex.y = y;
                                }
                            }
                            else if (drawEdges[i].end_vertex.x > drawEdges[i].start_vertex.x && drawEdges[i].end_vertex.y > drawEdges[i].start_vertex.y)
                            {
                                int x = 0, y = 0;
                                if ((x = get_x_of_line(drawEdges[i], 600)) < right_boundary)
                                {
                                    canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, x, 600);
                                    drawEdges[i].end_vertex.x = x;
                                    drawEdges[i].end_vertex.y = 600;
                                }
                                else
                                {
                                    y = get_y_of_line(drawEdges[i], right_boundary);
                                    canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, right_boundary, y);
                                    drawEdges[i].end_vertex.x = right_boundary;
                                    drawEdges[i].end_vertex.y = y;
                                }
                            }
                        }
                    }
                    //線段畫線
                    else if (!drawEdges[i].end_vertex.infinity && !drawEdges[i].start_vertex.infinity)
                    {
                        canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, drawEdges[i].end_vertex.x, drawEdges[i].end_vertex.y);
                    }
                    //直線畫線
                    else if (drawEdges[i].end_vertex.infinity && drawEdges[i].start_vertex.infinity && drawEdges[i].is_line)
                    {
                        if (drawEdges[i].vector_x == 0)
                        {
                            int x1 = get_x_of_line(drawEdges[i], 0);
                            int x2 = get_x_of_line(drawEdges[i], 600);
                            canvas.DrawLine(pen, x1, 0, x2, 600);
                            drawEdges[0].start_vertex.x = x1;
                            drawEdges[0].start_vertex.y = 0;
                            drawEdges[0].end_vertex.x = x2;
                            drawEdges[0].end_vertex.y = 600;
                        }
                        else
                        {
                            int y1 = get_y_of_line(drawEdges[i], left_boundary);
                            int y2 = get_y_of_line(drawEdges[i], right_boundary);
                            canvas.DrawLine(pen, left_boundary, y1, right_boundary, y2);
                            drawEdges[0].start_vertex.x = left_boundary;
                            drawEdges[0].start_vertex.y = y1;
                            drawEdges[0].end_vertex.x = right_boundary;
                            drawEdges[0].end_vertex.y = y2;
                        }
                    }
                }
            }
            HPs = edges_lexcial_order(HPs);
            //先把各convex hull塗上顏色
            drawConvexHull(canvas, drawEdges, points, left_boundary, right_boundary);
            //畫點
            for (int i = 0; i < points.Count; i++)
            {
                if (i <= right && i >= left)
                {
                    canvas.FillRectangle(new SolidBrush(Color.White), points[i].x, points[i].y, 2, 2);
                }
                else
                {
                    canvas.FillRectangle(new SolidBrush(Color.Black), points[i].x, points[i].y, 2, 2);
                }
            }
            //畫邊
            for (int i=0; i<drawEdges.Count; i++)
            {
                canvas.DrawLine(pen, drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y, drawEdges[i].end_vertex.x, drawEdges[i].end_vertex.y);
            }
            //標示convex hull
            show_convex_hull(canvas, drawEdges, points);
        }

        public void drawConvexHull(Graphics canvas, List<Edge> drawEdges, List<Vertex> points, int left_boundary, int right_boundary)
        {
            List<Point> shadePoints = new List<Point>();
            if (drawEdges.Count == 1)
            {
                if (drawEdges[0].vector_x == 0 && drawEdges[0].vector_y != 0)
                {
                    //左半邊
                    shadePoints.Add(new Point(left_boundary, 0));
                    shadePoints.Add(new Point(left_boundary, 600));
                    shadePoints.Add(new Point(drawEdges[0].end_vertex.x, drawEdges[0].end_vertex.y));
                    shadePoints.Add(new Point(drawEdges[0].start_vertex.x, drawEdges[0].start_vertex.y));
                    canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                    palette_cnt++;
                    shadePoints.Clear();
                    //右半邊
                    shadePoints.Add(new Point(right_boundary, 0));
                    shadePoints.Add(new Point(right_boundary, 600));
                    shadePoints.Add(new Point(drawEdges[0].end_vertex.x, drawEdges[0].end_vertex.y));
                    shadePoints.Add(new Point(drawEdges[0].start_vertex.x, drawEdges[0].start_vertex.y));
                    canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                    palette_cnt++;
                }
                else
                {
                    //下半邊
                    shadePoints.Add(new Point(left_boundary, 0));
                    shadePoints.Add(new Point(right_boundary, 0));
                    shadePoints.Add(new Point(drawEdges[0].end_vertex.x, drawEdges[0].end_vertex.y));
                    shadePoints.Add(new Point(drawEdges[0].start_vertex.x, drawEdges[0].start_vertex.y));
                    canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                    palette_cnt++;
                    shadePoints.Clear();
                    //上半邊
                    shadePoints.Add(new Point(left_boundary, 600));
                    shadePoints.Add(new Point(right_boundary, 600));
                    shadePoints.Add(new Point(drawEdges[0].end_vertex.x, drawEdges[0].end_vertex.y));
                    shadePoints.Add(new Point(drawEdges[0].start_vertex.x, drawEdges[0].start_vertex.y));
                    canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                    palette_cnt++;
                }
            }
            else
            {
                int i, initial = -1;
                for(i=0; i<drawEdges.Count; i++)
                {
                    if (drawEdges[i].end_vertex.infinity)
                    {
                        initial = drawEdges[i].index;
                        break;
                    }
                }
                shadePoints.Add(new Point(drawEdges[i].end_vertex.x, drawEdges[i].end_vertex.y));
                shadePoints.Add(new Point(drawEdges[i].start_vertex.x, drawEdges[i].start_vertex.y));

                int draw_cnt = 0;
                int next = drawEdges[i].cw_successor;
                Vertex prev = assign_vertex(drawEdges[i].start_vertex);
                while(initial != next)
                {
                    int j;
                    for(j=0; j<drawEdges.Count; j++)
                    {
                        if(drawEdges[j].index == next)
                        {
                            break;
                        }
                    }
                    if (is_vertex_equal(drawEdges[j].start_vertex, prev))
                    {
                        shadePoints.Add(new Point(drawEdges[j].end_vertex.x, drawEdges[j].end_vertex.y));
                        prev = assign_vertex(drawEdges[j].end_vertex);
                    }
                    else
                    {
                        shadePoints.Add(new Point(drawEdges[j].start_vertex.x, drawEdges[j].start_vertex.y));
                        prev = assign_vertex(drawEdges[j].start_vertex);
                    }

                    if (drawEdges[j].end_vertex.infinity)
                    {
                        canvas.FillPolygon(new SolidBrush(palette[palette_cnt]), shadePoints.ToArray());
                        shadePoints.Clear();
                        palette_cnt++;

                        shadePoints.Add(new Point(drawEdges[j].end_vertex.x, drawEdges[j].end_vertex.y));
                        shadePoints.Add(new Point(drawEdges[j].start_vertex.x, drawEdges[j].start_vertex.y));
                        prev = assign_vertex(drawEdges[j].start_vertex);
                        next = drawEdges[j].cw_successor;
                        draw_cnt++;
                    }
                    else if(!drawEdges[j].end_vertex.infinity && draw_cnt % 2 == 1)
                    {
                        next = drawEdges[j].cw_predecessor;
                    }
                    else if (!drawEdges[j].end_vertex.infinity && draw_cnt % 2 == 0)
                    {
                        next = drawEdges[j].cw_successor;
                    }
                }
            }
        }

        public void show_convex_hull(Graphics canvas, List<Edge> drawEdges, List<Vertex> points)
        {
            Pen p = new Pen(Color.DarkGray);
            List<Point> convexHull = new List<Point>();
            int index = 0;
            int i;
            //先找到第一個邊
            for (i = 0; i < drawEdges.Count; i++)
            {
                if (HPs[i].end_vertex.infinity)
                {
                    convexHull.Add(new Point(points[drawEdges[i].left_polygon].x, points[drawEdges[i].left_polygon].y));
                    index++;
                    break;
                }
            }
            //表示正在搜尋的邊
            int j = 0;
            while (index < drawEdges.Count)
            {
                //找下個邊是射線且其左邊點是現在的右邊點
                if (HPs[i].end_vertex.infinity && HPs[j].left_polygon == HPs[i].right_polygon)
                {
                    convexHull.Add(new Point(points[drawEdges[i].right_polygon].x, points[drawEdges[i].right_polygon].y));
                    index++;
                    //從新的邊開始找下個邊
                    i = j;
                    //重新搜尋
                    j = 0;
                }
                j++;
            }

            for(int k=1; k<convexHull.Count; k++)
            {
                canvas.DrawLine(p, convexHull[i - 1].X, convexHull[i - 1].Y, convexHull[i].X, convexHull[i].Y);
            }
            canvas.DrawLine(p, convexHull[convexHull.Count - 1].X, convexHull[convexHull.Count - 1].Y, convexHull[0].X, convexHull[0].Y);
        }

        public bool is_in_convex_hull(List<Vertex> points, int[] convex_hull, Vertex v)
        {
            for(int i=0; i<convex_hull.Length; i++)
            {
                if(is_vertex_equal(v, points[convex_hull[i]]))
                {
                    return true;
                }
            }
            return false;
        }

        public int find_points(List<Vertex> points, Vertex v)
        {
            int index = -1;
            for(int i=0; i<points.Count; i++)
            {
                if(is_vertex_equal(v, points[i]))
                {
                    index = i;
                }
            }
            return index;
        }
        //用來合併前一個迭代產生的邊
        public int merge_previous(Edge e, int cw)
        {
            for(int i=0; i<HPs.Count; i++)
            {
                //看這個邊是否跟e相同(同個三角形外心)
                if (e.left_polygon == HPs[i].left_polygon && e.right_polygon == HPs[i].right_polygon ||
                    e.right_polygon == HPs[i].left_polygon && e.left_polygon == HPs[i].right_polygon)
                {
                    HPs[i].vector_x = e.vector_x;
                    HPs[i].vector_y = e.vector_y;
                    if (HPs[i].start_vertex.infinity && HPs[i].end_vertex.infinity)
                    {
                        HPs[i].start_vertex = assign_vertex(e.start_vertex);
                        HPs[i].start_vertex.infinity = false;
                        HPs[i].end_vertex.x = HPs[i].start_vertex.x + HPs[i].vector_x;
                        HPs[i].end_vertex.y = HPs[i].start_vertex.y + HPs[i].vector_y;
                    }
                    else if (!HPs[i].start_vertex.infinity && HPs[i].end_vertex.infinity)
                    {
                        HPs[i].end_vertex = assign_vertex(e.start_vertex);
                        HPs[i].end_vertex.infinity = false;
                    }
                    HPs[i].left_polygon = e.left_polygon;
                    HPs[i].right_polygon = e.right_polygon;
                    HPs[i].is_line = false;
                    HPs[i].cw_successor = cw;

                    return HPs[i].index;
                }
            }

            return -1;
        }

        public void check_and_clear(Edge BS, Edge HP)
        {
            for (int i = 0; i < HPs.Count; i++)
            {
                if(BS.left_polygon == HPs[i].left_polygon && BS.right_polygon == HPs[i].right_polygon ||
                    BS.right_polygon == HPs[i].left_polygon && BS.left_polygon == HPs[i].right_polygon ||
                    HP.left_polygon == HPs[i].left_polygon && HP.right_polygon == HPs[i].right_polygon ||
                    HP.right_polygon == HPs[i].left_polygon && HP.left_polygon == HPs[i].right_polygon)
                {
                    HPs.RemoveAt(i);
                }
            }
        }

        public void check_if_obtuse_triangle(Edge BS, Edge HP, Edge e_HP)
        {
            if(!is_vertex_equal(BS.start_vertex, BS.end_vertex) && !is_vertex_equal(HP.start_vertex, HP.end_vertex))
            {
                double a1 = caculateAngle(BS, e_HP);
                double a2 = caculateAngle(BS, HP);
                double a3 = caculateAngle(HP, e_HP);
                string _a1 = a1.ToString("#0.000");
                string _a2 = a2.ToString("#0.000");
                string _a3 = a3.ToString("#0.000");
                a1 = Double.Parse(_a1);
                a2 = Double.Parse(_a2);
                a3 = Double.Parse(_a3);

                if (ceiling(a1) == ceiling(a2 + a3))
                {
                    HP.vector_x = -HP.vector_x;
                    HP.vector_y = -HP.vector_y;
                    HP.end_vertex.x = HP.start_vertex.x + HP.vector_x;
                    HP.end_vertex.y = HP.start_vertex.y + HP.vector_y;
                }
                else if (ceiling(a3) == ceiling(a1 + a2))
                {
                    BS.vector_x = -BS.vector_x;
                    BS.vector_y = -BS.vector_y;
                    BS.end_vertex.x = BS.start_vertex.x + BS.vector_x;
                    BS.end_vertex.y = BS.start_vertex.y + BS.vector_y;
                }
                else if (ceiling(a2) == ceiling(a1 + a3))
                {
                    e_HP.vector_x = -e_HP.vector_x;
                    e_HP.vector_y = -e_HP.vector_y;
                    e_HP.end_vertex.x = e_HP.start_vertex.x + e_HP.vector_x;
                    e_HP.end_vertex.y = e_HP.start_vertex.y + e_HP.vector_y;
                }
            }
        }

        //無條件進位, 因為判斷鈍角時, 會有小數點誤差問題
        public double ceiling(double value)
        {
            string _value = value.ToString("#0.00");
            value = Double.Parse(_value);

            return value + 0.01;
        }

        public void check_if_right_triangle(Edge BS, Edge HP)
        {
            if (is_vertex_equal(BS.start_vertex, BS.end_vertex))
            {
                BS.end_vertex.x = BS.start_vertex.x + BS.vector_x;
                BS.end_vertex.y = BS.start_vertex.y + BS.vector_y;
                if (caculateAngle(BS, HP) < Math.PI / 2.0)
                {
                    BS.end_vertex.x = BS.start_vertex.x - BS.vector_x;
                    BS.end_vertex.y = BS.start_vertex.y - BS.vector_y;
                    BS.vector_x = -BS.vector_x;
                    BS.vector_y = -BS.vector_y;
                }
            }
            else if(is_vertex_equal(HP.start_vertex, HP.end_vertex))
            {
                HP.end_vertex.x = HP.start_vertex.x + HP.vector_x;
                HP.end_vertex.y = HP.start_vertex.y + HP.vector_y;
                if (caculateAngle(BS, HP) < Math.PI / 2.0)
                {
                    HP.end_vertex.x = HP.start_vertex.x - HP.vector_x;
                    HP.end_vertex.y = HP.start_vertex.y - HP.vector_y;
                    HP.vector_x = -HP.vector_x;
                    HP.vector_y = -HP.vector_y;
                }
            }
        }

        //判斷ab向量和ac向量間的夾角為鈍角還是銳角
        public int kind_of_triangle(Vertex a, Vertex b, Vertex c)
        {
            double ab_len = Math.Pow(a.x - b.x, 2) + Math.Pow(a.y - b.y, 2);
            double ac_len = Math.Pow(a.x - c.x, 2) + Math.Pow(a.y - c.y, 2);
            double bc_len = Math.Pow(b.x - c.x, 2) + Math.Pow(b.y - c.y, 2);

            List<double> edge_length = new List<double>();
            edge_length.Add(ab_len);
            edge_length.Add(ac_len);
            edge_length.Add(bc_len);
            edge_length.Sort();

            if(edge_length[0] + edge_length[1] > edge_length[2])
            {
                return 1;//銳角
            }
            else if(edge_length[0] + edge_length[1] == edge_length[2])
            {
                return 2;//直角
            }
            return 3;//鈍角
        }

        public double caculateAngle(Edge u, Edge v)
        {
            double len_u = Math.Sqrt(Math.Pow(u.start_vertex.x - u.end_vertex.x, 2) + Math.Pow(u.start_vertex.y - u.end_vertex.y, 2));
            double len_v = Math.Sqrt(Math.Pow(v.start_vertex.x - v.end_vertex.x, 2) + Math.Pow(v.start_vertex.y - v.end_vertex.y, 2));
            double u1 = u.end_vertex.x - u.start_vertex.x;
            double u2 = u.end_vertex.y - u.start_vertex.y;
            double v1 = v.end_vertex.x - v.start_vertex.x;
            double v2 = v.end_vertex.y - v.start_vertex.y;
            double dot_uv = v1 * u1 + v2 * u2;
            double rad = Math.Acos(dot_uv / (len_u * len_v));
            return rad;
        }

        public Edge find_mid_line(Vertex l, Vertex r)
        {
            Edge edge = new Edge();
            int delta_x = r.x - l.x;
            int delta_y = r.y - l.y;
            //先把中點當起點
            edge.start_vertex.x = (int)((l.x + r.x) / 2);
            edge.start_vertex.y = (int)((l.y + r.y) / 2);
            //找該中垂線向量
            edge.vector_x = delta_y * -1;
            edge.vector_y = delta_x;
            edge.start_vertex.infinity = true;
            edge.end_vertex.infinity = true;

            return edge;
        }

        public Vertex assign_vertex(Vertex v)
        {
            Vertex u = new Vertex();
            u.x = v.x;
            u.y = v.y;
            u.infinity = v.infinity;

            return u;
        }

        public int[] find_convex_hull(List<Vertex> points, int left, int right)
        {
            int[] convex_hull_points = new int[right-left+1];
            //找出最外圍都是射線的點
            if(left == right)
            {
                convex_hull_points[0] = left;
            }
            else if (right - left == 1)
            {
                convex_hull_points[0] = left;
                convex_hull_points[1] = right;
            }
            else
            {
                int index = 0;
                int i;
                //先找到第一個邊
                for (i= convex_hull_hps_cnt; i<HPs.Count; i++)
                {
                    if (HPs[i].end_vertex.infinity)
                    {
                        convex_hull_points[index] = HPs[i].left_polygon;
                        convex_hull_hps_cnt++;
                        index++;
                        break;
                    }
                }
                //表示正在搜尋的邊
                int j = 0;
                while(index < right - left + 1)
                {
                    //找下個邊是射線且其左邊點是現在的右邊點
                    if (HPs[i].end_vertex.infinity && HPs[j].left_polygon == HPs[i].right_polygon)
                    {
                        convex_hull_points[index] = HPs[i].right_polygon;
                        convex_hull_hps_cnt++;
                        index++;
                        //從新的邊開始找下個邊
                        i = j;
                        //重新搜尋
                        j = 0;
                    }
                    j++;
                }
            }
            return convex_hull_points;
        }

        public bool is_lower_than_line(Edge e, Vertex v)
        {
            if (e.vector_x == 0 && e.vector_y != 0)
            {
                return true;
            }
            else if (e.vector_x != 0 && e.vector_y == 0)
            {
                if (v.y >= e.start_vertex.y)
                {
                    return false;
                }
            }
            else
            {
                if (e.start_vertex.y - (e.start_vertex.x - v.x) * e.vector_y / e.vector_x <= v.y)
                {
                    return false;
                }
            }
            return true;
        }

        public bool is_upper_than_line(Edge e, Vertex v)
        {
            if(e.vector_x == 0 && e.vector_y != 0)
            {
                return true;
            }
            else if (e.vector_x != 0 && e.vector_y == 0)
            {
                if(v.y <= e.start_vertex.y)
                {
                    return false;
                }
            }
            else
            {
                if (e.start_vertex.y - (e.start_vertex.x - v.x) * e.vector_y / e.vector_x >= v.y)
                {
                    return false;
                }
            }
            return true;
        }

        public bool find_uppest_tangent(List<Vertex> points, Edge edge, int left, int right)
        {
            for(int i=left; i<=right; i++)
            {
                if(is_upper_than_line(edge, points[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public bool find_lowest_tangent(List<Vertex> points, Edge edge, int left, int right)
        {
            for (int i = left; i <= right; i++)
            {
                if (is_lower_than_line(edge, points[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public int find_shortest_edge(List<Edge> edges)
        {
            if (edges.Count <= 1)
            {
                return 0;
            }
            //如果有兩個以上的最小邊長, 取最右邊的
            List<int> min_edge = new List<int>();
            double min = Double.MaxValue;
            for(int i=0; i<edges.Count; i++)
            {
                double dist = edges[i].get_distance();
                if(dist == min)
                {
                    min_edge.Add(i);
                }
                if (dist < min)
                {
                    min_edge.Clear();
                    min = dist;
                    min_edge.Add(i);
                }
            }
            return min_edge[min_edge.Count - 1];
        }

        public int find_closest_point(List<Vertex> points, Vertex v, int left, int right)
        {
            int min_point = 0;
            double min_dist = Math.Sqrt(Math.Pow(points[min_point].x - v.x, 2) + Math.Pow(points[min_point].y - v.y, 2));
            for(int i=left; i <= right; i++)
            {
                double dist = Math.Sqrt(Math.Pow(points[i].x - v.x, 2) + Math.Pow(points[i].y - v.y, 2));
                //要避免選到同一點的情況
                if((dist < min_dist && dist != 0) || min_dist == 0)
                {
                    min_point = i;
                    min_dist = dist;
                }
            }

            return min_point;
        }

        public bool is_edge_equal(Edge u, Edge v)
        {
            if(is_vertex_equal(u.start_vertex, v.start_vertex) && is_vertex_equal(u.end_vertex, v.end_vertex) ||
                is_vertex_equal(u.end_vertex, v.start_vertex) && is_vertex_equal(u.start_vertex, v.end_vertex))
            {
                return true;
            }
            return false;
        }

        public bool is_vertex_equal(Vertex v, Vertex u)
        {
            if(v.x == u.x && v.y == u.y)
            {
                return true;
            }
            return false;
        }

        public Vertex find_intersect(Edge u, Edge v)
        {
            Vertex intersect = new Vertex();
            //垂直中線
            if(u.vector_x == 0 && u.vector_y != 0 && v.vector_x != 0 && v.vector_y != 0)
            {
                double v_slope = (double)v.vector_y / (double)v.vector_x;
                double y = (double)v.start_vertex.y - ((double)v.start_vertex.x - u.start_vertex.x) * v_slope;
                intersect.x = u.start_vertex.x;
                intersect.y = (int)y;
            }
            //水平中線
            else if (u.vector_x != 0 && u.vector_y == 0 && v.vector_x != 0 && v.vector_y != 0)
            {
                double inverse_v_slope = (double)v.vector_x / (double)v.vector_y;
                double x = (double)v.start_vertex.x - ((double)v.start_vertex.y - u.start_vertex.y) * inverse_v_slope;
                intersect.x = (int)x;
                intersect.y = u.start_vertex.y;
            }
            //垂直中線
            else if (u.vector_x != 0 && u.vector_y != 0 && v.vector_x == 0 && v.vector_y != 0)
            {
                double u_slope = (double)u.vector_y / (double)u.vector_x;
                double y = (double)u.start_vertex.y - ((double)u.start_vertex.x - v.start_vertex.x) * u_slope;
                intersect.x = v.start_vertex.x;
                intersect.y = (int)y;
            }
            //水平中線
            else if (u.vector_x != 0 && u.vector_y != 0 && v.vector_x != 0 && v.vector_y == 0)
            {
                double inverse_u_slope = (double)u.vector_x / (double)u.vector_y;
                double x = (double)u.start_vertex.x - ((double)u.start_vertex.y - v.start_vertex.y) * inverse_u_slope;
                intersect.x = (int)x;
                intersect.y = v.start_vertex.y;
            }
            //水平中線+垂直中線
            else if (u.vector_x == 0 && u.vector_y != 0 && v.vector_x != 0 && v.vector_y == 0)
            {
                intersect.y = v.start_vertex.y;
                intersect.x = u.start_vertex.x;
            }
            else if (u.vector_x != 0 && u.vector_y == 0 && v.vector_x == 0 && v.vector_y != 0)
            {
                intersect.y = u.start_vertex.y;
                intersect.x = v.start_vertex.x;
            }
            //斜線
            else
            {
                double u_slope = (double)u.vector_y / (double)u.vector_x;
                double v_slope = (double)v.vector_y / (double)v.vector_x;
                double a = (double)u.start_vertex.x * u_slope;
                double b = (double)v.start_vertex.x * v_slope;
                double c = (double)v.start_vertex.y - (double)u.start_vertex.y + a - b;
                double d = u_slope - v_slope;
                double x = c / d;
                double y = (double)u.start_vertex.y - ((double)u.start_vertex.x - c / d) * u_slope;
                intersect.x = (int)x;
                intersect.y = (int)y;
            }
            return intersect;
        }
        
        //candidate_edge: x或y與候選點相連的邊
        public bool find_z_point(List<Vertex> candidates, Edge BS, Vertex c, Edge candidate_mid_line)
        {
            //外接圓圓心
            Vertex center = find_intersect(BS, candidate_mid_line);
            //外接圓半徑
            double radius = Math.Sqrt(Math.Pow(center.x - c.x, 2) + Math.Pow(center.y - c.y, 2));
            //若在半徑範圍內, 表示還有更近的外心存在
            for (int i=0; i<candidates.Count; i++)
            {
                Vertex v = candidates[i];
                double dist = Math.Sqrt(Math.Pow(v.x - center.x, 2) + Math.Pow(v.y - center.y, 2));
                if (dist < radius)
                {
                    return false;
                }
            }
            return true;
        }

        public bool is_point_on_line(Edge e, Vertex v)
        {
            if(e.vector_x == 0 && e.vector_y != 0)
            {
                if(e.start_vertex.x == v.x)
                {
                    return true;
                }
            }
            else if (e.vector_x != 0 && e.vector_y == 0)
            {
                if(e.start_vertex.y == v.y)
                {
                    return true;
                }
            }
            else
            {
                if (e.start_vertex.y - (e.start_vertex.x - v.x) * e.vector_y / e.vector_x == v.y)
                {
                    return true;
                }
            }
            return false;
        }
        
        public int get_x_of_line(Edge e, int y)
        {
            if(e.vector_x == 0 && e.vector_y != 0)
            {
                return e.start_vertex.x;
            }
            //因為是判定畫布上射線要碰到哪個邊, 所以設超過800或600即可
            else if (e.vector_x != 0 && e.vector_y == 0)
            {
                return 1000;
            }
            return (int)((double)e.start_vertex.x - (double)(e.start_vertex.y - y) * (double)e.vector_x / (double)e.vector_y);
        }

        public int get_y_of_line(Edge e, int x)
        {
            if (e.vector_x == 0 && e.vector_y != 0)
            {
                return 1000;
            }
            //因為是判定畫布上射線要碰到哪個邊, 所以設超過800或600即可
            else if (e.vector_x != 0 && e.vector_y == 0)
            {
                return e.start_vertex.y;
            }
            return (int)(e.start_vertex.y - (e.start_vertex.x - x) * e.vector_y / e.vector_x);
        }

        public void pop(List<Vertex> points, Vertex v)
        {
            if(find_points(points, v) != -1)
            {
                int p = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    if (is_vertex_equal(v, points[i]))
                    {
                        p = i;
                    }
                }
                points.RemoveAt(p);
            }
        }

        public List<Vertex> check_same_points(List<Vertex> points)
        { 
            for(int i=0; i<points.Count; i++)
            {
                for(int j=0; j<points.Count; j++)
                {
                    if(i != j && is_vertex_equal(points[i], points[j]))
                    {
                        points.RemoveAt(i);
                        break;
                    }
                }
            }
            return points;
        }

        public bool is_all_on_a_line(List<Vertex> points)
        {
            for (int i = 1; i < points.Count - 1; i++)
            {
                double tmp_v1 = (double)(points[i + 1].y - points[i].y) / (double)(points[i + 1].x - points[i].x);
                double tmp_v2 = (double)(points[i].y - points[i-1].y) / (double)(points[i].x - points[i-1].x);
                if (tmp_v1 != tmp_v2)
                {
                    return false;
                }
            }
            return true;
        }

        public bool is_all_on_vertical_line(List<Vertex> points)
        {
            for (int i = 1; i < points.Count - 1; i++)
            {
                if ((points[i + 1].x != points[i].x) || (points[i - 1].x != points[i].x))
                {
                    return false;
                }
            }
            return true;
        }

        public bool is_all_on_horizontal_line(List<Vertex> points)
        {
            for (int i = 1; i < points.Count - 1; i++)
            {
                if ((points[i + 1].y != points[i].y) || (points[i - 1].y != points[i].y))
                {
                    return false;
                }
            }
            return true;
        }

        public List<Vertex> y_sorting(List<Vertex> sortedVertices)
        {
            for (int i = 0; i < sortedVertices.Count; i++)
            {
                for (int j = i + 1; j < sortedVertices.Count; j++)
                {
                    if (sortedVertices[i].x == sortedVertices[j].x)
                    {
                        int sort_start = i;
                        int sort_end = j;
                        while (sort_end < sortedVertices.Count - 1 && sortedVertices[sort_end].x == sortedVertices[sort_end + 1].x)
                        {
                            sort_end++;
                        }
                        for (int m = sort_start; m <= sort_end; m++)
                        {
                            for (int n = m; n <= sort_end; n++)
                            {
                                if (sortedVertices[m].y > sortedVertices[n].y)
                                {
                                    int tmp = sortedVertices[m].y;
                                    sortedVertices[m].y = sortedVertices[n].y;
                                    sortedVertices[n].y = tmp;
                                }
                            }
                        }
                        i = sort_end;
                        break;
                    }
                }
            }
            return sortedVertices;
        }

        public List<Edge> edges_lexcial_order(List<Edge> e)
        {
            //start_vertex
            for (int i = 0; i < e.Count - 1; i++)
            {
                for (int j = i + 1; j < e.Count; j++)
                {
                    if (e[i].start_vertex.x > e[j].start_vertex.x)
                    {
                        Edge tmp = e[i];
                        e[i] = e[j];
                        e[j] = tmp;
                    }
                }
            }

            for (int i = 0; i < e.Count - 1; i++)
            {
                for (int j = i + 1; j < e.Count; j++)
                {
                    if (e[i].start_vertex.x == e[j].start_vertex.x)
                    {
                        int sort_start = i;
                        int sort_end = j;
                        while (sort_end < e.Count - 1 && e[sort_end].start_vertex.x == e[sort_end + 1].start_vertex.x)
                        {
                            sort_end++;
                        }
                        for (int m = sort_start; m <= sort_end; m++)
                        {
                            for (int n = m; n <= sort_end; n++)
                            {
                                if (e[m].start_vertex.y > e[n].start_vertex.y)
                                {
                                    Edge tmp = e[m];
                                    e[m] = e[n];
                                    e[n] = tmp;
                                }
                            }
                        }
                        i = sort_end;
                        break;
                    }
                }
            }
            bool is_start_same = true;
            for (int i = 0; i < e.Count - 1; i++)
            {
                if (e[i].start_vertex.x != e[i + 1].start_vertex.x || e[i].start_vertex.y != e[i + 1].start_vertex.y)
                {
                    is_start_same = false;
                }
            }
            //end
            if (is_start_same)
            {
                for (int i = 0; i < e.Count - 1; i++)
                {
                    for (int j = i + 1; j < e.Count; j++)
                    {
                        if (e[i].end_vertex.x > e[j].end_vertex.x)
                        {
                            Edge tmp = e[i];
                            e[i] = e[j];
                            e[j] = tmp;
                        }
                    }
                }

                for (int i = 0; i < e.Count - 1; i++)
                {
                    for (int j = i + 1; j < e.Count; j++)
                    {
                        if (e[i].end_vertex.x == e[j].end_vertex.x)
                        {
                            int sort_start = i;
                            int sort_end = j;
                            while (sort_end < e.Count - 1 && e[sort_end].end_vertex.x == e[sort_end + 1].end_vertex.x)
                            {
                                sort_end++;
                            }
                            for (int m = sort_start; m <= sort_end; m++)
                            {
                                for (int n = m; n <= sort_end; n++)
                                {
                                    if (e[m].end_vertex.y > e[n].end_vertex.y)
                                    {
                                        Edge tmp = e[m];
                                        e[m] = e[n];
                                        e[n] = tmp;
                                    }
                                }
                            }
                            i = sort_end;
                            break;
                        }
                    }
                }
            }


            return e;
        }

        public bool check_rectangle(List<Edge> edges)
        {
            for (int i = 1; i < edges.Count; i++)
            {
                if (edges[i].start_vertex.x != edges[i - 1].start_vertex.x || edges[i].start_vertex.y != edges[i - 1].start_vertex.y)
                {
                    return false;
                }
            }
            return true;
        }
    }

    class Vertex
    {
        public int x;
        public int y;
        //該點是否為射線
        public bool infinity;
    }

    class Edge
    {
        //陣列中編號
        public int index;
        //該邊右邊的多邊形
        public int right_polygon;
        //該邊左邊的多邊形
        public int left_polygon;
        //該邊起點
        public Vertex start_vertex = new Vertex();
        //該邊終點
        public Vertex end_vertex = new Vertex();
        //該邊起點上順時鐘方向的相連邊
        public int cw_predecessor = -1;
        //該邊起點上逆時鐘方向的相連邊
        public int ccw_predecessor;
        //該邊終點順時鐘方向的相連邊
        public int cw_successor = -1;
        //該邊終點逆時鐘方向的相連邊
        public int ccw_successor;
        //該直線的向量X
        public int vector_x;
        //該直線的向量Y
        public int vector_y;
        //是否為直線
        public bool is_line = false;
        //取兩點距離
        public double get_distance()
        {
            return Math.Sqrt(Math.Pow(end_vertex.x - start_vertex.x, 2) + Math.Pow(end_vertex.y - start_vertex.y, 2));
        }
    }
}
