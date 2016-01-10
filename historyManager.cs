using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LabTech.Interfaces;

namespace ScriptAnalyzer.ToolBar
{
    public partial class historyManager : Form
    {
        private readonly IControlCenter _Host;
        
        public historyManager()
        {
            InitializeComponent();
        }

        public historyManager(IEnumerable<mainForm.scriptRow>  currentRow, IControlCenter host)
        {
            InitializeComponent();
            _Host = host;
            dgv_UpdateHistory.CellMouseClick += dgv_UpdateHistory_MouseDown;

            if (currentRow == null)
            {
                return;
            }

            DataSet historyItems = get_RevisionsForScript(currentRow);
            
            if (historyItems == null)
            {
                return;
            }

            BindingSource bsReportBinding = new BindingSource();
            bsReportBinding.DataMember = historyItems.Tables[0].TableName;
            bsReportBinding.DataSource = historyItems;


            dgv_UpdateHistory.AutoGenerateColumns = true;

            dgv_UpdateHistory.DataSource = bsReportBinding;

            //dgv_UpdateHistory.Sort(dgv_UpdateHistory.Columns[0], ListSortDirection.Descending);
            // must be script history records to render in DGV


        }

        private DataSet get_RevisionsForScript(IEnumerable<mainForm.scriptRow> scriptToFetch)
        {
            var script = scriptToFetch.ToList();
            if (scriptToFetch == null)
            {
                MessageBox.Show("No script object was passed into the history form.");
                return null;
            }

            DataSet scriptHistory = null;
            var scriptId = script.Select(l => l.scriptId).ToList().FirstOrDefault();
            
            try
            {
                string SqlQuery = string.Format("SELECT * from plugin_lt_scriptbackups where scriptId = {0};",scriptId);

                SqlQuery = string.Format(@"
SELECT
    `ScripthistoryID` as `Script History Id`,
    `ScriptID` as `Script ID`,
	`Versionnumber` AS `Revision Number`,
	CONVERT(
		COALESCE(c.`ControlTableProduction`,c.`HighestFoundInBackups`), DECIMAL(4,1)) AS `Current Production Version`,
	# Date Since Created
	#DATEDIFF(NOW(), b.BackupCreateDate) as `Days Since Updated`,
	CONCAT(
FLOOR(HOUR(TIMEDIFF(NOW(), b.BackupCreateDate)) / 24), 'd ',
MOD(HOUR(TIMEDIFF(NOW(), b.BackupCreateDate)), 24), 'h ',
MINUTE(TIMEDIFF(NOW(), b.BackupCreateDate)), 'm ') AS `Days Since Updated`,
	`ScriptName` AS `Script Name`,
	BackupCreateDate AS `Date Updated`,
	COALESCE(SUBSTRING_INDEX(SUBSTRING_INDEX(`Last_User`, '@', 1), ' ', -1),'Unknown') AS `Last Updated By`
	FROM plugin_lt_scriptbackups b
LEFT OUTER JOIN (
	SELECT MAX(VersionNumber) AS `HighestFoundInBackups`,
		b.ScriptGuid, 
		c.CurrentProductionVersion AS `ControlTableProduction` 
	FROM plugin_lt_scriptbackups b
	LEFT OUTER JOIN plugin_lt_scriptversioncontrol c ON c.ScriptGUID = b.ScriptGUID
	GROUP BY ScriptGUID
) AS c ON b.ScriptGuid = c.ScriptGuid
WHERE b.ScriptID = {0};
", scriptId);

                scriptHistory = _Host.GetDataSet(SqlQuery);

                if (scriptHistory.Tables.Count == 0)
                {
                    // no records returned
                    MessageBox.Show("No script history records found for this script.");
                    return null;
                }
                else if (scriptHistory.Tables[0].Rows.Count > 0)
                {
                    return scriptHistory;
                }

                return null;
     
            }
            catch
            {
                MessageBox.Show("An error was encountered while attempting to gather the dataset containing script history for this script.");
                return null;
            }


        }

        private void show_OptionsContext(object sender, DataGridViewCellMouseEventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem mnuAnalyzeItem = new MenuItem("Make Production Version");
            mnuAnalyzeItem.Click += Make_ProductionVersion;

            MenuItem mnuViewItem = new MenuItem("Create Copy of this Version");
            mnuViewItem.Click += Create_CopyOfVersion;

           

            menu.MenuItems.AddRange(
                new MenuItem[] { mnuAnalyzeItem, mnuViewItem }


        );

            menu.Show(dgv_UpdateHistory, dgv_UpdateHistory.PointToClient(Cursor.Position));
        }

        void mnuAnalyzeItem_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = dgv_UpdateHistory.Rows[dgv_UpdateHistory.SelectedCells[0].RowIndex];
            int selectedScriptId = (int)row.Cells["ScriptID"].Value;


            // NEED TO REPLACE THE BELOW WITH PROPER LOGIC
            return;

            //IEnumerable<mainForm.scriptRow> currentRow = _scriptList.Where(o => o.scriptId == selectedScriptId);

            // instantiate and show form with current row's data.
            // AnalysisForm outputForm = new AnalysisForm(currentRow, _Host);
            // outputForm.Show();

        }

