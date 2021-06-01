using System.Drawing;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;

namespace SandWorm
{
    public class MenuStaticText : GH_Attr_Widget
    {
        private string _text;
        


        /// <summary>
        /// The text shown as a label
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }


        public MenuStaticText()
            : base(0, "")
        {
        }

        public MenuStaticText(string label, string tooltip = "")
            : base(0, "")
        {
            Text = label;
            Header = tooltip;
        }

        public override bool Write(GH_IWriter writer)
        {
            return true;
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



        public override void PostUpdateBounds(out float outHeight)
        {
            outHeight = ComputeMinSize().Height;
        }

        public override SizeF ComputeMinSize()
        {
            if (Text == null)
            {
                return default;
            }
            Size size = TextRenderer.MeasureText(Text, WidgetServer.Instance.TextFont);
            return new SizeF(size.Width, size.Height);
        }


        public override void TooltipSetup(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
        {
            e.Icon = null;
            e.Title = Text.Replace("\n", "");
            e.Text = _header;
            if (_header != null)
            {
                e.Text += "\n";
            }

            e.Description = _description;
        }

        public override void Render(WidgetRenderArgs args)
        {
            GH_Canvas canvas = args.Canvas;
            if (Text != null)
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
                SolidBrush brush = new SolidBrush(Color.FromArgb(num, 45, 45, 45));
                PointF point = new PointF(base.CanvasPivot.X, base.CanvasPivot.Y);
                graphics.DrawString(_text, WidgetServer.Instance.MenuHeaderFont, brush, point);
            }
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            return GH_ObjectResponse.Release;
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            return GH_ObjectResponse.Ignore;
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            return GH_ObjectResponse.Capture;
        }
    }
}