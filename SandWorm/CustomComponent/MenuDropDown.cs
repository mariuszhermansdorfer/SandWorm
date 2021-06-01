using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;

namespace SandWorm
{
    public class MenuDropDown : GH_Attr_Widget
    {
        private class MenuDropDownWindow : GH_Attr_Widget
        {
            private MenuDropDown _dropMenu;

            private int _tempActive = -1;

            private int _tempStart;

            private int _maxLen;

            private Rectangle _contentBox;

            public MenuDropDownWindow(MenuDropDown parent)
                : base(0, "")
            {
                _dropMenu = parent;
            }

            public void Update()
            {
                int count = _dropMenu.Items.Count;
                if (_dropMenu.LastValidValue > count)
                    _dropMenu.Value = -1;

                if (_dropMenu.Items.Count == 0)
                    _tempStart = 0;

                _maxLen = count;
                int num2 = count * 20;
                base.Height = num2;
                _contentBox = new Rectangle((int)base.CanvasPivot.X, (int)base.CanvasPivot.Y, (int)base.Width, num2);
            }

            public override SizeF ComputeMinSize()
            {
                return new SizeF(10f, 10f);
            }

            public override void Layout()
            {
                Update();
            }

            public override void Render(WidgetRenderArgs args)
            {
                if (args.Channel != WidgetChannel.Overlay)
                {
                    return;
                }
                Graphics graphics = args.Canvas.Graphics;
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                Pen pen = new Pen(Brushes.Gray);

                System.Drawing.Drawing2D.GraphicsPath path = RoundedRect(_contentBox, 2);
                graphics.DrawPath(pen, path);
                graphics.FillPath(Brushes.White, path);
                int num = 0;
                for (int i = _tempStart; i < _tempStart + _maxLen; i++)
                {
                    Brush white = Brushes.White;
                    Brush white2 = Brushes.White;
                    if (i == _tempActive)
                    {
                        white = new SolidBrush(Color.FromArgb(174, 213, 129));
                        white2 = Brushes.White;
                    }
                    else if (i == _dropMenu.Value)
                    {
                        white = new SolidBrush(Color.FromArgb(238, 238, 238));
                        white2 = new SolidBrush(Color.FromArgb(45, 45, 45));
                    }
                    else
                    {
                        white = new SolidBrush(Color.White);
                        white2 = new SolidBrush(Color.FromArgb(45, 45, 45));
                    }
                    System.Drawing.Drawing2D.GraphicsPath rect = RoundedRect(new Rectangle((int)base.Transform.X, (int)base.Transform.Y + 20 * num, (int)base.Width, 20), 2);
                    graphics.FillPath(white, rect);
                    graphics.DrawString(_dropMenu.Items[i].content, WidgetServer.Instance.DropdownFont, white2, base.Transform.X + base.Width / 2f, (int)base.Transform.Y + 20 * num + 5, stringFormat);
                    num++;
                }
            }

            public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                return GH_ObjectResponse.Capture;
            }

            public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (Contains(e.CanvasLocation))
                    _tempActive = _tempStart + (int)((e.CanvasLocation.Y - base.Transform.Y) / 20f);
                else
                    _tempActive = -1;

                return GH_ObjectResponse.Capture;
            }

            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {

                if (_contentBox.Contains((int)e.CanvasLocation.X, (int)e.CanvasLocation.Y))
                {
                    _dropMenu.Value = _tempStart + (int)((e.CanvasLocation.Y - base.Transform.Y) / 20f);
                    _tempActive = -1;
                    _dropMenu.HideWindow(fire: true);
                    return GH_ObjectResponse.Release;
                }
                _dropMenu.HideWindow(fire: false);
                return GH_ObjectResponse.Release;
            }

