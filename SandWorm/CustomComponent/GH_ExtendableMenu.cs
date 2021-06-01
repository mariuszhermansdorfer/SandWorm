using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace SandWorm
{
	public class GH_ExtendableMenu : GH_Attr_Widget
	{
		private List<ExtendedPlug> inputs;

		private List<ExtendedPlug> outputs;

		private string name;

		private GH_MenuCollection collection;

		private GH_Capsule _menu;

		private RectangleF _headBounds;

		private RectangleF _contentBounds;

		private List<GH_Attr_Widget> _controls;

		private GH_Attr_Widget _activeControl;

		private bool _expanded;

		public List<ExtendedPlug> Inputs => inputs;

		public List<ExtendedPlug> Outputs => outputs;

		public bool Expanded => _expanded;

		public GH_MenuCollection Collection
		{
			get
			{
				return collection;
			}
			set
			{
				collection = value;
			}
		}

		public List<GH_Attr_Widget> Controlls => _controls;

		/// <summary>
		/// The text on the foldable button
		/// </summary>
		public override string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
			}
		}


		

		public float TotalHeight
		{
			get
			{
				if (_expanded)
				{
					int num = Math.Max(inputs.Count, outputs.Count) * 20;
					if (num > 0)
					{
						num += 5;
					}
					return base.Height + (float)num;
				}
				return base.Height;
			}
		}

		public GH_ExtendableMenu(int index, string id)
			: base(index, id)
		{
			inputs = new List<ExtendedPlug>();
			outputs = new List<ExtendedPlug>();
			_controls = new List<GH_Attr_Widget>();
			_headBounds = default;
			_contentBounds = default;
		}

		public void RegisterInputPlug(ExtendedPlug plug)
		{
			plug.IsMenu = true;
			inputs.Add(plug);
		}

		public void RegisterOutputPlug(ExtendedPlug plug)
		{
			plug.IsMenu = true;
			outputs.Add(plug);
		}

		public void Expand()
		{
			if (!_expanded)
			{
				_expanded = true;
			}
		}

		public void Collapse()
		{
			if (_expanded)
			{
				_expanded = false;
			}
		}

		public void AddControl(GH_Attr_Widget control)
		{
			control.Parent = this;
			_controls.Add(control);
		}

		public void MakeAllInActive()
		{
			int count = _controls.Count;
			for (int i = 0; i < count; i++)
			{
				if (_controls[i] is MenuPanel panel)
				{
					panel._activeControl = null;
				}
			}
			_activeControl = null;
		}

		public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
		{
			if (e.Button != MouseButtons.Left)
				return GH_ObjectResponse.Ignore;

			if (_activeControl != null)
			{
				GH_ObjectResponse gH_ObjectResponse = _activeControl.RespondToMouseUp(sender, e);
				switch (gH_ObjectResponse)
				{
					case GH_ObjectResponse.Release:
						_activeControl = null;
						return gH_ObjectResponse;
					default:
						return gH_ObjectResponse;
					case GH_ObjectResponse.Ignore:
						break;
				}
				_activeControl = null;
			}
			return GH_ObjectResponse.Ignore;
		}

		public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
		{
			if (e.Button != MouseButtons.Left)
				return GH_ObjectResponse.Ignore;
			if (_headBounds.Contains(e.CanvasLocation))
			{
				if (Expanded)
				{
					_activeControl = null;
				}
				_expanded = !_expanded;
				return GH_ObjectResponse.Handled;
			}
			if (_expanded)
			{
				if (_contentBounds.Contains(e.CanvasLocation))
				{
					for (int i = 0; i < inputs.Count; i++)
					{
						if (inputs[i].Parameter.Attributes.Bounds.Contains(e.CanvasLocation))
						{
							return inputs[i].Parameter.Attributes.RespondToMouseDown(sender, e);
						}
					}
					for (int j = 0; j < _controls.Count; j++)
					{
						if (_controls[j].Contains(e.CanvasLocation))
						{
							_activeControl = _controls[j];
							return _controls[j].RespondToMouseDown(sender, e);
						}
					}
				}
				else if (_activeControl != null)
				{
					_activeControl.RespondToMouseDown(sender, e);
					_activeControl = null;
					return GH_ObjectResponse.Handled;
				}
			}
			return GH_ObjectResponse.Ignore;
		}

		public override GH_Attr_Widget IsTtipPoint(PointF pt)
		{
			if (_headBounds.Contains(pt))
			{
				return this;
			}
			if (_expanded && _contentBounds.Contains(pt))
			{
				int count = _controls.Count;
				for (int i = 0; i < count; i++)
				{
					if (_controls[i].Contains(pt))
					{
						GH_Attr_Widget gH_Attr_Widget = _controls[i].IsTtipPoint(pt);
						if (gH_Attr_Widget != null)
						{
							return gH_Attr_Widget;
						}
					}
				}
			}
			return null;
		}

		public override void TooltipSetup(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
		{
			e.Icon = null;
			e.Title = "Menu (" + name + ")";
			e.Text = _header;
			if (_header != null)
			{
				e.Text += "\n";
			}
			if (_expanded)
			{
				e.Text += "Click to close menu";
			}
			else
			{
				e.Text += "Click to open menu";
			}
			e.Description = _description;
		}

		public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
		{
			if (e.Button != MouseButtons.Left)
				return GH_ObjectResponse.Ignore;

			if (_activeControl != null)
			{
				return _activeControl.RespondToMouseMove(sender, e);
			}
			return GH_ObjectResponse.Ignore;
		}

		public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
		{
			_ = base.CanvasPivot;
			if (_headBounds.Contains(e.CanvasLocation))
			{
				return GH_ObjectResponse.Handled;
			}
			if (_expanded && _contentBounds.Contains(e.CanvasLocation))
			{
				int count = _controls.Count;
				for (int i = 0; i < count; i++)
				{
					if (_controls[i].Contains(e.CanvasLocation))
					{
						return _controls[i].RespondToMouseDoubleClick(sender, e);
					}
				}
			}
			return GH_ObjectResponse.Ignore;
		}

		public override bool Write(GH_IWriter writer)
		{
			writer.SetBoolean("Expanded", _expanded);
			for (int i = 0; i < _controls.Count; i++)
			{
				_controls[i].Write(writer);
			}
			return base.Write(writer);
		}

		public override bool Read(GH_IReader reader)
		{
			_expanded = reader.GetBoolean("Expanded");
			for (int i = 0; i < _controls.Count; i++)
			{
				_controls[i].Read(reader);
			}
			return base.Read(reader);
		}

		public override SizeF ComputeMinSize()
		{
			SizeF menuHeadTextSize = GetMenuHeadTextSize();
			float num = menuHeadTextSize.Width;
			float num2 = menuHeadTextSize.Height;
			foreach (GH_Attr_Widget control in _controls)
			{
				SizeF sizeF = control.ComputeMinSize();
				num = Math.Max(sizeF.Width, num);
				if (_expanded)
				{
					num2 += sizeF.Height;
				}
			}
			return new SizeF(num, num2);
		}

		private SizeF GetMenuHeadTextSize()
		{
			Size size = TextRenderer.MeasureText(name, WidgetServer.Instance.MenuHeaderFont);
			return new SizeF(size.Width + 8, size.Height + 4);
		}

		public override void Layout()
		{
			SizeF menuHeadTextSize = GetMenuHeadTextSize();
			_headBounds = new RectangleF(base.CanvasPivot.X, base.CanvasPivot.Y, base.Width, menuHeadTextSize.Height);
			_contentBounds = new RectangleF(base.CanvasPivot.X, base.CanvasPivot.Y + menuHeadTextSize.Height, base.Width, base.Height - menuHeadTextSize.Height);
			Rectangle rectangle = new Rectangle((int)_headBounds.X + 3, (int)_headBounds.Y + 1, (int)_headBounds.Width - 6, (int)_headBounds.Height - 2);
			_menu = GH_Capsule.CreateTextCapsule(rectangle, rectangle, GH_Palette.Normal, name, WidgetServer.Instance.MenuHeaderFont, GH_Orientation.horizontal_center, 2, 0); //TODO Button color
			float num = menuHeadTextSize.Height;
			if (_expanded)
			{
				PointF transform = new PointF(base.CanvasPivot.X, base.CanvasPivot.Y + menuHeadTextSize.Height);
				foreach (GH_Attr_Widget control in _controls)
				{
					control.UpdateBounds(transform, base.Width);
					control.Transform = transform;
					control.Style = style;
					control.Palette = palette;
					control.Layout();
					num += control.Height;
				}
			}
			base.Height = num;
		}

		public override void Render(WidgetRenderArgs args)
		{
			GH_Canvas canvas = args.Canvas;
			_ = args.Channel;
			float zoom = canvas.Viewport.Zoom;
			int num = 255;
			if (zoom < 1f)
			{
				float num2 = (zoom - 0.5f) * 2f;
				num = (int)((float)num * num2);
			}
			_menu.Render(canvas.Graphics, selected: false, locked: false, hidden: false);
			if (!_expanded || num <= 0)
			{
				return;
			}
			RenderMenuParameters(canvas, canvas.Graphics);
			foreach (GH_Attr_Widget control in _controls)
			{
				control.OnRender(args);
			}
		}

		public void RenderMenuParameters(GH_Canvas canvas, Graphics graphics)
		{
			if (Math.Max(inputs.Count, outputs.Count) == 0)
			{
				return;
			}
			int zoomFadeLow = GH_Canvas.ZoomFadeLow;
			if (zoomFadeLow < 5)
			{
				return;
			}
			StringFormat farCenter = GH_TextRenderingConstants.FarCenter;
			canvas.SetSmartTextRenderingHint();
			SolidBrush solidBrush = new SolidBrush(Color.FromArgb(zoomFadeLow, style.Text));
			foreach (ExtendedPlug input in inputs)
			{
				IGH_Param parameter = input.Parameter;
				RectangleF bounds = parameter.Attributes.Bounds;
				if (!(bounds.Width >= 1f))
				{
					continue;
				}
				graphics.DrawString(parameter.NickName, StandardFont.Font(), solidBrush, bounds, farCenter);
                GH_LinkedParamAttributes obj = (GH_LinkedParamAttributes)parameter.Attributes;
                FieldInfo field = typeof(GH_LinkedParamAttributes).GetField("m_renderTags", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    object value = field.GetValue(obj);
                    if (value != null)
                    {
                        ((GH_StateTagList)value).RenderStateTags(graphics);
                    }
                }
            }
			farCenter = GH_TextRenderingConstants.NearCenter;
			foreach (ExtendedPlug output in outputs)
			{
				IGH_Param parameter2 = output.Parameter;
				RectangleF bounds2 = parameter2.Attributes.Bounds;
				if (!(bounds2.Width >= 1f))
				{
					continue;
				}
				graphics.DrawString(parameter2.NickName, StandardFont.Font(), solidBrush, bounds2, farCenter);
				GH_LinkedParamAttributes obj2 = (GH_LinkedParamAttributes)parameter2.Attributes;
				FieldInfo field2 = typeof(GH_LinkedParamAttributes).GetField("m_renderTags", BindingFlags.Instance | BindingFlags.NonPublic);
				if (field2 != null)
				{
					object value2 = field2.GetValue(obj2);
					if (value2 != null)
					{
						((GH_StateTagList)value2).RenderStateTags(graphics);
					}
				}
			}
			solidBrush.Dispose();
		}

		public override string GetWidgetDescription()
		{
			string str = base.GetWidgetDescription() + "{\n";
			foreach (GH_Attr_Widget control in _controls)
			{
				str = str + control.GetWidgetDescription() + "\n";
			}
			return str + "}";
		}
	}
}