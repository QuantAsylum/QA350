using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QA350
{
    public partial class DlgEntry : Form
    {
        public DlgEntry(int interval)
        {
            InitializeComponent();

            numericUpDown1.Value = Convert.ToDecimal(interval);
        }
    }
}
