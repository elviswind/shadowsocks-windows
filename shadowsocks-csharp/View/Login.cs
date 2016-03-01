using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class Login : Form
    {
        Configuration config = Configuration.Load();
        public Login()
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            tbCdkey.Text = config.cdkey;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            var server = CDKey.readCDKEY(tbCdkey.Text);
            if (!string.IsNullOrEmpty(server.server))
            {
                config.cdkey = tbCdkey.Text;
                Configuration.Save(config);
                this.Close();
            }
            else
            {
                lbError.Text = "序列号有误";
            }
        }
    }
}
