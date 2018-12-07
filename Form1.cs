using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VoronoiDiagram
{
    public partial class Form1 : Form
    {
        private List<Voronoi> voronoiList = new List<Voronoi>();
        private int dataNumber = -1;
        private Graphics canvas;
        private Brush brush = new SolidBrush(Color.Black);
        private Pen pen = new Pen(Color.Black);
        private Random rd = new Random();
        private int now_x, now_y;
        private List<Vertex> saveVertices = new List<Vertex>();
        private bool is_open_file = false, is_run = false, is_step_by_step = false;
        //
        private List<Edge> Es = new List<Edge>();
        private List<Vertex> Ps = new List<Vertex>();

        public Form1()
        {
            InitializeComponent();
            canvas = pictureBox1.CreateGraphics();
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.toolStripButton1.Enabled = false;
            canvas.Clear(Color.White);
            voronoiList.Clear();
            saveVertices.Clear();
            Ps.Clear();
            Es.Clear();
            this.dataNumber = -1;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            dialog.Title = "選擇檔案";
            dialog.InitialDirectory = ".\\";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            is_open_file = true;
            int index = 0;
            string line;
            //encoding.default避免中文亂碼
            System.IO.StreamReader file = new System.IO.StreamReader(dialog.FileName, Encoding.Default);
            while ((line = file.ReadLine()) != null)
            {
                index++;
                if (line.Contains('#'))
                {
                    continue;
                }
                else
                {
                    int num = 0;
                    string[] words = line.Split(' ');
                    Voronoi voronoi;
                    if(words[0] == "0")
                    {
                        break;
                    }
                    else if (words[0] != "" && words.Length == 1 && words[0] != "P" && words[0] != "E")
                    {
                        num = Int32.Parse(words[0]);
                        voronoi = new Voronoi(num);
                        for(int i=0; i<num; i++)
                        {
                            line = file.ReadLine();
                            string[] points = line.Split();
                            voronoi.setVertex(Int32.Parse(points[0]), Int32.Parse(points[1]));
                        }
                        this.voronoiList.Add(voronoi);
                    }
                    else if(words[0] == "P")
                    {
                        Vertex tmp_v = new Vertex();
                        tmp_v.x = Int32.Parse(words[1]);
                        tmp_v.y = Int32.Parse(words[2]);
                        Ps.Add(tmp_v);
                    }
                    else if (words[0] == "E")
                    {
                        Edge tmp_edge = new Edge();
                        tmp_edge.start_vertex.x = Int32.Parse(words[1]);
                        tmp_edge.start_vertex.y = Int32.Parse(words[2]);
                        tmp_edge.end_vertex.x = Int32.Parse(words[3]);
                        tmp_edge.end_vertex.y = Int32.Parse(words[4]);
                        Es.Add(tmp_edge);
                    }
                }
            }
            this.dataNumber++;
            //自己點的檔案格式
            if (Ps.Count != 0)
            {
                Voronoi voronoi = new Voronoi(Ps.Count);
                for (int i = 0; i < Ps.Count; i++)
                {
                    voronoi.setVertex(Ps[i].x, Ps[i].y);
                    canvas.FillRectangle(brush, Ps[i].x, Ps[i].y, 2, 2);
                }
                voronoiList.Add(voronoi);
            }
            else
            {
                for (int i = 0; i < voronoiList[dataNumber].Num; i++)
                {
                    canvas.FillRectangle(brush, voronoiList[dataNumber].getVertex(i).x, voronoiList[dataNumber].getVertex(i).y, 2, 2);
                }
            }
            this.voronoiList[dataNumber].sort();
            //若有邊要畫
            if (Es.Count != 0)
            {
                for(int i=0; i< Es.Count; i++)
                {
                    this.canvas.DrawLine(pen, Es[i].start_vertex.x, Es[i].start_vertex.y, Es[i].end_vertex.x, Es[i].end_vertex.y);
                }
            }
            //MessageBox.Show(index.ToString());
            this.toolStripStatusLabel1.Text = "Number " + (dataNumber+1) + " of Data";
            if(voronoiList.Count > 1)
            {
                this.toolStripButton1.Enabled = true;
            }
            input_palette();
            this.toolStripButton2.Enabled = true;
            this.toolStripButton3.Enabled = true;
            this.pictureBox1.MouseClick -= pictureBox1_MouseClick;
            file.Dispose();
            file.Close();
        }
        //下一筆
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            canvas.Clear(Color.White);

            if (is_open_file)
            {
                this.dataNumber = (dataNumber + 1) % voronoiList.Count;
                if(dataNumber + 1 == voronoiList.Count)
                {
                    this.toolStripButton1.Enabled = false;
                }
                input_palette();
                for (int i = 0; i < voronoiList[dataNumber].Num; i++)
                {
                    canvas.FillRectangle(brush, voronoiList[dataNumber].getVertex(i).x, voronoiList[dataNumber].getVertex(i).y, 2, 2);
                }
                //MessageBox.Show(index.ToString());
                this.toolStripStatusLabel1.Text = "Number " + (dataNumber + 1) + " of Data";
                is_run = false;
                is_step_by_step = false;
            }
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (!is_open_file)
            {
                Vertex tmp = new Vertex();
                tmp.x = e.Location.X;
                tmp.y = e.Location.Y;
                saveVertices.Add(tmp);
                canvas.FillRectangle(brush, now_x, now_y, 2, 2);
            }
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.Title = "Save an Output File";
            //
            if(saveVertices.Count != 0)
            {
                this.dataNumber++;
                Voronoi voronoi = new Voronoi(saveVertices.Count);
                List<Vertex> sortedVertices = saveVertices.OrderBy(o => o.x).ToList();
                sortedVertices = voronoi.y_sorting(sortedVertices);
                for (int i = 0; i < sortedVertices.Count; i++)
                {
                    voronoi.setVertex(sortedVertices[i].x, sortedVertices[i].y);
                }
                this.voronoiList.Add(voronoi);
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                System.IO.StreamWriter writer = new System.IO.StreamWriter(saveFileDialog.OpenFile());
                for (int j = 0; j < voronoiList[dataNumber].Num; j++)
                {
                    writer.WriteLine("P " + voronoiList[dataNumber].getVertex(j).x + " " + voronoiList[dataNumber].getVertex(j).y);
                }
                if (voronoiList[dataNumber].HPs.Count != 0)
                {
                    for (int j = 0; j < voronoiList[dataNumber].HPs.Count; j++)
                    {
                        if (voronoiList[dataNumber].HPs[j].is_line)
                        {
                            writer.WriteLine("E " + voronoiList[dataNumber].HPs[j].start_vertex.x + " " + voronoiList[dataNumber].HPs[j].start_vertex.y + " " + voronoiList[dataNumber].HPs[j].end_vertex.x + " " + voronoiList[dataNumber].HPs[j].end_vertex.y);
                        }
                        if (voronoiList[dataNumber].HPs[j].start_vertex.infinity && voronoiList[dataNumber].HPs[j].end_vertex.infinity)
                        {
                            continue;
                        }
                        writer.WriteLine("E " + voronoiList[dataNumber].HPs[j].start_vertex.x + " " + voronoiList[dataNumber].HPs[j].start_vertex.y + " " + voronoiList[dataNumber].HPs[j].end_vertex.x + " " + voronoiList[dataNumber].HPs[j].end_vertex.y);
                    }
                }
                writer.Dispose();
                writer.Close();
            }
            canvas.Clear(Color.White);
            saveVertices.Clear();
            voronoiList.Clear();
            Es.Clear();
            Ps.Clear();
        }

        //清除畫布 = 關掉檔案
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            canvas.Clear(Color.White);
            saveVertices.Clear();
            voronoiList.Clear();
            Es.Clear();
            Ps.Clear();
            is_open_file = false;
            is_run = false;
            is_step_by_step = false;
            this.toolStripButton1.Enabled = false;
            this.toolStripButton2.Enabled = false;
            this.toolStripButton3.Enabled = false;
            this.pictureBox1.MouseClick += pictureBox1_MouseClick;
            this.toolStripStatusLabel1.Text = "No Data Input";
        }
        //RUN並畫出來
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (!is_run)
            {
                is_step_by_step = true;
                List<Vertex> points = new List<Vertex>();
                for (int i = 0; i < voronoiList[dataNumber].Num; i++)
                {
                    points.Add(voronoiList[dataNumber].getVertex(i));
                }
                List<Vertex> sortedPoints = points.OrderBy(o => o.x).ToList();
                sortedPoints = this.voronoiList[dataNumber].check_same_points(sortedPoints);
                if (this.voronoiList[dataNumber].run(sortedPoints, 0, sortedPoints.Count - 1))
                {
                    is_run = true;
                    List<Edge> edges = voronoiList[dataNumber].HPs;
                    //重劃一遍點避免受step by step影響
                    canvas.Clear(Color.White);
                    for (int i = 0; i < points.Count; i++)
                    {
                        canvas.FillRectangle(brush, points[i].x, points[i].y, 2, 2);
                    }

                    //只有一個邊
                    if (edges.Count == 1)
                    {
                        int y1 = this.voronoiList[dataNumber].get_y_of_line(edges[0], 0);
                        int y2 = this.voronoiList[dataNumber].get_y_of_line(edges[0], 800);
                        this.canvas.DrawLine(pen, 0, y1, 800, y2);
                        voronoiList[dataNumber].HPs[0].start_vertex.x = 0;
                        voronoiList[dataNumber].HPs[0].start_vertex.y = y1;
                        voronoiList[dataNumber].HPs[0].end_vertex.x = 800;
                        voronoiList[dataNumber].HPs[0].end_vertex.y = y2;
                    }
                    //有多個邊
                    else
                    {
                        //確認是否為矩形
                        if (this.voronoiList[dataNumber].check_rectangle(edges) && points.Count == 4)
                        {
                            //把水平射線改成直線
                            for (int i = 0; i < edges.Count; i++)
                            {
                                if (edges[i].vector_x != 0 && edges[i].vector_y == 0)
                                {
                                    edges[i].end_vertex.infinity = true;
                                    edges[i].start_vertex.infinity = true;
                                    edges[i].is_line = true;
                                }
                            }
                            //把非水平和垂直線去掉
                            bool is_finish = false;
                            //由於刪除元素後串列會縮短以至於可能會有該刪的沒刪到, 所以要一直檢查
                            while(!is_finish)
                            {
                                for(int j=0; j<edges.Count; j++)
                                {
                                    if (edges[j].vector_x != 0 && edges[j].vector_y != 0)
                                    {
                                        is_finish = false;
                                        edges.RemoveAt(j);
                                        break;
                                    }
                                    else
                                    {
                                        is_finish = true;
                                    }
                                }
                            }
                        }

                        for (int i = 0; i < edges.Count; i++)
                        {
                            //射線畫線
                            if (edges[i].end_vertex.infinity && !edges[i].start_vertex.infinity)
                            {
                                if (edges[i].vector_x == 0 && edges[i].vector_y != 0)
                                {
                                    if (edges[i].end_vertex.y < edges[i].start_vertex.y)
                                    {
                                        this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, edges[i].start_vertex.x, 0);
                                        voronoiList[dataNumber].HPs[i].end_vertex.x = edges[i].start_vertex.x;
                                        voronoiList[dataNumber].HPs[i].end_vertex.y = 0;
                                    }
                                    else
                                    {
                                        this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, edges[i].start_vertex.x, 600);
                                        voronoiList[dataNumber].HPs[i].end_vertex.x = edges[i].start_vertex.x;
                                        voronoiList[dataNumber].HPs[i].end_vertex.y = 600;
                                    }
                                }
                                else if (edges[i].vector_x != 0 && edges[i].vector_y == 0)
                                {
                                    if (edges[i].end_vertex.x < edges[i].start_vertex.x)
                                    {
                                        this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, 0, edges[i].start_vertex.y);
                                        voronoiList[dataNumber].HPs[i].end_vertex.x = 0;
                                        voronoiList[dataNumber].HPs[i].end_vertex.y = edges[i].start_vertex.y;

                                    }
                                    else
                                    {
                                        this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, 800, edges[i].start_vertex.y);
                                        voronoiList[dataNumber].HPs[i].end_vertex.x = 800;
                                        voronoiList[dataNumber].HPs[i].end_vertex.y = edges[i].start_vertex.y;
                                    }
                                }
                                else
                                {
                                    if (edges[i].end_vertex.x < edges[i].start_vertex.x && edges[i].end_vertex.y < edges[i].start_vertex.y)
                                    {
                                        int x = 0, y = 0;
                                        if ((x = this.voronoiList[dataNumber].get_x_of_line(edges[i], 0)) > 0)
                                        {
                                            this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, x, 0);
                                            voronoiList[dataNumber].HPs[i].end_vertex.x = x;
                                            voronoiList[dataNumber].HPs[i].end_vertex.y = 0;
                                        }
                                        else
                                        {
                                            y = this.voronoiList[dataNumber].get_y_of_line(edges[i], 0);
                                            this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, 0, y);
                                            voronoiList[dataNumber].HPs[i].end_vertex.x = 0;
                                            voronoiList[dataNumber].HPs[i].end_vertex.y = y;
                                        }
                                    }
                                    else if (edges[i].end_vertex.x > edges[i].start_vertex.x && edges[i].end_vertex.y < edges[i].start_vertex.y)
                                    {
                                        int x = 0, y = 0;
                                        if ((x = this.voronoiList[dataNumber].get_x_of_line(edges[i], 0)) < 800)
                                        {
                                            this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, x, 0);
                                            voronoiList[dataNumber].HPs[i].end_vertex.x = x;
                                            voronoiList[dataNumber].HPs[i].end_vertex.y = 0;
                                        }
                                        else
                                        {
                                            y = this.voronoiList[dataNumber].get_y_of_line(edges[i], 800);
                                            this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, 800, y);
                                            voronoiList[dataNumber].HPs[i].end_vertex.x = 800;
                                            voronoiList[dataNumber].HPs[i].end_vertex.y = y;
                                        }
                                    }
                                    else if (edges[i].end_vertex.x < edges[i].start_vertex.x && edges[i].end_vertex.y > edges[i].start_vertex.y)
                                    {
                                        int x = 0, y = 0;
                                        if ((x = this.voronoiList[dataNumber].get_x_of_line(edges[i], 600)) > 0)
                                        {
                                            this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, x, 600);
                                            voronoiList[dataNumber].HPs[i].end_vertex.x = x;
                                            voronoiList[dataNumber].HPs[i].end_vertex.y = 600;
                                        }
                                        else
                                        {
                                            y = this.voronoiList[dataNumber].get_y_of_line(edges[i], 0);
                                            this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, 0, y);
                                            voronoiList[dataNumber].HPs[i].end_vertex.x = 0;
                                            voronoiList[dataNumber].HPs[i].end_vertex.y = y;
                                        }
                                    }
                                    else if (edges[i].end_vertex.x > edges[i].start_vertex.x && edges[i].end_vertex.y > edges[i].start_vertex.y)
                                    {
                                        int x = 0, y = 0;
                                        if ((x = this.voronoiList[dataNumber].get_x_of_line(edges[i], 600)) < 800)
                                        {
                                            this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, x, 600);
                                            voronoiList[dataNumber].HPs[i].end_vertex.x = x;
                                            voronoiList[dataNumber].HPs[i].end_vertex.y = 600;
                                        }
                                        else
                                        {
                                            y = this.voronoiList[dataNumber].get_y_of_line(edges[i], 800);
                                            this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, 800, y);
                                            voronoiList[dataNumber].HPs[i].end_vertex.x = 800;
                                            voronoiList[dataNumber].HPs[i].end_vertex.y = y;
                                        }
                                    }
                                }
                            }
                            //線段畫線
                            else if (!edges[i].end_vertex.infinity && !edges[i].start_vertex.infinity)
                            {
                                this.canvas.DrawLine(pen, edges[i].start_vertex.x, edges[i].start_vertex.y, edges[i].end_vertex.x, edges[i].end_vertex.y);
                            }
                            //直線畫線
                            else if (edges[i].end_vertex.infinity && edges[i].start_vertex.infinity && edges[i].is_line)
                            {
                                if (edges[i].vector_x == 0)
                                {
                                    int x1 = this.voronoiList[dataNumber].get_x_of_line(edges[i], 0);
                                    int x2 = this.voronoiList[dataNumber].get_x_of_line(edges[i], 600);
                                    this.canvas.DrawLine(pen, x1, 0, x2, 600);
                                    voronoiList[dataNumber].HPs[i].start_vertex.x = x1;
                                    voronoiList[dataNumber].HPs[i].start_vertex.y = 0;
                                    voronoiList[dataNumber].HPs[i].end_vertex.x = x2;
                                    voronoiList[dataNumber].HPs[i].end_vertex.y = 600;
                                }
                                else
                                {
                                    int y1 = this.voronoiList[dataNumber].get_y_of_line(edges[i], 0);
                                    int y2 = this.voronoiList[dataNumber].get_y_of_line(edges[i], 800);
                                    this.canvas.DrawLine(pen, 0, y1, 800, y2);
                                    voronoiList[dataNumber].HPs[i].start_vertex.x = 0;
                                    voronoiList[dataNumber].HPs[i].start_vertex.y = y1;
                                    voronoiList[dataNumber].HPs[i].end_vertex.x = 800;
                                    voronoiList[dataNumber].HPs[i].end_vertex.y = y2;
                                }
                            }
                        }
                    }
                    edges = this.voronoiList[dataNumber].edges_lexcial_order(edges);
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            now_x = e.Location.X;
            now_y = e.Location.Y;
            this.toolStripStatusLabel2.Text = "(X, Y) = " + "(" + now_x + " , " + now_y + ")";
            this.Invalidate();      //促使表單重畫(Form1_Paint函式)
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        { 
            if (!is_step_by_step)
            {
                is_run = true;
                is_step_by_step = true;
                Thread thread = new Thread(new ThreadStart(go_by_step));
                thread.Start();
            }
            else
            {
                this.voronoiList[dataNumber].pause = false;
            }
        }

        private void input_palette()
        {
            //20種顏色
            this.voronoiList[dataNumber].palette.Add(Color.DodgerBlue);
            this.voronoiList[dataNumber].palette.Add(Color.Red);
            this.voronoiList[dataNumber].palette.Add(Color.DarkGoldenrod);
            this.voronoiList[dataNumber].palette.Add(Color.LightSteelBlue);
            this.voronoiList[dataNumber].palette.Add(Color.DarkOliveGreen);
            this.voronoiList[dataNumber].palette.Add(Color.OrangeRed);
            this.voronoiList[dataNumber].palette.Add(Color.CornflowerBlue);
            this.voronoiList[dataNumber].palette.Add(Color.PaleVioletRed);
            this.voronoiList[dataNumber].palette.Add(Color.Purple);
            this.voronoiList[dataNumber].palette.Add(Color.DarkSlateGray);
            this.voronoiList[dataNumber].palette.Add(Color.ForestGreen);
            this.voronoiList[dataNumber].palette.Add(Color.MediumOrchid);
            this.voronoiList[dataNumber].palette.Add(Color.Brown);
            this.voronoiList[dataNumber].palette.Add(Color.Crimson);
            this.voronoiList[dataNumber].palette.Add(Color.Maroon);
            this.voronoiList[dataNumber].palette.Add(Color.Chocolate);
            this.voronoiList[dataNumber].palette.Add(Color.DarkSlateBlue);
            this.voronoiList[dataNumber].palette.Add(Color.IndianRed);
            this.voronoiList[dataNumber].palette.Add(Color.Teal);
            this.voronoiList[dataNumber].palette.Add(Color.Tomato);
        }

        private void go_by_step()
        {
            List<Vertex> points = new List<Vertex>();
            for (int i = 0; i < voronoiList[dataNumber].Num; i++)
            {
                points.Add(voronoiList[dataNumber].getVertex(i));
            }
            List<Vertex> sortedPoints = points.OrderBy(o => o.x).ToList();
            sortedPoints = this.voronoiList[dataNumber].check_same_points(sortedPoints);
            this.voronoiList[dataNumber].step_by_step(sortedPoints, 0, sortedPoints.Count - 1, canvas, new Pen(Color.White), 0, 800);
        }
    }
}
