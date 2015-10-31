using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScriptAnalyzer.ToolBar
{
    public partial class viewRawScriptXML : Form
    {
        public viewRawScriptXML()
        {
            InitializeComponent();
        }

        public viewRawScriptXML(string scriptdata)
        {
            InitializeComponent();
            
                richTextBox1.Text += scriptdata;
        }

    }
}
