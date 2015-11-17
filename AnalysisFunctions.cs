using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Windows.Forms;
using LabTech.Interfaces;

namespace ScriptAnalyzer.ToolBar
{
    public class AnalysisFunctions
    {
        private static List<AnalysisFunctions.ScriptNoteLblFound> labelsUsedByIFs = new List<AnalysisFunctions.ScriptNoteLblFound>();
        private static IControlCenter _host;

        public static Converter<string, int> Converter = str =>
        {
            int data;
            if (!Int32.TryParse(str, out data))
            {
                // custom business logic for such cases
                data = Int32.MinValue;
            }

            return data;
        };
       

        public static List<ScriptNoteLblFound> AnalyzeScriptNotes(List<AnalysisForm.ScriptStepRec> scriptnoteRecs,
            RichTextBox rTxtBox)
        {
            var parsedLabelsFound = new List<ScriptNoteLblFound>();

            foreach (var entry in scriptnoteRecs)
            {
                // Does it start with :, indicating it's a script note
                //var es = scriptdata.Where(e => int.Parse(e.functionId) == 139).ToString();

                byte flag = 0;
                var stepsToSkip = 0;
                Enum resultEnum = null;

                // prep work
                int lineNumber = 0;
                bool isInt = false;
                Int32.TryParse(entry.sort, out lineNumber);

                if (isInt = false)
                    lineNumber = 9999;
                else
                {
                    lineNumber = lineNumber + 1;
                } 


                var currentP1 = entry.param1;
                var currentP2 = entry.param2;
                var currentP3 = entry.param3;
                var currentP4 = entry.param4;
                var currentP5 = entry.param5;

                var currentFid = Int32.Parse(entry.functionId);
                var isNumber = Int32.TryParse(entry.param1, out stepsToSkip);

                //////////////////////////////////////
                // logic for script notes           //
                //////////////////////////////////////

                if (currentP1.Length > 2 && !currentP1.StartsWith(":") || currentP1.Equals(""))
                {
                    resultEnum = AnalysisForm.ScriptNoteFlags.NotesOnly;
                    //rTxtBoxResults.Text += String.Format("[Line: {0}]\t[NOTES ONLY]  -  Value:\t{1} \n", entry.sort.ToString(), currentP1);
                    LogResults(lineNumber.ToString(), currentP1, resultEnum, rTxtBox);
                }
                else if (currentP1.StartsWith(":"))
                {
                    resultEnum = AnalysisForm.ScriptNoteFlags.GotoLabel;
                    //rTxtBoxResults.Text += String.Format("[Line: {0}]\t[VALID LABEL]  -  Value:\t{1} \n", entry.sort.ToString(), currentP1);
                    LogResults(lineNumber.ToString(), currentP1, resultEnum, rTxtBox);
                }


                var currentItem = new ScriptNoteLblFound(lineNumber, entry.param1, resultEnum);

                parsedLabelsFound.Add(currentItem);
            }


            return parsedLabelsFound;
        }

