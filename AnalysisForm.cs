using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using LabTech.Interfaces;
using LabTechCommon;

namespace ScriptAnalyzer.ToolBar
{
    public partial class AnalysisForm : Form
    {
        public enum ScriptNoteFlags : byte
        {
            GotoLabel = 0, // GOOD!  goto:   :LabelExample
            NotesOnly = 1, // blank or text 
            FoundRecord = 2
        }

        public static List<LabTechFunctions> _GotoFunctionList = new List<LabTechFunctions>();
        public static List<LabTechFunctions> _FuncsRequiringCOF = new List<LabTechFunctions>();
        public static List<FuncNeedingCacheRefresh> _FuncsNeedingResend = new List<FuncNeedingCacheRefresh>();

        // stats that will be updated as the script _CurrentScriptsSteps are processed
        private static int _numberOfMissingLabels;
        private mainForm.scriptRow _currentScriptRecord;
        private List<ScriptStepRec> _CurrentScriptsSteps = new List<ScriptStepRec>();
        public string _CurrentScriptXml;
        private readonly IControlCenter _Host;
        private string _originalData;
        private string currentScriptData;
        public List<object> labelsFound = new List<object>();
        private string _originalReportOutput;

        public AnalysisForm()
        {
            InitializeComponent();
        }

        // i need the script ID and a function to call that will fetch NEW script data
        public AnalysisForm(IEnumerable<mainForm.scriptRow> curRow, IControlCenter host)
        {

            InitializeComponent();
            _Host = host;

            txtBoxMissingCof.Text = "0";
            _numberOfMissingLabels = 0;
            _currentScriptRecord = curRow.First();
            PopulateGotoFuncs();
            ExtractScriptData(_currentScriptRecord);
            PerformQaOperations();

            richTextBox1.Rtf = @"{\rtf1\ansi \b
I hope that this plugin makes LabTech scripting easier for you, and will reduce the time required to develop/test your LabTech scripts. \line\line

Too many times have I seen my scripts and those of others fail due to typos in script note labels or labels that were pasted and needed to be renamed. 
\line
Happy LabTech Scripting!\line\line

- Marcus \line


}";

        }



        public void ExtractScriptData(mainForm.scriptRow thisScriptData)
        {
            var scriptDataComp = Encoding.UTF8.GetString(thisScriptData.byteScriptData);
            // populate property with all IF xxx GOTO this label. Each function has a different parameter that the label name is stored in.

            currentScriptData = DecompScriptData(scriptDataComp);
            _CurrentScriptXml = currentScriptData;
            ParseXmlToObject(currentScriptData);
        }

        private void btnBuildReport_Click(object sender, EventArgs e)
        {
            // need to repopulate _CurrentScriptsSteps
            // on this button's click, create a script row for the given script by using getscripts

            // uncheck "Show only errors" because we're re-populating box.
            checkBox1.Checked = false;

            var SqlQuery =
                string.Format(
                    "SELECT ScriptID, TRIM( TRAILING '\\\\' FROM CONVERT(CONCAT(IFNULL(CONCAT(f.Name ,' \\\\ '),''),IFNULL(CONCAT(s.Name ,'\\\\\\\\'),''),'' ) USING utf8)) AS `Script Folder`,c.ScriptName AS `Script Name`, ScriptData AS `ScriptData`, last_User,last_date FROM LT_scripts c LEFT JOIN scriptfolders s 	ON s.folderid=c.folderid LEFT JOIN scriptfolders f ON f.folderid=s.parentid WHERE (COALESCE(s.parentid,0)=0 OR s.name NOT LIKE '\\\\_%') AND c.ScriptID={0} ORDER BY `Script Folder` ASC;",
                    _currentScriptRecord.scriptId);
            var ds = _Host.GetDataSet(SqlQuery);

            // clear the property value
            _numberOfMissingLabels = 0;


            if (ds.Tables[0].Rows.Count == 1)
            {
                rTxtBoxResults.Text = "";
                _CurrentScriptsSteps = new List<ScriptStepRec>();

                var row = ds.Tables[0].Rows[0];
                var newScriptrow = generateScriptRow(row);

                _currentScriptRecord = newScriptrow;


                ExtractScriptData(_currentScriptRecord);

                PerformQaOperations();

                // AnalyzeScript(_CurrentScriptsSteps);
            }
            else
            {
                rTxtBoxResults.Text =
                    "Failed to open script because it could not be found. Please try closing the plugin and reloading the script.";
            }
        }

        public static mainForm.scriptRow generateScriptRow(DataRow row)
        {
            var curScriptId = (int) row["ScriptID"];
            var curScriptData = (byte[]) row["ScriptData"];
            var curFolderName = row["Script Folder"].ToString();
            var curScriptName = row["Script Name"].ToString();
            var curLastUser = row["Last_User"].ToString();
            var curLastUpdated = row["Last_Date"].ToString();
            var decompScript = "";

            var newScriptRow = new mainForm.scriptRow
            {
                scriptId = curScriptId,
                folderName = curFolderName,
                scriptName = curScriptName,
                scriptData = decompScript,
                lastUser = curLastUser,
                byteScriptData = curScriptData,
                lastDate = curLastUpdated
            };

            return newScriptRow;
        }

