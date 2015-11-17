using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using LabTech.Interfaces;
using LabTechCommon;

namespace ScriptAnalyzer.ToolBar
{
    public partial class mainForm : Form
    {
        private static IControlCenter _Host;
        public SortableBindingList<scriptRow> _scriptList = new SortableBindingList<scriptRow>();
        
        public mainForm(IControlCenter host)
        {
            InitializeComponent();
            _Host = host;
            dataGridView1.CellMouseClick += dataGridView1_MouseDown;
            load_ScriptData();
        }

        public class scriptRow
        {
            public int scriptId { get; set; }
            public string scriptName { get; set; }
            public string scriptData { get; set; }
            public byte[] byteScriptData { get; set; }
            public string lastUser { get; set; }
            public string folderName { get; set; }
            public string lastDate { get; set; }
        }

        public class viewScript
        {
            public int scriptId { get; set; }
            public string scriptName { get; set; }
            public string lastUser { get; set; }
        }

        private void load_ScriptData()
        {
            DataSet scriptDs = null;

            try
            {

                scriptDs = LabTechFunctions.GetScripts();
                
                if (scriptDs.Tables.Count == 0)
                {
                    textBox1.Text = "0";
                }
                else
                {
                    textBox1.Text = scriptDs.Tables[0].Rows.Count.ToString();
                }
            }
            catch (Exception ex)
            {
                richTextBox1.Text += ex.Message;
                richTextBox1.Text += ex.Source;
                richTextBox1.Text += ex.ToString();
                return;
            }

            // populate checklist
            //  var dictScriptData = new List<scriptRow>();
            Library.Forms.SortableBindingList<scriptRow> rows = new Library.Forms.SortableBindingList<scriptRow>();
            foreach (DataRow row in scriptDs.Tables[0].Rows)
            {
                int curScriptId = (int)row["ScriptID"];
                byte[] curScriptData = (byte[])row["ScriptData"];
                string curFolderName = row["Script Folder"].ToString();
                string curScriptName = row["Script Name"].ToString();
                string curLastUser = row["Last_User"].ToString();
                string curLastUpdated = row["Last_Date"].ToString();
                string decompScript = "";

                //decompScript = StringUtilities.GunzipString(curScriptData.ToString());

                // add the list to my array.
                _scriptList.Add(new scriptRow
                {
                    scriptId = curScriptId,
                    folderName = curFolderName,
                    scriptName = curScriptName,
                    scriptData = decompScript,
                    lastUser = curLastUser,
                    byteScriptData = curScriptData,
                    lastDate = curLastUpdated
                });

                rows.Add(new scriptRow
                {
                    scriptId = curScriptId,
                    folderName = curFolderName,
                    scriptName = curScriptName,
                    scriptData = decompScript,
                    lastUser = curLastUser,
                    byteScriptData = curScriptData,
                    lastDate = curLastUpdated
                });
            }

            
            dataGridView1.DataSource = rows;
            AdjustDataColumns();
            dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Descending);

        }

        private void AdjustDataColumns()
        {
            if (dataGridView1.ColumnCount > 0)
            {

                try
                {
                    this.dataGridView1.Columns["byteScriptData"].Visible = false;
                    this.dataGridView1.Columns["scriptData"].Visible = false;

                    this.dataGridView1.Columns["scriptName"].DisplayIndex = 2;
                    this.dataGridView1.Columns["scriptName"].HeaderText = "Script Name";
                    this.dataGridView1.Columns["scriptId"].DisplayIndex = 0;
                    this.dataGridView1.Columns["scriptId"].HeaderText = "Script ID";
                    
                    this.dataGridView1.Columns["folderName"].DisplayIndex = 1;
                    this.dataGridView1.Columns["folderName"].HeaderText = "Script Folder";

                    this.dataGridView1.Columns["lastUser"].DisplayIndex = 4;
                    this.dataGridView1.Columns["lastUser"].HeaderText = "Last User";

                    this.dataGridView1.Columns["lastDate"].DisplayIndex = 5;
                    this.dataGridView1.Columns["lastDate"].HeaderText = "Date Last Updated";

                    this.dataGridView1.AllowUserToResizeColumns = true;

                    dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dataGridView1.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dataGridView1.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dataGridView1.Columns[6].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    this.dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader;
                }
                catch (Exception)
                {
                    
                    throw;
                }
           
            }
        }
      
        public class LabTechFunctions
        {
            //private static readonly string SqlQuery = "SELECT ScriptID, ScriptName, ScriptData, Last_User from lt_scripts;";

