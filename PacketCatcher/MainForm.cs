using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PacketCatcher
{
    public partial class MainForm : Form
    {
        private InfoForm inForm;

        public MainForm()
        {
            InitializeComponent();
            inForm = new InfoForm();
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Show info form
            inForm.Show();
        }
    }
}
