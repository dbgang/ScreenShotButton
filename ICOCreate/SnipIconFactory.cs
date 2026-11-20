using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ICOCreate
{
  
    public static class SnipIconFactory
    {/// <summary>
     /// 将 Bitmap 保存为真正的标准 ICO 文件（支持主流尺寸或单尺寸嵌入）
     /// </summary>
        public static void SaveAsIco(Bitmap bmp, string filePath)
        {
            // 确保使用 32 位 ARGB 格式以保留透明通道
            using (Bitmap argbBmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(argbBmp))
                {
                    g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                }

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    // 1. 图标文件头 (ICONDIR)
                    bw.Write((short)0); // 保留字，必须为 0
                    bw.Write((short)1); // 资源类型：1 =图标 (Icon)
                    bw.Write((short)1); // 图像数量：这里写入 1 个主尺寸

                    // 将 Bitmap 转换为 PNG 字节流（现代 ICO 支持内嵌 PNG 格式，兼容性极佳且不失真）
                    byte[] pngBytes;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        argbBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        pngBytes = ms.ToArray();
                    }

                    // 2. 图像目录项 (ICONDIRENTRY)
                    byte width = (byte)(argbBmp.Width >= 256 ? 0 : argbBmp.Width);
                    byte height = (byte)(argbBmp.Height >= 256 ? 0 : argbBmp.Height);

                    bw.Write(width);                    // 宽度
                    bw.Write(height);                   // 高度
                    bw.Write((byte)0);                  // 调色板颜色数 (无调色板填 0)
                    bw.Write((byte)0);                  // 保留字
                    bw.Write((short)1);                 // 颜色平面数
                    bw.Write((short)32);                // 位深度
                    bw.Write(pngBytes.Length);          // 图像数据字节大小
                    bw.Write(6 + 16);                   // 图像数据在文件中的偏移量 (头部6字节 + 目录项16字节)

                    // 3. 写入实际图像数据
                    bw.Write(pngBytes);
                    bw.Flush();
                }
            }
        }
        /// <summary>
        /// 生成“区域截屏”矢量图标 (Rectangular Snip)
        /// </summary>
        public static Bitmap CreateRectSnipIcon(int size = 16)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 绘制相框/裁剪框主体
                using (Pen pen = new Pen(Color.FromArgb(51, 153, 255), 1.5f))
                {
                    pen.DashStyle = DashStyle.Dash; // 虚线边框，体现截屏框
                    g.DrawRectangle(pen, 2, 2, size - 5, size - 5);
                }

                // 绘制右下角的调整手柄（小方块）
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                {
                    g.FillRectangle(brush, size - 5, size - 5, 4, 4);
                }

                // 绘制中心十字准星
                using (Pen crossPen = new Pen(Color.FromArgb(100, 100, 100), 1f))
                {
                    int mid = size / 2;
                    g.DrawLine(crossPen, mid, mid - 3, mid, mid + 3);
                    g.DrawLine(crossPen, mid - 3, mid, mid + 3, mid);
                }
            }
            return bmp;
        }

        /// <summary>
        /// 生成“全屏截屏”矢量图标 (Full Screen Snip)
        /// </summary>
        public static Bitmap CreateFullSnipIcon(int size = 16)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 外框表示屏幕
                using (Pen pen = new Pen(Color.FromArgb(51, 153, 255), 1.5f))
                {
                    g.DrawRectangle(pen, 1, 2, size - 3, size - 6);
                }

                // 顶部模拟系统标题栏
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(51, 153, 255)))
                {
                    g.FillRectangle(brush, 1, 2, size - 3, 3);
                }

                // 内部小光标/快门装饰
                using (SolidBrush innerBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                {
                    g.FillEllipse(innerBrush, size / 2 - 2, size / 2 - 1, 4, 4);
                }
            }
            return bmp;
        }

        /// <summary>
        /// 生成“窗口截屏”矢量图标 (Window Snip)
        /// </summary>
        public static Bitmap CreateWindowSnipIcon(int size = 16)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 背景窗口（阴影/错层效果）
                using (Pen bgPen = new Pen(Color.FromArgb(180, 180, 180), 1f))
                {
                    g.DrawRectangle(bgPen, 4, 4, size - 7, size - 8);
                }

                // 前景激活窗口
                using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(240, 248, 255)))
                using (Pen fgPen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                {
                    g.FillRectangle(fillBrush, 1, 1, size - 7, size - 7);
                    g.DrawRectangle(fgPen, 1, 1, size - 7, size - 7);
                }

                // 前景窗口标题栏
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                {
                    g.FillRectangle(titleBrush, 1, 1, size - 7, 3);
                }
            }
            return bmp;
        }
    }
}