            public static string SqlQuery ="SELECT ScriptID, TRIM( TRAILING '\\\\' FROM CONVERT(CONCAT(IFNULL(CONCAT(f.Name ,' \\\\ '),''),IFNULL(CONCAT(s.Name ,'\\\\\\\\'),''),'' ) USING utf8)) AS `Script Folder`,c.ScriptName AS `Script Name`, ScriptData AS `ScriptData`, last_User,last_date FROM LT_scripts c LEFT JOIN scriptfolders s 	ON s.folderid=c.folderid LEFT JOIN scriptfolders f ON f.folderid=s.parentid WHERE (COALESCE(s.parentid,0)=0 OR s.name NOT LIKE '\\\\_%') ORDER BY `ScriptID` desc;";
            public static DataSet GetScripts()
            {
                
                var ds = _Host.GetDataSet(SqlQuery);
               
                if (ds.Tables[0].Rows.Count > 0)
                {
                    return ds;
                }

                return null;
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void show_OptionsContext(object sender,DataGridViewCellMouseEventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem mnuAnalyzeItem = new MenuItem("Analyze (QA)");
            mnuAnalyzeItem.Click += mnuAnalyzeItem_Click;

            MenuItem mnuViewItem = new MenuItem("View History (coming soon)");
               mnuViewItem.Click += mnuViewItem_Click;

            menu.MenuItems.AddRange(
                new MenuItem[] { mnuAnalyzeItem, mnuViewItem}
        );

            menu.Show(dataGridView1, dataGridView1.PointToClient(Cursor.Position));
        }

void mnuViewItem_Click(object sender, EventArgs e)
{
 	//throw new NotImplementedException();
}

void mnuAnalyzeItem_Click(object sender, EventArgs e)
{
 	
    DataGridViewRow row = dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex];
    int selectedScriptId = (int) row.Cells["ScriptID"].Value;

    IEnumerable<scriptRow> currentRow = _scriptList.Where(o => o.scriptId == selectedScriptId);
   
    // instantiate and show form with current row's data.
    AnalysisForm outputForm = new AnalysisForm(currentRow, _Host);
    outputForm.Show();

}
        private void dataGridView1_MouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {

            if (e.Button == MouseButtons.Right)
            {
                try
                {
                    this.dataGridView1.CurrentCell = this.dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    show_OptionsContext(sender, e);
                }
                catch (Exception)
                {
                    
                }
                

            }

        }

       
        
/*
          Private Sub DataGridView1_CellMouseUp_1(sender As Object, e As DataGridViewCellMouseEventArgs) _
        Handles DataGridView1.CellMouseUp

        If e.Button = MouseButtons.Right Then
            DataGridView1.Rows(e.RowIndex).Selected = True
            'DataGridView1.CurrentCell = Me.DataGridView1.Rows(e.RowIndex).Cells(1)
            ContextMenuStrip1.Show(DataGridView1, e.Location)
            ContextMenuStrip1.Show(Cursor.Position)
        End If
    End Sub
        */

    }

    public class SortableBindingList<T> : BindingList<T>
    {
        private bool isSortedValue;
        ListSortDirection sortDirectionValue;
        PropertyDescriptor sortPropertyValue;

        public SortableBindingList()
        {
        }

        public SortableBindingList(IList<T> list)
        {
            foreach (object o in list)
            {
                this.Add((T)o);
            }
        }

        protected override void ApplySortCore(PropertyDescriptor prop,
            ListSortDirection direction)
        {
            Type interfaceType = prop.PropertyType.GetInterface("IComparable");

            if (interfaceType == null && prop.PropertyType.IsValueType)
            {
                Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);

                if (underlyingType != null)
                {
                    interfaceType = underlyingType.GetInterface("IComparable");
                }
            }

            if (interfaceType != null)
            {
                sortPropertyValue = prop;
                sortDirectionValue = direction;

                IEnumerable<T> query = base.Items;

                if (direction == ListSortDirection.Ascending)
                {
                    query = query.OrderBy(i => prop.GetValue(i));
                }
                else
                {
                    query = query.OrderByDescending(i => prop.GetValue(i));
                }

                int newIndex = 0;
                foreach (object item in query)
                {
                    this.Items[newIndex] = (T)item;
                    newIndex++;
                }

                isSortedValue = true;
                this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
            else
            {
                throw new NotSupportedException("Cannot sort by " + prop.Name +
                    ". This" + prop.PropertyType.ToString() +
                    " does not implement IComparable");
            }
        }

        protected override PropertyDescriptor SortPropertyCore
        {
            get { return sortPropertyValue; }
        }

        protected override ListSortDirection SortDirectionCore
        {
            get { return sortDirectionValue; }
        }

        protected override bool SupportsSortingCore
        {
            get { return true; }
        }

        protected override bool IsSortedCore
        {
            get { return isSortedValue; }
        }
    }


}