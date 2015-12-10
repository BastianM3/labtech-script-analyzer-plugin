
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using LabTech.Interfaces;

namespace ScriptAnalyzer.ToolBar
{
	public class PluginInfo : IPlugin
	{
		public string About
		{
			get
			{
				return "Performs a static analysis of your LabTech scripts in order to identify problems before a script is ran against production agents.";
			}
		}

		public string Author
		{
			get
			{
				return "Marcus Bastian";
			}
		}

		public string Filename
		{
			get
			{
				return "LTScriptProblemAnalyzer.dll";
			}
			set
			{
				//do nothing
			}
		}

		public string hMD5 {get; set;}

		public bool Install(IControlCenter host)
		{
			return true;
		}

		public bool IsCompatible(IControlCenter host)
		{
			return true;
		}

		public bool IsLicensed()
		{
			return true;
		}

		public bool IsLicensed(IControlCenter host)
		{
			return true;
		}

		public string Name
		{
			get
			{
				return "LabTech Script Analyzer";
			}
		}

		public bool Remove(IControlCenter host)
		{
			return true;
		}

		public int Version
		{
			get
			{
				return 1;
			}
		}
	}
}