        public static void AnalyzeIfFunctions(List<AnalysisForm.ScriptStepRec> allScriptSteps, RichTextBox rTxtBox,
            List<ScriptNoteLblFound> labelsFound)
        {
            rTxtBox.Text += string.Format(
"================================================================================================{0}" +
"={0}"+
"=  Beginning Analysis of IFs/Gotos - Verifying that the script label in every IF/Goto function's \"script note\" parameter exists{0}"+
"={0} \t\t See notes 1 & 2 - Click the \"View Suggested Practices\" button for more info {0}" +
//"=     i.e.)  line 3   IF @Ticket#@ Exists goto :TicketExists{0}"+
//"=             line  9          :TicketExist      <----- Typos happen!{0}"+
//"=                                     Since this label doesn't exist, the script will exit immediately on line 3{0}"+
//"={0}"+
"================================================================================================",Environment.NewLine);

            if (allScriptSteps == null)
            {
                rTxtBox.Text += "No problems detected!" + Environment.NewLine;
                return;
            }


            // FOR EACH IF FUNCTION ...
            foreach (var item in AnalysisForm._GotoFunctionList.ToList())
            {
                
                var funcId = item.FunctionId;
                var paramName = item.ParamIdForLabel;
                var funcName = item.FunctionName;

                var currentItems = new List<AnalysisForm.ScriptStepRec>();

                switch (paramName)
                {
                    case "Param1":
                    {
                        currentItems =
                            allScriptSteps.Where(o => o != null)
                                            .Where(o=>o.osLimit != "-2147483648")
                                            .Where(x => Int32.Parse(x.functionId) == funcId && Converter(x.action) != 1)
                                             .Select(z => new AnalysisForm.ScriptStepRec { param1 = z.param1, sort = z.sort, functionId = z.functionId }).ToList();
                        break;
                    }
                    case "Param2":
                    {
                        currentItems =
                           allScriptSteps.Where(o => o != null)
                                            .Where(o => o.osLimit != "-2147483648")
                                            .Where(x => Int32.Parse(x.functionId) == funcId && Converter(x.action) != 1)
                                            .Select(z => new AnalysisForm.ScriptStepRec { param1 = z.param2, sort = z.sort, functionId = z.functionId }).ToList();
                        break;
                    }
                    case "Param3":
                    {
                        currentItems =
                                  allScriptSteps.Where(o => o != null)
                                                .Where(o => o.osLimit != "-2147483648")
                                                .Where(x => Int32.Parse(x.functionId) == funcId && Converter(x.action) != 1)
                                                .Select(z => new AnalysisForm.ScriptStepRec { param1 = z.param3, sort = z.sort, functionId = z.functionId}).ToList();
                        break;
                    }
                    case "Param4":
                    {
                        currentItems =
                           allScriptSteps.Where(o => o != null)
                                            .Where(o => o.osLimit != "-2147483648")
                                            .Where(x => Int32.Parse(x.functionId) == funcId && Converter(x.action) != 1)
                                            .Select(z => new AnalysisForm.ScriptStepRec { param1 = z.param4, sort = z.sort, functionId = z.functionId }).ToList();
                        break;
                    }
                }
               
                // don't iterate unless we identified steps of this function
                var relativeStepsFound = currentItems.Any();

                if (relativeStepsFound == false)
                {
                    continue;
                }


                rTxtBox.Text += String.Format(
                    "{0}----------------------------------------------------------------------------------------------------------------------------------------------------{0}" +
                    "Analyzing detected \"{1}\" script steps ....\t Number of steps found:  {2}"+
                    "{0}----------------------------------------------------------------------------------------------------------------------------------------------------{0}{0}", 
                        Environment.NewLine,
                        funcName, 
                        currentItems.Count);
                
                currentItems.Sort((a,x) => int.Parse(a.sort).CompareTo(int.Parse(x.sort)));

                foreach (var specifiedJump in currentItems)
                {
                    
                    if (specifiedJump == null)
                    {
                        continue;
                    }
                    
                    // prep work for line number
                    int lineNumber = 9999;
                    bool isLineNumInt = false;
                    Int32.TryParse(specifiedJump.sort, out lineNumber);
                    
                    // Best-practice - should NOT be skipping lines by #
                    int linesToSkip = -9999;
                    bool isGotoSkip = false;
                    specifiedJump.param1 = specifiedJump.param1.TrimStart('!');
                    isGotoSkip = int.TryParse(specifiedJump.param1, out linesToSkip);

                    if (isLineNumInt = true)
                    {
                        lineNumber = lineNumber + 1;
                    }

                    // just using param1 in this object for simplicity. May be 4, 3 2 or 1...

                    var logmsg = String.Format("[Line {1}] \tJump Label Specified: {0}", specifiedJump.param1, lineNumber).PadRight(70, '.');
                            rTxtBox.Text += logmsg;
                    
                    
                    // identify matched labels
                    var matchedObjects =
                        labelsFound.Where(
                            o =>
                                o.ItemFlag.Equals(AnalysisForm.ScriptNoteFlags.GotoLabel) && string.Equals(o.ItemName, specifiedJump.param1.Replace("!:",":"), StringComparison.OrdinalIgnoreCase) )
                            .ToList();

                    var foundMatchingLabel = matchedObjects.Any();

                    if (foundMatchingLabel)
                    {

                        rTxtBox.Text += String.Format("\t[SUCCESS] Found label: {0}{1}",
                            matchedObjects[0].ItemName, Environment.NewLine);


                        // Ok, now that we've joined two labels together, we need to add this label name
                        // to the list of accounted for labels.
                        var labelUsed = new ScriptNoteLblFound(lineNumber, specifiedJump.param1, AnalysisForm.ScriptNoteFlags.FoundRecord);
                        labelsUsedByIFs.Add(labelUsed);
                      

                    }
                    else if ( String.IsNullOrEmpty(specifiedJump.param1) || specifiedJump.param1 == "0" ) 
                    {
                        if (specifiedJump.functionId == "129")
                        {
                            // It's a GOTO ... this is ok
                            rTxtBox.Text += String.Format("\t[NOTE] - Exit Specified (Exits immediately){0}", Environment.NewLine);
                        }
                        else
                        {
                            rTxtBox.Text += String.Format("\t[FAIL] - Exit Specified. See note 4 on suggestions tab.{0}", Environment.NewLine,specifiedJump.functionId);
                        }
                    }
                    else if (isGotoSkip == true && linesToSkip != 0)
                    {
                        rTxtBox.Text += String.Format("\t[FAIL] - Skip {1} lines detected (bad practice)! {0}", Environment.NewLine, linesToSkip.ToString());
                    }
                    else
                    {
                        rTxtBox.Text += String.Format("\t[FAIL]  Did not find matching label: {0}{1}",
                            specifiedJump.param1, Environment.NewLine);
                        AnalysisForm.AddOneMissingLabel();
                    }
                }
            }

            rTxtBox.Text += Environment.NewLine;
        }

