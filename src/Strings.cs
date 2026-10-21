using System;
using System.Collections.Generic;

namespace BeYourself
{
    internal static class Strings
    {
        internal static readonly string[] Languages = { "zh-CN", "en", "ja" };
        internal static readonly string[] LanguageNames = { "简体中文", "English", "日本語" };
        private static readonly Dictionary<string, string[]> Values = new Dictionary<string, string[]>
        {
            {"settings", new[] {"设置", "Settings", "設定"}},
            {"show", new[] {"显示标语", "Show motto", "表示する"}},
            {"hide", new[] {"隐藏标语", "Hide motto", "非表示にする"}},
            {"exit", new[] {"退出", "Exit", "終了"}},
            {"language", new[] {"界面语言", "Language", "表示言語"}},
            {"text", new[] {"你的标语（支持多行）", "Your motto (multiple lines supported)", "ひとこと（改行できます）"}},
            {"font", new[] {"选择字体…", "Choose font…", "フォント…"}},
            {"color", new[] {"文字颜色…", "Text color…", "文字色…"}},
            {"opacity", new[] {"不透明度", "Opacity", "不透明度"}},
            {"position", new[] {"显示位置", "Position", "表示位置"}},
            {"screen", new[] {"显示器", "Monitor", "モニター"}},
            {"primary", new[] {"主显示器", "Primary monitor", "メインモニター"}},
            {"margin", new[] {"边缘间距", "Edge margin", "端からの余白"}},
            {"startup", new[] {"登录 Windows 时自动启动", "Start when I sign in to Windows", "Windows サインイン時に自動起動"}},
            {"preview", new[] {"文字预览", "Text preview", "プレビュー"}},
            {"hint", new[] {"标语位于应用窗口后、壁纸前，点击穿透。\n右键右下角托盘图标可再次打开设置。", "Below app windows, above the wallpaper. Click-through.\nRight-click the tray icon to open settings again.", "アプリの背面、壁紙の前面に表示。クリックは透過します。\nトレイアイコンの右クリックから設定を開けます。"}},
            {"save", new[] {"保存", "Save", "保存"}},
            {"cancel", new[] {"取消", "Cancel", "キャンセル"}},
            {"empty", new[] {"请输入至少一个可见字符。", "Please enter at least one visible character.", "文字を入力してください。"}},
            {"error", new[] {"无法保存，请检查文件或启动项的访问权限。", "Could not save. Check access to the settings file or startup entry.", "保存できません。設定ファイルや自動起動の権限をご確認ください。"}},
            {"loadError", new[] {"设置文件无法读取，已使用默认值。原文件未改动。", "Settings could not be read. Defaults are in use; the original file is preserved.", "設定を読み込めないため初期値を使います。元のファイルは保持されています。"}},
            {"desktopError", new[] {"暂未找到 Windows 桌面，正在等待桌面就绪。", "Waiting for the Windows desktop to become available.", "Windows デスクトップの準備を待っています。"}},
            {"welcome", new[] {"正在桌面上显示标语。右键此图标打开设置。", "Your motto is on the desktop. Right-click this icon for settings.", "デスクトップに表示中です。このアイコンを右クリックして設定できます。"}},
            {"sample", new[] {"今天的自律，是明天的自由。", "Small steps today. More freedom tomorrow.", "今日の一歩が、明日の自由になる。"}},
            {"p0", new[] {"左上", "Top left", "左上"}},
            {"p1", new[] {"上方居中", "Top center", "上中央"}},
            {"p2", new[] {"右上", "Top right", "右上"}},
            {"p3", new[] {"左侧居中", "Middle left", "中央左"}},
            {"p4", new[] {"屏幕中央", "Center", "中央"}},
            {"p5", new[] {"右侧居中", "Middle right", "中央右"}},
            {"p6", new[] {"左下", "Bottom left", "左下"}},
            {"p7", new[] {"下方居中", "Bottom center", "下中央"}},
            {"p8", new[] {"右下", "Bottom right", "右下"}}
        };

        public static string DetectLanguage(string name)
        {
            if (name != null && name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
            if (name != null && name.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return "ja";
            return "en";
        }
        public static string Get(string language, string key)
        {
            int index = Array.IndexOf(Languages, DetectLanguage(language));
            return Values[key][index];
        }
        internal static IEnumerable<string> Keys { get { return Values.Keys; } }
    }
}
