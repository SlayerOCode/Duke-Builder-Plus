
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using mxd.DukeBuilder.Geometry;
using mxd.DukeBuilder.IO;
using mxd.DukeBuilder.Windows;
using mxd.DukeBuilder.Map;
using mxd.DukeBuilder.Rendering;
using System.Drawing;
using mxd.DukeBuilder.Editing;
using mxd.DukeBuilder.Plugins;
using mxd.DukeBuilder.Data;
using mxd.DukeBuilder.Controls;

#endregion

namespace mxd.DukeBuilder.EditModes
{
	public class BuilderPlug : Plug
	{
		#region ================== API Declarations

		[DllImport("user32.dll")]
		internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		
		#endregion
		
		#region ================== Constants

		//internal const int WS_HSCROLL = 0x100000;
		internal const int WS_VSCROLL = 0x200000;
		internal const int GWL_STYLE = -16;

		internal const float IMAGE_TO_MU_SCALER = 1 / 16f;
		
		#endregion

		#region ================== Variables

		// Static instance
		private static BuilderPlug me;
		
		// Main objects
		private MenusForm menusform;
		private CurveLinedefsForm curvelinedefsform;
		private FindReplaceForm findreplaceform;
		private ErrorCheckForm errorcheckform;
		private PreferencesForm preferencesform;
		
		// Dockers
		private UndoRedoPanel undoredopanel;
		private Docker undoredodocker;
		
		// Settings
		private int showvisualthings;			// 0 = none, 1 = sprite only, 2 = sprite caged
		private bool usegravity;
		private int changeheightbysidedef;		// 0 = nothing, 1 = change ceiling, 2 = change floor
		private int splitlinebehavior;			// 0 = adjust texcoords, 1 = copy texcoords, 2 = reset texcoords
		private bool editnewthing;
		private bool editnewsector;
		private bool additiveselect;
		private bool autoclearselection;
		private bool visualmodeclearselection;
		//private string copiedflat;
		private Point copiedoffsets;
		//private VertexProperties copiedvertexprops;
		private bool viewselectionnumbers;
		private float stitchrange;
		private float highlightrange;
		private float highlightthingsrange;
		private float splitlinedefsrange;
		private bool usehighlight;
		private bool autodragonpaste;
		
		#endregion

		#region ================== Properties
		
		public override string Name { get { return "Duke Builder"; } }
		public static BuilderPlug Me { get { return me; } }

		// It is only safe to do this dynamically because we compile and distribute both
		// the core and this plugin together with the same revision number! In third party
		// plugins this should just contain a fixed number.
		public override int MinimumRevision { get { return Assembly.GetExecutingAssembly().GetName().Version.Revision; } }
		
		public MenusForm MenusForm { get { return menusform; } }
		public CurveLinedefsForm CurveLinedefsForm { get { return curvelinedefsform; } }
		public FindReplaceForm FindReplaceForm { get { return findreplaceform; } }
		public ErrorCheckForm ErrorCheckForm { get { return errorcheckform; } }
		//public PreferencesForm PreferencesForm { get { return preferencesform; } }

		// Settings
		public int ShowVisualThings { get { return showvisualthings; } set { showvisualthings = value; } }
		public bool UseGravity { get { return usegravity; } set { usegravity = value; } }
		public int ChangeHeightBySidedef { get { return changeheightbysidedef; } }
		public int SplitLineBehavior { get { return splitlinebehavior; } }
		public bool EditNewThing { get { return editnewthing; } }
		public bool EditNewSector { get { return editnewsector; } }
		public bool AdditiveSelect { get { return additiveselect; } }
		public bool AutoClearSelection { get { return autoclearselection; } }
		public bool VisualModeClearSelection { get { return visualmodeclearselection; } }
		public int CopiedImageIndex = -1;
		//public string CopiedFlat { get { return copiedflat; } set { copiedflat = value; } }
		public Point CopiedOffsets { get { return copiedoffsets; } set { copiedoffsets = value; } }
		//public VertexProperties CopiedVertexProps { get { return copiedvertexprops; } set { copiedvertexprops = value; } }
		public BuildSector CopiedSectorProps;
		public BuildWall CopiedWallProps;
		public LineProperties CopiedLineProps;
		public BuildSprite CopiedSpriteProps;
		public bool ViewSelectionNumbers { get { return viewselectionnumbers; } set { viewselectionnumbers = value; } }
		public float StitchRange { get { return stitchrange; } }
		public float HighlightRange { get { return highlightrange; } }
		public float HighlightThingsRange { get { return highlightthingsrange; } }
		public float SplitLinedefsRange { get { return splitlinedefsrange; } }
		public bool UseHighlight { get { return usehighlight; } set { usehighlight = value; } }
		public bool AutoDragOnPaste { get { return autodragonpaste; } set { autodragonpaste = value; } }
		
