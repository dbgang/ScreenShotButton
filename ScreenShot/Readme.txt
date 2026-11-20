要在你的 .NET 8 WinForms 项目中安装这些依赖项，可以通过命令行（Terminal / 命令行提示符）在你的项目根目录下执行以下 NuGet 安装命令：

### 1. 安装 Selenium 相关包

打开终端，进入你的项目文件夹（包含 `.csproj` 文件的目录），运行以下两条命令：

```bash
dotnet add package Selenium.WebDriver
dotnet add package Selenium.WebDriver.ChromeDriver

```

---

### 2. 关于 `Microsoft.AspNetCore.App` 的说明

`Microsoft.AspNetCore.App` **通常不需要**通过 `dotnet add package` 来安装。

因为你的项目是一个 **WinForms 窗体应用程序**（默认使用的是 Windows Forms SDK），如果你想在其中内嵌 ASP.NET Core Web API 服务，只需要直接修改你的 `.csproj` 项目文件，在里面加入一个框架引用即可。

**操作步骤：**

1. 用文本编辑器（如 VS Code 或记事本）打开你的 `.csproj` 文件。
2. 在 `<ItemGroup>` 标签中手动添加一行框架引用：

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>

```

保存文件后，.NET 8 SDK 会自动加载 ASP.NET Core 的所有核心类库和命名空间，你就可以直接在代码中使用 `WebApplication.CreateBuilder(args)` 了。