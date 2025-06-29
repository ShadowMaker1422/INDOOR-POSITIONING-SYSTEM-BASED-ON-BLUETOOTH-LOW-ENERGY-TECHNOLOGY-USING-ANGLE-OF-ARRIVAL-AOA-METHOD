using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZedGraph;
using static SOFTWARE.Form2;

namespace SOFTWARE
{
    public partial class TagFaceplateForm : Form
    {
        private Tag tag;
        private Anchor anchor1;
        private Anchor anchor2;
        private ZedGraphControl zgcTagVelocity;

        // Custom colors for better aesthetics
        private static readonly Color headerColor = Color.FromArgb(0, 122, 204);
        private static readonly Color backgroundColor = Color.White;
        private static readonly Color panelColor = Color.White;
        private static readonly Color connectedColor = Color.FromArgb(0, 170, 0);
        private static readonly Color disconnectedColor = Color.FromArgb(217, 83, 79);
        private static readonly Color alarmColor = Color.FromArgb(217, 83, 79);
        private static readonly Color normalColor = Color.FromArgb(92, 184, 92);
        private static readonly Color labelColor = Color.FromArgb(70, 70, 70);

        public TagFaceplateForm(Tag tag, Anchor anchor1, Anchor anchor2)
        {
            this.tag = tag;
            this.anchor1 = anchor1;
            this.anchor2 = anchor2;

            // Set form style to a more modern look
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = backgroundColor;

            InitializeComponents();

            this.Load += (sender, e) => {
                Console.WriteLine("Form loaded, ensuring graph is initialized");
                if (zgcTagVelocity != null)
                {
                    SetupVelocityGraph();
                }
            };

            // Set up refresh timer to update data
            System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 500; // Update every 500ms
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        private void EnsureGraphInitialized()
        {
            if (zgcTagVelocity == null || zgcTagVelocity.GraphPane == null)
            {
                Console.WriteLine("Re-initializing velocity graph");

                if (zgcTagVelocity != null)
                {
                    TableLayoutPanel panel = null;
                    foreach (Control control in this.Controls)
                    {
                        if (control is TableLayoutPanel)
                        {
                            panel = (TableLayoutPanel)control;
                            break;
                        }
                    }

                    if (panel != null)
                    {
                        panel.Controls.Remove(zgcTagVelocity);
                    }
                }

                zgcTagVelocity = new ZedGraphControl();
                zgcTagVelocity.Dock = DockStyle.Fill;

                TableLayoutPanel mainPanel = null;
                foreach (Control control in this.Controls)
                {
                    if (control is TableLayoutPanel)
                    {
                        mainPanel = (TableLayoutPanel)control;
                        break;
                    }
                }

                if (mainPanel != null && mainPanel.RowCount > 9)
                {
                    mainPanel.Controls.Add(zgcTagVelocity, 0, 9);
                    mainPanel.SetColumnSpan(zgcTagVelocity, 2);
                }

                SetupVelocityGraph();
            }
        }

        private void InitializeComponents()
        {
            // Modern form setup with better proportions
            this.Text = $"Tag {tag.tagID} Details";
            this.Size = new Size(520, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            // Create header panel with tag ID
            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 60;
            headerPanel.BackColor = headerColor;

            System.Windows.Forms.Label headerLabel = new System.Windows.Forms.Label();
            headerLabel.Text = $"Tag {tag.tagID}";
            headerLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            headerLabel.ForeColor = Color.White;
            headerLabel.AutoSize = true;
            headerLabel.Location = new Point(20, 15);
            headerPanel.Controls.Add(headerLabel);

            this.Controls.Add(headerPanel);

            // Create a TableLayoutPanel with better spacing
            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Padding = new Padding(20, 20, 20, 20);
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.RowCount = 10;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            // Set consistent row heights with a bit more space
            for (int i = 0; i < tableLayoutPanel.RowCount; i++)
            {
                tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            }

            // Row for the velocity graph (taller)
            tableLayoutPanel.RowStyles[9] = new RowStyle(SizeType.Absolute, 250F);

            // Create information panels with rounded corners
            Panel infoPanel = CreateRoundedPanel();
            infoPanel.Dock = DockStyle.Fill;
            infoPanel.Padding = new Padding(15);

            // Add labels for tag information with improved styling
            AddLabelRow(tableLayoutPanel, 0, "Tag ID:", tag.tagID);
            AddLabelRow(tableLayoutPanel, 1, "Position:", "");
            AddLabelRow(tableLayoutPanel, 2, "Velocity:", "");
            AddLabelRow(tableLayoutPanel, 3, "Status:", "");
            AddLabelRow(tableLayoutPanel, 4, "Distance to Anchor 1:", "");
            AddLabelRow(tableLayoutPanel, 5, "Distance to Anchor 2:", "");
            AddLabelRow(tableLayoutPanel, 6, "Last Update:", "");
            AddLabelRow(tableLayoutPanel, 7, "Alarm Status:", "");
            AddLabelRow(tableLayoutPanel, 8, "Alarm Message:", "");

            // Add velocity graph
            zgcTagVelocity = new ZedGraphControl();
            zgcTagVelocity.Dock = DockStyle.Fill;
            zgcTagVelocity.BorderStyle = BorderStyle.None;
            zgcTagVelocity.BackColor = Color.White;

            // Use a panel to contain the graph for better styling
            Panel graphPanel = CreateRoundedPanel();
            graphPanel.Padding = new Padding(10);
            graphPanel.Dock = DockStyle.Fill;
            graphPanel.Controls.Add(zgcTagVelocity);
            tableLayoutPanel.Controls.Add(graphPanel, 0, 9);
            tableLayoutPanel.SetColumnSpan(graphPanel, 2);

            SetupVelocityGraph();

            this.Controls.Add(tableLayoutPanel);
        }

        private Panel CreateRoundedPanel()
        {
            Panel panel = new Panel();
            panel.BackColor = panelColor;
            panel.Paint += (sender, e) => {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RoundedRectangle.FillRoundedRectangle(g, new SolidBrush(panelColor),
                    new Rectangle(0, 0, panel.Width, panel.Height), 8);
            };
            return panel;
        }

        private void AddLabelRow(TableLayoutPanel table, int row, string labelText, string valueText)
        {
            System.Windows.Forms.Label lblName = new System.Windows.Forms.Label();
            lblName.Text = labelText;
            lblName.Dock = DockStyle.Fill;
            lblName.TextAlign = ContentAlignment.MiddleRight;
            lblName.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblName.ForeColor = labelColor;

            System.Windows.Forms.Label lblValue = new System.Windows.Forms.Label();
            lblValue.Text = valueText;
            lblValue.Dock = DockStyle.Fill;
            lblValue.TextAlign = ContentAlignment.MiddleLeft;
            lblValue.Tag = labelText; // Store label name in Tag for easy identification during updates
            lblValue.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            lblValue.ForeColor = labelColor;

            // Add a small padding between label and value
            lblValue.Padding = new Padding(10, 0, 0, 0);

            table.Controls.Add(lblName, 0, row);
            table.Controls.Add(lblValue, 1, row);
        }

        private void SetupVelocityGraph()
        {
            if (zgcTagVelocity == null)
            {
                Console.WriteLine("ZedGraph control is null in SetupVelocityGraph");
                return;
            }

            try
            {
                // Configure graph with more modern styling
                zgcTagVelocity.GraphPane.Title.Text = "X Position Over Time";
                zgcTagVelocity.GraphPane.Title.FontSpec.Family = "Segoe UI";
                zgcTagVelocity.GraphPane.Title.FontSpec.Size = 14;
                zgcTagVelocity.GraphPane.Title.FontSpec.IsBold = true;
                zgcTagVelocity.GraphPane.Title.FontSpec.FontColor = headerColor;

                // Axis styling
                zgcTagVelocity.GraphPane.XAxis.Title.Text = "Time";
                zgcTagVelocity.GraphPane.XAxis.Title.FontSpec.Family = "Segoe UI";
                zgcTagVelocity.GraphPane.XAxis.Title.FontSpec.Size = 11;
                zgcTagVelocity.GraphPane.XAxis.Color = Color.DarkGray;

                zgcTagVelocity.GraphPane.YAxis.Title.Text = "X Position (units)";
                zgcTagVelocity.GraphPane.YAxis.Title.FontSpec.Family = "Segoe UI";
                zgcTagVelocity.GraphPane.YAxis.Title.FontSpec.Size = 11;
                zgcTagVelocity.GraphPane.YAxis.Color = Color.DarkGray;

                // Graph background
                zgcTagVelocity.GraphPane.Fill = new Fill(Color.White);
                zgcTagVelocity.GraphPane.Chart.Fill = new Fill(Color.WhiteSmoke);

                // Time scale configuration
                zgcTagVelocity.GraphPane.XAxis.Type = AxisType.Date;
                zgcTagVelocity.GraphPane.XAxis.Scale.Format = "HH:mm:ss";
                zgcTagVelocity.GraphPane.XAxis.Scale.MajorUnit = DateUnit.Second;
                zgcTagVelocity.GraphPane.XAxis.Scale.MajorStep = 5;

                // Position scale - will be adjusted dynamically
                zgcTagVelocity.GraphPane.YAxis.Scale.Min = 0;
                zgcTagVelocity.GraphPane.YAxis.Scale.Max = 100; // Will be adjusted based on actual data

                // Create x position curve with improved styling
                if (tag.xPositionHistory == null)
                {
                    // If no x position history exists, initialize it with current position
                    tag.xPositionHistory = new RollingPointPairList(100);
                    tag.xPositionHistory.Add(DateTime.Now.ToOADate(), tag.x);
                }

                LineItem positionCurve = zgcTagVelocity.GraphPane.AddCurve(
                    "X Position",
                    tag.xPositionHistory,
                    Color.FromArgb(0, 122, 204),
                    SymbolType.Circle);

                positionCurve.Symbol.Size = 6;
                positionCurve.Symbol.Fill = new Fill(Color.FromArgb(0, 122, 204));
                positionCurve.Line.Width = 2;

                zgcTagVelocity.AxisChange();
                zgcTagVelocity.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetupVelocityGraph: {ex.Message}");
            }
        }

        private void UpdateDisplay()
        {
            if (this.Controls.Count > 1 && this.Controls[1] is TableLayoutPanel tablePanel)
            {
                foreach (Control control in tablePanel.Controls)
                {
                    if (control is System.Windows.Forms.Label label && label.Tag != null)
                    {
                        switch (label.Tag.ToString())
                        {
                            case "Position:":
                                label.Text = $"({tag.x:F2}, {tag.y:F2})";
                                break;
                            case "Velocity:":
                                label.Text = $"{tag.velocityMagnitude:F2} units/sec";
                                break;
                            case "Status:":
                                label.Text = tag.IsConnected ? "Connected" : "Disconnected";
                                label.ForeColor = tag.IsConnected ? connectedColor : disconnectedColor;
                                break;
                            case "Distance to Anchor 1:":
                                double dist1 = Math.Sqrt(Math.Pow(tag.x - anchor1.x, 2) + Math.Pow(tag.y - anchor1.y, 2));
                                label.Text = $"{dist1:F2} units";
                                break;
                            case "Distance to Anchor 2:":
                                double dist2 = Math.Sqrt(Math.Pow(tag.x - anchor2.x, 2) + Math.Pow(tag.y - anchor2.y, 2));
                                label.Text = $"{dist2:F2} units";
                                break;
                            case "Last Update:":
                                label.Text = tag.LastUpdateTime.ToString("HH:mm:ss.fff");
                                break;
                            case "Alarm Status:":
                                label.Text = tag.HasAlarm ? "ALARM ACTIVE" : "Normal";
                                label.ForeColor = tag.HasAlarm ? alarmColor : normalColor;
                                break;
                            case "Alarm Message:":
                                label.Text = tag.AlarmMessage;
                                break;
                        }
                    }
                }
            }

            UpdateVelocityGraph();
        }

        private void UpdateVelocityGraph()
        {
            EnsureGraphInitialized();

            if (zgcTagVelocity == null || zgcTagVelocity.GraphPane == null)
            {
                Console.WriteLine("ZedGraph control or GraphPane is null");
                return;
            }

            try
            {
                // Get current time for X axis
                double now = DateTime.Now.ToOADate();
                double oneMinuteAgo = DateTime.Now.AddMinutes(-1).ToOADate();

                // Set X axis display range (last minute)
                zgcTagVelocity.GraphPane.XAxis.Scale.Min = oneMinuteAgo;
                zgcTagVelocity.GraphPane.XAxis.Scale.Max = now;

                // Add current position to history if needed
                if (tag.xPositionHistory == null)
                {
                    tag.xPositionHistory = new RollingPointPairList(100);
                }

                // Add current position to history only if it's changed significantly
                if (tag.xPositionHistory.Count == 0 ||
                    Math.Abs(tag.x - tag.xPositionHistory[tag.xPositionHistory.Count - 1].Y) > 0.1)
                {
                    tag.xPositionHistory.Add(now, tag.x);
                }

                // Calculate Y axis range based on data
                if (tag.xPositionHistory.Count > 0)
                {
                    double minX = double.MaxValue;
                    double maxX = double.MinValue;

                    // Find min and max values
                    for (int i = 0; i < tag.xPositionHistory.Count; i++)
                    {
                        if (tag.xPositionHistory[i].Y < minX) minX = tag.xPositionHistory[i].Y;
                        if (tag.xPositionHistory[i].Y > maxX) maxX = tag.xPositionHistory[i].Y;
                    }

                    // Add margin
                    double margin = (maxX - minX) * 0.1;
                    if (margin < 1) margin = 1; // Minimum margin

                    double lowerLimit = Math.Max(0, minX - margin);
                    double upperLimit = maxX + margin;

                    // Update Y axis if needed
                    if (Math.Abs(zgcTagVelocity.GraphPane.YAxis.Scale.Min - lowerLimit) > 1.0 ||
                        Math.Abs(zgcTagVelocity.GraphPane.YAxis.Scale.Max - upperLimit) > 1.0)
                    {
                        zgcTagVelocity.GraphPane.YAxis.Scale.Min = lowerLimit;
                        zgcTagVelocity.GraphPane.YAxis.Scale.Max = upperLimit;
                    }
                }

                // Update the curve if it exists
                if (zgcTagVelocity.GraphPane.CurveList.Count > 0)
                {
                    LineItem positionCurve = zgcTagVelocity.GraphPane.CurveList[0] as LineItem;
                    if (positionCurve != null)
                    {
                        positionCurve.Points = tag.xPositionHistory;
                    }
                }

                // Update graph
                zgcTagVelocity.AxisChange();
                zgcTagVelocity.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateVelocityGraph: {ex.Message}");
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            // Update the display with latest data
            UpdateDisplay();
        }
    }

    // Helper class for drawing rounded rectangles
    public static class RoundedRectangle
    {
        public static void FillRoundedRectangle(Graphics g, Brush brush,
                                              Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            // Top left arc
            path.AddArc(arc, 180, 90);

            // Top right arc
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom right arc
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom left arc
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            g.FillPath(brush, path);
            path.Dispose();
        }
    }
}