        private void PerformQaOperations()
        {
            AnalyzeScript(_CurrentScriptsSteps);

            // Populate the informational textboxes 
            var numsteps = _CurrentScriptsSteps.Where(i => i.osLimit != "-2147483648").ToList().Count;
            textBox1.Text = (numsteps - 1).ToString();
            textBox2.Text = _numberOfMissingLabels.ToString();

            var scriptName = _currentScriptRecord.scriptName;
            var scriptId = _currentScriptRecord.scriptId.ToString();
            var lastUser = _currentScriptRecord.lastUser;
            var lastDate = _currentScriptRecord.lastDate;
            Text = string.Format("Script Analysis - {0} (ID: {1})", scriptName, scriptId);

            txtBoxLastUpdatedBy.Text = lastUser;
            txtBoxScrId.Text = scriptId;
            txtBoxScrName.Text = scriptName;
            txtBoxLastUpdatedBy.Text = lastUser;
            txtBoxLastDate.Text = lastDate;
        }

        private string DecompScriptData(string data)
        {
            try
            {
                var decompScriptData = "";
                decompScriptData = StringUtilities.GunzipString(data);

                return decompScriptData;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static object GetNodeValue(XmlNode node, string xpath, out string innerText)
        {
            Debug.Assert(!string.IsNullOrEmpty(xpath));

            innerText = null;

            if (node == null)
            {
                return false;
            }

            var selectedNode = node.SelectSingleNode(xpath);
            if (selectedNode == null)
                return false;

            innerText = selectedNode.InnerText;
            return true;
        }

        public static void AddOneMissingLabel()
        {
            _numberOfMissingLabels += 1;
        }

        private bool ParseXmlToObject(string scriptXmlContent)
        {
            var scriptXml = new XmlDocument();
            scriptXml.LoadXml(scriptXmlContent);

            var nodes = scriptXml.SelectNodes("/ScriptData/ScriptSteps");

            foreach (XmlNode node in nodes)
            {
                var step = new ScriptStepRec();

                GetNodeValue(node, "Action", out step.action);
                GetNodeValue(node, "FunctionId", out step.functionId);
                GetNodeValue(node, "Param1", out step.param1);
                GetNodeValue(node, "Param2", out step.param2);
                GetNodeValue(node, "Param3", out step.param3);
                GetNodeValue(node, "Param4", out step.param4);
                GetNodeValue(node, "Param5", out step.param5);
                GetNodeValue(node, "Sort", out step.sort);
                GetNodeValue(node, "Continue", out step.stepContinue);
                GetNodeValue(node, "OsLimit", out step.osLimit);
                GetNodeValue(node, "Indentation", out step.indentation);


                _CurrentScriptsSteps.Add(step);
            }
            return true;
        }


        private void AnalyzeScript(List<ScriptStepRec> scriptdata)
        {
            var badRecs = new List<ScriptStepRec>();
            //List<string> la  = new List<string>();
            //List<object> labelsFound = new List<object> {};

            // Did we find data?
            if (scriptdata == null)
                return;

            // Get a list of all script notes (funtion ID = 139)
            var scriptnoterecs =
                scriptdata.Where(e => int.Parse(e.functionId) == 139 && e.osLimit != "-2147483648").ToList();

            // CALL function that analyzes the script notes
            var listOfLabelsFound = AnalysisFunctions.AnalyzeScriptNotes(scriptnoterecs, rTxtBoxResults);

            // CALL function that analyzes the IF functions. This looks for missing labelsEXIT
            AnalysisFunctions.AnalyzeIfFunctions(scriptdata, rTxtBoxResults, listOfLabelsFound);

            // To DO:  CALL function that looks for unused labels.
            var numberOfUnused = AnalysisFunctions.IdentifyUnusedLabels(listOfLabelsFound, rTxtBoxResults);
            textBox3.Text = numberOfUnused.ToString();

            // To DO: CALL function that identifies commands w/o continue on failure

            try
            {
                var linesMissingCOF = AnalysisFunctions.IdentifyNonCof(scriptdata, rTxtBoxResults);
                txtBoxMissingCof.Text = linesMissingCOF.ToString();
            }
            catch (Exception exc)
            {
                rTxtBoxResults.Text += exc.Message;
                rTxtBoxResults.Text += exc.StackTrace;
                throw;
            }

            try
            {
                AnalysisFunctions.FindMissingResends(scriptdata, rTxtBoxResults);
            }
            catch (Exception ex)
            {
                rTxtBoxResults.Text +=
                    "An error was encountered while trying to analyze IF functions for matching RESEND script steps..." +
                    Environment.NewLine;
                rTxtBoxResults.Text += ex.Message + Environment.NewLine;
                rTxtBoxResults.Text += ex.StackTrace + Environment.NewLine;

            }

        }

        private static void PopulateGotoFuncs()
        {
            _GotoFunctionList.Clear();
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 70,
                ParamIdForLabel = "Param4",
                FunctionName = "Variable Check"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 110,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Process Exists"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 111,
                ParamIdForLabel = "Param4",
                FunctionName = "IF Registry Check"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 113,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Service is Running"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 114,
                ParamIdForLabel = "Param3",
                FunctionName = "IF File Check"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 115,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Console Logged On"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 116,
                ParamIdForLabel = "Param3",
                FunctionName = "IF User Response"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 117,
                ParamIdForLabel = "Param4",
                FunctionName = "IF SQL Data Check"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 118,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Smart Attributes"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 119,
                ParamIdForLabel = "Param4",
                FunctionName = "IF Drive Status"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 120,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Software Installed"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 121,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Patch Installed"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 122,
                ParamIdForLabel = "Param4",
                FunctionName = "IF AutoStartup Check"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 123,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Tool Installed"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 124,
                ParamIdForLabel = "Param2",
                FunctionName = "IF New Unassigned Ticket"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 129,
                ParamIdForLabel = "Param1",
                FunctionName = "Script Goto"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 131,
                ParamIdForLabel = "Param3",
                FunctionName = "IF Group Member"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 140,
                ParamIdForLabel = "Param3",
                FunctionName = "IF Ticket Status"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 212,
                ParamIdForLabel = "Param4",
                FunctionName = "IF File Compare"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 225,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Plugin Enabled"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 226,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Plugin Agent Command Available"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 227,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Plugin Server Function Available"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 237,
                ParamIdForLabel = "Param2",
                FunctionName = "IF Role Detected"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 238,
                ParamIdForLabel = "Param1",
                FunctionName = "Script Call"
            });
            _GotoFunctionList.Add(new LabTechFunctions
            {
                FunctionId = 253,
                ParamIdForLabel = "Param3",
                FunctionName = "IF Group Member"
            });
            _GotoFunctionList.Add(new LabTechFunctions 
            {
                FunctionId = 255,
                ParamIdForLabel = "Param3",
                FunctionName = "IF Group Member"
            });



            #region FunctionsRequiringCOF

            _FuncsRequiringCOF.Clear();
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 214,
                ParamIdForLabel = "Bulk Registry Write",
                FunctionName = "Bulk Registry Write"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 2,
                ParamIdForLabel = "LabTech Command",
                FunctionName = "LabTech Command"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 12,
                ParamIdForLabel = "Process Kill",
                FunctionName = "Process Kill"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 13,
                ParamIdForLabel = "File Delete",
                FunctionName = "File Delete"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 14,
                ParamIdForLabel = "Registry Delete Key",
                FunctionName = "Registry Delete Key"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 15,
                ParamIdForLabel = "Process Execute",
                FunctionName = "Process Execute"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 16,
                ParamIdForLabel = "Shell",
                FunctionName = "Shell"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 18,
                ParamIdForLabel = "File Upload",
                FunctionName = "File Upload"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 19,
                ParamIdForLabel = "File Download URL",
                FunctionName = "File Download URL"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 20,
                ParamIdForLabel = "Variable Set",
                FunctionName = "Variable Set"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 22,
                ParamIdForLabel = "Reboot",
                FunctionName = "Reboot"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 23,
                ParamIdForLabel = "File Rename",
                FunctionName = "File Rename"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 24,
                ParamIdForLabel = "Email",
                FunctionName = "Email"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 25,
                ParamIdForLabel = "Console Show Message",
                FunctionName = "Console Show Message"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 26,
                ParamIdForLabel = "Console Open Browser",
                FunctionName = "Console Open Browser"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 27,
                ParamIdForLabel = "Registry Set Value",
                FunctionName = "Registry Set Value"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 28,
                ParamIdForLabel = "File Download",
                FunctionName = "File Download"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 30,
                ParamIdForLabel = "Console Logoff User",
                FunctionName = "Console Logoff User"
            });
            //_FuncsRequiringCOF.Add(new LabTechFunctions { FunctionId = 31, ParamIdForLabel = "LabTech FasTalk", FunctionName = "LabTech FasTalk" });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 32,
                ParamIdForLabel = "Printer Set Default",
                FunctionName = "Printer Set Default"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 33,
                ParamIdForLabel = "Printer Clear Queue",
                FunctionName = "Printer Clear Queue"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 34,
                ParamIdForLabel = "File Copy",
                FunctionName = "File Copy"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 35,
                ParamIdForLabel = "Service Start",
                FunctionName = "Service Start"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 36,
                ParamIdForLabel = "Service Stop",
                FunctionName = "Service Stop"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 37,
                ParamIdForLabel = "Console Screen Capture",
                FunctionName = "Console Screen Capture"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 39,
                ParamIdForLabel = "Disk Defrag",
                FunctionName = "Disk Defrag"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 40,
                ParamIdForLabel = "Disk Check",
                FunctionName = "Disk Check"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 41,
                ParamIdForLabel = "Performance Counter Get ",
                FunctionName = "Performance Counter Get "
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 42,
                ParamIdForLabel = "Console Execute",
                FunctionName = "Console Execute"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 43,
                ParamIdForLabel = "Share Create",
                FunctionName = "Share Create"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 44,
                ParamIdForLabel = "Share Delete",
                FunctionName = "Share Delete"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 45,
                ParamIdForLabel = "Resend Error Logs",
                FunctionName = "Resend Error Logs"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 46,
                ParamIdForLabel = "Ticket Create",
                FunctionName = "Ticket Create"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 47,
                ParamIdForLabel = "LTServer Create Alert",
                FunctionName = "LTServer Create Alert"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 48,
                ParamIdForLabel = "Email Alerts",
                FunctionName = "Email Alerts"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 49,
                ParamIdForLabel = "Resend EventLogs",
                FunctionName = "Resend EventLogs"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 50,
                ParamIdForLabel = "Resend Hardware",
                FunctionName = "Resend Hardware"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 51,
                ParamIdForLabel = "Resend Process List",
                FunctionName = "Resend Process List"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 52,
                ParamIdForLabel = "Resend Autostartup List",
                FunctionName = "Resend Autostartup List"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 53,
                ParamIdForLabel = "Resend Drive Info",
                FunctionName = "Resend Drive Info"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 54,
                ParamIdForLabel = "Resend Software",
                FunctionName = "Resend Software"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 55,
                ParamIdForLabel = "Resend Config",
                FunctionName = "Resend Config"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 56,
                ParamIdForLabel = "Resend Service List",
                FunctionName = "Resend Service List"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 57,
                ParamIdForLabel = "Resend Printers",
                FunctionName = "Resend Printers"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 58,
                ParamIdForLabel = "Net Wake on Lan",
                FunctionName = "Net Wake on Lan"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 66,
                ParamIdForLabel = "Offline Backup",
                FunctionName = "Offline Backup"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 67,
                ParamIdForLabel = "SQL Execute",
                FunctionName = "SQL Execute"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 69,
                ParamIdForLabel = "Tool Install",
                FunctionName = "Tool Install"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 72,
                ParamIdForLabel = "Ticket Assign",
                FunctionName = "Ticket Assign"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 73,
                ParamIdForLabel = "LTServer Execute",
                FunctionName = "LTServer Execute"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 74,
                ParamIdForLabel = "Disk Cleanup",
                FunctionName = "Disk Cleanup"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 75,
                ParamIdForLabel = "LTServer Write to File",
                FunctionName = "LTServer Write to File"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 76,
                ParamIdForLabel = "LTServer Net Send",
                FunctionName = "LTServer Net Send"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 77,
                ParamIdForLabel = "LTServer Voice Message",
                FunctionName = "LTServer Voice Message"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 78,
                ParamIdForLabel = "LTServer Pager Message",
                FunctionName = "LTServer Pager Message"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 79,
                ParamIdForLabel = "LTServer Send Fax",
                FunctionName = "LTServer Send Fax"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 80,
                ParamIdForLabel = "ExtraData Set Value",
                FunctionName = "ExtraData Set Value"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 81,
                ParamIdForLabel = "Net Get SNMP",
                FunctionName = "Net Get SNMP"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 82,
                ParamIdForLabel = "File Zip",
                FunctionName = "File Zip"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 83,
                ParamIdForLabel = "Add User Accounts",
                FunctionName = "Add User Accounts"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 84,
                ParamIdForLabel = "Net TFTP Send",
                FunctionName = "Net TFTP Send"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 85,
                ParamIdForLabel = "Play Sound",
                FunctionName = "Play Sound"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 86,
                ParamIdForLabel = "Net Renew IP",
                FunctionName = "Net Renew IP"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 87,
                ParamIdForLabel = "Net Ping",
                FunctionName = "Net Ping"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 88,
                ParamIdForLabel = "Net IPConfig",
                FunctionName = "Net IPConfig"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 89,
                ParamIdForLabel = "Net DNS Lookup",
                FunctionName = "Net DNS Lookup"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 90,
                ParamIdForLabel = "File Write Text",
                FunctionName = "File Write Text"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 91,
                ParamIdForLabel = "Net Get IP Port",
                FunctionName = "Net Get IP Port"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 92,
                ParamIdForLabel = "Script For Each",
                FunctionName = "Script For Each"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 93,
                ParamIdForLabel = "Report Email",
                FunctionName = "Report Email"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 94,
                ParamIdForLabel = "Report Print",
                FunctionName = "Report Print"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 101,
                ParamIdForLabel = "Script Math",
                FunctionName = "Script Math"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 102,
                ParamIdForLabel = "Script RegEx",
                FunctionName = "Script RegEx"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 103,
                ParamIdForLabel = "ExtraData Get Value",
                FunctionName = "ExtraData Get Value"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 104,
                ParamIdForLabel = "Ticket Combine",
                FunctionName = "Ticket Combine"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 105,
                ParamIdForLabel = "Ticket Finish",
                FunctionName = "Ticket Finish"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 106,
                ParamIdForLabel = "Ticket Stall",
                FunctionName = "Ticket Stall"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 107,
                ParamIdForLabel = "Ticket Open",
                FunctionName = "Ticket Open"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 108,
                ParamIdForLabel = "Ticket Comment",
                FunctionName = "Ticket Comment"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 109,
                ParamIdForLabel = "Shell Enhanced",
                FunctionName = "Shell Enhanced"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 125,
                ParamIdForLabel = "Process Execute as Admin",
                FunctionName = "Process Execute as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 126,
                ParamIdForLabel = "Shell as Admin",
                FunctionName = "Shell as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 127,
                ParamIdForLabel = "Process Execute as User",
                FunctionName = "Process Execute as User"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 128,
                ParamIdForLabel = "Shell as User",
                FunctionName = "Shell as User"
            });
            // _FuncsRequiringCOF.Add(new LabTechFunctions { FunctionId = 129, ParamIdForLabel = "Script Goto", FunctionName = "Script Goto" });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 132,
                ParamIdForLabel = "Patch Install All",
                FunctionName = "Patch Install All"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 133,
                ParamIdForLabel = "Windows Update Settings",
                FunctionName = "Windows Update Settings"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 134,
                ParamIdForLabel = "Ticket Add Time",
                FunctionName = "Ticket Add Time"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 135,
                ParamIdForLabel = "Powershell Command",
                FunctionName = "Powershell Command"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 136,
                ParamIdForLabel = "Patch Approve",
                FunctionName = "Patch Approve"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 137,
                ParamIdForLabel = "LTServer Record Stat",
                FunctionName = "LTServer Record Stat"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 138,
                ParamIdForLabel = "LabTech Send Message to Computer",
                FunctionName = "LabTech Send Message to Computer"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 142,
                ParamIdForLabel = "Ticket Reading View",
                FunctionName = "Ticket Reading View"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 143,
                ParamIdForLabel = "Ticket Update",
                FunctionName = "Ticket Update"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 144,
                ParamIdForLabel = "Ticket Elevate",
                FunctionName = "Ticket Elevate"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 146,
                ParamIdForLabel = "Script For Each SQL",
                FunctionName = "Script For Each SQL"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 147,
                ParamIdForLabel = "LabTech Probe Control",
                FunctionName = "LabTech Probe Control"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 148,
                ParamIdForLabel = "Resend System Information",
                FunctionName = "Resend System Information"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 149,
                ParamIdForLabel = "Resend Network information",
                FunctionName = "Resend Network information"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 150,
                ParamIdForLabel = "Service Startup Control",
                FunctionName = "Service Startup Control"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 151,
                ParamIdForLabel = "Net Set SNMP",
                FunctionName = "Net Set SNMP"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 152,
                ParamIdForLabel = "Resend Patch information",
                FunctionName = "Resend Patch information"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 153,
                ParamIdForLabel = "Patch Install",
                FunctionName = "Patch Install"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 154,
                ParamIdForLabel = "Folder Create",
                FunctionName = "Folder Create"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 155,
                ParamIdForLabel = "Folder Create as Admin",
                FunctionName = "Folder Create as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 156,
                ParamIdForLabel = "Folder Create as User",
                FunctionName = "Folder Create as User"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 157,
                ParamIdForLabel = "Folder Move",
                FunctionName = "Folder Move"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 158,
                ParamIdForLabel = "Folder Move as Admin",
                FunctionName = "Folder Move as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 159,
                ParamIdForLabel = "Folder Move as User",
                FunctionName = "Folder Move as User"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 160,
                ParamIdForLabel = "Folder Delete",
                FunctionName = "Folder Delete"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 161,
                ParamIdForLabel = "Folder Delete as Admin",
                FunctionName = "Folder Delete as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 162,
                ParamIdForLabel = "Folder Delete as User",
                FunctionName = "Folder Delete as User"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 163,
                ParamIdForLabel = "File Delete as Admin",
                FunctionName = "File Delete as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 164,
                ParamIdForLabel = "File Delete as User",
                FunctionName = "File Delete as User"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 165,
                ParamIdForLabel = "File Rename as Admin",
                FunctionName = "File Rename as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 166,
                ParamIdForLabel = "File Rename as User",
                FunctionName = "File Rename as User"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 167,
                ParamIdForLabel = "File Copy as Admin",
                FunctionName = "File Copy as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 168,
                ParamIdForLabel = "File Copy as User",
                FunctionName = "File Copy as User"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 169,
                ParamIdForLabel = "Console Shell",
                FunctionName = "Console Shell"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 170,
                ParamIdForLabel = "Console Registry Read",
                FunctionName = "Console Registry Read"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 171,
                ParamIdForLabel = "Script String Functions",
                FunctionName = "Script String Functions"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 172,
                ParamIdForLabel = "SQL Get Value",
                FunctionName = "SQL Get Value"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 173,
                ParamIdForLabel = "Telnet Open Connection",
                FunctionName = "Telnet Open Connection"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 174,
                ParamIdForLabel = "Telnet Send Raw",
                FunctionName = "Telnet Send Raw"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 175,
                ParamIdForLabel = "Telnet Send Secure",
                FunctionName = "Telnet Send Secure"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 176,
                ParamIdForLabel = "Telnet Close Connection",
                FunctionName = "Telnet Close Connection"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 177,
                ParamIdForLabel = "SSH Open Connection",
                FunctionName = "SSH Open Connection"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 178,
                ParamIdForLabel = "SSH Send Raw",
                FunctionName = "SSH Send Raw"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 179,
                ParamIdForLabel = "SSH Send Secure",
                FunctionName = "SSH Send Secure"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 180,
                ParamIdForLabel = "SSH Close Connection",
                FunctionName = "SSH Close Connection"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 181,
                ParamIdForLabel = "LTServer Shell Execute",
                FunctionName = "LTServer Shell Execute"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 182,
                ParamIdForLabel = "LTServer Download to Server",
                FunctionName = "LTServer Download to Server"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 183,
                ParamIdForLabel = "LTServer Call Alert Template",
                FunctionName = "LTServer Call Alert Template"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 184,
                ParamIdForLabel = "MatchGoto",
                FunctionName = "MatchGoto"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 185,
                ParamIdForLabel = "File BITS Download",
                FunctionName = "File BITS Download"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 186,
                ParamIdForLabel = "Virus Scan",
                FunctionName = "Virus Scan"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 187,
                ParamIdForLabel = "Virus Definition Update",
                FunctionName = "Virus Definition Update"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 188,
                ParamIdForLabel = "Reboot to Safemode",
                FunctionName = "Reboot to Safemode"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 189,
                ParamIdForLabel = "Reboot to Cmd Prompt",
                FunctionName = "Reboot to Cmd Prompt"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 190,
                ParamIdForLabel = "Hibernate",
                FunctionName = "Hibernate"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 191,
                ParamIdForLabel = "Suspend",
                FunctionName = "Suspend"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 192,
                ParamIdForLabel = "Reboot forced",
                FunctionName = "Reboot forced"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 193,
                ParamIdForLabel = "LabTech Agent Uninstall",
                FunctionName = "LabTech Agent Uninstall"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 194,
                ParamIdForLabel = "LabTech Agent Update",
                FunctionName = "LabTech Agent Update"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 195,
                ParamIdForLabel = "LTServer Alert Delete",
                FunctionName = "LTServer Alert Delete"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 196,
                ParamIdForLabel = "SQL Get DataSet",
                FunctionName = "SQL Get DataSet"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 197,
                ParamIdForLabel = "SQL Fetch DataSet Row",
                FunctionName = "SQL Fetch DataSet Row"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 198,
                ParamIdForLabel = "Resend Everything",
                FunctionName = "Resend Everything"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 199,
                ParamIdForLabel = "Script Stats Get",
                FunctionName = "Script Stats Get"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 200,
                ParamIdForLabel = "File Download (Forced)",
                FunctionName = "File Download (Forced)"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 201,
                ParamIdForLabel = "File Download URL (Forced)",
                FunctionName = "File Download URL (Forced)"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 202,
                ParamIdForLabel = "Ticket Attach File",
                FunctionName = "Ticket Attach File"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 203,
                ParamIdForLabel = "Powershell Command as Admin",
                FunctionName = "Powershell Command as Admin"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 204,
                ParamIdForLabel = "Mobile Command: Mobile Device Lock",
                FunctionName = "Mobile Command: Mobile Device Lock"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 205,
                ParamIdForLabel = "Mobile Command: Mobile Device Wipe",
                FunctionName = "Mobile Command: Mobile Device Wipe"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 206,
                ParamIdForLabel = "Mobile Command: Reset Password",
                FunctionName = "Mobile Command: Reset Password"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 207,
                ParamIdForLabel = "Mobile Command: Set New Password",
                FunctionName = "Mobile Command: Set New Password"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 208,
                ParamIdForLabel = "Mobile Command: Require New Password",
                FunctionName = "Mobile Command: Require New Password"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 209,
                ParamIdForLabel = "Mobile Command: Generic Command",
                FunctionName = "Mobile Command: Generic Command"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 210,
                ParamIdForLabel = "Mobile Command: Set Roaming",
                FunctionName = "Mobile Command: Set Roaming"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 211,
                ParamIdForLabel = "LabTech Plugin Alert",
                FunctionName = "LabTech Plugin Alert"
            });

