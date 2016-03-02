﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.View
{
    public class MenuViewController
    {
        // yes this is just a menu view controller
        // when config form is closed, it moves away from RAM
        // and it should just do anything related to the config form

        private ShadowsocksController controller;

        private NotifyIcon _notifyIcon;
        private ContextMenu contextMenu1;

        private bool _isFirstRun;
        private MenuItem changeCDKEY;
        private MenuItem enableItem;
        private MenuItem modeItem;
        private MenuItem AutoStartupItem;
        private MenuItem ShareOverLANItem;
        private MenuItem globalModeItem;
        private MenuItem PACModeItem;
        private MenuItem localPACItem;
        private MenuItem onlinePACItem;
        private MenuItem editLocalPACItem;
        private MenuItem updateFromGFWListItem;
        private MenuItem editGFWUserRuleItem;
        private MenuItem editOnlinePACItem;

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            LoadMenu();

            controller.EnableStatusChanged += controller_EnableStatusChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_FileReadyToOpen;
            controller.UserRuleFileReadyToOpen += controller_FileReadyToOpen;
            controller.ShareOverLANStatusChanged += controller_ShareOverLANStatusChanged;
            controller.EnableGlobalChanged += controller_EnableGlobalChanged;
            controller.Errored += controller_Errored;
            controller.UpdatePACFromGFWListCompleted += controller_UpdatePACFromGFWListCompleted;
            controller.UpdatePACFromGFWListError += controller_UpdatePACFromGFWListError;

            _notifyIcon = new NotifyIcon();
            UpdateTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu1;
            _notifyIcon.MouseDoubleClick += notifyIcon1_DoubleClick;

            LoadCurrentConfiguration();

            Configuration config = controller.GetConfigurationCopy();

            if (config.isDefault)
            {
                _isFirstRun = true;
                ShowLoginForm();
            }
        }

        Form loginForm;
        private void ShowLoginForm()
        {
            if (loginForm != null)
            {
                loginForm.Activate();
            }
            else
            {
                loginForm = new Login(this.controller);
                loginForm.Disposed += LoginForm_Disposed;
                loginForm.Show();
            }
        }

        private void LoginForm_Disposed(object sender, EventArgs e)
        {
            Utils.ReleaseMemory(true);
            loginForm = null;
        }

        void controller_Errored(object sender, System.IO.ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().ToString(), String.Format(I18N.GetString("Error: {0}"), e.GetException().Message));
        }

        private void UpdateTrayIcon()
        {
            int dpi;
            Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
            dpi = (int)graphics.DpiX;
            graphics.Dispose();
            Bitmap icon = null;
            if (dpi < 97)
            {
                // dpi = 96;
                icon = Resources.ss16;
            }
            else if (dpi < 121)
            {
                // dpi = 120;
                icon = Resources.ss20;
            }
            else
            {
                icon = Resources.ss24;
            }
            Configuration config = controller.GetConfigurationCopy();
            bool enabled = config.enabled;
            bool global = config.global;
            if (!enabled)
            {
                Bitmap iconCopy = new Bitmap(icon);
                for (int x = 0; x < iconCopy.Width; x++)
                {
                    for (int y = 0; y < iconCopy.Height; y++)
                    {
                        Color color = icon.GetPixel(x, y);
                        iconCopy.SetPixel(x, y, Color.FromArgb((byte)(color.A / 2), color.R, color.G, color.B));
                    }
                }
                icon = iconCopy;
            }
            _notifyIcon.Icon = Icon.FromHandle(icon.GetHicon());

            string serverInfo = null;
            // we want to show more details but notify icon title is limited to 63 characters
            string text = I18N.GetString("baipass") + " " + UpdateChecker.Version + "\n" +
                (enabled ?
                    I18N.GetString("System Proxy On: ") + (global ? I18N.GetString("Global") : I18N.GetString("PAC")) :
                    String.Format(I18N.GetString("Running: Port {0}"), config.localPort))  // this feedback is very important because they need to know Shadowsocks is running
                + "\n" + serverInfo;
            _notifyIcon.Text = text.Substring(0, Math.Min(63, text.Length));
        }

        private MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MenuItem(I18N.GetString(text), click);
        }

        private MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MenuItem(I18N.GetString(text), items);
        }

        private void LoadMenu()
        {
            this.contextMenu1 = new ContextMenu(new MenuItem[] {
                this.changeCDKEY = CreateMenuItem("change cdkey", new EventHandler(this.changeCDKEY_Click)),
                this.enableItem = CreateMenuItem("Enable System Proxy", new EventHandler(this.EnableItem_Click)),
                this.modeItem = CreateMenuGroup("Mode", new MenuItem[] {
                    this.PACModeItem = CreateMenuItem("PAC", new EventHandler(this.PACModeItem_Click)),
                    this.globalModeItem = CreateMenuItem("Global", new EventHandler(this.GlobalModeItem_Click))
                }),
                CreateMenuGroup("PAC ", new MenuItem[] {
                    this.localPACItem = CreateMenuItem("Local PAC", new EventHandler(this.LocalPACItem_Click)),
                    this.onlinePACItem = CreateMenuItem("Online PAC", new EventHandler(this.OnlinePACItem_Click)),
                    new MenuItem("-"),
                    this.editLocalPACItem = CreateMenuItem("Edit Local PAC File...", new EventHandler(this.EditPACFileItem_Click)),
                    this.updateFromGFWListItem = CreateMenuItem("Update Local PAC from GFWList", new EventHandler(this.UpdatePACFromGFWListItem_Click)),
                    this.editGFWUserRuleItem = CreateMenuItem("Edit User Rule for GFWList...", new EventHandler(this.EditUserRuleFileForGFWListItem_Click)),
                    this.editOnlinePACItem = CreateMenuItem("Edit Online PAC URL...", new EventHandler(this.UpdateOnlinePACURLItem_Click)),
                }),
                new MenuItem("-"),
                this.AutoStartupItem = CreateMenuItem("Start on Boot", new EventHandler(this.AutoStartupItem_Click)),
                this.ShareOverLANItem = CreateMenuItem("Allow Clients from LAN", new EventHandler(this.ShareOverLANItem_Click)),
                new MenuItem("-"),
                CreateMenuItem("Show Logs...", new EventHandler(this.ShowLogItem_Click)),
                new MenuItem("-"),
                CreateMenuItem("Quit", new EventHandler(this.Quit_Click))
            });
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        private void controller_EnableStatusChanged(object sender, EventArgs e)
        {
            enableItem.Checked = controller.GetConfigurationCopy().enabled;
            modeItem.Enabled = enableItem.Checked;
        }

        void controller_ShareOverLANStatusChanged(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = controller.GetConfigurationCopy().shareOverLan;
        }

        void controller_EnableGlobalChanged(object sender, EventArgs e)
        {
            globalModeItem.Checked = controller.GetConfigurationCopy().global;
            PACModeItem.Checked = !globalModeItem.Checked;
        }

        void controller_FileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = content;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(timeout);
        }

        void controller_UpdatePACFromGFWListError(object sender, System.IO.ErrorEventArgs e)
        {
            ShowBalloonTip(I18N.GetString("Failed to update PAC file"), e.GetException().Message, ToolTipIcon.Error, 5000);
            Logging.LogUsefulException(e.GetException());
        }

        void controller_UpdatePACFromGFWListCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            string result = e.Success ? I18N.GetString("PAC updated") : I18N.GetString("No updates found. Please report to GFWList if you have problems with it.");
            ShowBalloonTip(I18N.GetString("Shadowsocks"), result, ToolTipIcon.Info, 1000);
        }

        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.GetConfigurationCopy();
            enableItem.Checked = config.enabled;
            modeItem.Enabled = config.enabled;
            globalModeItem.Checked = config.global;
            PACModeItem.Checked = !config.global;
            ShareOverLANItem.Checked = config.shareOverLan;
            AutoStartupItem.Checked = AutoStartup.Check();
            onlinePACItem.Checked = onlinePACItem.Enabled && config.useOnlinePac;
            localPACItem.Checked = !onlinePACItem.Checked;
            UpdatePACItemsEnabledStatus();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            controller.Stop();
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private void ShowFirstTimeBalloon()
        {
            if (_isFirstRun)
            {
                _notifyIcon.BalloonTipTitle = I18N.GetString("Shadowsocks is here");
                _notifyIcon.BalloonTipText = I18N.GetString("You can turn on/off Shadowsocks in the context menu");
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                _notifyIcon.ShowBalloonTip(0);
                _isFirstRun = false;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowLoginForm();
            }
        }

        private void changeCDKEY_Click(object sender, EventArgs e)
        {
            ShowLoginForm();
        }

        private void EnableItem_Click(object sender, EventArgs e)
        {
            controller.ToggleEnable(!enableItem.Checked);
        }

        private void GlobalModeItem_Click(object sender, EventArgs e)
        {
            controller.ToggleGlobal(true);
        }

        private void PACModeItem_Click(object sender, EventArgs e)
        {
            controller.ToggleGlobal(false);
        }

        private void ShareOverLANItem_Click(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = !ShareOverLANItem.Checked;
            controller.ToggleShareOverLAN(ShareOverLANItem.Checked);
        }

        private void EditPACFileItem_Click(object sender, EventArgs e)
        {
            controller.TouchPACFile();
        }

        private void UpdatePACFromGFWListItem_Click(object sender, EventArgs e)
        {
            controller.UpdatePACFromGFWList();
        }

        private void EditUserRuleFileForGFWListItem_Click(object sender, EventArgs e)
        {
            controller.TouchUserRuleFile();
        }

        private void AServerItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.SelectServerIndex((int)item.Tag);
        }

        private void AStrategyItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.SelectStrategy((string)item.Tag);
        }

        private void ShowLogItem_Click(object sender, EventArgs e)
        {
            Process.Start(Logging.LogFilePath);
        }

        private void AutoStartupItem_Click(object sender, EventArgs e)
        {
            AutoStartupItem.Checked = !AutoStartupItem.Checked;
            if (!AutoStartup.Set(AutoStartupItem.Checked))
            {
                MessageBox.Show(I18N.GetString("Failed to update registry"));
            }
        }

        private void LocalPACItem_Click(object sender, EventArgs e)
        {
            if (!localPACItem.Checked)
            {
                localPACItem.Checked = true;
                onlinePACItem.Checked = false;
                controller.UseOnlinePAC(false);
                UpdatePACItemsEnabledStatus();
            }
        }

        private void OnlinePACItem_Click(object sender, EventArgs e)
        {
            if (!onlinePACItem.Checked)
            {
                if (controller.GetConfigurationCopy().pacUrl.IsNullOrEmpty())
                {
                    UpdateOnlinePACURLItem_Click(sender, e);
                }
                if (!controller.GetConfigurationCopy().pacUrl.IsNullOrEmpty())
                {
                    localPACItem.Checked = false;
                    onlinePACItem.Checked = true;
                    controller.UseOnlinePAC(true);
                }
                UpdatePACItemsEnabledStatus();
            }
        }

        private void UpdateOnlinePACURLItem_Click(object sender, EventArgs e)
        {
            string origPacUrl = controller.GetConfigurationCopy().pacUrl;
            string pacUrl = Microsoft.VisualBasic.Interaction.InputBox(
                I18N.GetString("Please input PAC Url"),
                I18N.GetString("Edit Online PAC URL"),
                origPacUrl, -1, -1);
            if (!pacUrl.IsNullOrEmpty() && pacUrl != origPacUrl)
            {
                controller.SavePACUrl(pacUrl);
            }
        }

        private void UpdatePACItemsEnabledStatus()
        {
            if (this.localPACItem.Checked)
            {
                this.editLocalPACItem.Enabled = true;
                this.updateFromGFWListItem.Enabled = true;
                this.editGFWUserRuleItem.Enabled = true;
                this.editOnlinePACItem.Enabled = false;
            }
            else
            {
                this.editLocalPACItem.Enabled = false;
                this.updateFromGFWListItem.Enabled = false;
                this.editGFWUserRuleItem.Enabled = false;
                this.editOnlinePACItem.Enabled = true;
            }
        }
    }
}
