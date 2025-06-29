#region Reference
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SOFTWARE.Form2;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Globalization;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using static SOFTWARE.Form3;
using ZedGraph;
#endregion 

namespace SOFTWARE
{
    public partial class Form2 : Form
    {
        #region Variables
        System.Drawing.Font font = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
        public bool isetup = false;
        public bool isConnect = false;
        float W;
        float H;
        Bitmap mapBitmap;
        Anchor Anchor_1 = new Anchor();
        Anchor Anchor_2 = new Anchor();
        //Tag Tag_1 = new Tag();
        List<Tag> Tags = new List<Tag>();
        //after calculating data, all indicators on panel must be scaled
        float scalex;
        float scaley;
        int TagCount;
        // Add MQTT class
        MQTT MqttClient = new MQTT();
        // Variables for storing data from Anchors
        private string dataAnchor1 = string.Empty;
        private string dataAnchor2 = string.Empty;
        // Variable to track if there is new data
        private bool hasNewData = false;
        // Variable to track MQTT connection status
        private bool mqttConnected = false;
        double distToAnchor1;
        double distToAnchor2;
        private Queue<string> messageQueue = new Queue<string>();
        private List<string> pendingAC1Messages = new List<string>();
        private List<string> pendingAC2Messages = new List<string>();
        private object queueLock = new object();
        private bool isProcessing = false;
        private TimeSpan connectionTimeout = TimeSpan.FromSeconds(5); // Timeout duration
        public string IP;
        private ZedGraphControl zgcVelocity;
        private Dictionary<string, LineItem> velocityCurves = new Dictionary<string, LineItem>();
        private TagFaceplateForm tagFaceplateForm;
        private Dictionary<string, PictureBox> anchorPictureBoxes = new Dictionary<string, PictureBox>();
        private Dictionary<string, PictureBox> tagPictureBoxes = new Dictionary<string, PictureBox>();
        private int velocityGraphUpdateCounter = 0;
        private const int VELOCITY_UPDATE_INTERVAL = 500; // Cập nhật mỗi 500 lần
        #endregion
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event Handler 
        public Form2()
        {
            InitializeComponent();
            SetupPanelForDoubleBuffering();
            // Optimized timer settings
            MonitorTimer.Interval = 1;     // Increase slightly to reduce UI updates
            ProcessingTimer.Interval = 1;  // Reduce to process data more frequently
            SimulationTimer.Interval = 1;  // Increase to reduce load while keeping trail updates smooth
            foreach (Tag tag in Tags)
            {
                //có thể thay đổi xem sao 
                tag.smoothingFactor = 0.8f;
            }
            pnlMap.MouseClick += new MouseEventHandler(pnlMap_MouseClick);
        }
        private void btnSetup_Click(object sender, EventArgs e)
        {
            ParametersSetup();
            LoadMapBitmap();
            isetup = true;
            pnlMap.Invalidate();
            SetupAnchorIcons();
            SetupTagIcons();
            UpdateIconPositions();
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (isetup)
            {
                isConnect = true;
                // Initialize MQTT connection if not already connected
                // Initialize tag status when starting
                foreach (Tag tag in Tags)
                {
                    tag.IsConnected = true;
                    tag.LastConnectionTime = DateTime.Now;
                }

                // Update status display
                UpdateTagStatusDisplay(Tags.Count);

                SimulationTimer.Start();
                MonitorTimer.Start();
                ProcessingTimer.Start();
                pnlMap.Invalidate();
                if (!mqttConnected)
                {
                    ConnectToMQTT();
                }
            }
            else
            {
                MessageBox.Show("Please set up the map before starting.",
                    "Notification",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void pnlMap_Paint(object sender, PaintEventArgs e)
        {
            if (isetup)
            {
                Graphics g = e.Graphics;

                // Draw the bitmap if it exists
                if (mapBitmap != null)
                {
                    g.DrawImage(mapBitmap, 0, 0, pnlMap.Width, pnlMap.Height);
                }
                else
                {
                    using (Pen pen = new Pen(Color.Blue, 2))
                    {
                        // Draw a rectangle with top-left corner at origin (0,0)
                        g.DrawRectangle(pen, 0, 0, W * scalex, H * scaley);

                        // Optional: Fill the rectangle with color
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(100, 0, 0, 255)))
                        {
                            g.FillRectangle(brush, 0, 0, W * scalex, H * scaley);
                        }
                    }
                }
                DrawPoint(g, $"Anchor1({Anchor_1.x},{Anchor_1.y})", Anchor_1.x, Anchor_1.y, Color.Red);
                DrawPoint(g, $"Anchor2({Anchor_2.x},{Anchor_2.y}", Anchor_2.x, Anchor_2.y, Color.Red);
                if (isConnect)
                {
                    foreach (Tag tag in Tags)
                    {
                        TagFunction(g, tag);
                    }


                }
            }
        }
        private void pnlMap_MouseClick(object sender, MouseEventArgs e)
        {
            if (!isetup || !isConnect) return;

            // Convert screen coordinates to logical coordinates
            float logicalX = e.X / scalex;
            float logicalY = e.Y / scaley;

            // Search for a tag near the click position
            foreach (Tag tag in Tags)
            {
                // Calculate distance between click and tag position
                float dx = tag.x - logicalX;
                float dy = tag.y - logicalY;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                // Define click threshold (in logical units)
                float threshold = 5.0f / scalex; // 5 pixels in logical units

                if (distance <= threshold)
                {
                    // Close existing faceplate if open
                    /* if (tagFaceplateForm != null && !tagFaceplateForm.IsDisposed)
                     {
                         tagFaceplateForm.Close();
                     }*/

                    // Open new faceplate for this tag
                    tagFaceplateForm = new TagFaceplateForm(tag, Anchor_1, Anchor_2);
                    tagFaceplateForm.Show();
                    break;
                }
            }
        }
        private void chkEnableTrail_CheckedChanged(object sender, EventArgs e)
        {
            foreach (Tag tag in Tags)
            {
                tag.TrackTrail = chkEnableTrail.Checked;
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {

            try
            {
                if (pnlMap.Width > 0 && pnlMap.Height > 0)
                    mapBitmap = new Bitmap(pnlMap.Width, pnlMap.Height);
                else
                    Console.WriteLine("Warning: pnlMap has Width or Height = 0");

                Anchor1.Text = "";
                Anchor2.Text = "";
                AddTrailTrackingControls();
                // Initialize DataGridView
                InitializeTagDataGridView();
                InitializeAlarmsDataGridView();
                InitializeVelocityGraph();
                InitializeAboutTab();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Form2_Load: {ex.Message}");
                MessageBox.Show($"Error during initialization: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            if (isetup && isConnect)
            {
                UpdateSmoothPositions();
                pnlMap.Invalidate();
                Anchor1.Text = $"Anchor1: ({Anchor_1.x},{Anchor_1.y})";
                Anchor2.Text = $"Anchor2: ({Anchor_2.x},{Anchor_2.y})";
                // number of tags changed

                label16.Text = "System Status: ONLINE";
                label19.Text = "Last Update: " + DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

                // Add tag status check
                CheckTagStatus();
                // Check for alarms
                CheckForAlarms();
                // Update DataGridView
                UpdateTagsDataGridView();
                // Update velocity graph
                velocityGraphUpdateCounter++;
                if (velocityGraphUpdateCounter >= VELOCITY_UPDATE_INTERVAL)
                {
                    UpdateVelocityGraph();
                    velocityGraphUpdateCounter = 0;
                }
                UpdateIconPositions();
            }
            else
            {
                label16.Text = "System Status: OFFLINE";
            }
        }
        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (isetup && isConnect)
            {
                foreach (Tag tag in Tags)
                {
                    UpdateTrail(tag);
                }
            }

        }
        private void ProcessingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (pendingAC1Messages.Count > 0 && pendingAC2Messages.Count > 0)
                {
                    // Get median value from each Anchor
                    string medianAC1 = GetMedianMessage(pendingAC1Messages);
                    string medianAC2 = GetMedianMessage(pendingAC2Messages);

                    if (medianAC1 != null && medianAC2 != null)
                    {
                        // Process this pair
                        ProcessMessagePair(medianAC1, medianAC2);

                        // Clear all processed messages
                        lock (queueLock)
                        {
                            pendingAC1Messages.Clear();
                            pendingAC2Messages.Clear();
                        }
                    }
                }
                else
                {
                    // Fallback method: Process data as before
                    ProcessMessageBatch();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcessingTimer Error: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Stop all timers and disconnect first
            SimulationTimer.Stop();
            MonitorTimer.Stop();
            ProcessingTimer.Stop();
            isConnect = false;

            // Disconnect MQTT if connected
            if (mqttConnected)
            {
                MqttClient.Disconnect();
                mqttConnected = false;
            }

            // Check and save trail data
            bool hasTrailData = false;
            foreach (Tag tag in Tags)
            {
                if (tag.TrailPoints.Count > 0)
                {
                    hasTrailData = true;
                    SaveTrailData(tag);
                }
            }

            // Show notification if there are no trails to save
            if (!hasTrailData)
            {
                MessageBox.Show("No trail data to save.", "Notification",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Clear trail data and update interface
            foreach (Tag tag in Tags)
            {
                tag.TrailPoints.Clear();
            }
            SaveAlarmsToCSV();

            // Redraw one last time to clear points
            pnlMap.Invalidate();

            // To this more complete termination:
            System.Windows.Forms.Application.ExitThread();
            Environment.Exit(0);
        }
        private void btnSetting_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 2;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
        }

        private void btHelp_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 3;
        }

        #endregion
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Sub Function
        private void SetupTagIcons()
        {
            try
            {
                foreach (Tag tag in Tags)
                {
                    PictureBox pbTag = new PictureBox();
                    pbTag.Size = new Size(48, 48);
                    using (MemoryStream ms = new MemoryStream(Properties.Resources.Person_Female))
                    {
                        pbTag.Image = System.Drawing.Image.FromStream(ms);
                    }
                    pbTag.SizeMode = PictureBoxSizeMode.StretchImage;
                    pbTag.BackColor = Color.Transparent;
                    pbTag.Cursor = Cursors.Hand; // Con trỏ chuột thay đổi để biết đây là vùng có thể nhấp

                    // Thêm sự kiện click để chuyển tiếp tới sự kiện pnlMap_MouseClick
                    pbTag.Click += (sender, e) => ForwardClickToPanel(tag);
                    // Thêm vào từ điển để dễ tham chiếu
                    tagPictureBoxes.Add(tag.tagID, pbTag);

                    // Thêm vào panel
                    pnlMap.Controls.Add(pbTag);
                }
            }
            catch (Exception ex) { }

        }
        private void ForwardClickToPanel(Tag tag)
        {
            if (!isetup || !isConnect) return;

            // Tính toán vị trí của Tag trong tọa độ của panel
            // Chúng ta sử dụng tọa độ của Tag, không phải vị trí thực tế của PictureBox
            // để đảm bảo tính nhất quán với logic hiện tại
            int clickX = (int)(tag.x * scalex);
            int clickY = (int)(tag.y * scaley);

            // Tạo sự kiện MouseEventArgs giả lập với vị trí của Tag
            MouseEventArgs fakeClickEvent = new MouseEventArgs(
                MouseButtons.Left,  // Giả lập nhấp chuột trái
                1,                  // Số lần nhấp
                clickX,             // Vị trí X
                clickY,             // Vị trí Y
                0                   // Cuộn chuột (không cần thiết)
            );

            // Gọi trực tiếp phương thức xử lý sự kiện pnlMap_MouseClick
            pnlMap_MouseClick(pnlMap, fakeClickEvent);
        }
        private void SetupAnchorIcons()
        {
            try
            {  // Tạo PictureBox cho Anchor 1
                PictureBox pbAnchor1 = new PictureBox();
                pbAnchor1.Size = new Size(48, 48);
                using (MemoryStream ms = new MemoryStream(Properties.Resources.Radio_Waves))
                {
                    pbAnchor1.Image = System.Drawing.Image.FromStream(ms);
                }
                pbAnchor1.SizeMode = PictureBoxSizeMode.StretchImage;
                pbAnchor1.BackColor = Color.Transparent;

                // Tạo PictureBox cho Anchor 2
                PictureBox pbAnchor2 = new PictureBox();
                pbAnchor2.Size = new Size(48, 48);
                using (MemoryStream ms = new MemoryStream(Properties.Resources.Radio_Waves))
                {
                    pbAnchor2.Image = System.Drawing.Image.FromStream(ms);
                }
                pbAnchor2.SizeMode = PictureBoxSizeMode.StretchImage;
                pbAnchor2.BackColor = Color.Transparent;

                // Thêm vào từ điển để dễ tham chiếu
                anchorPictureBoxes.Add("Anchor1", pbAnchor1);
                anchorPictureBoxes.Add("Anchor2", pbAnchor2);

                // Thêm vào panel
                pnlMap.Controls.Add(pbAnchor1);
                pnlMap.Controls.Add(pbAnchor2);
            }
            catch (Exception ex) { }
        }
        private void UpdateIconPositions()
        {
            // Cập nhật vị trí cho Anchor
            if (anchorPictureBoxes.ContainsKey("Anchor1"))
            {
                PictureBox pbAnchor1 = anchorPictureBoxes["Anchor1"];
                pbAnchor1.Location = new Point(
                    (int)(Anchor_1.x * scalex - pbAnchor1.Width / 2),
                    (int)(Anchor_1.y * scaley - pbAnchor1.Height / 2)
                );
            }

            if (anchorPictureBoxes.ContainsKey("Anchor2"))
            {
                PictureBox pbAnchor2 = anchorPictureBoxes["Anchor2"];
                pbAnchor2.Location = new Point(
                    (int)(Anchor_2.x * scalex - pbAnchor2.Width / 2),
                    (int)(Anchor_2.y * scaley - pbAnchor2.Height / 2)
                );
            }

            // Cập nhật vị trí cho tất cả Tags
            foreach (Tag tag in Tags)
            {
                if (tagPictureBoxes.ContainsKey(tag.tagID))
                {
                    PictureBox pbTag = tagPictureBoxes[tag.tagID];

                    // Cập nhật vị trí
                    pbTag.Location = new Point(
                        (int)(tag.x * scalex - pbTag.Width / 2),
                        (int)(tag.y * scaley - pbTag.Height / 2)
                    );
                }
            }
        }
        private void InitializeAlarmsDataGridView()
        {
            // Set name for DataGridView for easy reference
            dgvAlarms.Name = "dgvAlarms";

            // Clear all existing columns
            dgvAlarms.Columns.Clear();

            // Add necessary columns
            dgvAlarms.Columns.Add("Time", "Time");
            dgvAlarms.Columns.Add("TagID", "Tag ID");
            dgvAlarms.Columns.Add("AlarmType", "Alarm Type");
            dgvAlarms.Columns.Add("Message", "Message");
            dgvAlarms.Columns.Add("Value", "Value");

            // Set properties for DataGridView
            dgvAlarms.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAlarms.AllowUserToAddRows = false;
            dgvAlarms.AllowUserToDeleteRows = false;
            dgvAlarms.ReadOnly = true;
            dgvAlarms.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Set background color and font
            dgvAlarms.DefaultCellStyle.BackColor = Color.White;
            dgvAlarms.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvAlarms.EnableHeadersVisualStyles = false;
            dgvAlarms.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgvAlarms.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAlarms.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font(dgvAlarms.Font, FontStyle.Bold);
        }
        private void CheckForAlarms()
        {
            foreach (Tag tag in Tags)
            {
                bool hadAlarmBefore = tag.HasAlarm;
                AlarmType previousAlarm = tag.CurrentAlarm;

                // Reset alarm status
                tag.HasAlarm = false;
                tag.AlarmMessage = "";
                tag.CurrentAlarm = AlarmType.None;

                // Check velocity alarm (if tag moves too fast)
                if (tag.velocityMagnitude > tag.velocityThreshold)
                {
                    tag.HasAlarm = true;
                    tag.AlarmMessage = $"High velocity: {tag.velocityMagnitude:F2} > {tag.velocityThreshold:F2}";
                    tag.CurrentAlarm = AlarmType.VelocityTooHigh;
                    tag.AlarmTime = DateTime.Now;

                    // If it's a new alarm or different from previous, add to datagridview
                    if (!hadAlarmBefore || previousAlarm != AlarmType.VelocityTooHigh)
                    {
                        AddAlarmToDataGridView(tag, tag.velocityMagnitude.ToString("F2"));
                    }
                }
                // Check connection alarm
                else if (!tag.IsConnected)
                {
                    tag.HasAlarm = true;
                    tag.AlarmMessage = "Connection lost";
                    tag.CurrentAlarm = AlarmType.ConnectionLost;
                    tag.AlarmTime = DateTime.Now;

                    // If it's a new alarm or different from previous, add to datagridview
                    if (!hadAlarmBefore || previousAlarm != AlarmType.ConnectionLost)
                    {
                        AddAlarmToDataGridView(tag, "N/A");
                    }
                }
                // Check out of bounds alarm
                else if (tag.x < 0 || tag.x > W || tag.y < 0 || tag.y > H)
                {
                    tag.HasAlarm = true;
                    tag.AlarmMessage = $"Out of bounds: ({tag.x:F2}, {tag.y:F2})";
                    tag.CurrentAlarm = AlarmType.OutOfBounds;
                    tag.AlarmTime = DateTime.Now;

                    // If it's a new alarm or different from previous, add to datagridview
                    if (!hadAlarmBefore || previousAlarm != AlarmType.OutOfBounds)
                    {
                        AddAlarmToDataGridView(tag, $"({tag.x:F2}, {tag.y:F2})");
                    }
                }
            }
        }
        private void AddAlarmToDataGridView(Tag tag, string value)
        {
            try
            {
                // Create new row for alarm
                int rowIndex = dgvAlarms.Rows.Add();
                DataGridViewRow row = dgvAlarms.Rows[rowIndex];

                // Update data in cells
                row.Cells["Time"].Value = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                row.Cells["TagID"].Value = tag.tagID;
                row.Cells["AlarmType"].Value = GetAlarmTypeName(tag.CurrentAlarm);
                row.Cells["Message"].Value = tag.AlarmMessage;
                row.Cells["Value"].Value = value;

                // Customize color based on alarm type
                switch (tag.CurrentAlarm)
                {
                    case AlarmType.VelocityTooHigh:
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 200); // Light yellow
                        break;
                    case AlarmType.ConnectionLost:
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200); // Light red
                        break;
                    case AlarmType.OutOfBounds:
                        row.DefaultCellStyle.BackColor = Color.FromArgb(200, 200, 255); // Light blue
                        break;
                    default:
                        row.DefaultCellStyle.BackColor = Color.White;
                        break;
                }

                // Scroll to the newly added row
                dgvAlarms.FirstDisplayedScrollingRowIndex = dgvAlarms.RowCount - 1;

                // Play alarm sound (optional)
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding alarm: {ex.Message}");
            }
        }

        private string GetAlarmTypeName(AlarmType alarmType)
        {
            switch (alarmType)
            {
                case AlarmType.VelocityTooHigh:
                    return "High Velocity";
                case AlarmType.ConnectionLost:
                    return "Connection Lost";
                case AlarmType.OutOfBounds:
                    return "Out of Bounds";
                case AlarmType.InvalidPosition:
                    return "Invalid Position";
                default:
                    return "Undefined";
            }
        }
        private void SaveAlarmsToCSV()
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    saveFileDialog.Title = "Save Alarm Log";
                    saveFileDialog.DefaultExt = "csv";
                    saveFileDialog.FileName = $"Alarm_Log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                        {
                            // Write header
                            writer.WriteLine("Time,Tag ID,Alarm Type,Message,Value");

                            // Write data from DataGridView
                            foreach (DataGridViewRow row in dgvAlarms.Rows)
                            {
                                string time = row.Cells["Time"].Value?.ToString() ?? "";
                                string tagID = row.Cells["TagID"].Value?.ToString() ?? "";
                                string alarmType = row.Cells["AlarmType"].Value?.ToString() ?? "";
                                string message = row.Cells["Message"].Value?.ToString() ?? "";
                                string value = row.Cells["Value"].Value?.ToString() ?? "";

                                // Ensure valid CSV data
                                message = message.Replace(",", ";");

                                writer.WriteLine($"{time},{tagID},{alarmType},{message},{value}");
                            }
                        }
                        MessageBox.Show("Alarm log saved successfully!", "Notification",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving alarm log: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateTagStatus(string tagID)
        {
            // Find tag by ID and update connection time
            foreach (Tag tag in Tags)
            {
                if (tag.tagID == tagID)
                {
                    tag.IsConnected = true;
                    tag.LastConnectionTime = DateTime.Now;
                    break;
                }
            }
        }
        private void CheckTagStatus()
        {
            DateTime currentTime = DateTime.Now;
            int connectedTags = 0;

            foreach (Tag tag in Tags)
            {
                // Check last connection time
                if (currentTime - tag.LastConnectionTime > connectionTimeout)
                {
                    tag.IsConnected = false;
                }

                // Count connected tags
                if (tag.IsConnected)
                {
                    connectedTags++;
                }
            }

            // Update display
            UpdateTagStatusDisplay(connectedTags);
        }
        private void UpdateTagStatusDisplay(int connectedCount)
        {
            // Update text of label showing number of connected tags
            label17.Text = $"{connectedCount}/{Tags.Count} Online";

            // Change color based on status
            if (connectedCount == 0)
            {
                label17.ForeColor = Color.Red;
            }
            else if (connectedCount < Tags.Count)
            {
                label17.ForeColor = Color.Orange;
            }
            else
            {
                label17.ForeColor = Color.Green;
            }
        }
        private void ProcessMessagePair(string messageAC1, string messageAC2)
        {
            try
            {
                // Parse messages
                string[] partsAC1 = messageAC1.Split(',');
                string[] partsAC2 = messageAC2.Split(',');

                // Check message format
                if (partsAC1.Length < 3 || partsAC2.Length < 3)
                    return;

                // Check if both messages have the same tagID
                if (partsAC1[1] != partsAC2[1])
                    return;

                string tagId = partsAC1[1];

                // Extract azimuth angles
                float azimuth1 = float.Parse(partsAC1[2], CultureInfo.InvariantCulture);
                float azimuth2 = float.Parse(partsAC2[2], CultureInfo.InvariantCulture);

                // Find corresponding tag
                foreach (Tag tag in Tags)
                {
                    if (tag.tagID == tagId)
                    {
                        // Calculate position from azimuth angles
                        CalculatePositionImproved(tag, azimuth1, azimuth2);

                        // Mark as having new data to update UI
                        partsAC1 = null;
                        partsAC2 = null;
                        hasNewData = true;
                        // Update tag connection status
                        UpdateTagStatus(tagId);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception safely
                Console.WriteLine($"Error processing message pair: {ex.Message}");
            }
        }
        private string GetMedianMessage(List<string> messages)
        {
            if (messages == null || messages.Count == 0)
                return null;

            if (messages.Count == 1)
                return messages[0];

            // Extract angle values
            List<float> angles = new List<float>();
            foreach (string msg in messages)
            {
                string[] parts = msg.Split(',');
                if (parts.Length >= 3)
                {
                    if (float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float angle))
                    {
                        angles.Add(angle);
                    }
                }
            }

            if (angles.Count == 0)
                return messages[0];

            // Sort and get median value
            angles.Sort();
            float medianAngle = angles[angles.Count / 2];

            // Find message with angle closest to median
            string closestMessage = messages[0];
            float minDiff = float.MaxValue;

            foreach (string msg in messages)
            {
                string[] parts = msg.Split(',');
                if (parts.Length >= 3)
                {
                    if (float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float angle))
                    {
                        float diff = Math.Abs(angle - medianAngle);
                        if (diff < minDiff)
                        {
                            minDiff = diff;
                            closestMessage = msg;
                        }
                    }
                }
            }

            return closestMessage;
        }
        private void UpdateTrail(Tag tag)
        {
            if (tag.TrackTrail)
            {
                tag.TrailPoints.Add(new PointF(tag.x, tag.y));
                Console.WriteLine($"Added trail point for {tag.tagID}: ({tag.x}, {tag.y}) - Count: {tag.TrailPoints.Count}");

                if (tag.TrailPoints.Count > tag.MaxTrailPoints)
                {
                    tag.TrailPoints.RemoveAt(0);
                }
            }
            else
            {
                Console.WriteLine($"Trail tracking disabled for {tag.tagID}");
            }
        }
        private void InterpolatePosition(Tag tag, float targetX, float targetY, double timeFactor)
        {
            // Linear interpolation from current position to target position
            tag.x = tag.x + (targetX - tag.x) * (float)timeFactor;
            tag.y = tag.y + (targetY - tag.y) * (float)timeFactor;
        }
        private void ConnectToMQTT()
        {
            try
            {
                string brokerAddress = tbIP.Text.Trim().ToString();//"192.168.0.100"; // IP address of MQTT broker
                MqttClient.BrokerAddress = brokerAddress;
                MqttClient.Connect("PC");
                MqttClient.DataReceived += OnMqttDataReceived;
                MqttClient.Subscribe("cuong151");
                mqttConnected = true;
                MessageBox.Show("Successfully connected to MQTT broker!", "Notification",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine($"CONNECTED {brokerAddress}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MQTT connection error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                mqttConnected = false;
            }
        }
        private void OnMqttDataReceived(object sender, MqttDataEventArgs e)
        {
            string message = e.Message;
            Console.WriteLine($"Received MQTT: {message}"); // Add log for debug

            if (message.StartsWith("AC1"))
            {
                lock (queueLock)
                {
                    pendingAC1Messages.Add(message);
                    if (pendingAC1Messages.Count > 2) pendingAC1Messages.RemoveAt(0);
                    dataAnchor1 = message; // Still update old variable for backward compatibility
                }
            }
            else if (message.StartsWith("AC2"))
            {
                lock (queueLock)
                {
                    pendingAC2Messages.Add(message);
                    if (pendingAC2Messages.Count > 2) pendingAC2Messages.RemoveAt(0);
                    dataAnchor2 = message; // Still update old variable for backward compatibility
                }
            }
        }
        private void TryProcessData()
        {
            // Process data immediately if there is enough information
            if (!string.IsNullOrEmpty(dataAnchor1) && !string.IsNullOrEmpty(dataAnchor2))
            {
                string[] partsAnchor1 = dataAnchor1.Split(',');
                string[] partsAnchor2 = dataAnchor2.Split(',');

                // Check if both messages have the same tagID
                if (partsAnchor1.Length >= 4 && partsAnchor2.Length >= 4 &&
                    partsAnchor1[1] == partsAnchor2[1])
                {
                    string tagId = partsAnchor1[1];

                    // Find corresponding tag
                    foreach (Tag tag in Tags)
                    {
                        if (tag.tagID == tagId)
                        {
                            float azimuth1 = float.Parse(partsAnchor1[2], CultureInfo.InvariantCulture);
                            float azimuth2 = float.Parse(partsAnchor2[2], CultureInfo.InvariantCulture);

                            // Calculate new position
                            CalculatePositionImproved(tag, azimuth1, azimuth2);

                            // Reset data and mark as having new data
                            dataAnchor1 = string.Empty;
                            dataAnchor2 = string.Empty;
                            hasNewData = true;
                            break;
                        }
                    }
                }
            }
        }
        private void CalculatePositionImproved(Tag tag, float Azimuth1, float Azimuth2)
        {
            float x1 = Anchor_1.x;
            float y1 = Anchor_1.y;
            float x2 = Anchor_2.x;
            float y2 = Anchor_2.y;

            // Convert degrees to radians
            float Azimuth1_Radian = Azimuth1 * ((float)Math.PI / 180);
            float Azimuth2_Radian = Azimuth2 * ((float)Math.PI / 180);
            float Tan_Azimuth1 = (float)Math.Tan(Azimuth1_Radian);
            float Tan_Azimuth2 = (float)Math.Tan(Azimuth2_Radian);

            // Check division by zero
            if (Math.Abs(Tan_Azimuth2 - Tan_Azimuth1) < 0.00001f)
                return;

            // Correct equation for intersection point
            float newX = (x1 * Tan_Azimuth2 - x2 * Tan_Azimuth1) / (Tan_Azimuth2 - Tan_Azimuth1);
            float newY = (x2 - x1) / (Tan_Azimuth2 - Tan_Azimuth1);
            if (Math.Abs(newX - tag.x) < 0.1 && Math.Abs(newY - tag.y) < 0.1)
            {
                // Ignore changes that are too small
                newX = tag.x;
                newY = tag.y;
            }
            // Calculate slope of line from each Anchor

            // Check division by zero

            // Calculate intersection coordinates (correct formula)

            // Update time for velocity calculation
            DateTime currentTime = DateTime.Now;
            double timeDelta = (currentTime - tag.LastUpdateTime).TotalSeconds;

            // Update velocity if not first time
            if (tag.LastUpdateTime != DateTime.MinValue && timeDelta > 0)
            {
                UpdateVelocity(tag, newX, newY, timeDelta);
            }

            // Apply Kalman filter or adaptive filter
            ApplyPositionFilter(tag, newX, newY);

            // Update time
            tag.LastUpdateTime = currentTime;
            // Update distances
            distToAnchor1 = CalculateDistance(tag.x, tag.y, x1, y1);
            distToAnchor2 = CalculateDistance(tag.x, tag.y, x2, y2);

        }
        private void ProcessMessageBatch()
        {
            // Get a batch of messages from the queue
            List<string> messageBatch = new List<string>();

            lock (queueLock)
            {
                // Get at most 10 messages each time (adjust as needed)
                int batchSize = Math.Min(10, messageQueue.Count);
                for (int i = 0; i < batchSize; i++)
                {
                    if (messageQueue.Count > 0)
                        messageBatch.Add(messageQueue.Dequeue());
                }
            }


            // Only process latest AC1 and AC2 messages
            string latestAC1 = null;
            string latestAC2 = null;

            foreach (string message in messageBatch)
            {
                if (message.StartsWith("AC1"))
                    latestAC1 = message;
                else if (message.StartsWith("AC2"))
                    latestAC2 = message;
            }

            // Update stored data
            if (latestAC1 != null)
                dataAnchor1 = latestAC1;
            if (latestAC2 != null)
                dataAnchor2 = latestAC2;

            // Process data if both are available
            if (!string.IsNullOrEmpty(dataAnchor1) && !string.IsNullOrEmpty(dataAnchor2))
            {
                foreach (Tag tag in Tags)
                {
                    ProcessMQTTdata(tag);
                }
            }
        }
        private bool IsValidPosition(float x, float y, Tag tag)
        {
            // Check if position is within reasonable limits
            if (x < 0 || x > W || y < 0 || y > H)
                return false;

            // Increase threshold to allow faster movement
            if (tag.x != 0 && tag.y != 0)
            {
                double distance = CalculateDistance(x, y, tag.x, tag.y);
                if (distance > 20) // Increased from 10 to 20
                    return false;
            }

            return true;
        }
        private void ProcessMQTTdata(Tag tag)
        {
            try
            {
                if (!string.IsNullOrEmpty(dataAnchor1) && !string.IsNullOrEmpty(dataAnchor2))
                {
                    string[] partsAnchor1 = dataAnchor1.Split(',');
                    if (partsAnchor1.Length >= 4)
                    {
                        string anchorInfo1 = partsAnchor1[0];
                        string tagInfo = partsAnchor1[1];
                        string azimuth1 = partsAnchor1[2];
                        string[] partsAnchor2 = dataAnchor2.Split(',');
                        if (partsAnchor1.Length >= 4 && partsAnchor2[1] == tagInfo)
                        {
                            // Console.WriteLine("\nFound couple of data!!!");
                            //if string is correct for that tag, draw point
                            if (tagInfo == tag.tagID)
                            {
                                //  Console.WriteLine($"Begin calculating of {tag.tagID}");
                                string azimuth2 = partsAnchor2[2];
                                // have 2 azimuth angles, calculate coordinates ==> tagID + 2 coordinates
                                float Azimuth1 = float.Parse(azimuth1, CultureInfo.InvariantCulture);
                                float Azimuth2 = float.Parse(azimuth2, CultureInfo.InvariantCulture);
                                CalculatePosition(tag, Azimuth1, Azimuth2);
                                dataAnchor1 = string.Empty;
                                dataAnchor2 = string.Empty;
                                // hasNewData = false;
                            }

                        }
                    }
                }
            }
            catch (Exception ex) { }
        }
        private void CalculatePosition(Tag tag, float Azimuth1, float Azimuth2)
        {
            float x1 = Anchor_1.x;
            float y1 = Anchor_1.y;
            float x2 = Anchor_2.x;
            float y2 = Anchor_2.y;

            float Azimuth1_Radian = Azimuth1 * ((float)Math.PI / 180);
            float Azimuth2_Radian = Azimuth2 * ((float)Math.PI / 180);
            float Tan_Azimuth1 = (float)Math.Tan(Azimuth1_Radian);
            float Tan_Azimuth2 = (float)Math.Tan(Azimuth2_Radian);

            // Check division by zero
            if (Math.Abs(Tan_Azimuth2 - Tan_Azimuth1) < 0.00001f)
                return;

            // Correct equation for intersection point
            float newX = (x1 * Tan_Azimuth2 - x2 * Tan_Azimuth1) / (Tan_Azimuth2 - Tan_Azimuth1);
            float newY = (x2 - x1) / (Tan_Azimuth2 - Tan_Azimuth1);
            if (IsValidPosition(newX, newY, tag))
            {
                tag.targetX = newX;
                tag.targetY = newY;
            }
            distToAnchor1 = CalculateDistance(tag.x, tag.y, x1, y1);
            distToAnchor2 = CalculateDistance(tag.x, tag.y, x2, y2);
            //  Console.WriteLine($"{tag.tagID}({tag.x};{tag.y})\n");
        }
        private void UpdateSmoothPositions()
        {

            foreach (Tag tag in Tags)
            {
                try
                {
                    // If tag not initialized, skip
                    if (tag.x == 0 && tag.y == 0 && tag.targetX == 0 && tag.targetY == 0)
                        continue;

                    // Initialize previousX/Y if first time
                    if (tag.previousX == 0 && tag.previousY == 0)
                    {
                        tag.previousX = tag.x;
                        tag.previousY = tag.y;
                    }

                    // Check if tag is stationary
                    float movement = (float)Math.Sqrt(
                        Math.Pow(tag.x - tag.previousX, 2) +
                        Math.Pow(tag.y - tag.previousY, 2));

                    if (movement < 0.1f)  // If movement < 2mm
                    {
                        tag.stabilityCounter++;
                        if (tag.stabilityCounter > 2)  // After 10 cycles
                        {
                            tag.isStable = true;
                        }
                    }
                    else
                    {
                        tag.stabilityCounter = 0;
                        tag.isStable = false;
                    }

                    // Calculate distance to target position
                    float dx = tag.targetX - tag.x;
                    float dy = tag.targetY - tag.y;

                    // Prevent tag from standing still at start
                    if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001 && tag.x == tag.targetX && tag.targetX == 0)
                    {
                        // Tag has no position yet, don't update
                        continue;
                    }

                    // If tag is stationary and movement is small, maintain position
                    if (tag.isStable && Math.Abs(dx) < 0.5f && Math.Abs(dy) < 0.5f)
                    {
                        // Don't update position or update very slightly
                        tag.x += dx * 0.01f;
                        tag.y += dy * 0.01f;
                        continue;
                    }

                    // Limit movement
                    float maxMovement = 1.0f;  // 5mm per update
                    if (Math.Abs(dx) > maxMovement) dx = Math.Sign(dx) * maxMovement;
                    if (Math.Abs(dy) > maxMovement) dy = Math.Sign(dy) * maxMovement;

                    // Update position with smoothing factor
                    tag.x += dx * tag.smoothingFactor;
                    tag.y += dy * tag.smoothingFactor;

                    // Save current position to check stability
                    tag.previousX = tag.x;
                    tag.previousY = tag.y;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UpdateSmoothPositions Error: {ex.Message}");
                }
            }
        }
        private void ParametersSetup()
        {
            try
            {
                Tags.Clear();
                TagCount = Convert.ToInt16(tbTagcount.Text);
                for (int i = 0; i < TagCount; i++) { Tag tag = new Tag(); Tags.Add(tag); }
                label18.Text = "Anchors: 2 Online";
                NameTagID();
                Anchor_1.x = (float)Convert.ToDouble(Anchor1x.Text);
                Anchor_1.y = (float)Convert.ToDouble(Anchor1y.Text);
                Anchor_2.x = (float)Convert.ToDouble(Anchor2x.Text);
                Anchor_2.y = (float)Convert.ToDouble(Anchor2y.Text);
                W = (float)Convert.ToDouble(Width.Text);
                H = (float)Convert.ToDouble(Height.Text);
                scalex = pnlMap.Width / W;
                scaley = pnlMap.Height / H;
                // Update DataGridView when tag count changes
                InitializeTagRows();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Please set up the map before starting.", "Notification",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void NameTagID()
        {
            Tags[0].tagID = "6C1DEBAFADB4";
            Tags[1].tagID = "6C1DEBAFAE18";
        }
        // Function to draw a point at coordinates x, y with color and size
        public void DrawPoint(Graphics g, string text, float x, float y, Color color, float size = 10)
        {
            // Apply scale to convert from logical to screen coordinates
            float screenX = x * scalex;
            float screenY = y * scaley;

            // Draw point as solid circle
            using (SolidBrush brush = new SolidBrush(color))
            {
                // Position circle so its center is at the point to be drawn
                g.FillEllipse(brush, screenX - size / 2, screenY - size / 2, size, size);
            }

            using (SolidBrush brush = new SolidBrush(color))
            {
                g.DrawString(text, font, brush, screenX, screenY);
            }
        }
        public void DrawLine(Graphics g, float x1, float y1, float x2, float y2, Color color, float thickness = 2)
        {
            // Apply scale to convert from logical to screen coordinates
            float screenX1 = x1 * scalex;
            float screenY1 = y1 * scaley;
            float screenX2 = x2 * scalex;
            float screenY2 = y2 * scaley;

            // Draw line between two points
            using (Pen pen = new Pen(color, thickness))
            {
                g.DrawLine(pen, screenX1, screenY1, screenX2, screenY2);
            }
        }
        public void DrawText(Graphics g, Tag tag, Color color)
        {
            // Apply scale to convert from logical to screen coordinates
            float screenX = tag.x * scalex;
            float screenY = tag.y * scaley;
            //Text
            string text = $"Tag({tag.x}, {tag.y})";
            // Draw text
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.DrawString(text, font, brush, screenX, screenY);
            }
        }
        private void TagFunction(Graphics g, Tag tag)
        {
            // Draw tag trail if enabled
            if (tag.TrailPoints.Count > 0)
            {
                DrawTrail(g, tag, Color.FromArgb(150, Color.Blue));
            }
            //draw Tag point
            DrawPoint(g, $"    Tag {tag.tagID}", tag.x, tag.y, Color.Red);
            // Draw line from Anchor_1 to tag
            DrawLine(g, Anchor_1.x, Anchor_1.y, tag.x, tag.y, Color.Yellow);
            // Draw line from Anchor_2 to tag
            DrawLine(g, Anchor_2.x, Anchor_2.y, tag.x, tag.y, Color.Yellow);
            //draw coordinates
            //DrawText(g,Tags[0] ,Color.White);
            // Show distance from Anchor_1 to tag
            DrawDistanceLabel(g, Anchor_1.x, Anchor_1.y, tag.x, tag.y, distToAnchor1);

            // Show distance from Anchor_2 to tag
            DrawDistanceLabel(g, Anchor_2.x, Anchor_2.y, tag.x, tag.y, distToAnchor2);

        }
        private void DrawDistanceLabel(Graphics g, float x1, float y1, float x2, float y2, double distance)
        {
            // Calculate midpoint of line segment
            float midX = (x1 + x2) / 2;
            float midY = (y1 + y2) / 2;

            // Apply scale to convert from logical to screen coordinates
            float screenMidX = midX * scalex;
            float screenMidY = midY * scaley;

            // Create text showing distance
            string distanceText = $"{distance:F4}";

            // Create bold font and background to highlight text
            using (System.Drawing.Font boldFont = new System.Drawing.Font("Arial", 10, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            using (SolidBrush backBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            {
                // Measure size of text string
                SizeF textSize = g.MeasureString(distanceText, boldFont);

                // Draw background rectangle
                g.FillRectangle(backBrush,
                    screenMidX - textSize.Width / 2 - 2,
                    screenMidY - textSize.Height / 2 - 2,
                    textSize.Width + 4,
                    textSize.Height + 4);

                // Draw distance text
                g.DrawString(distanceText,
                    boldFont,
                    textBrush,
                    screenMidX - textSize.Width / 2,
                    screenMidY - textSize.Height / 2);
            }
        }
        private void ReceiveData(Tag tag)
        {
            ProcessMQTTdata(tag);
            tag.LastUpdateTime = DateTime.Now;

            // Add current position to trail if tracking enabled
            if (tag.TrackTrail)
            {
                tag.TrailPoints.Add(new PointF(tag.x, tag.y));

                // Limit trail length
                if (tag.TrailPoints.Count > tag.MaxTrailPoints)
                {
                    tag.TrailPoints.RemoveAt(0);
                }
            }
        }
        private void SetupPanelForDoubleBuffering()
        {

            pnlMap.AllowDrop = false;
            // Enable double buffering for panel using reflection (since Panel doesn't have public DoubleBuffered property)
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, pnlMap, new object[] { true });

        }
        private void LoadMapBitmap()
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select map image";
                    openFileDialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Release old bitmap if exists
                        if (mapBitmap != null)
                        {
                            mapBitmap.Dispose();
                        }

                        // Load original image
                        Bitmap originalBitmap = new Bitmap(openFileDialog.FileName);

                        // Calculate ratio to maintain aspect ratio
                        float ratio = Math.Min(
                            (float)pnlMap.Width / originalBitmap.Width,
                            (float)pnlMap.Height / originalBitmap.Height
                        );

                        // Calculate new size maintaining aspect ratio
                        int newWidth = (int)(originalBitmap.Width * pnlMap.Width / originalBitmap.Width);
                        int newHeight = (int)(originalBitmap.Height * pnlMap.Height / originalBitmap.Height);

                        // Create bitmap with panel size
                        mapBitmap = new Bitmap(pnlMap.Width, pnlMap.Height);

                        // Create Graphics object to draw on new bitmap
                        using (Graphics g = Graphics.FromImage(mapBitmap))
                        {
                            // Set high quality drawing settings
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                            // Make background white
                            g.Clear(Color.White);

                            // Calculate position to center image
                            int x = (pnlMap.Width - newWidth) / 2;
                            int y = (pnlMap.Height - newHeight) / 2;

                            // Draw scaled image with centered position
                            g.DrawImage(originalBitmap, x, y, newWidth, newHeight);
                        }

                        // Release original image
                        originalBitmap.Dispose();

                        // Redraw panel after loading image
                        pnlMap.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading image: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddTrailTrackingControls()
        {
        }
        private void DrawTrail(Graphics g, Tag tag, Color color, float thickness = 3)
        {

            if (tag.TrailPoints.Count < 2) return;

            // Only draw n newest points, e.g. 10-20 points
            int pointsToShow = Math.Min(1000, tag.TrailPoints.Count);
            int startIndex = tag.TrailPoints.Count - pointsToShow;

            PointF[] screenPoints = new PointF[pointsToShow];
            for (int i = 0; i < pointsToShow; i++)
            {
                screenPoints[i] = new PointF(
                    tag.TrailPoints[startIndex + i].X * scalex,
                    tag.TrailPoints[startIndex + i].Y * scaley
                );
            }

            using (Pen pen = new Pen(Color.FromArgb(150, color), thickness))
            {
                g.DrawLines(pen, screenPoints);
            }
        }

        private void SaveTrailData(Tag tag)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    saveFileDialog.Title = "Save Trail Data";
                    saveFileDialog.DefaultExt = "csv";
                    saveFileDialog.FileName = $"TagTrail_{tag.tagID}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                        {
                            // Header
                            writer.WriteLine("ID,Seq,X,Y,Time,Distance to Anchor1,Distance to Anchor2");

                            // Data
                            for (int i = 0; i < tag.TrailPoints.Count; i++)
                            {
                                PointF point = tag.TrailPoints[i];

                                // Calculate distance from point to anchors
                                distToAnchor1 = CalculateDistance(point.X, point.Y, Anchor_1.x, Anchor_1.y);
                                distToAnchor2 = CalculateDistance(point.X, point.Y, Anchor_2.x, Anchor_2.y);


                                // Write data for each point
                                writer.WriteLine($"{tag.ToString()},{i + 1},{point.X},{point.Y}," +
                                    $"{DateTime.Now.AddSeconds(-tag.TrailPoints.Count + i).ToString("yyyy-MM-dd HH:mm:ss.fff")}," +
                                    $"{distToAnchor1:F2},{distToAnchor2:F2}");
                            }
                        }
                        MessageBox.Show("Trail data has been saved successfully.", "Notification",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving trail data: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private double CalculateDistance(float x1, float y1, float x2, float y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }
        private void ApplyPositionFilter(Tag tag, float newX, float newY)
        {
            try
            {
                // Initialize filter if first time
                if (tag.lastFilteredX == 0 && tag.lastFilteredY == 0)
                {
                    tag.filterX.SetInitialValue(newX);
                    tag.filterY.SetInitialValue(newY);
                    tag.lastFilteredX = newX;
                    tag.lastFilteredY = newY;

                    // Initialize list with first value
                    tag.recentX = new List<float> { newX, newX, newX };
                    tag.recentY = new List<float> { newY, newY, newY };

                    tag.targetX = newX;
                    tag.targetY = newY;
                    return;
                }

                // Apply Kalman filter
                float filteredX = tag.filterX.Update(newX);
                float filteredY = tag.filterY.Update(newY);

                // Save filtered value
                tag.lastFilteredX = filteredX;
                tag.lastFilteredY = filteredY;

                // Check for outliers
                if (tag.recentX.Count >= 3)
                {
                    if (IsOutlier(filteredX, filteredY, tag))
                    {
                        // Skip outlier point
                        return;
                    }
                }

                // Add to recent values list
                tag.recentX.Add(filteredX);
                tag.recentY.Add(filteredY);
                if (tag.recentX.Count > 5) tag.recentX.RemoveAt(0);
                if (tag.recentY.Count > 5) tag.recentY.RemoveAt(0);

                // Median filter if enough data
                if (tag.recentX.Count >= 3)
                {
                    tag.targetX = tag.recentX.OrderBy(x => x).ElementAt(tag.recentX.Count / 2);
                    tag.targetY = tag.recentY.OrderBy(y => y).ElementAt(tag.recentY.Count / 2);
                }
                else
                {
                    // Not enough data, use filtered value directly
                    tag.targetX = filteredX;
                    tag.targetY = filteredY;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApplyPositionFilter Error: {ex.Message}");
            }
        }

        /*private void UpdateVelocity(Tag tag, float newX, float newY, double timeDelta)
        {
            // Calculate velocity from previous position
            tag.velocityX = (newX - tag.lastFilteredX) / (float)timeDelta;
            tag.velocityY = (newY - tag.lastFilteredY) / (float)timeDelta;

            // Limit velocity to avoid abnormal values
            float maxVelocity = 50.0f; // Units per second
            tag.velocityX = Math.Max(-maxVelocity, Math.Min(maxVelocity, tag.velocityX));
            tag.velocityY = Math.Max(-maxVelocity, Math.Min(maxVelocity, tag.velocityY));
        }*/
        private bool IsOutlier(float newX, float newY, Tag tag)
        {
            if (tag.recentX.Count < 3) return false;

            // Calculate recent average
            float avgX = tag.recentX.Average();
            float avgY = tag.recentY.Average();

            // Calculate standard deviation
            float stdX = (float)Math.Sqrt(tag.recentX.Select(x => Math.Pow(x - avgX, 2)).Average());
            float stdY = (float)Math.Sqrt(tag.recentY.Select(y => Math.Pow(y - avgY, 2)).Average());

            // Increase factor from 3 to 5 to accept more points
            return Math.Abs(newX - avgX) > 5 * stdX || Math.Abs(newY - avgY) > 5 * stdY;
        }
        private void InitializeTagDataGridView()
        {
            // Set name for DataGridView for easy reference
            dgvTags.Name = "dgvTags";

            // Clear all existing columns
            dgvTags.Columns.Clear();

            // Add necessary columns
            dgvTags.Columns.Add("TagID", "ID");
            dgvTags.Columns.Add("X", "X");
            dgvTags.Columns.Add("Y", "Y");
            dgvTags.Columns.Add("Status", "Status");

            // Set properties for DataGridView
            dgvTags.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTags.AllowUserToAddRows = false;
            dgvTags.AllowUserToDeleteRows = false;
            dgvTags.ReadOnly = true;
            // Set background color and font
            dgvTags.DefaultCellStyle.BackColor = Color.White;
            dgvTags.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvTags.EnableHeadersVisualStyles = false;
            dgvTags.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgvTags.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTags.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font(dgvTags.Font, FontStyle.Bold);
            // Initialize row for each tag
            InitializeTagRows();
        }
        private void InitializeTagRows()
        {
            // Clear all existing rows
            dgvTags.Rows.Clear();

            // Add a row for each tag
            foreach (Tag tag in Tags)
            {
                // Add a new row with tag ID
                int rowIndex = dgvTags.Rows.Add();
                DataGridViewRow row = dgvTags.Rows[rowIndex];

                // Set ID column value and assign Tag ID to Row's Tag for easy lookup later
                row.Cells["TagID"].Value = tag.tagID;
                row.Cells["X"].Value = "0";
                row.Cells["Y"].Value = "0";
                row.Cells["Status"].Value = tag.IsConnected ? "Connected" : "Disconnected";

                // Store tag ID as Tag of row for easy lookup
                row.Tag = tag.tagID;
            }
        }
        private void UpdateTagsDataGridView()
        {
            // Loop through each tag and update corresponding row
            foreach (Tag tag in Tags)
            {
                // Find row corresponding to this tag
                DataGridViewRow row = FindTagRow(tag.tagID);

                if (row != null)
                {
                    // Update data in cells
                    row.Cells["X"].Value = tag.x.ToString("F4");
                    row.Cells["Y"].Value = tag.y.ToString("F4");
                    row.Cells["Status"].Value = tag.IsConnected ? "Online" : "Offline";

                    // Change row background color based on status
                    if (tag.HasAlarm)
                    {
                        // Use color based on alarm type
                        switch (tag.CurrentAlarm)
                        {
                            case AlarmType.VelocityTooHigh:
                                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 200); // Light yellow
                                break;
                            case AlarmType.ConnectionLost:
                                row.DefaultCellStyle.BackColor = Color.LightPink;
                                break;
                            case AlarmType.OutOfBounds:
                                row.DefaultCellStyle.BackColor = Color.FromArgb(200, 200, 255); // Light blue
                                break;
                            default:
                                row.DefaultCellStyle.BackColor = Color.Yellow;
                                break;
                        }
                        // Bold text for tag with alarm
                        row.DefaultCellStyle.Font = new System.Drawing.Font(dgvTags.Font, FontStyle.Bold);
                    }
                    else if (tag.IsConnected)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightGreen;
                        row.DefaultCellStyle.Font = new System.Drawing.Font(dgvTags.Font, FontStyle.Regular);
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.LightPink;
                        row.DefaultCellStyle.Font = new System.Drawing.Font(dgvTags.Font, FontStyle.Regular);
                    }
                }
            }
        }

        // Method to find tag row by ID
        private DataGridViewRow FindTagRow(string tagID)
        {
            foreach (DataGridViewRow row in dgvTags.Rows)
            {
                if (row.Tag != null && row.Tag.ToString() == tagID)
                {
                    return row;
                }
            }
            return null;
        }
        private void UpdateVelocity(Tag tag, float newX, float newY, double timeDelta)
        {
            // Calculate velocity for each axis
            tag.velocityX = (newX - tag.lastFilteredX) / (float)timeDelta;
            tag.velocityY = (newY - tag.lastFilteredY) / (float)timeDelta;

            // Calculate total velocity (magnitude)
            tag.velocityMagnitude = (float)Math.Sqrt(
                Math.Pow(tag.velocityX, 2) + Math.Pow(tag.velocityY, 2));

            // Save velocity to history for analysis
            if (tag.velocityHistory == null)
                tag.velocityHistory = new RollingPointPairList(1000);

            tag.velocityHistory.Add(DateTime.Now.ToOADate(), tag.velocityMagnitude);

            // Add X position tracking
            if (tag.xPositionHistory == null)
                tag.xPositionHistory = new RollingPointPairList(1000);

            tag.xPositionHistory.Add(DateTime.Now.ToOADate(), newX);

            // Limit velocity to avoid abnormal values
            float maxVelocity = 50.0f; // Units per second
            tag.velocityX = Math.Max(-maxVelocity, Math.Min(maxVelocity, tag.velocityX));
            tag.velocityY = Math.Max(-maxVelocity, Math.Min(maxVelocity, tag.velocityY));
            tag.velocityMagnitude = Math.Min(maxVelocity, tag.velocityMagnitude);
        }
        private void InitializeVelocityGraph()
        {
            zgcVelocity = new ZedGraphControl();
            zgcVelocity.Location = new System.Drawing.Point(3, 36);
            zgcVelocity.Size = new System.Drawing.Size(379, 146);
            zgcVelocity.Dock = DockStyle.Fill;
            zgcVelocity.GraphPane.Title.Text = "Tag X Position Over Time";
            zgcVelocity.GraphPane.XAxis.Title.Text = "Time";
            zgcVelocity.GraphPane.YAxis.Title.Text = "X Position (units)";

            // Set time scale
            zgcVelocity.GraphPane.XAxis.Type = AxisType.Date;
            zgcVelocity.GraphPane.XAxis.Scale.Format = "HH:mm:ss";
            zgcVelocity.GraphPane.XAxis.Scale.MajorUnit = DateUnit.Second;
            zgcVelocity.GraphPane.XAxis.Scale.MajorStep = 5; // 5 seconds per mark

            // Set position scale - automatic scaling will be applied later
            zgcVelocity.GraphPane.YAxis.Scale.Min = 0;
            zgcVelocity.GraphPane.YAxis.Scale.Max = W; // Use map width as initial scale

            // Color array for curves
            Color[] colors = { Color.Blue, Color.Green, Color.Orange, Color.Purple, Color.Brown };

            // Initialize curve for each tag
            for (int i = 0; i < Tags.Count; i++)
            {
                Tag tag = Tags[i];
                Color color = colors[i % colors.Length];

                // Initialize x position history if not already
                if (tag.xPositionHistory == null)
                    tag.xPositionHistory = new RollingPointPairList(100);

                // Create position curve
                LineItem positionCurve = zgcVelocity.GraphPane.AddCurve(
                    $"X Position - {tag.tagID}",
                    tag.xPositionHistory,
                    color,
                    SymbolType.Circle);

                positionCurve.Symbol.Size = 5;
                positionCurve.Symbol.Fill = new Fill(color);

                // Store reference to curve
                velocityCurves.Add(tag.tagID, positionCurve);
            }

            // Add to form
            panelVelocity.Controls.Add(zgcVelocity);

            zgcVelocity.AxisChange();
            zgcVelocity.Invalidate();
        }
        private void UpdateVelocityGraph()
        {
            if (zgcVelocity == null) return;

            // Get current time for X axis
            double now = DateTime.Now.ToOADate();
            double oneMinuteAgo = DateTime.Now.AddMinutes(-1).ToOADate();

            // Set X axis display range (last minute)
            zgcVelocity.GraphPane.XAxis.Scale.Min = oneMinuteAgo;
            zgcVelocity.GraphPane.XAxis.Scale.Max = now;

            // Find min and max x position to adjust scale
            float minX = 0;
            float maxX = W; // Default to map width

            foreach (Tag tag in Tags)
            {
                // Update maximum position to adjust Y axis
                if (tag.x > maxX)
                    maxX = tag.x;
                if (tag.x < minX)
                    minX = tag.x;

                // Check if tag has corresponding curve
                if (!velocityCurves.ContainsKey(tag.tagID))
                {
                    // Create new curve if none exists
                    Color color = Color.FromArgb(new Random().Next(256), new Random().Next(256), new Random().Next(256));

                    // Initialize x position history if not already
                    if (tag.xPositionHistory == null)
                        tag.xPositionHistory = new RollingPointPairList(100);

                    LineItem positionCurve = zgcVelocity.GraphPane.AddCurve(
                        $"X Position - {tag.tagID}",
                        tag.xPositionHistory,
                        color,
                        SymbolType.Circle);

                    positionCurve.Symbol.Size = 5;
                    positionCurve.Symbol.Fill = new Fill(color);

                    velocityCurves.Add(tag.tagID, positionCurve);
                }
                else
                {
                    // Update existing curve
                    LineItem positionCurve = velocityCurves[tag.tagID];

                    // Ensure curve data matches tag data
                    if (positionCurve != null && tag.xPositionHistory != null)
                    {
                        // Reassign point list to curve
                        positionCurve.Points = tag.xPositionHistory;
                    }
                }
            }

            // Add some margin to the Y scale
            float margin = (maxX - minX) * 0.1f;
            float lowerLimit = Math.Max(0, minX - margin);
            float upperLimit = Math.Max(W, maxX + margin);

            // Apply Y scale if significantly different from current
            if (Math.Abs(zgcVelocity.GraphPane.YAxis.Scale.Min - lowerLimit) > 2.0 ||
                Math.Abs(zgcVelocity.GraphPane.YAxis.Scale.Max - upperLimit) > 2.0)
            {
                zgcVelocity.GraphPane.YAxis.Scale.Min = lowerLimit;
                zgcVelocity.GraphPane.YAxis.Scale.Max = upperLimit;
            }

            // Update graph
            zgcVelocity.AxisChange();
            zgcVelocity.Invalidate();
        }
        // Thêm vào TabPage4 của Form2
        private void InitializeAboutTab()
        {
            tabPage4.Controls.Clear();

            // Tạo panel chứa toàn bộ nội dung
            Panel aboutPanel = new Panel();
            aboutPanel.Dock = DockStyle.Fill;
            aboutPanel.AutoScroll = true;
            aboutPanel.Padding = new Padding(40);
            aboutPanel.BackColor = Color.White;

            int yPosition = 0;

            // Header
            System.Windows.Forms.Label headerLabel = CreateStyledLabel("About Indoor Positioning System", 24, FontStyle.Bold, Color.FromArgb(0, 122, 204));
            headerLabel.Location = new Point(0, yPosition);
            headerLabel.Size = new Size(700, 40);
            aboutPanel.Controls.Add(headerLabel);
            yPosition += 60;

            // Version Info
            System.Windows.Forms.Label versionLabel = CreateStyledLabel("Version 1.0 | Developed by Thanh Huynh Chi", 12, FontStyle.Regular, Color.Gray);
            versionLabel.Location = new Point(0, yPosition);
            versionLabel.Size = new Size(700, 25);
            aboutPanel.Controls.Add(versionLabel);
            yPosition += 40;

            // Introduction Section
            AddSectionHeader(aboutPanel, "Project Overview", ref yPosition);
            AddParagraph(aboutPanel,
                "The Indoor Positioning System is an advanced solution designed to provide real-time location tracking " +
                "of assets and personnel within indoor environments. Utilizing the Angle of Arrival (AOA) method, " +
                "our system achieves high accuracy positioning without requiring complex infrastructure.",
                ref yPosition);

            // Key Features Section
            AddSectionHeader(aboutPanel, "Key Features", ref yPosition);
            AddBulletPoints(aboutPanel, new string[]
            {
        "Real-time position tracking with sub-meter accuracy",
        "Support for multiple tracking tags and anchors",
        "Advanced velocity monitoring and movement analysis",
        "Intelligent alarm system for safety and security",
        "Comprehensive dashboard with real-time analytics",
        "Flexible MQTT communication protocol",
        "Customizable trail recording and playback",
        "Professional reporting capabilities"
            }, ref yPosition);

            // Technology Stack Section
            AddSectionHeader(aboutPanel, "Technology Stack", ref yPosition);
            AddParagraph(aboutPanel,
                "Built on Microsoft .NET Framework with C# and Windows Forms, our application integrates " +
                "modern libraries including ZedGraph for data visualization, MQTT.NET for IoT communication, " +
                "and custom mathematical algorithms for precise position calculation. The system is designed " +
                "to be scalable and easily integrable with existing infrastructure.",
                ref yPosition);

            // How It Works Section
            AddSectionHeader(aboutPanel, "How It Works", ref yPosition);
            AddParagraph(aboutPanel,
                "The system employs the Angle of Arrival (AOA) method, where multiple anchors measure the angle " +
                "at which signals from tracking tags arrive. By combining these angle measurements through advanced " +
                "triangulation algorithms, the system calculates precise 2D coordinates. Our implementation includes:\n\n" +
                "• Kalman filtering for smoother position tracking\n" +
                "• Real-time processing with MQTT protocol\n" +
                "• Advanced error correction and outlier detection\n" +
                "• Adaptive positioning for dynamic environments",
                ref yPosition);

            // Applications Section
            AddSectionHeader(aboutPanel, "Applications", ref yPosition);
            AddParagraph(aboutPanel,
                "This system is ideal for various industries including manufacturing, warehousing, healthcare, " +
                "and educational institutions. Common use cases include asset tracking, personnel safety monitoring, " +
                "workflow optimization, and emergency response coordination.",
                ref yPosition);

            // Contact Section
            AddSectionHeader(aboutPanel, "Contact Information", ref yPosition);
            AddParagraph(aboutPanel,
                "For technical support, feature requests, or collaboration opportunities, please contact:\n\n" +
                "Developer: Thanh Huynh Chi\n" +
                "Email: thanh.huynhchi1422003@hcmut.edu.vn\n",
                ref yPosition);

            // Acknowledgments Section
            AddSectionHeader(aboutPanel, "Acknowledgments", ref yPosition);
            AddParagraph(aboutPanel,
                "Special thanks to the faculty advisors, research team members, and industry partners who " +
                "contributed to the development of this system. This project was developed as part of academic " +
                "research in collaboration with industry partners.",
                ref yPosition);

            // Footer
            System.Windows.Forms.Label footerLabel = CreateStyledLabel($"© {DateTime.Now.Year} Indoor Positioning System. All rights reserved.", 10, FontStyle.Italic, Color.Gray);
            footerLabel.Location = new Point(0, yPosition);
            footerLabel.Size = new Size(700, 20);
            aboutPanel.Controls.Add(footerLabel);

            tabPage4.Controls.Add(aboutPanel);
        }

        // Helper methods for creating styled components
        private System.Windows.Forms.Label CreateStyledLabel(string text, float fontSize, FontStyle style, Color color)
        {
            System.Windows.Forms.Label label = new System.Windows.Forms.Label();
            label.Text = text;
            label.Font = new System.Drawing.Font("Segoe UI", fontSize, style);
            label.ForeColor = color;
            label.AutoSize = true;
            label.MaximumSize = new Size(700, 0);
            return label;
        }

        private void AddSectionHeader(Panel panel, string text, ref int yPosition)
        {
            System.Windows.Forms.Label header = CreateStyledLabel(text, 16, FontStyle.Bold, Color.FromArgb(51, 51, 51));
            header.Location = new Point(0, yPosition);
            header.Size = new Size(700, 30);
            panel.Controls.Add(header);
            yPosition += 40;
        }

        private void AddParagraph(Panel panel, string text, ref int yPosition)
        {
            System.Windows.Forms.Label paragraph = CreateStyledLabel(text, 11, FontStyle.Regular, Color.FromArgb(70, 70, 70));
            paragraph.Location = new Point(0, yPosition);
            paragraph.MaximumSize = new Size(700, 0);
            panel.Controls.Add(paragraph);
            yPosition += paragraph.Height + 20;
        }

        private void AddBulletPoints(Panel panel, string[] points, ref int yPosition)
        {
            foreach (string point in points)
            {
                System.Windows.Forms.Label bullet = CreateStyledLabel("• " + point, 11, FontStyle.Regular, Color.FromArgb(70, 70, 70));
                bullet.Location = new Point(20, yPosition);
                bullet.Size = new Size(680, 20);
                panel.Controls.Add(bullet);
                yPosition += 25;
            }
            yPosition += 10;
        }

        // Gọi method này trong Form2_Load

        #endregion
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Class
        public class Anchor
        {
            public string anchorID { get; set; }
            public float x { get; set; }
            public float y { get; set; }
            public override string ToString()
            {
                return $"{anchorID}";
            }

        }
        public enum AlarmType
        {
            None,
            VelocityTooHigh,
            ConnectionLost,
            OutOfBounds,
            InvalidPosition
        }
        public class Tag
        { // Velocity properties
            public bool HasAlarm { get; set; } = false;
            public string AlarmMessage { get; set; } = "";
            public DateTime AlarmTime { get; set; } = DateTime.MinValue;
            public AlarmType CurrentAlarm { get; set; } = AlarmType.None;
            public float velocityX { get; set; } = 0;
            public float velocityY { get; set; } = 0;
            public float velocityMagnitude { get; set; } = 0;

            // Velocity and alarm related properties
            public RollingPointPairList velocityHistory { get; set; }
            public RollingPointPairList xPositionHistory { get; set; }
            public bool velocityAlarm { get; set; } = false;
            public float velocityThreshold { get; set; } = 5.0f;
            public float maxVelocity { get; set; } = 0;
            public float avgVelocity { get; set; } = 0;

            // Connection tracking properties
            public bool IsConnected { get; set; } = false;
            public DateTime LastConnectionTime { get; set; } = DateTime.MinValue;

            // Position and movement related properties
            public float previousX = 0;
            public float previousY = 0;
            public int stabilityCounter = 0;
            public bool isStable = false;
            public List<float> recentX = new List<float>(5);
            public List<float> recentY = new List<float>(5);
            public float targetX { get; set; }
            public float targetY { get; set; }
            public float smoothingFactor { get; set; } = 0.3f;

            // Basic Tag properties
            public string tagID { get; set; }
            public float x { get; set; }
            public float y { get; set; }

            // Trail related properties
            public List<PointF> TrailPoints { get; set; } = new List<PointF>();
            public bool TrackTrail { get; set; } = false;
            public int MaxTrailPoints { get; set; } = 10000;
            public DateTime LastUpdateTime { get; set; } = DateTime.Now;

            // Kalman filter related properties
            public KalmanFilter filterX = new KalmanFilter();
            public KalmanFilter filterY = new KalmanFilter();
            public float lastFilteredX = 0;
            public float lastFilteredY = 0;

            public override string ToString()
            {
                return $"{tagID}";
            }
        }
        public class MQTT
        {
            private MqttClient mqttClient;
            public string BrokerAddress { get; set; }

            // Event when data is received
            public event EventHandler<MqttDataEventArgs> DataReceived;

            public void Connect(string clientId)
            {
                try
                {
                    mqttClient = new MqttClient(BrokerAddress);
                    mqttClient.MqttMsgPublishReceived += OnMqttMsgReceived;
                    mqttClient.Connect(clientId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Cannot connect to MQTT broker: {ex.Message}");
                }
            }

            public void Subscribe(params string[] topics)
            {
                if (mqttClient != null && mqttClient.IsConnected)
                {
                    // Create QoS array corresponding to number of topics
                    byte[] qosLevels = new byte[topics.Length];
                    for (int i = 0; i < qosLevels.Length; i++)
                    {
                        qosLevels[i] = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;
                    }

                    mqttClient.Subscribe(topics, qosLevels);
                }
            }

            public void Publish(string topic, string message)
            {
                if (mqttClient != null && mqttClient.IsConnected)
                {
                    mqttClient.Publish(topic, Encoding.UTF8.GetBytes(message));
                }
            }

            public void Disconnect()
            {
                if (mqttClient != null && mqttClient.IsConnected)
                {
                    mqttClient.Disconnect();
                }
            }

            private void OnMqttMsgReceived(object sender, MqttMsgPublishEventArgs e)
            {
                string message = Encoding.UTF8.GetString(e.Message);
                string topic = e.Topic;

                // Trigger data received event
                DataReceived?.Invoke(this, new MqttDataEventArgs(topic, message));
            }
        }

        // Event parameter class for MQTT data
        public class MqttDataEventArgs : EventArgs
        {
            public string Topic { get; }
            public string Message { get; }

            public MqttDataEventArgs(string topic, string message)
            {
                Topic = topic;
                Message = message;
            }
        }
        public class KalmanFilter
        {
            // Filter parameters
            private float Q = 0.0001f;  // Reduced 100x from current value
            private float R = 0.5f;     // Increased to ignore measurement noise
            private float P = 1.0f;     // Estimated error
            private float K = 0.0f;     // Kalman gain
            private float X = 0.0f;     // Current value

            public void SetInitialValue(float initialValue)
            {
                X = initialValue;
            }

            public float Update(float measurement)
            {
                // Predict
                P = P + Q;

                // Update
                K = P / (P + R);
                X = X + K * (measurement - X);
                P = (1 - K) * P;

                return X;
            }
        }
        #endregion

    }
}