//_FuncsRequiringCOF.Add(new LabTechFunctions { FunctionId = 215, ParamIdForLabel = "Script Exit with Error", FunctionName = "Script Exit with Error" });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 217,
                ParamIdForLabel = "Template Property Get Value",
                FunctionName = "Template Property Get Value"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 218,
                ParamIdForLabel = "LabTech License Retrieve",
                FunctionName = "LabTech License Retrieve"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 219,
                ParamIdForLabel = "LabTech License Deactivate",
                FunctionName = "LabTech License Deactivate"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 223,
                ParamIdForLabel = "Plugin Server Function",
                FunctionName = "Plugin Server Function"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 224,
                ParamIdForLabel = "Plugin Agent Command",
                FunctionName = "Plugin Agent Command"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 228,
                ParamIdForLabel = "Maintenance Mode Start",
                FunctionName = "Maintenance Mode Start"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 229,
                ParamIdForLabel = "Maintenance Mode Clear",
                FunctionName = "Maintenance Mode Clear"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 230,
                ParamIdForLabel = "Email Load Attachment",
                FunctionName = "Email Load Attachment"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 231,
                ParamIdForLabel = "Generate Random Password",
                FunctionName = "Generate Random Password"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 235,
                ParamIdForLabel = "Execute Script",
                FunctionName = "Execute Script"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 240,
                ParamIdForLabel = "Check Connnectivity",
                FunctionName = "Check Connnectivity"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 241,
                ParamIdForLabel = "Get SNMP OID",
                FunctionName = "Get SNMP OID"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 242,
                ParamIdForLabel = "Set SNMP OID",
                FunctionName = "Set SNMP OID"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 243,
                ParamIdForLabel = "SSH Open",
                FunctionName = "SSH Open"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 244,
                ParamIdForLabel = "SSH Close",
                FunctionName = "SSH Close"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 245,
                ParamIdForLabel = "SSH Send Raw",
                FunctionName = "SSH Send Raw"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 246,
                ParamIdForLabel = "SSH Send Secure",
                FunctionName = "SSH Send Secure"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 247,
                ParamIdForLabel = "Telnet Open",
                FunctionName = "Telnet Open"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 248,
                ParamIdForLabel = "Telnet Close",
                FunctionName = "Telnet Close"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 249,
                ParamIdForLabel = "Telnet Send Raw",
                FunctionName = "Telnet Send Raw"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 250,
                ParamIdForLabel = "Telnet Send Secure",
                FunctionName = "Telnet Send Secure"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 251,
                ParamIdForLabel = "Script for Each Computer",
                FunctionName = "Script for Each Computer"
            });
            _FuncsRequiringCOF.Add(new LabTechFunctions
            {
                FunctionId = 252,
                ParamIdForLabel = "Ticket Create for Network Devices",
                FunctionName = "Ticket Create for Network Devices"
            });

            #endregion

            #region Functions Requiring Cache Refresh

           /* _FuncsNeedingResend.Add(
                new FuncNeedingCacheRefresh
                {
                    FunctionId =,
                    FunctionName = '',
                    ResendFunctionID = 45,
                    ResendFuncName = 'Resend Error Logs'
                };
            
            _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionId = ,
                FunctionName = '' ResendFunctionID = 49,
                ResendFuncName = 'Resend EventLogs'
            };
            * */
          /*  _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionId = ,
                FunctionName = '' ResendFunctionID = 50,
                ResendFuncName = 'Resend Hardware'
            };*/

            _FuncsNeedingResend.Clear();
            _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID = 110,
                FunctionName = "IF Process Exists",
                ResendFunctionID = 51,
                ResendFuncName = "Resend Process List"
            });

            _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID = 122,
                FunctionName = "IF AutoStartup Check",
                ResendFunctionID = 52,
                ResendFuncName = "Resend Autostartup List"
            });
            _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID = 119,
                FunctionName = "IF Drive Status",
                ResendFunctionID = 53,
                ResendFuncName = "Resend Drive Info"
            });
            _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID =120,
                FunctionName = "IF Software Installed", ResendFunctionID = 54,
                ResendFuncName = "Resend Software"
            });
           _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID = 118,
                FunctionName = "IF Smart Attributes", 
            ResendFunctionID = 53,
                ResendFuncName = "Resend Drive Info"
            });
            _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID = 113,
                FunctionName = "IF Service is Running",
                ResendFunctionID = 56,
                ResendFuncName = "Resend Service List"
            });
           /* _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID =,
                FunctionName = "", ResendFunctionID = 57,
                ResendFuncName = "Resend Printers"
            });*/
           /* _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID =,
                FunctionName = "", ResendFunctionID = 148,
                ResendFuncName = "Resend System Information"
            });
            * */
