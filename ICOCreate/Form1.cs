namespace ICOCreate
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {// 1. 生成 32x32 或 48x48 像素的高清图标 Bitmap
            Bitmap rectIconBmp = SnipIconFactory.CreateRectSnipIcon(32);

            // 2. 指定导出的 ico 文件路径
            string outputPath = @"D:\rect_snip.ico";

            // 3. 保存为真正的 ICO 文件
            SnipIconFactory.SaveAsIco(rectIconBmp, outputPath);

            Console.WriteLine($"ICO 文件已成功生成到: {outputPath}");
            //SnipIconFactory.CreateFullSnipIcon();
            //SnipIconFactory.CreateRectSnipIcon();
        }
    }
}