            public override bool Contains(PointF pt)
            {
                return canvasBounds.Contains(pt);
            }
        }

        public class Entry
        {
            public string content;

            public string name;

            public string Header { get; set; }

            public int index;

            public object data;

            public Entry(string name, string content, int ind)
            {
                this.content = content;
                this.name = name;
                index = ind;
            }

            public Entry(string name, string content, int ind, string header)
            {
                this.content = content;
                this.name = name;
                index = ind;
                Header = header;
            }
        }

        private MenuDropDownWindow _window;

        public bool expanded;

        private static int default_item_index = 0;

        private int current_value;

        private int last_valid_value;

        private List<Entry> _items;

        private string _emptyText = "empty";

        public int Value
        {
            get
            {
                return current_value;
            }
            set
            {
                current_value = Math.Max(value, 0);
                last_valid_value = ((value >= 0) ? value : 0);
            }
        }

        public int LastValidValue => last_valid_value;

        public List<Entry> Items => _items;
        /// <summary>
        /// Gets selected item
        /// </summary>
        public Entry Item => _items[Value];

        private bool Empty => _items.Count == 0;

        public event ValueChangeEventHandler ValueChanged;

        public int FindIndex(string name)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].name.Equals(name))
                {
                    return i;
                }
            }
            return -1;
        }

        public override void PostUpdateBounds(out float outHeight)
        {
            _window.Width = base.Width;
            outHeight = ComputeMinSize().Height;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="id"></param>
        /// <param name="tag">currently UNUSED</param>
        public MenuDropDown(int index, string id, string tag = "")
            : base(index, id)
        {
            _items = new List<Entry>();
            _window = new MenuDropDownWindow(this);
            _window.Parent = this;
        }

        /// <summary>
        /// Use this one if you want to create a dropdown but keep the desriptions from the textinput.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="id"></param>
        /// <param name="textInput"></param>
        public MenuDropDown(MenuStaticText textInput, int index = 0, string id = "")
            : base(index, id)
        {
            Header = textInput.Header;
            Name = textInput.Text;
            _items = new List<Entry>();
            _window = new MenuDropDownWindow(this);
            _window.Parent = this;
        }

        public MenuDropDown AddItem(string name, string cont, string header = "")
        {
            Entry item = new Entry(name, cont, _items.Count, header);
            
            _items.Add(item);
            Update();

            return this; //fluent interface ftw
        }

        public MenuDropDown AddItem(string name, string cont, object data)
        {
            Entry entry = new Entry(name, cont, _items.Count);
            entry.data = data;
            _items.Add(entry);
            Update();

            return this; //fluent interface ftw
        }

        /// <summary>
        /// Populate your dropdown menus from an Enum list. use typeof(yourEnum)
        /// </summary>
        /// <param name="enumType">input typeof(MyEnum)</param>
        public MenuDropDown AddEnum(Type enumType)
        {
            if (!typeof(Enum).IsAssignableFrom(enumType))
                throw new ArgumentException("enumType should describe an enum");

            Array names = Enum.GetNames(enumType);

            foreach (string name in names)
            {
                AddItem(name, name, name);
            }

            return this;

        }

   


        private void Update()
        {
            if (_items.Count == 0)
            {
                current_value = 0;
            }
            _window.Update();
        }

        public override void Layout()
        {
            _window.UpdateBounds(base.CanvasPivot, base.Width);
            _window.Layout();
        }

        public void Clear()
        {
            _items.Clear();
            Update();
        }

        public override SizeF ComputeMinSize()
        {
            int num = 0;
            int num2 = 0;
            if (Empty)
            {
                Size size = TextRenderer.MeasureText(_emptyText, WidgetServer.Instance.DropdownFont);
                num = size.Width + 4 + 10;
                num2 = size.Height + 2;
            }
            else
            {
                foreach (Entry item in _items)
                {
                    Size size2 = TextRenderer.MeasureText(item.content, WidgetServer.Instance.DropdownFont);
                    int val = size2.Width + 4 + 10;
                    int val2 = size2.Height + 2;
                    num = Math.Max(num, val);
                    num2 = Math.Max(num2, val2);
                }
            }
            return new SizeF(num, num2);
        }

        public override void Render(WidgetRenderArgs args)
        {
            GH_Canvas canvas = args.Canvas;
            if (args.Channel == WidgetChannel.Overlay)
            {
                if (expanded)
                {
                    _window.Render(args);
                }
            }
            else if (args.Channel == WidgetChannel.Object)
            {
                Graphics graphics = canvas.Graphics;
                float zoom = canvas.Viewport.Zoom;
                int num = 255;
                if (zoom < 1f)
                {
                    float num2 = (zoom - 0.5f) * 2f;
                    num = (int)((float)num * num2);
                }
                if (num < 0)
                {
                    num = 0;
                }
                num = GH_Canvas.ZoomFadeLow;
                SolidBrush brush = new SolidBrush(Color.FromArgb(num, 90, 90, 90));
                SolidBrush brush2 = new SolidBrush(Color.FromArgb(num, 190, 190, 190));
                SolidBrush brush3 = new SolidBrush(Color.FromArgb(num, 45, 45, 45));
                SolidBrush brush4 = new SolidBrush(Color.FromArgb(num, 255, 255, 255));
                Pen pen = new Pen(brush2);
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                if (Empty)
                {
                    PointF point = new PointF(base.CanvasPivot.X + base.Width / 2f, base.CanvasBounds.Y + 2f);
                    var path = RoundedRect(GH_Attr_Widget.Convert(base.CanvasBounds), 2);
                    graphics.DrawPath(pen, path);
                    graphics.FillPath(brush4, path);
                    graphics.DrawString(_emptyText, WidgetServer.Instance.DropdownFont, brush, point, stringFormat);
                }
                else
                {
                    PointF point2 = new PointF(base.CanvasPivot.X + (base.Width - 13f) / 2f, base.CanvasBounds.Y + 2f);

                    var path = RoundedRect(GH_Attr_Widget.Convert(base.CanvasBounds), 2);
                    graphics.DrawPath(pen, path);
                    graphics.FillPath(brush4, path);
                    graphics.DrawString(_items[current_value].content, WidgetServer.Instance.DropdownFont, brush, point2, stringFormat);

                    PointF p1 = new PointF(base.CanvasPivot.X + base.Width - 13, base.CanvasPivot.Y + 6);
                    PointF p2 = new PointF(base.CanvasPivot.X + base.Width - 5, base.CanvasPivot.Y + 6);
                    PointF p3 = new PointF(base.CanvasPivot.X + base.Width - 9, base.CanvasPivot.Y + 12);
                    PointF[] curvePoints = { p1, p2, p3 };

                    //graphics.DrawPolygon(pen, curvePoints);
                    graphics.FillPolygon(brush3, curvePoints);
                }
            }
        }


        // Fixed tooltip in the folded dropdown menu - TODO: Fix it in the unfolded!
        // It is hidden in the MenuScrollBar class, but I didn't manage to get the popup to work.
        public override void TooltipSetup(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
        {
            e.Icon = null;
            e.Title = _name;
            e.Text = _header;
            if (_header != null)
            {
                e.Text += "\n";
            }

            e.Description = _description;
        }

        // This allowed us to get the static text tooltip :-)
        public override GH_Attr_Widget IsTtipPoint(System.Drawing.PointF pt)
        {
            if (new System.Drawing.RectangleF(transfromation.X, transfromation.Y, base.Width, base.Height).Contains(pt))
            {
                return this;
            }
            return null;
        }

        public static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Empty)
            {
                return GH_ObjectResponse.Release;
            }
            if (expanded)
            {
                return _window.RespondToMouseUp(sender, e);
            }
            return GH_ObjectResponse.Ignore;
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (expanded)
            {
                return _window.RespondToMouseMove(sender, e);
            }
            return GH_ObjectResponse.Ignore;
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (Empty)
            {
                return GH_ObjectResponse.Handled;
            }
            if (expanded)
            {
                if (_window.Contains(e.CanvasLocation))
                {
                    return _window.RespondToMouseDown(sender, e);
                }
                HideWindow(fire: false);
                return GH_ObjectResponse.Release;
            }
            ShowWindow();
            return GH_ObjectResponse.Capture;
        }

        public void ShowWindow()
        {
            if (!expanded)
            {
                expanded = true;
                TopCollection.ActiveWidget = this;
                Update();
            }
        }

        public void HideWindow(bool fire)
        {
            if (expanded)
            {
                expanded = false;
                TopCollection.ActiveWidget = null;
                TopCollection.MakeAllInActive();
                if (fire && this.ValueChanged != null)
                {
                    this.ValueChanged(this, new EventArgs());
                }
            }
        }

        public override bool Contains(PointF pt)
        {
            return base.CanvasBounds.Contains(pt);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.CreateChunk("MenuDropDown", Index).SetInt32("ActiveItemIndex", current_value);
            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            GH_IReader gH_IReader = reader.FindChunk("MenuDropDown", Index);
            try
            {
                current_value = gH_IReader.GetInt32("ActiveItemIndex");
            }
            catch
            {
                current_value = default_item_index;
            }
            return true;
        }


        /// <summary>
		/// updates the refered variable to be same enum as the dropdown. it does NOT update the document
		/// </summary>
		/// <typeparam name="T">double, int or enum</typeparam>
		/// <param name="reference">Can be any field hosting an enum</param>
		public MenuDropDown SetDefault<T>(ref T reference) where T : IConvertible
        {
            if (typeof(T).IsEnum)
            {
                reference = (T)Enum.Parse(typeof(T), Item.name);
            }
            else if (typeof(T) == typeof(String))
            {
                reference = (T)(object)Item.name;
            }

            return this; // return allows to fluent interface

        }
    }
}