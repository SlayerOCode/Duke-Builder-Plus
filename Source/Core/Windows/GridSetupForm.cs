
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Drawing;
using System.Windows.Forms;
using mxd.DukeBuilder.Data;

#endregion

namespace mxd.DukeBuilder.Windows
{
	internal partial class GridSetupForm : DelayedForm
	{
		// Variables
		private string backgroundname;
		
		// Constructor
		public GridSetupForm()
		{
			// Initialize
			InitializeComponent();

			// Show grid size
			gridsize.Text = General.Map.Grid.GridSize.ToString();
			
			// Background image?
			if((General.Map.Grid.Background != null) &&
			   !(General.Map.Grid.Background is UnknownImage))
			{
				// Show background image
				showbackground.Checked = true;
				backgroundname = General.Map.Grid.BackgroundName;
				General.DisplayZoomedImage(backgroundimage, General.Map.Grid.Background.GetBitmap());
			}
			else
			{
				// No background image
				showbackground.Checked = false;
			}

			// Show background offset
			backoffsetx.Text = General.Map.Grid.BackgroundX.ToString();
			backoffsety.Text = General.Map.Grid.BackgroundY.ToString();
			int scalex = (int)(General.Map.Grid.BackgroundScaleX * 100.0f);
			int scaley = (int)(General.Map.Grid.BackgroundScaleY * 100.0f);
			backscalex.Text = General.Clamp(scalex, 1, 10000).ToString();
			backscaley.Text = General.Clamp(scaley, 1, 10000).ToString();
		}

		// Show Background changed
		private void showbackground_CheckedChanged(object sender, EventArgs e)
		{
			// Enable/disable controls
			groupbackground.Enabled = showbackground.Checked;
			/*selectfile.Enabled = showbackground.Checked;
			backoffset.Enabled = showbackground.Checked;
			backscale.Enabled = showbackground.Checked;
			backoffsetx.Enabled = showbackground.Checked;
			backoffsety.Enabled = showbackground.Checked;
			backscalex.Enabled = showbackground.Checked;
			backscaley.Enabled = showbackground.Checked;*/
		}

		// Browse file
		private void selectfile_Click(object sender, EventArgs e)
		{
			// Browse for file
			if(browsefile.ShowDialog(this) == DialogResult.OK)
			{
				// Set this file as background
				backgroundname = browsefile.FileName;
				ImageData img = new FileImage(-1, backgroundname);
				img.LoadImage();
				General.DisplayZoomedImage(backgroundimage, new Bitmap(img.GetBitmap()));
				img.Dispose();
			}
		}
		
		// Cancelled
		private void cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			this.Close();
		}

		// Apply
		private void apply_Click(object sender, EventArgs e)
		{
			// Apply
			General.Map.Grid.SetGridSize(gridsize.GetResult(General.Map.Grid.GridSize));
			General.Map.Grid.SetBackgroundView(backoffsetx.GetResult(General.Map.Grid.BackgroundX),
											   backoffsety.GetResult(General.Map.Grid.BackgroundY),
											   backscalex.GetResult((int)(General.Map.Grid.BackgroundScaleX * 100.0f)) / 100.0f,
											   backscaley.GetResult((int)(General.Map.Grid.BackgroundScaleY * 100.0f)) / 100.0f);
			
			// Set background image
			General.Map.Grid.SetBackground(showbackground.Checked ? backgroundname : null);

			// Done
			DialogResult = DialogResult.OK;
			this.Close();
		}

		// Help
		private void GridSetupForm_HelpRequested(object sender, HelpEventArgs hlpevent)
		{
			General.ShowHelp("w_gridsetup.html");
			hlpevent.Handled = true;
		}
	}
}