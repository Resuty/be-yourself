using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BeYourself
{
    internal sealed class SettingsForm : Form
    {
        private readonly Action<Settings, bool> save;
        private readonly ComboBox language = Combo();
        private readonly TextBox motto = new TextBox { Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, MaxLength = 2000, Dock = DockStyle.Fill };
        private readonly Button fontButton = new Button { AutoSize = true, Name = "FontButton" };
        private readonly Button colorButton = new Button { AutoSize = true, Name = "ColorButton" };
        private readonly Label fontInfo = new Label { AutoSize = true, Margin = new Padding(8, 9, 0, 0) };
        private readonly NumericUpDown opacity = new NumericUpDown { Minimum = 10, Maximum = 100, Width = 72 };
        private readonly NumericUpDown margin = new NumericUpDown { Minimum = 0, Maximum = 300, Width = 72 };
        private readonly ComboBox position = Combo();
        private readonly ComboBox screen = Combo();
        private readonly CheckBox startup = new CheckBox { AutoSize = true };
        private readonly Panel preview = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(29, 39, 51), Margin = new Padding(0, 4, 0, 0) };
        private readonly Button saveButton = new Button { Name = "SaveButton", Width = 104, Height = 34 };
        private readonly Button cancelButton = new Button { DialogResult = DialogResult.Cancel, Width = 104, Height = 34 };
        private readonly Label hint = new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(92, 104, 117), AutoSize = true };
        private readonly Label languageLabel = Label(), textLabel = Label(), opacityLabel = Label(), positionLabel = Label(), screenLabel = Label(), marginLabel = Label(), previewLabel = Label();
        private Settings draft;
        private Bitmap previewImage;

        internal Settings Draft { get { return draft.Copy(); } }

        public SettingsForm(Settings value, bool autoStart, Action<Settings, bool> onSave)
        {
            draft = value.Copy(); save = onSave;
            Text = "Be yourself";
            Font = new Font("Microsoft YaHei UI", 9f);
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(568, 696);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(248, 250, 252);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 18), ColumnCount = 1, RowCount = 13 };
            int[] heights = { 48, 40, 25, 104, 44, 40, 40, 40, 36, 26, 116, 58, 40 };
            for (int i = 0; i < heights.Length; i++) root.RowStyles.Add(new RowStyle(SizeType.Absolute, heights[i]));
            root.Controls.Add(new Label { Text = "Be yourself", AutoSize = true, Font = new Font("Segoe UI", 23f, FontStyle.Bold), ForeColor = Color.FromArgb(22, 65, 65), Margin = Padding.Empty }, 0, 0);

            language.Items.AddRange(Strings.LanguageNames);
            language.SelectedIndex = Array.IndexOf(Strings.Languages, draft.Language);
            root.Controls.Add(Row(languageLabel, language), 0, 1);
            root.Controls.Add(textLabel, 0, 2);
            motto.Name = "MottoText";
            motto.Text = draft.Text.Replace("\n", Environment.NewLine);
            root.Controls.Add(motto, 0, 3);

            var appearance = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0), WrapContents = false };
            appearance.Controls.Add(fontButton); appearance.Controls.Add(colorButton); appearance.Controls.Add(fontInfo);
            root.Controls.Add(appearance, 0, 4);
            var numbers = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            opacity.Value = draft.Opacity; margin.Value = draft.Margin;
            numbers.Controls.Add(opacity);
            numbers.Controls.Add(new Label { Text = "%", AutoSize = true, Margin = new Padding(2, 6, 18, 0) });
            marginLabel.Margin = new Padding(0, 6, 8, 0);
            numbers.Controls.Add(marginLabel); numbers.Controls.Add(margin);
            root.Controls.Add(Row(opacityLabel, numbers), 0, 5);
            root.Controls.Add(Row(positionLabel, position), 0, 6);
            root.Controls.Add(Row(screenLabel, screen), 0, 7);
            startup.Checked = autoStart;
            root.Controls.Add(startup, 0, 8);
            root.Controls.Add(previewLabel, 0, 9);
            root.Controls.Add(preview, 0, 10);
            hint.Margin = new Padding(0, 10, 0, 0);
            root.Controls.Add(hint, 0, 11);
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = Padding.Empty };
            saveButton.BackColor = Color.FromArgb(28, 99, 91);
            saveButton.ForeColor = Color.White;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.FlatAppearance.BorderSize = 0;
            footer.Controls.Add(saveButton); footer.Controls.Add(cancelButton);
            root.Controls.Add(footer, 0, 12);
            Controls.Add(root);
            CancelButton = cancelButton;
            UpdateLabels();
            UpdateFontInfo();

            language.SelectedIndexChanged += delegate
            {
                draft.Language = Strings.Languages[language.SelectedIndex];
                UpdateLabels();
            };
            motto.TextChanged += delegate { draft.Text = motto.Text; RefreshPreview(); };
            opacity.ValueChanged += delegate { draft.Opacity = (int)opacity.Value; RefreshPreview(); };
            margin.ValueChanged += delegate { draft.Margin = (int)margin.Value; };
            position.SelectedIndexChanged += delegate { if (position.SelectedIndex >= 0) { draft.Position = position.SelectedIndex; RefreshPreview(); } };
            screen.SelectedIndexChanged += delegate
            {
                draft.ScreenName = screen.SelectedIndex <= 0 ? "" : Screen.AllScreens[screen.SelectedIndex - 1].DeviceName;
            };
            fontButton.Click += ChooseFont;
            colorButton.Click += ChooseColor;
            saveButton.Click += Save;
            preview.Paint += PaintPreview;
            preview.Resize += delegate { RefreshPreview(); };
            Shown += delegate { motto.Focus(); motto.SelectionStart = motto.TextLength; RefreshPreview(); };
        }

        private static ComboBox Combo() { return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill }; }
        private static Label Label() { return new Label { AutoSize = true, Anchor = AnchorStyles.Left, Margin = Padding.Empty }; }
        private static TableLayoutPanel Row(Control label, Control input)
        {
            var row = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Margin = Padding.Empty };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.Controls.Add(label, 0, 0); row.Controls.Add(input, 1, 0);
            return row;
        }

        private string T(string key) { return Strings.Get(draft.Language, key); }
        private void UpdateLabels()
        {
            Text = "Be yourself · " + T("settings");
            languageLabel.Text = T("language"); textLabel.Text = T("text");
            fontButton.Text = T("font"); colorButton.Text = T("color");
            opacityLabel.Text = T("opacity"); positionLabel.Text = T("position");
            screenLabel.Text = T("screen"); marginLabel.Text = T("margin");
            startup.Text = T("startup"); previewLabel.Text = T("preview");
            hint.Text = T("hint"); saveButton.Text = T("save"); cancelButton.Text = T("cancel");
            int selectedPosition = draft.Position;
            position.Items.Clear();
            for (int i = 0; i < 9; i++) position.Items.Add(T("p" + i));
            position.SelectedIndex = selectedPosition;
            string screenName = draft.ScreenName;
            screen.Items.Clear(); screen.Items.Add(T("primary"));
            int selectedScreen = 0;
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                var item = Screen.AllScreens[i];
                screen.Items.Add((i + 1) + " · " + item.Bounds.Width + " × " + item.Bounds.Height + " · " + item.DeviceName);
                if (item.DeviceName == screenName) selectedScreen = i + 1;
            }
            screen.SelectedIndex = selectedScreen;
        }
        private void UpdateFontInfo()
        {
            fontInfo.Text = draft.FontSize.ToString("0.#") + " pt";
            colorButton.ForeColor = Color.FromArgb(draft.ColorArgb).GetBrightness() < 0.8f ? Color.FromArgb(draft.ColorArgb) : Color.FromArgb(28, 99, 91);
        }
        private void ChooseFont(object sender, EventArgs args)
        {
            using (var font = MottoRenderer.CreateFont(draft))
            using (var dialog = new FontDialog { Font = font, MinSize = 12, MaxSize = 144, ShowEffects = false, FontMustExist = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                draft.FontFamily = dialog.Font.FontFamily.Name;
                draft.FontSize = dialog.Font.SizeInPoints;
                draft.Bold = dialog.Font.Bold;
                UpdateFontInfo(); RefreshPreview();
            }
        }
        private void ChooseColor(object sender, EventArgs args)
        {
            using (var dialog = new ColorDialog { Color = Color.FromArgb(draft.ColorArgb), FullOpen = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                draft.ColorArgb = dialog.Color.ToArgb(); UpdateFontInfo(); RefreshPreview();
            }
        }
        private void Save(object sender, EventArgs args)
        {
            if (String.IsNullOrWhiteSpace(motto.Text))
            {
                MessageBox.Show(this, T("empty"), "Be yourself", MessageBoxButtons.OK, MessageBoxIcon.Information);
                motto.Focus(); return;
            }
            draft.Text = motto.Text;
            draft.Normalize();
            try
            {
                save(draft.Copy(), startup.Checked);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception error)
            {
                MessageBox.Show(this, T("error") + "\n\n" + error.Message, "Be yourself", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void RefreshPreview()
        {
            if (preview.Width <= 16 || preview.Height <= 16) return;
            var value = draft.Copy();
            if (String.IsNullOrWhiteSpace(value.Text)) value.Text = " ";
            value.FontSize = Math.Min(value.FontSize, 22);
            var replacement = MottoRenderer.Render(value, new Size(preview.Width - 24, preview.Height - 16), 96);
            if (previewImage != null) previewImage.Dispose();
            previewImage = replacement;
            preview.Invalidate();
        }
        private void PaintPreview(object sender, PaintEventArgs args)
        {
            if (previewImage == null) RefreshPreview();
            if (previewImage == null) return;
            using (var attributes = new System.Drawing.Imaging.ImageAttributes())
            {
                attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = draft.Opacity / 100f });
                var destination = new Rectangle((preview.Width - previewImage.Width) / 2, (preview.Height - previewImage.Height) / 2, previewImage.Width, previewImage.Height);
                args.Graphics.DrawImage(previewImage, destination, 0, 0, previewImage.Width, previewImage.Height, GraphicsUnit.Pixel, attributes);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && previewImage != null) { previewImage.Dispose(); previewImage = null; }
            base.Dispose(disposing);
        }
    }
}