        public static int IdentifyUnusedLabels(List<ScriptNoteLblFound> fullLabelList, RichTextBox txtBoxResults)
        {
            List<string> notesUsedInScript = new List<string>();
            List<string> notesFoundInScript = new List<string>();
            string detachedLabelMsg = "";

            
             notesFoundInScript = (from i in fullLabelList
                                where i != null
                                where i.ItemFlag.Equals(AnalysisForm.ScriptNoteFlags.GotoLabel)
                                select i.ItemName).ToList();

            
            notesUsedInScript = labelsUsedByIFs.Where(x=>x != null).Select(e => e.ItemName).ToList();
            
            // find intesect
            int numDetached = 0;
            List<string> detachedLabels = new List<string>();

            try
            {
                detachedLabels = notesFoundInScript.Except(notesUsedInScript, StringComparer.OrdinalIgnoreCase).ToList();
                numDetached = detachedLabels.Count;
            }
            catch (Exception ex)
            {
                _host.LogMessage("Script analysis error occurred. " + ex.ToString());
            }
            finally
            {

               
            }
            
             // prevent null ref exception
            if(numDetached > 0)
            {
                txtBoxResults.Text += String.Format("{0}----------------------------------------------------------------------------------------------------------------------------------------------------{0}", Environment.NewLine);
                txtBoxResults.Text += String.Format("Identifying unused script note labels ( example = :FileExists ) ....\t Number of steps found:  {0}", numDetached);
                txtBoxResults.Text += String.Format("{0}----------------------------------------------------------------------------------------------------------------------------------------------------{0}{0}", Environment.NewLine);

                foreach (var lonelyLabel in detachedLabels)
                {
                    // get line number
                    var thisLbl = lonelyLabel;
                    var lineNum = fullLabelList
                                    .Where(x=> x!= null)
                                    .Where(x => x.ItemName == thisLbl).Select(e => e.LineNumber).FirstOrDefault();

                    txtBoxResults.Text += string.Format("[Line {0}]\tUnused script note label detected:  {1}{2}", lineNum, lonelyLabel, Environment.NewLine);
                }
            }
            
            // if we found any recs in ALL script labels that aren't found in USED list

            return numDetached;
        }
      

