using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("Windows 更新暂停工具")]
[assembly: System.Reflection.AssemblyDescription("自定义暂停和恢复 Windows 更新")]
[assembly: System.Reflection.AssemblyCompany("开源社区")]
[assembly: System.Reflection.AssemblyProduct("Windows 更新暂停工具")]
[assembly: System.Reflection.AssemblyCopyright("Copyright © 2026")]
[assembly: System.Reflection.AssemblyVersion("1.2.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.2.0.0")]

namespace WindowsUpdatePauseTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly NumericUpDown yearInput;
        private readonly NumericUpDown monthInput;
        private readonly NumericUpDown dayInput;
        private readonly CheckBox lockCheckBox;
        private readonly Label statusLabel;
        private readonly Button applyButton;
        private readonly Button restoreButton;

        internal MainForm()
        {
            Text = "Windows 更新暂停工具";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            ClientSize = new Size(610, 430);
            MinimumSize = new Size(626, 469);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(246, 248, 251);

            var title = new Label
            {
                Text = "Windows 更新暂停工具",
                Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 41, 61),
                AutoSize = true,
                Location = new Point(28, 22)
            };

            var warning = new Label
            {
                Text = "暂停更新会延迟安全补丁安装。请按需设置，并定期手动检查重要更新。",
                ForeColor = Color.FromArgb(145, 83, 0),
                BackColor = Color.FromArgb(255, 244, 214),
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = false,
                Location = new Point(30, 70),
                Size = new Size(550, 42),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };

            var dateLabel = new Label
            {
                Text = "暂停更新至（本地日期）",
                AutoSize = true,
                Location = new Point(30, 139),
                ForeColor = Color.FromArgb(55, 65, 81)
            };

            yearInput = new NumericUpDown
            {
                Location = new Point(30, 164),
                Size = new Size(92, 28),
                Minimum = DateTime.Today.Year,
                Maximum = 9999,
                Value = Math.Min(DateTime.Today.Year + 100, 9999)
            };
            monthInput = new NumericUpDown
            {
                Location = new Point(155, 164),
                Size = new Size(62, 28),
                Minimum = 1,
                Maximum = 12,
                Value = DateTime.Today.Month
            };
            dayInput = new NumericUpDown
            {
                Location = new Point(250, 164),
                Size = new Size(62, 28),
                Minimum = 1,
                Maximum = 31,
                Value = DateTime.Today.Day
            };
            yearInput.ValueChanged += delegate { UpdateDayMaximum(); };
            monthInput.ValueChanged += delegate { UpdateDayMaximum(); };
            UpdateDayMaximum();

            var yearUnit = new Label { Text = "年", AutoSize = true, Location = new Point(127, 168) };
            var monthUnit = new Label { Text = "月", AutoSize = true, Location = new Point(222, 168) };
            var dayUnit = new Label { Text = "日", AutoSize = true, Location = new Point(317, 168) };

            var presets = new FlowLayoutPanel
            {
                Location = new Point(30, 207),
                Size = new Size(550, 38),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            AddPreset(presets, "7 天", delegate { SetEndDate(DateTime.Today.AddDays(7)); });
            AddPreset(presets, "35 天", delegate { SetEndDate(DateTime.Today.AddDays(35)); });
            AddPreset(presets, "1 年", delegate { SetEndDate(SafeAddYears(DateTime.Today, 1)); });
            AddPreset(presets, "10 年", delegate { SetEndDate(SafeAddYears(DateTime.Today, 10)); });
            AddPreset(presets, "100 年", delegate { SetEndDate(SafeAddYears(DateTime.Today, 100)); });
            AddPreset(presets, "9999 年", delegate { SetEndDate(new DateTime(9999, 12, 31)); });

            lockCheckBox = new CheckBox
            {
                Text = "长期锁定模式（忽略到期日期，必须点击“恢复更新”才能解除）",
                AutoSize = true,
                Location = new Point(30, 260),
                ForeColor = Color.FromArgb(123, 47, 47)
            };

            applyButton = new Button
            {
                Text = "应用设置",
                Location = new Point(30, 304),
                Size = new Size(145, 42),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.Click += ApplyButtonClick;

            restoreButton = new Button
            {
                Text = "恢复更新",
                Location = new Point(190, 304),
                Size = new Size(145, 42),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat
            };
            restoreButton.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
            restoreButton.Click += RestoreButtonClick;

            statusLabel = new Label
            {
                Text = "正在读取当前状态……",
                AutoSize = false,
                Location = new Point(30, 368),
                Size = new Size(550, 42),
                ForeColor = Color.FromArgb(75, 85, 99)
            };

            Controls.Add(title);
            Controls.Add(warning);
            Controls.Add(dateLabel);
            Controls.Add(yearInput);
            Controls.Add(monthInput);
            Controls.Add(dayInput);
            Controls.Add(yearUnit);
            Controls.Add(monthUnit);
            Controls.Add(dayUnit);
            Controls.Add(presets);
            Controls.Add(lockCheckBox);
            Controls.Add(applyButton);
            Controls.Add(restoreButton);
            Controls.Add(statusLabel);

            Shown += delegate { RefreshStatus(); };
        }

        private static DateTime SafeAddYears(DateTime value, int years)
        {
            return value.AddYears(years);
        }

        private void SetEndDate(DateTime value)
        {
            yearInput.Value = value.Year;
            monthInput.Value = value.Month;
            UpdateDayMaximum();
            dayInput.Value = Math.Min(value.Day, (int)dayInput.Maximum);
        }

        private void UpdateDayMaximum()
        {
            int year = (int)yearInput.Value;
            int month = (int)monthInput.Value;
            dayInput.Maximum = DateTime.DaysInMonth(year, month);
        }

        private DateTime GetSelectedEndDate()
        {
            return new DateTime((int)yearInput.Value, (int)monthInput.Value, (int)dayInput.Value);
        }

        private static void AddPreset(Control parent, string text, Action action)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 30,
                Margin = new Padding(0, 0, 9, 0),
                FlatStyle = FlatStyle.System
            };
            button.Click += delegate { action(); };
            parent.Controls.Add(button);
        }

        private void ApplyButtonClick(object sender, EventArgs e)
        {
            DateTime selectedEndDate = GetSelectedEndDate();
            if (selectedEndDate <= DateTime.Today)
            {
                MessageBox.Show("恢复日期必须晚于今天。", "日期无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string detail = lockCheckBox.Checked
                ? "长期锁定模式已开启，届时不会自动恢复，必须使用本工具手动解除。"
                : "到达所选日期后，Windows 可恢复自动检查更新。";
            var result = MessageBox.Show(
                "将暂停 Windows 更新至 " + selectedEndDate.ToString("yyyy 年 MM 月 dd 日") + "。\r\n\r\n" +
                detail + "\r\n\r\n暂停期间可能错过重要安全补丁，确定继续吗？",
                "确认暂停更新",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;

            RunOperation(delegate
            {
                DateTime endOfDay = selectedEndDate == DateTime.MaxValue.Date
                    ? DateTime.MaxValue
                    : selectedEndDate.AddDays(1).AddTicks(-1);
                UpdateSettings.Apply(endOfDay, lockCheckBox.Checked);
            }, "设置已应用。", true);
        }

        private void RestoreButtonClick(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "这会解除自动更新禁用策略，并将暂停截止时间调整到约 1 小时后。\r\n\r\n确定恢复吗？",
                "确认恢复更新",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result != DialogResult.Yes) return;

            RunOperation(UpdateSettings.Restore, "恢复设置已应用。Windows 将在约 1 小时后恢复自动检查更新；届时也可以手动点击“检查更新”。", false);
        }

        private void RunOperation(Action operation, string successMessage, bool applying)
        {
            SetBusy(true);
            try
            {
                operation();
                TryRefreshPolicy();
                MessageBox.Show(successMessage, "操作成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("操作需要管理员权限。请右键程序并选择“以管理员身份运行”。", "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                RefreshStatus();
            }
        }

        private void SetBusy(bool busy)
        {
            applyButton.Enabled = !busy;
            restoreButton.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private static void TryRefreshPolicy()
        {
            try
            {
                using (var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "gpupdate.exe",
                    Arguments = "/target:computer /force",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    if (process != null) process.WaitForExit(30000);
                }
            }
            catch
            {
                // 注册表设置已经保存；策略刷新失败不应撤销用户操作。
            }
        }

        private void RefreshStatus()
        {
            try
            {
                UpdateState state = UpdateSettings.ReadState();
                if (state.Locked)
                {
                    statusLabel.Text = "当前状态：长期锁定模式已开启；必须手动恢复更新。";
                    statusLabel.ForeColor = Color.FromArgb(153, 27, 27);
                }
                else if (state.EndUtc.HasValue && state.EndUtc.Value > DateTime.UtcNow)
                {
                    DateTime local;
                    try { local = state.EndUtc.Value.ToLocalTime(); }
                    catch (ArgumentException) { local = DateTime.MaxValue; }
                    statusLabel.Text = "当前状态：更新已暂停至 " + local.ToString("yyyy 年 MM 月 dd 日 HH:mm") + "。";
                    statusLabel.ForeColor = Color.FromArgb(30, 64, 175);
                    DateTime date = local.Date;
                    if (date >= DateTime.Today && date.Year <= 9999) SetEndDate(date);
                }
                else
                {
                    statusLabel.Text = "当前状态：未检测到有效的更新暂停设置。";
                    statusLabel.ForeColor = Color.FromArgb(22, 101, 52);
                }
                lockCheckBox.Checked = state.Locked;
            }
            catch (Exception ex)
            {
                statusLabel.Text = "状态读取失败：" + ex.Message;
                statusLabel.ForeColor = Color.FromArgb(153, 27, 27);
            }
        }
    }

    internal sealed class UpdateState
    {
        internal DateTime? EndUtc { get; set; }
        internal bool Locked { get; set; }
    }

    internal static class UpdateSettings
    {
        private const string SettingsPath = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
        private const string PolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";

        private static readonly string[] StartNames =
        {
            "PauseUpdatesStartTime", "PauseFeatureUpdatesStartTime", "PauseQualityUpdatesStartTime"
        };

        private static readonly string[] EndNames =
        {
            "PauseUpdatesExpiryTime", "PauseFeatureUpdatesEndTime", "PauseQualityUpdatesEndTime"
        };

        internal static void Apply(DateTime localEnd, bool locked)
        {
            DateTime startUtc = DateTime.UtcNow;
            DateTime endUtc = localEnd.Year == 9999
                ? DateTime.SpecifyKind(localEnd, DateTimeKind.Utc)
                : localEnd.ToUniversalTime();
            if (endUtc <= startUtc) throw new InvalidOperationException("恢复日期必须晚于当前时间。");

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey settings = baseKey.CreateSubKey(SettingsPath, true))
                {
                    if (settings == null) throw new InvalidOperationException("无法打开 Windows 更新设置。");
                    string startText = startUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                    string endText = endUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                    foreach (string name in StartNames) settings.SetValue(name, startText, RegistryValueKind.String);
                    foreach (string name in EndNames) settings.SetValue(name, endText, RegistryValueKind.String);
                    settings.SetValue("PauseUpdatesStartTime", startText, RegistryValueKind.String);
                }

                using (RegistryKey policy = baseKey.CreateSubKey(PolicyPath, true))
                {
                    if (policy == null) throw new InvalidOperationException("无法打开 Windows 更新策略。");
                    if (locked) policy.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
                    else policy.DeleteValue("NoAutoUpdate", false);
                }
            }
        }

        internal static void Restore()
        {
            DateTime startUtc = DateTime.UtcNow;
            DateTime resumeUtc = CalculateResumeUtc(startUtc);
            string startText = startUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            string resumeText = resumeUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey settings = baseKey.CreateSubKey(SettingsPath, true))
                {
                    if (settings == null) throw new InvalidOperationException("无法打开 Windows 更新设置。");
                    foreach (string name in StartNames) settings.SetValue(name, startText, RegistryValueKind.String);
                    foreach (string name in EndNames) settings.SetValue(name, resumeText, RegistryValueKind.String);
                }

                using (RegistryKey policy = baseKey.CreateSubKey(PolicyPath, true))
                {
                    if (policy == null) throw new InvalidOperationException("无法打开 Windows 更新策略。");
                    policy.DeleteValue("NoAutoUpdate", false);
                }

                baseKey.DeleteSubKeyTree(@"SOFTWARE\WindowsUpdatePauseTool", false);
            }
        }

        internal static DateTime CalculateResumeUtc(DateTime utcNow)
        {
            DateTime normalized = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime();
            return normalized.AddHours(1);
        }

        internal static UpdateState ReadState()
        {
            var state = new UpdateState();
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey settings = baseKey.OpenSubKey(SettingsPath, false))
                {
                    if (settings != null)
                    {
                        string text = settings.GetValue("PauseUpdatesExpiryTime") as string;
                        DateTime parsed;
                        if (!string.IsNullOrEmpty(text) && DateTime.TryParse(
                            text, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
                        {
                            state.EndUtc = parsed;
                        }
                    }
                }

                using (RegistryKey policy = baseKey.OpenSubKey(PolicyPath, false))
                {
                    object value = policy == null ? null : policy.GetValue("NoAutoUpdate");
                    state.Locked = value is int && (int)value == 1;
                }
            }
            return state;
        }

    }
}
