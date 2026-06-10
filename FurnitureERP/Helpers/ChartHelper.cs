using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    // Simple bar chart control drawn via GDI+
    public class ChartControl : Control
    {
        private string[] _labels;
        private decimal[] _values;
        private string _currency = "";
        // 💡 請將此程式碼貼入您的 ChartControl 類別內部：
        private object _dataSource;
        public object DataSource
        {
            get { return _dataSource; }
            set
            {
                _dataSource = value;
                // 如果內部有原生 chart，就傳遞給它：
                // this.chart.DataSource = value; 
                this.Invalidate(); // 觸發重繪
            }
        }

        public void SetBarData(string[] labels, decimal[] values, string currency = "")
        {
            _labels = labels;
            _values = values;
            _currency = currency;
            Invalidate();
        }

        public Bitmap ToBitmap(int width = 0, int height = 0)
        {
            int w = width > 0 ? width : Math.Max(Width, 420);
            int h = height > 0 ? height : Math.Max(Height, 240);
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                OnPaint(new PaintEventArgs(g, new Rectangle(0, 0, w, h)));
            }
            return bmp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_labels == null || _values == null || _labels.Length == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var area = e.ClipRectangle;
            int padL = 60, padR = 20, padT = 20, padB = 40;
            int chartW = area.Width - padL - padR;
            int chartH = area.Height - padT - padB;
            if (chartW <= 0 || chartH <= 0) return;

            decimal maxVal = 1;
            foreach (var v in _values) if (v > maxVal) maxVal = v;

            // Y grid lines
            using (var gridPen = new Pen(Color.FromArgb(220, 228, 240)))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = padT + (int)(chartH * i / 4.0);
                    g.DrawLine(gridPen, padL, y, padL + chartW, y);
                    decimal yVal = maxVal * (4 - i) / 4;
                    string label = yVal >= 1000 ? $"{yVal / 1000:0}K" : yVal.ToString("0");
                    g.DrawString(label, new Font("Segoe UI", 7), Brushes.Gray, 2, y - 7);
                }
            }

            int gap = Math.Max(1, chartW / _labels.Length);
            int barW = Math.Max(12, gap / 2);
            int maxBarW = Math.Max(48, chartW / 5);
            if (_labels.Length <= 2)
                barW = Math.Min(barW, maxBarW);

            Color[] colors = {
                Color.FromArgb(63, 118, 210), Color.FromArgb(0, 168, 120),
                Color.FromArgb(230, 120, 20), Color.FromArgb(160, 40, 180),
                Color.FromArgb(220, 60, 60),  Color.FromArgb(20, 160, 200)
            };

            for (int i = 0; i < _labels.Length; i++)
            {
                int barH = _values[i] == 0 ? 2 : (int)(chartH * _values[i] / maxVal);
                int x = padL + i * gap + (gap - barW) / 2;
                int y = padT + chartH - barH;

                using (var brush = new LinearGradientBrush(
                    new Rectangle(x, y, barW, barH),
                    colors[i % colors.Length],
                    Color.FromArgb(180, colors[i % colors.Length]),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, x, y, barW, barH);
                }

                // Label
                g.DrawString(_labels[i], new Font("Segoe UI", 7), Brushes.DimGray,
                    x + barW / 2 - 10, padT + chartH + 4);

                // Value on top
                string valStr = _values[i] >= 1000 ? $"{_values[i] / 1000:0}K" : _values[i].ToString("0");
                g.DrawString(valStr, new Font("Segoe UI", 7, FontStyle.Bold),
                    new SolidBrush(colors[i % colors.Length]), x, y - 14);
            }
        }
    }

    // Simple pie chart control
    public class PieChartControl : Control
    {
        private string[] _labels;
        private float[] _values;

        public void SetData(string[] labels, float[] values)
        {
            _labels = labels;
            _values = values;
            Invalidate();
        }

        public Bitmap ToBitmap(int width = 0, int height = 0)
        {
            int w = width > 0 ? width : System.Math.Max(Width, 420);
            int h = height > 0 ? height : System.Math.Max(Height, 240);
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                OnPaint(new PaintEventArgs(g, new Rectangle(0, 0, w, h)));
            }
            return bmp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_labels == null || _values == null || _labels.Length == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color[] colors = {
                Color.FromArgb(63, 118, 210), Color.FromArgb(0, 168, 120),
                Color.FromArgb(230, 120, 20), Color.FromArgb(160, 40, 180)
            };

            float total = 0;
            foreach (var v in _values) total += v;

            var area = e.ClipRectangle;
            int legendReserve = Math.Max(110, Math.Min(150, area.Width / 3));
            int size = Math.Min(area.Width - legendReserve - 16, area.Height - 24);
            size = Math.Max(size, 60);
            var rect = new Rectangle(10, area.Top + (area.Height - size) / 2, size, size);

            float startAngle = -90f;
            for (int i = 0; i < _values.Length; i++)
            {
                float sweep = _values[i] / total * 360f;
                using (var brush = new SolidBrush(colors[i % colors.Length]))
                    g.FillPie(brush, rect, startAngle, sweep);
                g.DrawPie(Pens.White, rect, startAngle, sweep);
                startAngle += sweep;
            }

            // Legend
            int legendX = rect.Right + 12;
            int legendY = area.Top + (area.Height - _labels.Length * 20) / 2;
            for (int i = 0; i < _labels.Length; i++)
            {
                using (var brush = new SolidBrush(colors[i % colors.Length]))
                    g.FillRectangle(brush, legendX, legendY + i * 22, 12, 12);
                float pct = total > 0 ? _values[i] / total * 100f : 0f;
                g.DrawString($"{_labels[i]} ({pct:0}%)",
                    new Font("Segoe UI", 7.5f), Brushes.DimGray, legendX + 16, legendY + i * 22 - 1);
            }
        }
    }
}
