namespace SOFTWARE
{
    public partial class Form1 : Form
    {
        bool isPainted = false;
        int Width = 0;
        int Height = 0;
        int Anchor1Height = 0;
        int Anchor1Width = 0;
        int Anchor2Height = 0;
        int Anchor2Width = 0;
        // Biến thành viên của lớp Form
        private CoordinateSystem coordSystem;

        // Thêm bitmap để lưu trữ bản đồ đã vẽ
        private Bitmap mapBitmap;
        private float currentScale = 1.0f;

        public Form1()
        {
            InitializeComponent();
            SetupPanelForDoubleBuffering();
            coordSystem = new CoordinateSystem(pnlMap, 20, 20);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Khởi tạo bitmap với kích thước của panel
            mapBitmap = new Bitmap(pnlMap.Width, pnlMap.Height);
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                Height = Convert.ToInt16(tbHeight.Text);
                Width = Convert.ToInt16(tbWidth.Text);
                Anchor1Height = Convert.ToInt16(tbAnchor1Height.Text);
                Anchor1Width = Convert.ToInt16(tbAnchor1Width.Text);
                Anchor2Height = Convert.ToInt16(tbAnchor2Height.Text);
                Anchor2Width = Convert.ToInt16(tbAnchor2Width.Text);
                isPainted = true;
                // Vẽ bản đồ vào bitmap
                DrawMapToBitmap();

                // Vẽ lại panel
                pnlMap.Invalidate();
                pnlDuplicate.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi nhập dữ liệu: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        PointF anchor1Point;
        PointF anchor2Point;
        // Phương thức mới để vẽ bản đồ vào bitmap
        private void DrawMapToBitmap()
        {
            // Tạo mới bitmap nếu kích thước panel thay đổi
            if (mapBitmap.Width != pnlMap.Width || mapBitmap.Height != pnlMap.Height)
            {
                mapBitmap.Dispose();
                mapBitmap = new Bitmap(pnlMap.Width, pnlMap.Height);
            }

            using (Graphics g = Graphics.FromImage(mapBitmap))
            {
                g.Clear(pnlMap.BackColor);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Tính toán giới hạn tọa độ để hiển thị
                float margin = 50; // Tạo lề xung quanh
                float xMin = -margin;
                float yMin = -margin;
                float xMax = Width + margin;
                float yMax = Height + margin;

                // Tính toán tỷ lệ để vừa với panel
                float scaleX = (pnlMap.Width - 40) / (xMax - xMin);
                float scaleY = (pnlMap.Height - 40) / (yMax - yMin);
                float scale = Math.Min(scaleX, scaleY);

                // Lưu lại tỷ lệ hiện tại để sử dụng khi vẽ điểm
                currentScale = scale;

                // Cập nhật hệ tọa độ với tỷ lệ mới
                coordSystem = new CoordinateSystem(pnlMap, 20, 20, scale, scale);

                // Vẽ lưới tọa độ với đường kẻ mờ
                using (Pen gridPen = new Pen(Color.LightGray, 1))
                {
                    coordSystem.DrawGrid(g, gridPen, 5, xMin, xMax, yMin, yMax);
                }

                // Vẽ trục tọa độ
                using (Pen axisPen = new Pen(Color.Black, 2))
                {
                    coordSystem.DrawAxes(g, axisPen, xMin, xMax, yMin, yMax);
                }

                // Vẽ hình chữ nhật chính (Map)
                using (Pen mapPen = new Pen(Color.Blue, 2))
                {
                    coordSystem.DrawRectangle(g, mapPen, 0, 0, Width, Height);
                }

                // Vẽ điểm Anchor1
                using (Brush anchorBrush = new SolidBrush(Color.Red))
                {
                    float anchorSize = 5 / scale;
                    PointF anchorPoint = coordSystem.WorldToScreen(Anchor1Width, Anchor1Height);
                    g.FillEllipse(anchorBrush,
                        anchorPoint.X - anchorSize * scale / 2,
                        anchorPoint.Y - anchorSize * scale / 2,
                        anchorSize * scale,
                        anchorSize * scale);
                    anchor1Point = anchorPoint;
                }

                // Vẽ điểm Anchor2
                using (Brush anchorBrush = new SolidBrush(Color.Red))
                {
                    float anchorSize = 5 / scale;
                    PointF anchorPoint = coordSystem.WorldToScreen(Anchor2Width, Anchor2Height);
                    g.FillEllipse(anchorBrush,
                        anchorPoint.X - anchorSize * scale / 2,
                        anchorPoint.Y - anchorSize * scale / 2,
                        anchorSize * scale,
                        anchorSize * scale);
                    anchor2Point = anchorPoint;
                }
                using (Brush fillBrush = new SolidBrush(Color.FromArgb(200, 230, 255))) // Màu xanh nhạt với độ trong suốt nhẹ
                {
                    coordSystem.FillRectangle(g, fillBrush, 0, 0, Width, Height);
                }
            }
        }

        private void pnlMap_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Nếu đã vẽ bản đồ, vẽ từ bitmap đã lưu
            if (isPainted)
            {
                g.DrawImage(mapBitmap, 0, 0);
                // Nếu đang chạy simulation, vẽ thêm điểm blue
                if (isPoint)
                {
                    PointF anchorPoint;
                    using (Brush anchorBrush = new SolidBrush(Color.Blue))
                    {

                        float anchorSize = 5 / currentScale;
                        anchorPoint = coordSystem.WorldToScreen(x, y);
                        g.FillEllipse(anchorBrush,
                            anchorPoint.X - anchorSize * currentScale / 2,
                            anchorPoint.Y - anchorSize * currentScale / 2,
                            anchorSize * currentScale,
                            anchorSize * currentScale);
                    }
                    using (Pen redPen = new Pen(Color.Red, 1))
                    {
                        g.DrawLine(redPen, anchorPoint, anchor1Point);
                    }
                    using (Pen redPen = new Pen(Color.Red, 1))
                    {
                        g.DrawLine(redPen, anchorPoint, anchor2Point);
                    }
                }

            }
        }

