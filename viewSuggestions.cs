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
    public partial class viewSuggestions : Form
    {
        public viewSuggestions()
        {
            InitializeComponent();
            PopulateSuggestionsBox(richTextBoxSuggestions);
        }

        public void PopulateSuggestionsBox(RichTextBox rtb)
        {

            rtb.Rtf = @"{\rtf1\ansi 
\b Note 1: \b0  Script labels specified by IF/Goto functions must exist. Be careful of typos :)
\line \line
We must make sure that the script label specified in the 'label to jump to' parameter of a step leveraging an IF function exists. 
If the label specified doesn't exist, or the value is 0 or blank, the script will exit on the spot. This is undesirable since each script should ideally exit gracefully with a preceding script log message indicating why it failed.
\line\line\b
Example scenario: \b0
\line\line
line  3   IF @Ticket#@ Exists goto :TicketExists\line
line  9          :TicketExist      <----- Typos happen!\line
\line
Since this label doesn't exist, the script will exit immediately on line 3\line\line
\line



\b Note 2: \b0 Script goto line number (skip x # of steps)
\line\line
When specifying a numerical value in the 'label to jump to or \b steps to skip \b0' parameter, this can lead to issues when modifying the script later on. 
If a script step is added between the originating IF/Variable Check step and the target jump TO line (per # of steps to skip specified), the script will no longer jump to the intended line. See below for an example.
\line\line\b
Example scenario: \b0 \line \line A ""Send Email"" script step added between Script Log and Script Exit with Error causing unintended exiting of script \line
\line
line 35    :BuildInstaller\line
line 36        Shell as Admin Command - Builds MST file for Acronis\line
line 37     IF FILE EXISTS %windir%\ltsvc\acronis\AcronisAgentWindows.MST   Jump to line 40    (intended to go to :EndbuildInstaller)\line
line 38        LOG:  Failed to create transformation file (acronisAgentWindows.mst). Script exiting!\line
line 39        Send Email to me@myMSP.com  (Install failed!) \b    < ---- After adding this line, JUMP goes to EXIT instead of continuing\b0 \line
line 40      \bScript Exit with Error \b0 < ---- New target line to skip to \line
line 41     :EndBuildInstaller\line
\line\line

\b Note 3: \b0 Script functions requiring ""Continue on Failure""
\line\line
While a LabTech script is pushing commands down to the remote computer, there are certain conditions which can cause the script step to ""fail"". For example, if the service account the LabTech service is running under doesn't have write-access to a particular directory, the command will fail. 
The script engine will find out about this failure, and will look to the step's ""Continue on Failure"" checkbox value to determine how it should proceed. If unchecked, the script will exit immediately on the script step from which the failing command originated. 
Conversely, if continue on failure IS checked, the script will be allowed to continue. This in turn will allow for you to identify, capture and report on the failure. 
\line
\line
\b
Example scenario:\b0 \line \line
File Download (URL) script step lacks continue on failure checkbox, resulting in unintended exiting of script
\line 
\line
line 6   Variable Set \line
                    Variable Name = @SaveHere@ \line
                    Variable Type = Constant \line
                    Value = %windir%\\temp\\installer.msi \line
line 7   File Download (URL) \line
                     Param 1 - Remote URL       =  http://s3.amazon.com/myInstaller.msi \line
                     Param 2 - Destination Path = @SaveHere@ \line
line 8   IF File Not Exists @SaveHere@ Goto  :FailedToDownload \line
\line
          If Local System doesn't have write access to the destination path's directory for example, this command could fail. In this case, the script will exit immediately on line 7. \line
          Ideally, we would want to verify the results of the download on our own, and create a ticket / notify ourselves that there was an issue downloading the file. \line



}";
        }


    }


}