        public static int IdentifyNonCof(List<AnalysisForm.ScriptStepRec> allScriptSteps, RichTextBox rTxtBox)
        {
            int linesMissingCof = 0;
            bool headerPosted = false;

            rTxtBox.Text += string.Format(
                "{0}================================================================================================{0}" +
                "= {0}" +
                "= Beginning Analysis of Functions - Verifying that every script function that can fail has the \"Continue on Failure\" box checked{0}" +
                "={0} \t \t See note 3 - Click the \"View Suggested Practices\" button for more info {0}" +
             //   "=     i.e.)  line 7   File Download (URL){0}" +
              //  "=                     Param 1 - Remote URL  {0}" +
              //  "=                     Param 2 - Destination Path  {0}" +
              //  "=                 If Local System doesn't have write access to the drive location for example, this command will fail.{0}" +
              //  "=                 If the command fails and the \"continue on failure\" checkbox is unchecked, the script will exit immediately on line 7.{0}" +
               // "={0}" +
                "================================================================================================{0}",
                Environment.NewLine);

            if (!allScriptSteps.Any())
            {
                rTxtBox.Text += "No script steps passed in for verification. Is this script blank?";
                return linesMissingCof;
            }


           var linesToCheck = allScriptSteps.Where(x => x != null)
                .Where(x => (x.osLimit != "-2147483648" && x.stepContinue != "1" && x.functionId != "1"))
                .OrderBy(x => x.sort)
                .ToList();

            foreach (var x in linesToCheck)
            {
                var allCofSteps = AnalysisForm._FuncsRequiringCOF.Select(x2 => x2.FunctionId).ToList();

                if(allCofSteps.Contains(int.Parse(x.functionId)))
                    linesMissingCof += 1;

            }
            


           rTxtBox.Text += 
                 string.Format(
                        "----------------------------------------------------------------------------------------------------------------------------------------------------{0}" +
                        "Number of steps missing COF in this script:  {1} {0}" +
                        "----------------------------------------------------------------------------------------------------------------------------------------------------{0}" +
                        "{0}", Environment.NewLine, linesMissingCof);
            
            try
            {         
                if (linesMissingCof == 0)
                {
                    rTxtBox.Text += "No problems detected!" + Environment.NewLine;
                    return linesMissingCof;
                }

            }
            catch (Exception ex)
            {
                rTxtBox.Text += "Source: " + ex.Source.ToString() + "Message:" + ex.Message.ToString();
                return linesMissingCof;
            }

            foreach (var item in AnalysisForm._FuncsRequiringCOF.ToList())
            {

                // declare variables for the current function ID from list we want to verify COF for.
                var funcId = item.FunctionId;
                var paramName = item.ParamIdForLabel;
                var funcName = item.FunctionName;
                bool headerposted = false;
                
                var stepsOfCurrentFunction = new List<AnalysisForm.ScriptStepRec>();

                stepsOfCurrentFunction = allScriptSteps.Where(x=> x!= null)
                                            .Where(x => int.Parse(x.functionId) == funcId && x.osLimit != "-2147483648" && x.stepContinue != "1").OrderBy(x=>x.sort)
                                             .Select(z => new AnalysisForm.ScriptStepRec { param1 = z.param1, sort = z.sort, stepContinue  = z.stepContinue}).ToList();

              
           
                if (stepsOfCurrentFunction.Any())
                {
                    // Records without COF detected for the current function.
                    foreach (var cofStep in stepsOfCurrentFunction)
                    {

                        int lineNum = int.Parse(cofStep.sort) + 1;

                        try
                        {
                            var sb = new StringBuilder();
                            sb.Append(string.Format("[Line {0}]\t Found",lineNum));
                            sb.Append(string.Format("\t \"{0}\"  missing 'continue on failure'!  {1}", funcName, Environment.NewLine));
                            rTxtBox.Text += sb.ToString();
                        }
                        catch (Exception ex)
                        {
                            rTxtBox.Text += "Source: " + ex.Source.ToString() + "Message:" + ex.Message.ToString();
                            throw;
                        }
                      
                    }

                }
                else
                {
                    //rTxtBox.Text += "Sucess -No  steps missing continue on failure for this function";
                    continue;
                }

                
            }


            return linesMissingCof;

        }


        public static void LogResults(string lineNum, string paramName, Enum result, RichTextBox rtb)
        {
            var comment = "N/A";

            switch ((AnalysisForm.ScriptNoteFlags) result)
            {
                case AnalysisForm.ScriptNoteFlags.GotoLabel:
                    comment = "[VALID GOTO LABEL]";
                    break;
                case AnalysisForm.ScriptNoteFlags.NotesOnly:
                    comment = "[NOTES ONLY]";
                    break;
            }

//rtb.Text += string.Format("[Line: {0}] - {1}\t{2}{3}", lineNum, paramName, comment,Environment.NewLine);
        }

