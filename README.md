# Be yourself

轻量的 Windows 桌面自律标语。只有文字，没有边框、背景或任务栏窗口；通过右下角托盘图标的右键菜单设置。

## 中文

### 使用

1. 下载 Release 中的 `BeYourself-v1.0.0-windows.zip`，解压到固定位置。
2. 双击 `BeYourself.exe`。请保留旁边的 `BeYourself.exe.config`。
3. 在右下角托盘找到绿色 **B** 图标（可能收在 `^` 中），右键 → **设置**。
4. 输入标语，可换行；调整字体、颜色、不透明度、九宫格位置、显示器和边缘间距，点击 **保存**。
5. 需要自启动时，勾选 **登录 Windows 时自动启动** 并保存。

**显示层级：应用窗口 → 标语 → 普通壁纸/标准桌面层的动态壁纸。** 标语可点击穿透，不抢焦点，不在任务栏或 Alt+Tab 中出现。设置窗口仅从托盘菜单打开；单击文字没有操作。

支持简体中文、English、日本語界面；标语内容可混用这些语言。更换界面语言不会改写你的标语。托盘菜单也可隐藏/显示标语或退出。

文字块超出可用显示区域时会自动缩小以适配。建议使用简短标语，最多 2000 字符。

### 轻量与本地存储

- C#、Windows Forms、Windows 自带 .NET Framework 4.8；无 Electron、WebView、第三方包、网络请求或后台服务。
- 文字只在需要时绘制；每 3 秒做一次桌面宿主有效性检查，以应对资源管理器/壁纸软件重建桌面。不持续刷新画面。
- 设置保存于 `%LOCALAPPDATA%\BeYourself\settings.json`，成功覆盖前保留上一版 `.bak`。
- 自启动仅在当前用户的 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 下登记 `Be yourself`，不需要管理员权限。默认关闭。
- 勾选自启动后请勿移动程序；移动后从新位置打开并再次保存设置，更新路径。Windows 任务管理器仍可能禁用启动项。
- 卸载：先取消自启动并保存，再从托盘退出并删除程序文件。需要彻底清除设置时，再删除上述 `BeYourself` 配置目录。

### 系统与兼容性

目标系统为 Windows 10 / 11，需 .NET Framework 4.8。只支持 Windows。使用 Explorer 的桌面图标宿主（`Progman` / `WorkerW` + `SHELLDLL_DefView`）将文字固定在桌面层。未找到该宿主时保持隐藏并重试，不提升为普通应用窗口。

壁纸软件若使用标准桌面背景层，文字显示于其上方。使用独立置顶窗口或自定义桌面外壳的软件需要另行实测，不能保证所有产品兼容。显示器断开时回退到主显示器。文字以所选字体显示，缺少字符时取决于系统字体回退。

### 从源码构建

在项目目录用 Windows PowerShell 执行：

```powershell
.\build.ps1 -Test
# 本机桌面宿主、点击穿透、窗口样式及三语设置界面验证：
.\build.ps1 -DesktopTest
```

使用 Windows 自带 C# 编译器，无需 NuGet。输出可执行程序、配置和 ZIP 到 `dist`；测试输出到 `artifacts`。测试不修改用户配置或自启动项。`-DesktopTest` 需要可用的 Windows Explorer 桌面，会短暂显示测试文字和设置界面，然后自动关闭。

## English

**Be yourself** is a small, offline Windows desktop motto app. Text appears **behind application windows and above wallpapers hosted in the standard desktop layer**, with a transparent background and click-through behavior. There is no taskbar or Alt+Tab entry.

Extract the release ZIP, keep the EXE and CONFIG together, and run `BeYourself.exe`. Right-click the green **B** icon in the notification area (check the `^` overflow) → **Settings**. Customize multiline text, font, color, opacity, position, monitor and margin. Select **Start when I sign in to Windows** to enable per-user startup. Keep the app in a permanent folder; after moving it, save settings from the new location to update startup.

The UI supports Simplified Chinese, English and Japanese. Settings are stored locally in `%LOCALAPPDATA%\BeYourself\settings.json`. Startup is off by default. To uninstall, turn off startup and save, exit from the tray, then delete the files. Requires Windows 10/11 and .NET Framework 4.8. Standard Explorer desktop hosts are supported; wallpaper apps using custom topmost windows require separate compatibility testing.

Build: `powershell -File .\build.ps1 -Test`. Desktop integration checks: add `-DesktopTest` instead of `-Test`. No external packages or network access are needed to build or run.

## 日本語

**Be yourself** は、Windows デスクトップに好きな言葉を表示する軽量アプリです。**アプリのウィンドウより背面、標準デスクトップ層の壁紙より前面**に、背景や枠のない文字だけを表示します。クリックは透過し、タスクバーや Alt+Tab には表示されません。

Release の ZIP を固定のフォルダーに展開し、EXE と CONFIG を同じ場所に置いて `BeYourself.exe` を実行してください。通知領域の緑色の **B** アイコン（`^` 内の場合もあります）を右クリック → **設定**。複数行の文字、フォント、色、不透明度、位置、モニター、余白を変更できます。**Windows サインイン時に自動起動**にチェックして保存すると、自動起動が有効になります。保存場所を移動した場合は、新しい場所から起動して設定を保存し直してください。

表示言語は簡体字中国語・英語・日本語に対応。設定は `%LOCALAPPDATA%\BeYourself\settings.json` に保存されます。自動起動は初期状態で無効です。削除するときは、自動起動を無効にして保存し、トレイから終了してファイルを削除します。Windows 10/11 と .NET Framework 4.8 が必要です。標準 Explorer のデスクトップ層に対応しますが、独自の最前面ウィンドウを使う壁紙ソフトは個別の検証が必要です。

ビルドは `powershell -File .\build.ps1 -Test`。実際のデスクトップとの連携確認には `-DesktopTest` を指定します。外部パッケージや通信は不要です。

## Implementation references

- [Microsoft: SetParent and window styles](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setparent)
- [Microsoft: layered windows and click-through behavior](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features)
- [Microsoft: Windows Forms DPI support](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms)
- [Microsoft: per-user startup entries](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