        private void Make_ProductionVersion(object sender, EventArgs e)
        {

            DataGridViewRow row = dgv_UpdateHistory.Rows[dgv_UpdateHistory.SelectedCells[0].RowIndex];
            
            int selectedScriptId = (int)row.Cells["Script ID"].Value;
            int selectedHistId = (int)row.Cells["Script History Id"].Value;
            var revisionNumber = row.Cells["Revision Number"].Value;
            
            // RULES I want to enforce:
            // 1.  The script cannot be running in the system.
            // 2.    Must insert script
            MessageBox.Show(string.Format("Script ID: {0} Version requested: {1}",selectedScriptId,revisionNumber));

            // using transaction ... delete from lt_scripts where scriptID = current script ID;
            // Insert into lt_scripts (select all columns I want except concatenated column for keyword that prevents update trigger from fking me)  
                // ----> ScriptNotes LIKE "[Script Revision Manager - Restored this%][|][|]%",'PROCESS IT','NVM'
            // SELECT ....

            // TODO: Build insert
            string insertForRestoration = string.Format(@"
            
INSERT INTO `lt_scripts` 
	(`ScriptId`, 
	`FolderId`, 
	`ScriptName`, 
	`ScriptNotes`, 
	`Permission`, 
	`EditPermission`, 
	`ComputerScript`, 
	`LocationScript`, 
	`MaintenanceScript`, 
	`FunctionScript`, 
	`LicenseData`, 
	`ScriptData`, 
	`ScriptVersion`, 
	`ScriptGuid`, 
	`ScriptFlags`, 
	`Parameters`, 
	`Last_User`, 
	`Last_Date`
	)
	SELECT 
	('ScriptId', 
	'FolderId', 
	'ScriptName', 
	'ScriptNotes', 
	'Permission', 
	'EditPermission', 
	'ComputerScript', 
	'LocationScript', 
	'MaintenanceScript', 
	'FunctionScript', 
	'LicenseData', 
	'ScriptData', 
	'ScriptVersion', 
	'ScriptGuid', 
	'ScriptFlags', 
	'Parameters', 
	'Last_User', 
	'Last_Date'
	)
FROM plugin_lt_scriptbackups
WHERE ScriptID = {0} AND ScriptHistoryID = {1}
ORDER BY Last_Date Desc # this and limit is just in case..
LIMIT 0,1;

", selectedScriptId, selectedHistId);


            // TODO: Build and Execute Delete
            string DeleteForProdScript = string.Format("Delete from lt_scripts where ScriptID = '{0}';", selectedScriptId);
            

            // TODO: EXECUTE INSERT

            // TODO: EXECUTE 



        }

        private void Create_CopyOfVersion(object sender, EventArgs e)
        {
            
        }

        private void dgv_UpdateHistory_MouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {

            if (e.Button == MouseButtons.Right)
            {
                try
                {
                    this.dgv_UpdateHistory.CurrentCell = this.dgv_UpdateHistory.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    show_OptionsContext(sender, e);
                }
                catch (Exception)
                {

                }


            }

        }



    }


}
