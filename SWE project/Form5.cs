using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SWE_project
{
    public partial class Form5 : Form


    {

        public void setReport(object reportSource)
        {
            crystalReportViewer1.ReportSource = reportSource;
        }
        public Form5()
        {
            InitializeComponent();
        }

        private void crystalReportViewer1_Load(object sender, EventArgs e)
        {

        }

        private void Form5_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            crystalReportViewer1.Dock = DockStyle.Fill;
        }
    }
}