        private void btnSetup_MouseDown(object sender, MouseEventArgs e)
        {
        }

        private void btnSetup_MouseUp(object sender, MouseEventArgs e)
        {
        }

        bool isPoint = false;
        int x;
        int y;
        Random random = new Random();

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            x = random.Next(0, Width);  // Giới hạn trong phạm vi bản đồ
            y = random.Next(0, Height); // Giới hạn trong phạm vi bản đồ
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            // Chỉ vẽ lại panel khi isPoint = true và isPainted = true
            if (isPoint && isPainted)
            {
                pnlMap.Invalidate();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            SimulationTimer.Stop();
            MonitorTimer.Stop();
            isPoint = false;
            pnlMap.Invalidate(); // Vẽ lại lần cuối để xóa điểm
        }

        private void btnTag_Click(object sender, EventArgs e)
        {
            if (isPainted) // Chỉ bắt đầu nếu đã vẽ bản đồ
            {
                SimulationTimer.Start();
                MonitorTimer.Start();
                isPoint = true;
            }
            else
            {
                MessageBox.Show("Vui lòng thiết lập bản đồ trước khi bắt đầu.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }



        // Phương thức thiết lập double buffering cho panel
        private void SetupPanelForDoubleBuffering()
        {
            // Bật double buffering cho panel bằng reflection (vì Panel không có thuộc tính DoubleBuffered công khai)
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, pnlMap, new object[] { true });

        }

        private void pnlDuplicate_Paint(object sender, PaintEventArgs e)
        {
            
            if (isPainted && mapBitmap != null)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Vẽ hình ảnh từ mapBitmap lên panel khác
                g.DrawImage(mapBitmap, 0, 0);
            }
        
    }
    }
}