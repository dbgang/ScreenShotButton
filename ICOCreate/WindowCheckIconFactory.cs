using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace ICOCreate
{
   

public static class WindowCheckIconFactory
    {
        /// <summary>
        /// 动态生成类似图中的“多窗口/窗口选择并勾选”矢量图标
        /// </summary>
        /// <param name="size">图标尺寸，推荐 16, 32, 48 等</param>
        public static Bitmap CreateIcon(int size = 32)
        {
            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // 比例缩放基准（以 32x32 为基准计算坐标）
                float scale = size / 32f;
                g.ScaleTransform(scale, scale);

                // 1. 绘制背景/最下层窗口阴影与主体
                // 背景窗口 1 (左上)
                DrawWindow(g, 3, 6, 16, 12, Color.FromArgb(220, 225, 230), Color.FromArgb(140, 150, 160));

                // 中间窗口 (靠左后方)
                DrawWindow(g, 9, 8, 18, 13, Color.FromArgb(240, 245, 250), Color.FromArgb(0, 102, 180));

                // 前景主窗口 (右下方)
                DrawWindow(g, 12, 12, 18, 14, Color.FromArgb(255, 255, 255), Color.FromArgb(0, 120, 215), true);

                // 2. 在前景窗口中绘制勾选标志 (Checkmark)
                using (Pen checkPen = new Pen(Color.FromArgb(0, 102, 204), 2.5f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    // 勾选路径坐标
                    g.DrawLine(checkPen, 19, 18, 22, 21);
                    g.DrawLine(checkPen, 22, 21, 27, 15);
                }
            }
            return bmp;
        }

        /// <summary>
        /// 辅助方法：绘制单个模拟的 Windows 窗口
        /// </summary>
        private static void DrawWindow(Graphics g, float x, float y, float w, float h, Color bgColor, Color borderColor, bool hasTitleBarControls = false)
        {
            RectangleF rect = new RectangleF(x, y, w, h);

            // 窗口主体填充
            using (SolidBrush brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, rect);
            }

            // 标题栏
            float titleHeight = h > 12 ? 4.5f : 3f;
            RectangleF titleRect = new RectangleF(x, y, w, titleHeight);
            using (SolidBrush titleBrush = new SolidBrush(borderColor))
            {
                g.FillRectangle(titleBrush, titleRect);
            }

            // 窗口外边框
            using (Pen pen = new Pen(borderColor, 1.2f))
            {
                g.DrawRectangle(pen, x, y, w, h);
            }

            // 如果是前景窗口，绘制标题栏上的最小化/关闭等模拟小按钮
            if (hasTitleBarControls)
            {
                using (Pen controlPen = new Pen(Color.White, 0.8f))
                {
                    // 关闭按钮 'X'
                    g.DrawLine(controlPen, x + w - 3.5f, y + 1.2f, x + w - 1.5f, y + 3.2f);
                    g.DrawLine(controlPen, x + w - 3.5f, y + 3.2f, x + w - 1.5f, y + 1.2f);
                    // 最小化/还原短横线
                    g.DrawLine(controlPen, x + w - 6.5f, y + 2.2f, x + w - 5.0f, y + 2.2f);
                }
            }
        }
    }
} 