/*            _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID =,
                FunctionName = "", ResendFunctionID = 149,
                ResendFuncName = "Resend Network information"
            });*/
            _FuncsNeedingResend.Add(new FuncNeedingCacheRefresh
            {
                FunctionID = 121,
                FunctionName = "IF Patch Installed", ResendFunctionID = 152,
                ResendFuncName = "Resend Patch information"
            });
       

#endregion
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var frmRawScript = new viewRawScriptXML(_CurrentScriptXml);
            frmRawScript.Show();
        }

        public void UpdateCOFTextBox(string txt)
        {
            txtBoxMissingCof.Text = txt;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (rTxtBoxResults.TextLength > 0)
            {
                _originalData = rTxtBoxResults.Text;

                foreach (var line in rTxtBoxResults.Lines)
                {
                    if (line.Contains("SUCCESS"))
                    {
                    }
                }
            }
        }

        public class LabTechFunctions
        {
            public int FunctionId { get; set; }
            public string ParamIdForLabel { get; set; }
            public string FunctionName { get; set; }
        }

        public class FuncNeedingCacheRefresh
        {
            public int FunctionID { get; set; }
            public string FunctionName { get; set; }
            public string ResendFuncName { get; set; }
            public int ResendFunctionID { get; set; }
        }
        
        public class ScriptStepRec
        {
            public string action;
            public string functionId;
            public string indentation;
            public string osLimit;
            public string param1;
            public string param2;
            public string param3;
            public string param4;
            public string param5;
            public string sort;
            public string stepContinue;
        }

        public struct LabelEntry
        {
            private int lineNumber { get; set; }
            private string itemName { get; set; }
            private ScriptNoteFlags noteitemFlag { get; set; }
        }

        public class ScriptObject
        {
            private int TotalCount { get; set; }
            private List<ScriptStepRec> scriptSteps { get; set; }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBox1.Checked && rTxtBoxResults.TextLength > 0)
            {
                
                _originalReportOutput = rTxtBoxResults.Text;
                string pattern = "(?:)(\\[.*SUCCESS].*\\n)";

                /* Original regex that didn't remove heading before ===== of next section
                string patternForRmvHeadings = "(-+\\n(?:Analyzing|Identifying)\\s.+\\n-+\\n{3,})(?:-|\\n{0,}$)";
                */
                string patternForRmvHeadings = "(-+\\n(?:Analyzing|Identifying)\\s.+\\n-+\\n{3,})(?:-|\\n{0,})";
                string patternUnusedLabels = "(=+\\n=\\n.*\\n=.*\\n=.*\\n=.*\\n=.*\\n=+\\n-+\\n.*\\n+-+[\\w\\d\n]+.*\\n{0,})(?:=)";
                // store contents in variable to make modifications to. Should prevent gui tweaking
                string newContents = rTxtBoxResults.Text;

                foreach (var line in rTxtBoxResults.Lines)
                {

                    if (line != null && line.IndexOf("SUCCESS", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Found it!
                        //newContents = newContents.Replace(line + Environment.NewLine, "");
                        Regex rgx = new Regex(pattern);
                        newContents = rgx.Replace(newContents, "");
                    }

                }

                Regex rmHeadingRgx = new Regex(patternForRmvHeadings);
                Regex rmUnused = new Regex(patternUnusedLabels);

                newContents = rmHeadingRgx.Replace(newContents, "");
                newContents = rmUnused.Replace(newContents, "");

                rTxtBoxResults.Text = newContents;

            }
            else
            {
                if (_originalReportOutput.Length > 0)
                {
                    rTxtBoxResults.Text = _originalReportOutput;
                }
            }
                    

         }

        private void button2_Click_1(object sender, EventArgs e)
        {
            viewSuggestions vwSuggestions = new viewSuggestions();
            vwSuggestions.Show();
        }
       }
}


/*
<ScriptData>
  <Scripts>
    <ExtraDataFields />
    <Parameters />
    <Globals />
    <ScriptVersion>1298993736</ScriptVersion>
    <ScriptGuid>aa01fb3b-0603-11df-a9df-d4a2ae817be5</ScriptGuid>
  </Scripts>
  <ScriptSteps>
    <Action>1</Action>
    <FunctionId>1</FunctionId>
    <Param1 />
    <Param2 />
    <Param3 />
    <Param4 />
    <Param5 />
    <Sort>0</Sort>
    <Continue>0</Continue>
    <OsLimit>0</OsLimit>
    <Indentation>0</Indentation>
  </ScriptSteps>
*/