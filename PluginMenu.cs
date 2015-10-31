
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LabTech.Interfaces;

namespace ScriptAnalyzer.ToolBar
{
	public class PluginMenu : IMenu2
	{
		private IControlCenter controlCenterHost;
		private Form exampleInstance = null;

		public System.Windows.Forms.ToolStripItem[] CreateMainToolStrip()
		{
			// Adding to buttons to our collection, these will be populated when the control center loads
			try
			{
				return new[]
				{
					new ToolStripButton("LabTech Script Analyzer", Properties.Resources.ScriptAnalyzerIcon, ShowForm) {ImageScaling = ToolStripItemImageScaling.None}
					// new ToolStripButton("Example - Toolbar Buttons - Close Control Center", Properties.Resources.LabTechART, ShutDown) {ImageScaling = ToolStripItemImageScaling.None}
				};
			}
			catch (Exception ex)
			{
				controlCenterHost.LogMessage(string.Format("Plugin {0} was unable to CreateMainToolStrip {1}.", Name, ex.Message));
				return null;
			}
		}

		public void ShowForm(object sender, EventArgs e)
		{
			// Just opening a form which has an need to save checkbox
			if (controlCenterHost == null)
			{
				MessageBox.Show("Plugin load error", "Please close and reopen the Control Center, then reload the plugin.");
				return;
			}
            
			if (exampleInstance != null && !exampleInstance.IsDisposed)
			{
				exampleInstance.BringToFront();
			}
			else
			{
                exampleInstance = new mainForm(controlCenterHost);
				exampleInstance.Text = Name;
				exampleInstance.Show();
			}
             
		}

		private void ShutDown(object sender, EventArgs e)
		{
			// We will pull the value and check if we need to make saves before closing
			LabTech.Interfaces.ILabTechWindowInformation f = (LabTech.Interfaces.ILabTechWindowInformation)exampleInstance;

			if (f.AttentionRequired)
			{
				MessageBox.Show("There is unsaved information in the Toolbar Button Information Window.");
			}
			else
			{
				Application.Exit();
			}

		}

		public void Decommision()
		{
			controlCenterHost = null;
		}

		public void Initialize(LabTech.Interfaces.IControlCenter host)
		{
			try
			{
				controlCenterHost = host;
			}
			catch (Exception ex)
			{
				controlCenterHost.LogMessage(string.Format("Plugin {0} was unable to initialize {1}.", Name, ex.Message));
			}
		}

		public string Name
		{
			get
			{
				return "LabTech Script Analyzer";
			}
		}

#region Unused Items
		public System.Windows.Forms.MenuItem[] CreateMainMenu()
		{
			return null;
		}

		public System.Windows.Forms.MenuItem[] CreateToolsMenu()
		{
			return null;
		}

		public System.Windows.Forms.MenuItem[] CreateViewMenu()
		{
			return null;
		}
#endregion

	}

}