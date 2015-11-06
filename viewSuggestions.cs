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

\b Note 2: \b0 Specifying a number of steps to skip instead of a script note label can cause problems down the road

\line\line
When specifying a numerical value in the 'label to jump to or \b steps to skip \b0' parameter, this can lead to issues when modifying the script later on. 
If a script step is added between the originating IF/Variable Check step and the target jump TO line (per # of steps to skip specified), the script will no longer jump to the intended line. See below for an example.
\line\line\b
Example scenario: \b0 \line \line A ""Send Email"" script step added between Script Log and Script Exit with Error causing unintended exiting of script \line
\line
line 35    :BuildInstaller\line
line 36        Shell as Admin Command - Builds MST file for Acronis\line
line 37     IF FILE EXISTS %windir%\ltsvc\acronis\AcronisAgentWindows.MST   Jump to line 40    (intended to go to :EndBuildInstaller)\line
line 38        LOG:  Failed to create transformation file (acronisAgentWindows.mst). Script exiting!\line
line 39        Send Email to me@myMSP.com  (Install failed!) \b    < ---- After adding this line, JUMP goes to EXIT instead of continuing\b0 \line
line 40      \bScript  Exit with Error \b0 < ---- New target line to skip to \line
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
          If Local System doesn't have write-access to the destination path's directory, for example, this command could fail. In this case, the script will exit immediately on line 7. \line
          Ideally we would want to verify the results of the download on our own. If the download fails, we then create a ticket or in some way notify ourselves that there was an issue downloading the file. \line
\line\line

\b Note 4: \b0 Jumping to labels instead of exiting via IF functions
\line \line
When trying to exit the script from an IF function, such as ""IF File Check"" or ""IF Software Installed"" -- with the exception of 'script goto,' it is generally better to jump to a label instead of exiting immediately. This is because when you're exiting your script due to some reason, it is helpful and important to write out a script log message indicating WHY the script failed. If you're looking at a computer's script history and you notice your script is exiting without any output/messaging, you may be wondering ""what happened....?"".
\line\line
\line
Additionally, any colleagues that see the script exit without an apparent reason may need to open the script and compare the final script log message to figure out why it failed. What if the script failed in a subscript? You'd then have to track through the final script log message to determine what line of what subscript it exited on. 
The philosophy here is that someone should be able to get an idea as to why the script failed just by looking at the script history of the respective agent. Looking through scripts takes time, and time is a hot commodity! 

\line\line 
For example, if a file failed to download, have your script jump to a script label called "":FailedToDownloadMSI"" and use a script log message to tell why the script failed.

Thorough script logging definitely characterizes a ""good script.""
\line \line
In some of my recent scripts that are heavily utilized, i'm using a bullet point list that details some possible causes of the failure. This gives them a good place to start, and most of the time they're able to figure things out without needing to escalate the issue.


I hope that this plugin makes LabTech scripting easier for you, and will reduce the time required to develop/test your LabTech scripts. I have created this plugin to allow for faster and more accurate QA'ing of scripts. I have seen my scripts and those of others fail many times due to typos in script note labels or labels that needed to be renamed. Happy LabTech Scripting!
}";
        }


    }


}