        public class txtBoxMsgForSorting
        {
            public int lineNum { get; set; }
            public string result { get; set; }
            public string firstPortion { get; set; }
        }
        public static void FindMissingResends(List<AnalysisForm.ScriptStepRec> allScriptSteps, RichTextBox rTxtBox)
        {
            bool noRecordsProcessed = true;
            List<txtBoxMsgForSorting> outputLineObject = new List<txtBoxMsgForSorting>();
            rTxtBox.Text += string.Format(
"{0}================================================================================================{0}" +
"={0}" +
"=  Beginning Analysis of IFs that use cached data - Verifying that a resend script step is found prior to using an IF-based function{0}" +
"={0} \t\t See note 5 - Click the \"View Suggested Practices\" button for details {0}" +
"================================================================================================{0}{0}", Environment.NewLine);

            if (!allScriptSteps.Any())
            {
                rTxtBox.Text += "No script steps passed in for verification. Is this script blank?";
                return;
            }

            var linesToCheck = allScriptSteps
                    .Where(x => x != null)
                    .Where(x => (x.osLimit != "-2147483648" && x.functionId != "1"))
                     .OrderBy(x => x.sort)
                    .ToList();

            if (!linesToCheck.Any())
            {
                rTxtBox.Text += "No problems detected!" + Environment.NewLine;
                return;
            }
            else
            {
                //rTxtBox.Text += "Found sometthing ....";

            }

            // Just a place holder
            foreach (var functionToSeek in AnalysisForm._FuncsNeedingResend.ToList())
            {
             
                
                var seekFunctionId = functionToSeek.FunctionID;
                var seekFunctionName = functionToSeek.FunctionName;
                var resendFuncName = functionToSeek.ResendFuncName;
                var resendFuncId= functionToSeek.ResendFunctionID;
                int lineNumOfIf = 0;
                
                int cursorSortLastIf = 0;

                // can I find any rows in list that match this function?
                var listMatchesSeeked =
                    allScriptSteps
                    .Where(x => x != null)
                    .Where(x => x.osLimit != "-2147483648" && x.functionId == seekFunctionId.ToString())
                    .OrderBy(x=>x.sort)
                    .ToList();

                if (!listMatchesSeeked.Any())
                {
                    // didn't find anything
                    continue;
                }
                else
                {
                    
                    
                    foreach (var lineForThisFunction in listMatchesSeeked.OrderBy(x => int.Parse(x.sort) ))
                    {
                        lineNumOfIf = int.Parse(lineForThisFunction.sort) + 1;
                        
                        // get count of rows where line # > current line. 
                        var foundResends = allScriptSteps
                            .Where(x=>x!=null)
                            .Where(x => x.osLimit != "-2147483648" 
                                    && x.functionId == resendFuncId.ToString()
                                    && int.Parse(x.sort)+1 < lineNumOfIf 
                                    && int.Parse(x.sort)+1 > cursorSortLastIf)
                                    .OrderBy(x=> x.sort )
                            .ToList();

                        
                        var lineStart = String.Format("[Line {1}] \t Cached data leveraging function \"{0}\":", seekFunctionName, lineNumOfIf).PadRight(70, '.');
                        //rTxtBox.Text += logmsg;

                        var logmsg = "";

                        if (foundResends.Any())
                        {
                             // found one, yay!
                             int resendMatchLineNum = int.Parse(foundResends.OrderBy(x => int.Parse(x.sort) + 1).LastOrDefault().sort) + 1;
                             logmsg = String.Format("\t[SUCCESS] Required \"{0}\" found on line {2} {1}", resendFuncName, Environment.NewLine, resendMatchLineNum);
                            

                        }
                        else
                        {
                            // ut-oh ... didn't find a matching resend event!
                           logmsg = String.Format("\t[FAIL] - \"{1}\" required before this step to pull fresh data!{0}", Environment.NewLine, resendFuncName);
                           noRecordsProcessed = false;
                        }

                        outputLineObject.Add(new txtBoxMsgForSorting { lineNum = lineNumOfIf, firstPortion = lineStart, result = logmsg });
                        cursorSortLastIf = lineNumOfIf;
                    }


                   // var listFoundResend = allScriptSteps.Where((x=> x !=null))
                     //   .Where(x=> x.functionId == )
                    // If so, is there at least one matching resend?

                }

            }


            outputLineObject = outputLineObject.Where(x => x != null).OrderBy(o => o.lineNum).ToList();

            foreach (var lineToWriteToBox in outputLineObject)
            {
                rTxtBox.Text += lineToWriteToBox.firstPortion + lineToWriteToBox.result;
            }

            if (noRecordsProcessed)
            {
                rTxtBox.Text += "No problems detected!" + Environment.NewLine;
            }

        }





        public class ScriptNoteLblFound
        {
            public ScriptNoteLblFound(int lineNumber, string itemName, Enum itemFlag)
            {
                LineNumber = lineNumber;
                ItemName = itemName;
                ItemFlag = itemFlag;
            }
            

            public int LineNumber { get; set; }
            public string ItemName { get; set; }
            public Enum ItemFlag { get; set; }
        }
    }
}