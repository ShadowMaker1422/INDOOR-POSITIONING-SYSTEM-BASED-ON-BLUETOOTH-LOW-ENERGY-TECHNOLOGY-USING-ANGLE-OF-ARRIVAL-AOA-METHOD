using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOFTWARE
{
    class CoordinateSystem
    {
        private Panel panel;
        private float originX;
        private float originY;
        private float scaleX;
        private float scaleY;

        public CoordinateSystem(Panel panel, float originX = 0, float originY = 0, float scaleX = 1, float scaleY = 1)
        {
            this.panel = panel;
            this.originX = originX;
            this.originY = originY;
            this.scaleX = scaleX;
            this.scaleY = scaleY;
        }

        // Chuyển đổi từ tọa độ Oxy sang tọa độ màn hình
        public PointF WorldToScreen(float worldX, float worldY)
        {
            float screenX = originX + (worldX * scaleX);
            float screenY = panel.Height - originY - (worldY * scaleY);
            return new PointF(screenX, screenY);
        }

        // Chuyển đổi từ tọa độ màn hình sang tọa độ Oxy
        public PointF ScreenToWorld(float screenX, float screenY)
        {
            float worldX = (screenX - originX) / scaleX;
            float worldY = (panel.Height - screenY - originY) / scaleY;
            return new PointF(worldX, worldY);
        }

        // Vẽ hình chữ nhật trong hệ tọa độ Oxy
        public void DrawRectangle(Graphics g, Pen pen, float x, float y, float width, float height)
        {
            // Trong hệ tọa độ Oxy, y là tọa độ của cạnh dưới của hình chữ nhật
            // Chuyển sang hệ tọa độ màn hình, y là tọa độ của cạnh trên
            PointF screenPoint = WorldToScreen(x, y);

            // Lưu ý: Trong hệ tọa độ màn hình, y tăng khi đi xuống
            // nên cần điều chỉnh vị trí y để vẽ hình chữ nhật
            g.DrawRectangle(pen,
                             screenPoint.X,
                             screenPoint.Y - (height * scaleY),
                             width * scaleX,
                             height * scaleY);
        }

        // Tô hình chữ nhật trong hệ tọa độ Oxy
        public void FillRectangle(Graphics g, Brush brush, float x, float y, float width, float height)
        {
            PointF screenPoint = WorldToScreen(x, y);
            g.FillRectangle(brush,
                             screenPoint.X,
                             screenPoint.Y - (height * scaleY),
                             width * scaleX,
                             height * scaleY);
        }

        // Vẽ đường thẳng trong hệ tọa độ Oxy
        public void DrawLine(Graphics g, Pen pen, float x1, float y1, float x2, float y2)
        {
            PointF startPoint = WorldToScreen(x1, y1);
            PointF endPoint = WorldToScreen(x2, y2);
            g.DrawLine(pen, startPoint, endPoint);
        }

        // Vẽ văn bản trong hệ tọa độ Oxy
        public void DrawString(Graphics g, string text, Font font, Brush brush, float x, float y)
        {
            PointF screenPoint = WorldToScreen(x, y);
            g.DrawString(text, font, brush, screenPoint);
        }

        // Vẽ trục tọa độ
        public void DrawAxes(Graphics g, Pen pen, float xMin, float xMax, float yMin, float yMax)
        {
            // Vẽ trục X
            DrawLine(g, pen, xMin, 0, xMax, 0);

            // Vẽ mũi tên trục X
            float arrowSize = 10 / scaleX;
            DrawLine(g, pen, xMax - arrowSize, arrowSize / 2, xMax, 0);
            DrawLine(g, pen, xMax - arrowSize, -arrowSize / 2, xMax, 0);

            // Vẽ trục Y
            DrawLine(g, pen, 0, yMin, 0, yMax);

            // Vẽ mũi tên trục Y
            DrawLine(g, pen, arrowSize / 2, yMax - arrowSize, 0, yMax);
            DrawLine(g, pen, -arrowSize / 2, yMax - arrowSize, 0, yMax);

            // Vẽ gốc tọa độ
            DrawString(g, "O", new Font("Arial", 8), Brushes.Black, -arrowSize, -arrowSize);
            DrawString(g, "X", new Font("Arial", 8), Brushes.Black, xMax - arrowSize, -arrowSize);
            DrawString(g, "Y", new Font("Arial", 8), Brushes.Black, -arrowSize, yMax - arrowSize);
        }

        // Vẽ lưới tọa độ
        public void DrawGrid(Graphics g, Pen pen, float spacing, float xMin, float xMax, float yMin, float yMax)
        {
            // Vẽ các đường lưới ngang
            for (float y = 0; y <= yMax; y += spacing)
            {
                if (y >= yMin)
                    DrawLine(g, pen, xMin, y, xMax, y);
            }
            for (float y = -spacing; y >= yMin; y -= spacing)
            {
                if (y <= yMax)
                    DrawLine(g, pen, xMin, y, xMax, y);
            }

            // Vẽ các đường lưới dọc
            for (float x = 0; x <= xMax; x += spacing)
            {
                if (x >= xMin)
                    DrawLine(g, pen, x, yMin, x, yMax);
            }
            for (float x = -spacing; x >= xMin; x -= spacing)
            {
                if (x <= xMax)
                    DrawLine(g, pen, x, yMin, x, yMax);
            }
        }
    }
}
