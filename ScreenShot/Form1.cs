using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenShot
{
    public partial class Form1 : Form
    {
        private PictureBox pictureBox1;
        private Label lblStatus;
        private WebApplication _webApp;

        // 目标存储路径
        private readonly string sharedDir = @"D:\01ScreentShot";
        private readonly string tempDir = @"C:\TempScreenshots";

        // 常驻浏览器实例及状态标志
        private IWebDriver _driver;
        private bool _isBrowserReady = false;
        private readonly object _browserLock = new object();

        // 内部模式状态维护（1 或 2）
        private int currentMode = 1;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomControls();

            EnsureDirectoryExists(sharedDir);
            EnsureDirectoryExists(tempDir);
        }

        private void EnsureDirectoryExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"自动创建文件夹 [{path}] 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeCustomControls()
        {
            this.Width = 900;
            this.Height = 700;
            this.Text = "仪器截图服务端 (.NET 8 极速常驻版 - 模式切换控制)";

            pictureBox1 = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(pictureBox1);

            lblStatus = new Label
            {
                Text = "正在初始化服务...",
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblStatus);

            this.Load += new System.EventHandler(this.Form1_Load);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await StartEmbeddedWebServerAsync();
            Task.Run(() => RestartBrowserWithCurrentMode());
        }

        /// <label>根据当前模式启动或重启浏览器（支持动态控制无头模式）</label>
        private void RestartBrowserWithCurrentMode()
        {
            lock (_browserLock)
            {
                _isBrowserReady = false;

                // 1. 如果已有旧浏览器，先安全关闭并释放
                try
                {
                    if (_driver != null)
                    {
                        _driver.Quit();
                        _driver.Dispose();
                        _driver = null;
                    }
                }
                catch { }

                try
                {
                    UpdateStatus($"正在启动浏览器 (当前模式: {currentMode})...");
                    EnsureDirectoryExists(sharedDir);
                    EnsureDirectoryExists(tempDir);

                    var chromeOptions = new ChromeOptions();
                    var service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    // 基础下载配置
                    chromeOptions.AddUserProfilePreference("download.default_directory", tempDir);
                    chromeOptions.AddUserProfilePreference("download.prompt_for_download", false);
                    chromeOptions.AddUserProfilePreference("directory_upgrade", true);
                    chromeOptions.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);

                    // =========================================================================
                    // 【核心逻辑】：根据不同模式决定是否开启无头模式（完全后台隐藏运行）
                    // =========================================================================
                    if (currentMode == 1)
                    {
                        // 模式 1：显示浏览器界面（方便你随时看得到网页运行情况）
                        // （不加 headless 参数，浏览器窗口会正常弹出来）
                    }
                    else
                    {
                        // 模式 2：开启无头模式（完全隐藏浏览器界面，静默在后台运行）
                        chromeOptions.AddArgument("--headless=new");
                    }

                    _driver = new ChromeDriver(service, chromeOptions);
                    _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(20);

                    // 根据模式可以加载不同的仪器页面地址（如果地址一样则加载同一个）
                    string targetUrl = "http://192.168.1.48/Instrument/novnc/vnc_auto.php";

                    _driver.Navigate().GoToUrl(targetUrl);
                    Thread.Sleep(3000); // 等待 WebSocket 连接渲染
                    _isBrowserReady = true;

                    string modeDesc = (currentMode == 1) ? "模式 1 (显示窗口)" : "模式 2 (无头隐藏)";
                    UpdateStatus($"常驻浏览器已就绪 -> 当前处于【 {modeDesc} 】");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"[错误] 启动浏览器失败: {ex.Message}");
                }
            }
        }

        private Task StartEmbeddedWebServerAsync()
        {
            try
            {
                var builder = WebApplication.CreateBuilder();
                builder.WebHost.UseUrls("http://0.0.0.0:9991");
                _webApp = builder.Build();

                // 1. 截图接口
                _webApp.MapGet("/trigger-screenshot", async (HttpContext context) =>
                {
                    UpdateStatus($"收到截图请求 (当前模式: {currentMode})，正在后台执行...");

                    try
                    {
                        if (!_isBrowserReady || _driver == null)
                        {
                            return Results.Json(new { status = "error", message = "Browser not ready" }, statusCode: 500);
                        }

                        var tcs = new TaskCompletionSource<string>();

                        _ = Task.Run(() =>
                        {
                            try
                            {
                                string path = PerformFastSeleniumScreenshot();
                                tcs.SetResult(path);
                            }
                            catch (Exception ex)
                            {
                                tcs.SetException(ex);
                            }
                        });

                        if (await Task.WhenAny(tcs.Task, Task.Delay(15000)) != tcs.Task)
                        {
                            UpdateStatus("[错误] 截图操作超时");
                            return Results.Json(new { status = "error", message = "Timeout" }, statusCode: 500);
                        }

                        string savedPath = await tcs.Task;

                        if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                        {
                            this.Invoke(new Action(() =>
                            {
                                if (pictureBox1.Image != null)
                                {
                                    pictureBox1.Image.Dispose();
                                }
                                using (var stream = new FileStream(savedPath, FileMode.Open, FileAccess.Read))
                                {
                                    pictureBox1.Image = Image.FromStream(stream);
                                }
                                lblStatus.Text = $"[成功] 截图已存至 D 盘: {Path.GetFileName(savedPath)} (模式 {currentMode})";
                            }));

                            return Results.Ok(new { status = "success", path = savedPath, mode = currentMode });
                        }
                        else
                        {
                            return Results.Json(new { status = "error", message = "未生成图片" }, statusCode: 500);
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"[异常] {ex.Message}");
                        return Results.Json(new { status = "error", message = ex.Message }, statusCode: 500);
                    }
                });

                // 2. 切换模式接口（对应 ESP32 长按 2 秒请求）
                _webApp.MapGet("/switch-mode", (HttpContext context) =>
                {
                    // 自动在 1 和 2 之间切换
                    currentMode = (currentMode == 1) ? 2 : 1;

                    UpdateStatus($"[模式切换] ESP32 长按触发！切换至【 模式 {currentMode} 】，正在重新配置浏览器...");

                    // 在后台异步重启浏览器，应用新的无头配置，避免阻塞 HTTP 响应
                    Task.Run(() => RestartBrowserWithCurrentMode());

                    return Results.Ok(new { status = "success", currentMode = currentMode });
                });

                _ = _webApp.RunAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动 Web 服务失败: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private string PerformFastSeleniumScreenshot()
        {
            EnsureDirectoryExists(tempDir);
            EnsureDirectoryExists(sharedDir);

            lock (_browserLock)
            {
                if (_driver == null || !_isBrowserReady) throw new Exception("浏览器实例未初始化或正在重启");

                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                js.ExecuteScript("saveScreentoLocal();");

                FileInfo latestFile = null;
                int retry = 0;
                while (retry < 12)
                {
                    Thread.Sleep(500);
                    var directoryInfo = new DirectoryInfo(tempDir);

                    latestFile = directoryInfo.GetFiles()
                        .Where(f => (f.Extension.ToLower() == ".png" || f.Extension.ToLower() == ".jpg")
                                   && !f.Name.EndsWith(".crdownload"))
                        .OrderByDescending(f => f.CreationTime)
                        .FirstOrDefault();

                    if (latestFile != null && latestFile.Length > 0)
                    {
                        Thread.Sleep(200);
                        break;
                    }
                    retry++;
                }

                if (latestFile == null || !File.Exists(latestFile.FullName))
                {
                    throw new Exception("执行 saveScreentoLocal() 后未能检测到下载文件");
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string destFileName = $"Instrument_Shot_Mode{currentMode}_{timestamp}{latestFile.Extension}";
                string destPath = Path.Combine(sharedDir, destFileName);

                latestFile.CopyTo(destPath, true);

                try { latestFile.Delete(); } catch { }

                return destPath;
            }
        }

        private void UpdateStatus(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => lblStatus.Text = text));
            }
            else
            {
                lblStatus.Text = text;
            }
        }

        protected override async void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            try
            {
                lock (_browserLock)
                {
                    _driver?.Quit();
                    _driver?.Dispose();
                }
            }
            catch { }

            if (_webApp != null)
            {
                await _webApp.StopAsync();
            }
        }
    }
}