		#endregion

		#region ================== Initialize / Dispose

		// When plugin is initialized
		public override void OnInitialize()
		{
			// Setup
			me = this;

			// Settings
			showvisualthings = 2;
			usegravity = false;
			usehighlight = true;
			LoadSettings();
			
			// Load menus form and register it
			menusform = new MenusForm();
			menusform.Register();
			
			// Load curve linedefs form
			curvelinedefsform = new CurveLinedefsForm();
			
			// Load find/replace form
			findreplaceform = new FindReplaceForm();
			
			// Load error checking form
			errorcheckform = new ErrorCheckForm();
			
			// Load Undo\Redo docker
			undoredopanel = new UndoRedoPanel();
			undoredodocker = new Docker("undoredo", "Undo / Redo", undoredopanel);
			General.Interface.AddDocker(undoredodocker);
		}
		
		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!IsDisposed)
			{
				// Clean up
				General.Interface.RemoveDocker(undoredodocker);
				undoredopanel.Dispose();
				menusform.Unregister();
				menusform.Dispose();
				menusform = null;
				curvelinedefsform.Dispose();
				curvelinedefsform = null;
				findreplaceform.Dispose();
				findreplaceform = null;
				errorcheckform.Dispose();
				errorcheckform = null;
				
				// Done
				me = null;
				base.Dispose();
			}
		}

		#endregion

		#region ================== Methods

		// This loads the plugin settings
		private void LoadSettings()
		{
			changeheightbysidedef = General.Settings.ReadPluginSetting("changeheightbysidedef", 0);
			splitlinebehavior = General.Settings.ReadPluginSetting("splitlinebehavior", 0);
			editnewthing = General.Settings.ReadPluginSetting("editnewthing", true);
			editnewsector = General.Settings.ReadPluginSetting("editnewsector", false);
			additiveselect = General.Settings.ReadPluginSetting("additiveselect", false);
			autoclearselection = General.Settings.ReadPluginSetting("autoclearselection", false);
			visualmodeclearselection = General.Settings.ReadPluginSetting("visualmodeclearselection", false);
			viewselectionnumbers = General.Settings.ReadPluginSetting("viewselectionnumbers", true);
			stitchrange = General.Settings.ReadPluginSetting("stitchrange", 20);
			highlightrange = General.Settings.ReadPluginSetting("highlightrange", 20);
			highlightthingsrange = General.Settings.ReadPluginSetting("highlightthingsrange", 10);
			splitlinedefsrange = General.Settings.ReadPluginSetting("splitlinedefsrange", 10);
			autodragonpaste = General.Settings.ReadPluginSetting("autodragonpaste", false);
		}

		#endregion

		#region ================== Events

		// When floor surface geometry is created for classic modes
		public override void OnSectorFloorSurfaceUpdate(Sector s, ref FlatVertex[] vertices)
		{
			ImageData img = General.Map.Data.GetImageData(s.FloorTileIndex);
			if(img != null && img.IsImageLoaded)
				SetupSurfaceVertices(s, vertices, img, s.FloorOffsetX, s.FloorOffsetY, true);
		}

		// When ceiling surface geometry is created for classic modes
		public override void OnSectorCeilingSurfaceUpdate(Sector s, ref FlatVertex[] vertices)
		{
			ImageData img = General.Map.Data.GetImageData(s.CeilingTileIndex);
			if(img != null && img.IsImageLoaded)
				SetupSurfaceVertices(s, vertices, img, s.CeilingOffsetX, s.CeilingOffsetY, false);
		}

		//mxd. Applies UV coords to sector verts the Build way
		private static void SetupSurfaceVertices(Sector s, FlatVertex[] vertices, ImageData img, int xpanning, int ypanning, bool floor)
		{
			// Flags
			bool sloped = s.IsFlagSet(General.Map.FormatInterface.SectorFlags.Sloped, floor); // curstat & 2
			bool relativealignment = s.IsFlagSet(General.Map.FormatInterface.SectorFlags.RelativeAlignment, floor); // curstat & 64
			bool swapxy = s.IsFlagSet(General.Map.FormatInterface.SectorFlags.SwapXY, floor); // curstat & 4
			bool flipx = s.IsFlagSet(General.Map.FormatInterface.SectorFlags.FlipX, floor); // curstat & 16
			bool flipy = s.IsFlagSet(General.Map.FormatInterface.SectorFlags.FlipY, floor); // curstat & 32

			// Common vars
			int zpos = (floor ? s.FloorHeight : s.CeilingHeight);
			Vector2D fwstart = (s.FirstWall.IsFront ? s.FirstWall.Line.Start : s.FirstWall.Line.End).Position;
			float scalecoef = (s.IsFlagSet(General.Map.FormatInterface.SectorFlags.TextureExpansion, floor) ? 8.0f : 16.0f);
			float scalecoefx = scalecoef * img.Width;
			float scalecoefy = scalecoef * img.Height;

			int picwidth = General.NextPowerOf2(img.Width);
			int picheight = General.NextPowerOf2(img.Height);

			float secangcos, secangsin;
			if(relativealignment)
			{
				secangcos = (float)Math.Cos(s.FirstWall.Angle + Angle2D.PI);
				secangsin = (float)Math.Sin(s.FirstWall.Angle + Angle2D.PI);
			}
			else
			{
				secangcos = 0f;
				secangsin = 0f;
			}

			// Update UVs
			for(int i = 0; i < vertices.Length; i++)
			{
				int tex, tey;
				
				// Relative texturing
				if(relativealignment)
				{
					float relxpancoef = vertices[i].x - fwstart.x;
					float relypancoef = fwstart.y - vertices[i].y;

					tex = -(int)(relxpancoef * secangsin + relypancoef * secangcos); // +
					tey = (int)(relxpancoef * secangcos - relypancoef * secangsin);
				}
				else
				{
					tex = (int)vertices[i].x;
					tey = (int)vertices[i].y; // -
				}

				if(sloped && relativealignment)
				{
					float curzpos = (floor ? s.FloorPlane.GetZ(vertices[i].x, vertices[i].y) : s.CeilingPlane.GetZ(vertices[i].x, vertices[i].y));
					int heidiff = (int)(curzpos - zpos);
					
					// Don't forget the sign, it could be negative with concave sectors
					tey = (int)Math.Sqrt((tey * tey) + (heidiff * heidiff)) * (tey < 0 ? -1 : 1);
				}

				if(swapxy) General.Swap(ref tex, ref tey);
				if(flipx) tex = -tex;
				if(flipy) tey = -tey;

				float xpancoef = (xpanning != 0 ? picwidth  * ((xpanning) / (256.0f * (img.Width)))  : 0f);
				float ypancoef = (ypanning != 0 ? picheight * ((ypanning) / (256.0f * (img.Height))) : 0f);

				vertices[i].u = (tex / scalecoefx) + xpancoef;
				vertices[i].v = (tey / scalecoefy) + ypancoef;
			}
		}

		// When the editing mode changes
		public override bool OnModeChange(EditMode oldmode, EditMode newmode)
		{
			// Show the correct menu for the new mode
			menusform.ShowEditingModeMenu(newmode);
			
			return base.OnModeChange(oldmode, newmode);
		}

		// When the Preferences dialog is shown
		public override void OnShowPreferences(PreferencesController controller)
		{
			base.OnShowPreferences(controller);

			// Load preferences
			preferencesform = new PreferencesForm();
			preferencesform.Setup(controller);
		}

		// When the Preferences dialog is closed
		public override void OnClosePreferences(PreferencesController controller)
		{
			base.OnClosePreferences(controller);

			// Apply settings that could have been changed
			LoadSettings();
			
			// Unload preferences
			preferencesform.Dispose();
			preferencesform = null;
		}
		
		// New map created
		public override void OnMapNewEnd()
		{
			base.OnMapNewEnd();
			undoredopanel.SetBeginDescription("New Map");
			undoredopanel.UpdateList();
		}
		
		// Map opened
		public override void OnMapOpenEnd()
		{
			base.OnMapOpenEnd();
			undoredopanel.SetBeginDescription("Opened Map");
			undoredopanel.UpdateList();
		}
		
		// Map closed
		public override void OnMapCloseEnd()
		{
			base.OnMapCloseEnd();
			undoredopanel.UpdateList();
		}
		
		// Redo performed
		public override void OnRedoEnd()
		{
			base.OnRedoEnd();
			undoredopanel.UpdateList();
		}
		
		// Undo performed
		public override void OnUndoEnd()
		{
			base.OnUndoEnd();
			undoredopanel.UpdateList();
		}
		
		// Undo created
		public override void OnUndoCreated()
		{
			base.OnUndoCreated();
			undoredopanel.UpdateList();
		}
		
		// Undo withdrawn
		public override void OnUndoWithdrawn()
		{
			base.OnUndoWithdrawn();
			undoredopanel.UpdateList();
		}
		
		#endregion
		
		#region ================== Tools

		// This adjusts texture coordinates for splitted lines according to the user preferences
		public void AdjustSplitCoordinates(Linedef oldline, Linedef newline)
		{
			// Copy X and Y coordinates
			if(splitlinebehavior == 1)
			{
				if((oldline.Front != null) && (newline.Front != null))
				{
					newline.Front.OffsetX = oldline.Front.OffsetX;
					newline.Front.OffsetY = oldline.Front.OffsetY;
				}
				
				if((oldline.Back != null) && (newline.Back != null))
				{
					newline.Back.OffsetX = oldline.Back.OffsetX;
					newline.Back.OffsetY = oldline.Back.OffsetY;
				}
			}
			// Reset X coordinate, copy Y coordinate
			else if(splitlinebehavior == 2)
			{
				if((oldline.Front != null) && (newline.Front != null))
				{
					newline.Front.OffsetX = 0;
					newline.Front.OffsetY = oldline.Front.OffsetY;
				}
				
				if((oldline.Back != null) && (newline.Back != null))
				{
					newline.Back.OffsetX = 0;
					newline.Back.OffsetY = oldline.Back.OffsetY;
				}
			}
		}
		
		// This finds all class types that inherits from the given type
		public Type[] FindClasses(Type t)
		{
			List<Type> found = new List<Type>();

			// Get all exported types
			Type[] types = Assembly.GetExecutingAssembly().GetTypes();
			foreach(Type it in types)
			{
				// Compare types
				if(t.IsAssignableFrom(it)) found.Add(it);
			}

			// Return list
			return found.ToArray();
		}
		
		// This renders the associated sectors/linedefs with the indication color
		public void PlotAssociations(IRenderer2D renderer, Association asso)
		{
			// Tag must be above zero
			if(asso.tag <= 0) return;

			//TODO: PlotAssociations
			throw new NotImplementedException();

			// Sectors?
			/*if(asso.type == UniversalType.SectorTag)
			{
				foreach(Sector s in General.Map.Map.Sectors)
					if(s.Tag == asso.tag) renderer.PlotSector(s, General.Colors.Indication);
			}
			// Linedefs?
			else if(asso.type == UniversalType.LinedefTag)
			{
				foreach(Linedef l in General.Map.Map.Linedefs)
					if(l.Tag == asso.tag) renderer.PlotLinedef(l, General.Colors.Indication);
			}*/
		}
		

		// This renders the associated things with the indication color
		public void RenderAssociations(IRenderer2D renderer, Association asso)
		{
			// Tag must be above zero
			if(asso.tag <= 0) return;

			//TODO: RenderAssociations
			throw new NotImplementedException();

			// Things?
			/*if(asso.type == UniversalType.ThingTag)
			{
				foreach(Thing t in General.Map.Map.Things)
					if(t.Tag == asso.tag) renderer.RenderThing(t, General.Colors.Indication, 1.0f);
			}*/
		}
		

		// This renders the associated sectors/linedefs with the indication color
		public void PlotReverseAssociations(IRenderer2D renderer, Association asso)
		{
			// Tag must be above zero
			if(asso.tag <= 0) return;

			//TODO: PlotReverseAssociations
			throw new NotImplementedException();
			
			// Doom style referencing to sectors?
			/*if(General.Map.Config.LineTagIndicatesSectors && (asso.type == UniversalType.SectorTag))
			{
				// Linedefs
				foreach(Linedef l in General.Map.Map.Linedefs)
				{
					// Any action on this line?
					if(l.Action > 0)
					{
						if(l.Tag == asso.tag) renderer.PlotLinedef(l, General.Colors.Indication);
					}
				}
			}
			else
			{
				// Linedefs
				foreach(Linedef l in General.Map.Map.Linedefs)
				{
					// Known action on this line?
					if((l.Action > 0) && General.Map.Config.LinedefActions.ContainsKey(l.Action))
					{
						LinedefActionInfo action = General.Map.Config.LinedefActions[l.Action];
						if((action.Args[0].Type == (int)asso.type) && (l.Args[0] == asso.tag)) renderer.PlotLinedef(l, General.Colors.Indication);
						if((action.Args[1].Type == (int)asso.type) && (l.Args[1] == asso.tag)) renderer.PlotLinedef(l, General.Colors.Indication);
						if((action.Args[2].Type == (int)asso.type) && (l.Args[2] == asso.tag)) renderer.PlotLinedef(l, General.Colors.Indication);
						if((action.Args[3].Type == (int)asso.type) && (l.Args[3] == asso.tag)) renderer.PlotLinedef(l, General.Colors.Indication);
						if((action.Args[4].Type == (int)asso.type) && (l.Args[4] == asso.tag)) renderer.PlotLinedef(l, General.Colors.Indication);
					}
				}
			}*/
		}
		

		// This renders the associated things with the indication color
		public void RenderReverseAssociations(IRenderer2D renderer, Association asso)
		{
			// Tag must be above zero
			if(asso.tag <= 0) return;

			//TODO: RenderReverseAssociations
			throw new NotImplementedException();

			// Things
			/*foreach(Thing t in General.Map.Map.Things)
			{
				// Known action on this thing?
				if((t.Action > 0) && General.Map.Config.LinedefActions.ContainsKey(t.Action))
				{
					LinedefActionInfo action = General.Map.Config.LinedefActions[t.Action];
					if((action.Args[0].Type == (int)asso.type) && (t.Args[0] == asso.tag)) renderer.RenderThing(t, General.Colors.Indication, 1.0f);
					if((action.Args[1].Type == (int)asso.type) && (t.Args[1] == asso.tag)) renderer.RenderThing(t, General.Colors.Indication, 1.0f);
					if((action.Args[2].Type == (int)asso.type) && (t.Args[2] == asso.tag)) renderer.RenderThing(t, General.Colors.Indication, 1.0f);
					if((action.Args[3].Type == (int)asso.type) && (t.Args[3] == asso.tag)) renderer.RenderThing(t, General.Colors.Indication, 1.0f);
					if((action.Args[4].Type == (int)asso.type) && (t.Args[4] == asso.tag)) renderer.RenderThing(t, General.Colors.Indication, 1.0f);
				}
			}*/
		}

		#endregion
	}
}
