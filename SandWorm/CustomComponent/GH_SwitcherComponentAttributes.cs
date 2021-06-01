using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Rhino;

namespace SandWorm
{
    public class GH_SwitcherComponentAttributes : GH_ComponentAttributes
    {
        private int offset;

        private float _minWidth;

        private GH_Attr_Widget _activeToolTip;

        protected GH_MenuCollection unitMenuCollection;

        protected GH_MenuCollection collection;

        private GH_MenuCollection composedCollection;

        private MenuDropDown _UnitDrop;

        public float MinWidth
        {
            get
            {
                return _minWidth;
            }
            set
            {
                _minWidth = value;
            }
        }

        public GH_SwitcherComponentAttributes(GH_SwitcherComponent component)
            : base(component)
        {
            collection = new GH_MenuCollection();
            composedCollection = new GH_MenuCollection();
            composedCollection.Merge(collection);
            CreateUnitDropDown();
            InitializeUnitParameters();
        }

        public void InitializeUnitParameters()
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)base.Owner;
            if (gH_SwitcherComponent.EvalUnits == null)
            {
                return;
            }
            foreach (EvaluationUnit evalUnit in gH_SwitcherComponent.EvalUnits)
            {
                foreach (ExtendedPlug input in evalUnit.Inputs)
                {
                    if (input.Parameter.Attributes == null)
                    {
                        input.Parameter.Attributes = new GH_LinkedParamAttributes(input.Parameter, this);
                    }
                }
                foreach (ExtendedPlug output in evalUnit.Outputs)
                {
                    if (output.Parameter.Attributes == null)
                    {
                        output.Parameter.Attributes = new GH_LinkedParamAttributes(output.Parameter, this);
                    }
                }
            }
        }

        private void ComposeMenu()
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)base.Owner;
            composedCollection = new GH_MenuCollection();
            EvaluationUnit activeUnit = gH_SwitcherComponent.ActiveUnit;
            if (activeUnit != null && activeUnit.Context.Collection != null)
            {
                composedCollection.Merge(gH_SwitcherComponent.ActiveUnit.Context.Collection);
            }
            if (collection != null)
            {
                composedCollection.Merge(collection);
            }
            if (unitMenuCollection != null)
            {
                composedCollection.Merge(unitMenuCollection);
            }
        }

        protected void CreateUnitDropDown()
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)base.Owner;
            if (gH_SwitcherComponent.EvalUnits != null && gH_SwitcherComponent.EvalUnits.Count != 0 && (gH_SwitcherComponent.EvalUnits.Count != 1 || gH_SwitcherComponent.UnitlessExistence))
            {
                MenuPanel menuPanel = new MenuPanel(0, "panel_units")
                {
                    Header = "Unit selection"
                };
                string text = gH_SwitcherComponent.UnitMenuName;
                if (text == null)
                {
                    text = "Evaluation Units";
                }
                string text2 = gH_SwitcherComponent.UnitMenuHeader;
                if (text2 == null)
                {
                    text2 = "Select evaluation unit";
                }
                unitMenuCollection = new GH_MenuCollection();
                GH_ExtendableMenu gH_ExtendableMenu = new GH_ExtendableMenu(0, "menu_units")
                {
                    Name = text,
                    Header = text2
                };
                gH_ExtendableMenu.AddControl(menuPanel);
                _UnitDrop = new MenuDropDown(0, "dropdown_units", "units")
                {
                    //VisibleItemCount = 10
                };
                _UnitDrop.ValueChanged += _UnitDrop__valueChanged;
                _UnitDrop.Header = "Evaluation unit selector";
                menuPanel.AddControl(_UnitDrop);
                List<EvaluationUnit> evalUnits = gH_SwitcherComponent.EvalUnits;
                if (gH_SwitcherComponent.UnitlessExistence)
                {
                    _UnitDrop.AddItem("--NONE--", null);
                }
                for (int i = 0; i < evalUnits.Count; i++)
                {
                    _UnitDrop.AddItem(evalUnits[i].Name, evalUnits[i].DisplayName, evalUnits[i]);
                }
                gH_ExtendableMenu.Expand();
                unitMenuCollection.AddMenu(gH_ExtendableMenu);
            }
        }

        private void _UnitDrop__valueChanged(object sender, EventArgs e)
        {
            try
            {
                MenuDropDown menuDropDown = (MenuDropDown)sender;
                MenuDropDown.Entry entry = menuDropDown.Items[menuDropDown.Value];
                if (entry.data != null)
                {
                    EvaluationUnit evaluationUnit = (EvaluationUnit)entry.data;
                    ((GH_SwitcherComponent)base.Owner).SwitchUnit(evaluationUnit.Name);
                }
                else
                {
                    ((GH_SwitcherComponent)base.Owner).ClearUnit();
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Error selection:" + ex.StackTrace);
            }
        }

        public void OnSwitchUnit()
        {
            EvaluationUnit activeUnit = ((GH_SwitcherComponent)base.Owner).ActiveUnit;
            ComposeMenu();
            if (activeUnit != null)
            {
                if (_UnitDrop != null)
                {
                    int num = _UnitDrop.FindIndex(activeUnit.Name);
                    if (num != -1)
                    {
                        _UnitDrop.Value = num;
                    }
                }
            }
            else if (((GH_SwitcherComponent)base.Owner).UnitlessExistence && _UnitDrop != null)
            {
                _UnitDrop.Value = 0;
            }
        }

        public void AddMenu(GH_ExtendableMenu menu)
        {
            collection.AddMenu(menu);
        }

        public override bool Write(GH_IWriter writer)
        {
            try
            {
                collection.Write(writer);
            }
            catch (Exception)
            {
            }
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            try
            {
                collection.Read(reader);
            }
            catch (Exception)
            {
            }
            return base.Read(reader);
        }

        protected override void PrepareForRender(GH_Canvas canvas)
        {
            base.PrepareForRender(canvas);
            LayoutMenuCollection();
        }

        protected void LayoutBaseComponent()
        {
            _ = (GH_SwitcherComponent)base.Owner;
            Pivot = GH_Convert.ToPoint(Pivot);
            m_innerBounds = LayoutComponentBox2(base.Owner);
            int num = ComputeW_ico(base.Owner);
            float width = composedCollection.GetMinLayoutSize().Width;
            float num2 = Math.Max(MinWidth, width);
            int add_offset = 0;
            if (num2 > (float)num)
            {
                add_offset = (int)((double)(num2 - (float)num) / 2.0);
            }
            LayoutInputParams2(base.Owner, m_innerBounds, add_offset);
            LayoutOutputParams2(base.Owner, m_innerBounds, add_offset);
            Bounds = LayoutBounds2(base.Owner, m_innerBounds);
        }

        private int ComputeW_ico(IGH_Component owner)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)owner;
            int num = 24;
            int num2 = 0;
            int num3 = 0;
            foreach (IGH_Param componentInput in gH_SwitcherComponent.StaticData.GetComponentInputs())
            {
                int val = componentInput.StateTags.Count * 20;
                num3 = Math.Max(num3, val);
                num2 = Math.Max(num2, GH_FontServer.StringWidth(componentInput.NickName, StandardFont.Font()));
            }
            num2 = Math.Max(num2 + 6, 12);
            num2 += num3;
            int num4 = 0;
            int num5 = 0;
            foreach (IGH_Param componentOutput in gH_SwitcherComponent.StaticData.GetComponentOutputs())
            {
                int val2 = componentOutput.StateTags.Count * 20;
                num5 = Math.Max(num5, val2);
                num4 = Math.Max(num4, GH_FontServer.StringWidth(componentOutput.NickName, StandardFont.Font()));
            }
            num4 = Math.Max(num4 + 6, 12);
            num4 += num5;
            return num2 + num + num4 + 6;
        }

        public RectangleF LayoutBounds2(IGH_Component owner, RectangleF bounds)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)owner;
            foreach (IGH_Param item in gH_SwitcherComponent.StaticData.GetComponentInputSection())
            {
                bounds = RectangleF.Union(bounds, item.Attributes.Bounds);
            }
            foreach (IGH_Param item2 in gH_SwitcherComponent.StaticData.GetComponentOutputSection())
            {
                bounds = RectangleF.Union(bounds, item2.Attributes.Bounds);
            }
            bounds.Inflate(2f, 2f);
            return bounds;
        }

        public RectangleF LayoutComponentBox2(IGH_Component owner)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)owner;
            int val = Math.Max(gH_SwitcherComponent.StaticData.GetComponentInputSection().Count, gH_SwitcherComponent.StaticData.GetComponentOutputSection().Count) * 20;
            val = Math.Max(val, 24);
            int num = 24;
            if (!GH_Attributes<IGH_Component>.IsIconMode(owner.IconDisplayMode))
            {
                val = Math.Max(val, GH_Convert.ToSize(GH_FontServer.MeasureString(owner.NickName, StandardFont.LargeFont())).Width + 6);
            }
            return GH_Convert.ToRectangle(new RectangleF(owner.Attributes.Pivot.X - 0.5f * (float)num, owner.Attributes.Pivot.Y - 0.5f * (float)val, num, val));
        }

        public void LayoutInputParams2(IGH_Component owner, RectangleF componentBox, int add_offset)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)owner;
            List<IGH_Param> componentInputSection = gH_SwitcherComponent.StaticData.GetComponentInputSection();
            int count = componentInputSection.Count;
            if (count == 0)
            {
                return;
            }
            int num = 0;
            int num2 = 0;
            foreach (IGH_Param componentInput in gH_SwitcherComponent.StaticData.GetComponentInputs())
            {
                int val = componentInput.StateTags.Count * 20;
                num2 = Math.Max(num2, val);
                num = Math.Max(num, GH_FontServer.StringWidth(componentInput.NickName, StandardFont.Font()));
            }
            num = Math.Max(num + 6, 12);
            num += num2 + add_offset;
            float num3 = componentBox.Height / (float)count;
            for (int i = 0; i < count; i++)
            {
                IGH_Param iGH_Param = componentInputSection[i];
                if (iGH_Param.Attributes == null)
                {
                    iGH_Param.Attributes = new GH_LinkedParamAttributes(iGH_Param, owner.Attributes);
                }
                float num4 = componentBox.X - (float)num;
                float num5 = componentBox.Y + (float)i * num3;
                float width = num - 3;
                float height = num3;
                PointF pivot = new PointF(num4 + 0.5f * (float)num, num5 + 0.5f * num3);
                iGH_Param.Attributes.Pivot = pivot;
                RectangleF @in = new RectangleF(num4, num5, width, height);
                iGH_Param.Attributes.Bounds = GH_Convert.ToRectangle(@in);
            }
            bool flag = false;
            for (int j = 0; j < count; j++)
            {
                IGH_Param iGH_Param2 = componentInputSection[j];
                GH_LinkedParamAttributes gH_LinkedParamAttributes = (GH_LinkedParamAttributes)iGH_Param2.Attributes;
                FieldInfo field = typeof(GH_LinkedParamAttributes).GetField("m_renderTags", BindingFlags.Instance | BindingFlags.NonPublic);
                GH_StateTagList gH_StateTagList = iGH_Param2.StateTags;
                if (!(field != null))
                {
                    continue;
                }
                if (gH_StateTagList.Count == 0)
                {
                    gH_StateTagList = null;
                    field.SetValue(gH_LinkedParamAttributes, gH_StateTagList);
                }
                if (gH_StateTagList != null)
                {
                    flag = true;
                    Rectangle box = GH_Convert.ToRectangle(gH_LinkedParamAttributes.Bounds);
                    box.X += num2;
                    box.Width -= num2;
                    gH_StateTagList.Layout(box, GH_StateTagLayoutDirection.Left);
                    box = gH_StateTagList.BoundingBox;
                    if (!box.IsEmpty)
                    {
                        gH_LinkedParamAttributes.Bounds = RectangleF.Union(gH_LinkedParamAttributes.Bounds, box);
                    }
                    field.SetValue(gH_LinkedParamAttributes, gH_StateTagList);
                }
            }
            if (flag)
            {
                float num6 = float.MaxValue;
                for (int k = 0; k < count; k++)
                {
                    IGH_Attributes attributes = componentInputSection[k].Attributes;
                    num6 = Math.Min(num6, attributes.Bounds.X);
                }
                for (int l = 0; l < count; l++)
                {
                    IGH_Attributes attributes2 = componentInputSection[l].Attributes;
                    RectangleF bounds = attributes2.Bounds;
                    bounds.Width = bounds.Right - num6;
                    bounds.X = num6;
                    attributes2.Bounds = bounds;
                }
            }
        }

        public void LayoutOutputParams2(IGH_Component owner, RectangleF componentBox, int add_offset)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)owner;
            List<IGH_Param> componentOutputSection = gH_SwitcherComponent.StaticData.GetComponentOutputSection();
            int count = componentOutputSection.Count;
            if (count == 0)
            {
                return;
            }
            int num = 0;
            int num2 = 0;
            foreach (IGH_Param componentOutput in gH_SwitcherComponent.StaticData.GetComponentOutputs())
            {
                int val = componentOutput.StateTags.Count * 20;
                num2 = Math.Max(num2, val);
                num = Math.Max(num, GH_FontServer.StringWidth(componentOutput.NickName, StandardFont.Font()));
            }
            num = Math.Max(num + 6, 12);
            num += num2 + add_offset;
            float num3 = componentBox.Height / (float)count;
            for (int i = 0; i < count; i++)
            {
                IGH_Param iGH_Param = componentOutputSection[i];
                if (iGH_Param.Attributes == null)
                {
                    iGH_Param.Attributes = new GH_LinkedParamAttributes(iGH_Param, owner.Attributes);
                }
                float num4 = componentBox.Right + 3f;
                float num5 = componentBox.Y + (float)i * num3;
                float width = num;
                float height = num3;
                PointF pivot = new PointF(num4 + 0.5f * (float)num, num5 + 0.5f * num3);
                iGH_Param.Attributes.Pivot = pivot;
                RectangleF @in = new RectangleF(num4, num5, width, height);
                iGH_Param.Attributes.Bounds = GH_Convert.ToRectangle(@in);
            }
            bool flag = false;
            for (int j = 0; j < count; j++)
            {
                IGH_Param iGH_Param2 = componentOutputSection[j];
                GH_LinkedParamAttributes gH_LinkedParamAttributes = (GH_LinkedParamAttributes)iGH_Param2.Attributes;
                FieldInfo field = typeof(GH_LinkedParamAttributes).GetField("m_renderTags", BindingFlags.Instance | BindingFlags.NonPublic);
                GH_StateTagList gH_StateTagList = iGH_Param2.StateTags;
                if (!(field != null))
                {
                    continue;
                }
                if (gH_StateTagList.Count == 0)
                {
                    gH_StateTagList = null;
                    field.SetValue(gH_LinkedParamAttributes, gH_StateTagList);
                }
                if (gH_StateTagList != null)
                {
                    flag = true;
                    Rectangle box = GH_Convert.ToRectangle(gH_LinkedParamAttributes.Bounds);
                    box.Width -= num2;
                    gH_StateTagList.Layout(box, GH_StateTagLayoutDirection.Right);
                    box = gH_StateTagList.BoundingBox;
                    if (!box.IsEmpty)
                    {
                        gH_LinkedParamAttributes.Bounds = RectangleF.Union(gH_LinkedParamAttributes.Bounds, box);
                    }
                    field.SetValue(gH_LinkedParamAttributes, gH_StateTagList);
                }
            }
            if (flag)
            {
                float num6 = float.MinValue;
                for (int k = 0; k < count; k++)
                {
                    IGH_Attributes attributes = componentOutputSection[k].Attributes;
                    num6 = Math.Max(num6, attributes.Bounds.Right);
                }
                for (int l = 0; l < count; l++)
                {
                    IGH_Attributes attributes2 = componentOutputSection[l].Attributes;
                    RectangleF bounds = attributes2.Bounds;
                    bounds.Width = num6 - bounds.X;
                    attributes2.Bounds = bounds;
                }
            }
        }

        protected override void Layout()
        {
            Pivot = GH_Convert.ToPoint(Pivot);
            LayoutBaseComponent();
            _ = (GH_SwitcherComponent)base.Owner;
            List<ExtendedPlug> inputs = new List<ExtendedPlug>();
            List<ExtendedPlug> outputs = new List<ExtendedPlug>();
            composedCollection.GetMenuPlugs(ref inputs, ref outputs, onlyVisible: true);
            LayoutMenuInputs(m_innerBounds);
            LayoutMenuOutputs(m_innerBounds);
            Bounds = LayoutExtBounds(m_innerBounds, inputs, outputs);
            FixLayout(outputs);
            LayoutMenu();
        }

        public RectangleF LayoutExtBounds(RectangleF bounds, List<ExtendedPlug> ins, List<ExtendedPlug> outs)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)base.Owner;
            foreach (IGH_Param componentInput in gH_SwitcherComponent.StaticData.GetComponentInputs())
            {
                RectangleF bounds2 = componentInput.Attributes.Bounds;
                if (bounds2.X < bounds.X)
                {
                    float num = bounds.X - bounds2.X;
                    bounds.X = bounds2.X;
                    bounds.Width += num;
                }
                if (bounds2.X + bounds2.Width > bounds.X + bounds.Width)
                {
                    float num2 = bounds2.X + bounds2.Width - (bounds.X + bounds.Width);
                    bounds.Width += num2;
                }
            }
            foreach (IGH_Param componentOutput in gH_SwitcherComponent.StaticData.GetComponentOutputs())
            {
                RectangleF bounds3 = componentOutput.Attributes.Bounds;
                if (bounds3.X < bounds.X)
                {
                    float num3 = bounds.X - bounds3.X;
                    bounds.X = bounds3.X;
                    bounds.Width += num3;
                }
                if (bounds3.X + bounds3.Width > bounds.X + bounds.Width)
                {
                    float num4 = bounds3.X + bounds3.Width - (bounds.X + bounds.Width);
                    bounds.Width += num4;
                }
            }
            bounds.Inflate(2f, 2f);
            return bounds;
        }

        public void LayoutMenuInputs(RectangleF componentBox)
        {
            GH_SwitcherComponent obj = (GH_SwitcherComponent)base.Owner;
            float num = 0f;
            int num2 = 0;
            foreach (IGH_Param componentInput in obj.StaticData.GetComponentInputs())
            {
                int val = 20 * componentInput.StateTags.Count;
                num2 = Math.Max(num2, val);
                num = Math.Max(num, GH_FontServer.StringWidth(componentInput.NickName, StandardFont.Font()));
            }
            num = Math.Max(num + 6f, 12f);
            num += (float)num2;
            float num3 = Bounds.Height;
            for (int i = 0; i < composedCollection.Menus.Count; i++)
            {
                float num4 = -1f;
                float num5 = 0f;
                bool expanded = composedCollection.Menus[i].Expanded;
                if (expanded)
                {
                    num4 = num3 + composedCollection.Menus[i].Height;
                    num5 = Math.Max(composedCollection.Menus[i].Inputs.Count, composedCollection.Menus[i].Outputs.Count) * 20;
                }
                else
                {
                    num4 = num3 + 5f;
                    num5 = 0f;
                }
                List<ExtendedPlug> inputs = composedCollection.Menus[i].Inputs;
                int count = inputs.Count;
                if (count != 0)
                {
                    float num6 = num5 / (float)count;
                    for (int j = 0; j < count; j++)
                    {
                        IGH_Param parameter = inputs[j].Parameter;
                        if (parameter.Attributes == null)
                        {
                            parameter.Attributes = new GH_LinkedParamAttributes(parameter, this);
                        }
                        float num7 = componentBox.X - num;
                        float num8 = num4 + componentBox.Y + (float)j * num6;
                        float width = num - 3f;
                        float height = num6;
                        PointF pivot = new PointF(num7 + 0.5f * num, num8 + 0.5f * num6);
                        parameter.Attributes.Pivot = pivot;
                        RectangleF @in = new RectangleF(num7, num8, width, height);
                        parameter.Attributes.Bounds = GH_Convert.ToRectangle(@in);
                    }
                    for (int k = 0; k < count; k++)
                    {
                        IGH_Param parameter2 = inputs[k].Parameter;
                        GH_LinkedParamAttributes gH_LinkedParamAttributes = (GH_LinkedParamAttributes)parameter2.Attributes;
                        FieldInfo field = typeof(GH_LinkedParamAttributes).GetField("m_renderTags", BindingFlags.Instance | BindingFlags.NonPublic);
                        GH_StateTagList gH_StateTagList = parameter2.StateTags;
                        if (field != null)
                        {
                            if (gH_StateTagList.Count == 0)
                            {
                                gH_StateTagList = null;
                                field.SetValue(gH_LinkedParamAttributes, gH_StateTagList);
                            }
                            if (gH_StateTagList != null)
                            {
                                Rectangle box = GH_Convert.ToRectangle(gH_LinkedParamAttributes.Bounds);
                                box.X += num2;
                                box.Width -= num2;
                                gH_StateTagList.Layout(box, GH_StateTagLayoutDirection.Left);
                                box = gH_StateTagList.BoundingBox;
                                if (!box.IsEmpty)
                                {
                                    gH_LinkedParamAttributes.Bounds = RectangleF.Union(gH_LinkedParamAttributes.Bounds, box);
                                }
                                field.SetValue(gH_LinkedParamAttributes, gH_StateTagList);
                            }
                        }
                        if (!expanded)
                        {
                            gH_LinkedParamAttributes.Bounds = new RectangleF(gH_LinkedParamAttributes.Bounds.X, gH_LinkedParamAttributes.Bounds.Y, 5f, gH_LinkedParamAttributes.Bounds.Height);
                        }
                    }
                }
                num3 += composedCollection.Menus[i].TotalHeight;
            }
        }

        public void LayoutMenuOutputs(RectangleF componentBox)
        {
            GH_SwitcherComponent obj = (GH_SwitcherComponent)base.Owner;
            float num = 0f;
            int num2 = 0;
            foreach (IGH_Param componentOutput in obj.StaticData.GetComponentOutputs())
            {
                int val = 20 * componentOutput.StateTags.Count;
                num2 = Math.Max(num2, val);
                num = Math.Max(num, GH_FontServer.StringWidth(componentOutput.NickName, StandardFont.Font()));
            }
            num = Math.Max(num + 6f, 12f);
            num += (float)num2;
            float num3 = Bounds.Height;
            for (int i = 0; i < composedCollection.Menus.Count; i++)
            {
                float num4 = -1f;
                float num5 = 0f;
                bool expanded = composedCollection.Menus[i].Expanded;
                if (expanded)
                {
                    num4 = num3 + composedCollection.Menus[i].Height;
                    num5 = Math.Max(composedCollection.Menus[i].Inputs.Count, composedCollection.Menus[i].Outputs.Count) * 20;
                }
                else
                {
                    num4 = num3 + 5f;
                    num5 = 0f;
                }
                List<ExtendedPlug> outputs = composedCollection.Menus[i].Outputs;
                int count = outputs.Count;
                if (count != 0)
                {
                    float num6 = num5 / (float)count;
                    for (int j = 0; j < count; j++)
                    {
                        IGH_Param parameter = outputs[j].Parameter;
                        if (parameter.Attributes == null)
                        {
                            parameter.Attributes = new GH_LinkedParamAttributes(parameter, this);
                        }
                        float num7 = componentBox.Right + 3f;
                        float num8 = num4 + componentBox.Y + (float)j * num6;
                        float width = num;
                        float height = num6;
                        PointF pivot = new PointF(num7 + 0.5f * num, num8 + 0.5f * num6);
                        parameter.Attributes.Pivot = pivot;
                        RectangleF @in = new RectangleF(num7, num8, width, height);
                        parameter.Attributes.Bounds = GH_Convert.ToRectangle(@in);
                    }
                    for (int k = 0; k < count; k++)
                    {
                        IGH_Param parameter2 = outputs[k].Parameter;
                        GH_LinkedParamAttributes gH_LinkedParamAttributes = (GH_LinkedParamAttributes)parameter2.Attributes;
                        FieldInfo field = typeof(GH_LinkedParamAttributes).GetField("m_renderTags", BindingFlags.Instance | BindingFlags.NonPublic);
                        GH_StateTagList gH_StateTagList = parameter2.StateTags;
                        if (field != null)
                        {
                            if (gH_StateTagList.Count == 0)
                            {
                                gH_StateTagList = null;
                                field.SetValue(gH_LinkedParamAttributes, gH_StateTagList);
                            }
                            if (gH_StateTagList != null)
                            {
                                Rectangle box = GH_Convert.ToRectangle(gH_LinkedParamAttributes.Bounds);
                                box.Width -= num2;
                                gH_StateTagList.Layout(box, GH_StateTagLayoutDirection.Right);
                                box = gH_StateTagList.BoundingBox;
                                if (!box.IsEmpty)
                                {
                                    gH_LinkedParamAttributes.Bounds = RectangleF.Union(gH_LinkedParamAttributes.Bounds, box);
                                }
                                field.SetValue(gH_LinkedParamAttributes, gH_StateTagList);
                            }
                        }
                        if (!expanded)
                        {
                            gH_LinkedParamAttributes.Bounds = new RectangleF(gH_LinkedParamAttributes.Bounds.X + gH_LinkedParamAttributes.Bounds.Width - 5f, gH_LinkedParamAttributes.Bounds.Y, 5f, gH_LinkedParamAttributes.Bounds.Height);
                        }
                    }
                }
                num3 += composedCollection.Menus[i].TotalHeight;
            }
        }

        protected void FixLayout(List<ExtendedPlug> outs)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)base.Owner;
            float width = Bounds.Width;
            if (_minWidth > width)
            {
                Bounds = new RectangleF(Bounds.X, Bounds.Y, _minWidth, Bounds.Height);
            }
            float num = Bounds.Width - width;
            foreach (IGH_Param componentOutput in gH_SwitcherComponent.StaticData.GetComponentOutputs())
            {
                PointF pivot = componentOutput.Attributes.Pivot;
                RectangleF bounds = componentOutput.Attributes.Bounds;
                componentOutput.Attributes.Pivot = new PointF(pivot.X + num, pivot.Y);
                componentOutput.Attributes.Bounds = new RectangleF(bounds.Location.X + num, bounds.Location.Y, bounds.Width, bounds.Height);
            }
            foreach (IGH_Param componentInput in gH_SwitcherComponent.StaticData.GetComponentInputs())
            {
                PointF pivot2 = componentInput.Attributes.Pivot;
                RectangleF bounds2 = componentInput.Attributes.Bounds;
                componentInput.Attributes.Pivot = new PointF(pivot2.X + num, pivot2.Y);
                componentInput.Attributes.Bounds = new RectangleF(bounds2.Location.X + num, bounds2.Location.Y, bounds2.Width, bounds2.Height);
            }
        }

        private void LayoutMenuCollection()
        {
            GH_Palette impliedPalette = GH_CapsuleRenderEngine.GetImpliedPalette(base.Owner);
            GH_PaletteStyle impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(impliedPalette, Selected, base.Owner.Locked, base.Owner.Hidden);
            composedCollection.Style = impliedStyle;
            composedCollection.Palette = impliedPalette;
            composedCollection.Layout();
        }

        protected void LayoutMenu()
        {
            offset = (int)Bounds.Height;
            composedCollection.Pivot = new PointF(Bounds.X, (int)Bounds.Y + offset);
            composedCollection.Width = Bounds.Width;
            LayoutMenuCollection();
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + composedCollection.Height);
        }

        protected override void Render(GH_Canvas iCanvas, Graphics graph, GH_CanvasChannel iChannel)
        {
            if (iChannel == GH_CanvasChannel.First)
            {
                iCanvas.CanvasPostPaintWidgets -= RenderPostWidgets;
                iCanvas.CanvasPostPaintWidgets += RenderPostWidgets;
            }
            switch (iChannel)
            {
                default:
                    _ = 30;
                    break;
                case GH_CanvasChannel.Wires:
                    foreach (IGH_Param item in base.Owner.Params.Input)
                    {
                        item.Attributes.RenderToCanvas(iCanvas, GH_CanvasChannel.Wires);
                    }
                    break;
                case GH_CanvasChannel.Objects:
                    RenderComponentCapsule2(iCanvas, graph);
                    composedCollection.Render(new WidgetRenderArgs(iCanvas, WidgetChannel.Object));
                    break;
            }
        }

        private void RenderPostWidgets(GH_Canvas sender)
        {
            composedCollection.Render(new WidgetRenderArgs(sender, WidgetChannel.Overlay));
        }

        protected void RenderComponentCapsule2(GH_Canvas canvas, Graphics graphics)
        {
            RenderComponentCapsule2(canvas, graphics, drawComponentBaseBox: true, drawComponentNameBox: true, drawJaggedEdges: true, drawParameterGrips: true, drawParameterNames: true, drawZuiElements: true);
        }

        protected void RenderComponentCapsule2(GH_Canvas canvas, Graphics graphics, bool drawComponentBaseBox, bool drawComponentNameBox, bool drawJaggedEdges, bool drawParameterGrips, bool drawParameterNames, bool drawZuiElements)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)base.Owner;
            RectangleF rec = (Bounds = Bounds);
            if (!canvas.Viewport.IsVisible(ref rec, 10f))
            {
                return;
            }
            GH_Palette gH_Palette = GH_CapsuleRenderEngine.GetImpliedPalette(base.Owner);
            if (gH_Palette == GH_Palette.Normal && !base.Owner.IsPreviewCapable)
            {
                gH_Palette = GH_Palette.Hidden;
            }
            GH_Capsule gH_Capsule = GH_Capsule.CreateCapsule(Bounds, gH_Palette);
            bool left = base.Owner.Params.Input.Count == 0;
            bool right = base.Owner.Params.Output.Count == 0;
            gH_Capsule.SetJaggedEdges(left, right);
            GH_PaletteStyle impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(gH_Palette, Selected, base.Owner.Locked, base.Owner.Hidden);
            if (drawParameterGrips)
            {
                foreach (IGH_Param staticInput in gH_SwitcherComponent.StaticData.StaticInputs)
                {
                    gH_Capsule.AddInputGrip(staticInput.Attributes.InputGrip.Y);
                }
                foreach (IGH_Param dynamicInput in gH_SwitcherComponent.StaticData.DynamicInputs)
                {
                    gH_Capsule.AddInputGrip(dynamicInput.Attributes.InputGrip.Y);
                }
                foreach (IGH_Param staticOutput in gH_SwitcherComponent.StaticData.StaticOutputs)
                {
                    gH_Capsule.AddOutputGrip(staticOutput.Attributes.OutputGrip.Y);
                }
                foreach (IGH_Param dynamicOutput in gH_SwitcherComponent.StaticData.DynamicOutputs)
                {
                    gH_Capsule.AddOutputGrip(dynamicOutput.Attributes.OutputGrip.Y);
                }
            }
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            canvas.SetSmartTextRenderingHint();
            if (GH_Attributes<IGH_Component>.IsIconMode(base.Owner.IconDisplayMode))
            {
                if (drawComponentBaseBox)
                {
                    if (base.Owner.Message != null)
                    {
                        gH_Capsule.RenderEngine.RenderMessage(graphics, base.Owner.Message, impliedStyle);
                    }
                    gH_Capsule.Render(graphics, impliedStyle);
                }
                if (drawComponentNameBox && base.Owner.Icon_24x24 != null)
                {
                    if (base.Owner.Locked)
                    {
                        gH_Capsule.RenderEngine.RenderIcon(graphics, base.Owner.Icon_24x24_Locked, m_innerBounds);
                    }
                    else
                    {
                        gH_Capsule.RenderEngine.RenderIcon(graphics, base.Owner.Icon_24x24, m_innerBounds);
                    }
                }
            }
            else
            {
                if (drawComponentBaseBox)
                {
                    if (base.Owner.Message != null)
                    {
                        gH_Capsule.RenderEngine.RenderMessage(graphics, base.Owner.Message, impliedStyle);
                    }
                    gH_Capsule.Render(graphics, impliedStyle);
                }
                if (drawComponentNameBox)
                {
                    GH_Capsule gH_Capsule2 = GH_Capsule.CreateTextCapsule(m_innerBounds, m_innerBounds, GH_Palette.Black, base.Owner.NickName, StandardFont.LargeFont(), GH_Orientation.vertical_center, 3, 6);
                    gH_Capsule2.Render(graphics, Selected, base.Owner.Locked, hidden: false);
                    gH_Capsule2.Dispose();
                }
            }
            if (drawComponentNameBox && base.Owner.Obsolete && CentralSettings.CanvasObsoleteTags && canvas.DrawingMode == GH_CanvasMode.Control)
            {
                GH_GraphicsUtil.RenderObjectOverlay(graphics, base.Owner, m_innerBounds);
            }
            if (drawParameterNames)
            {
                RenderComponentParameters2(canvas, graphics, base.Owner, impliedStyle);
            }
            if (drawZuiElements)
            {
                RenderVariableParameterUI(canvas, graphics);
            }
            gH_Capsule.Dispose();
        }

        public static void RenderComponentParameters2(GH_Canvas canvas, Graphics graphics, IGH_Component owner, GH_PaletteStyle style)
        {
            GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)owner;
            int zoomFadeLow = GH_Canvas.ZoomFadeLow;
            if (zoomFadeLow < 5)
            {
                return;
            }
            StringFormat farCenter = GH_TextRenderingConstants.FarCenter;
            canvas.SetSmartTextRenderingHint();
            SolidBrush solidBrush = new SolidBrush(Color.FromArgb(zoomFadeLow, style.Text));
            foreach (IGH_Param staticInput in gH_SwitcherComponent.StaticData.StaticInputs)
            {
                RectangleF bounds = staticInput.Attributes.Bounds;
                if (bounds.Width >= 1f)
                {
                    graphics.DrawString(staticInput.NickName, StandardFont.Font(), solidBrush, bounds, farCenter);
                    GH_LinkedParamAttributes obj = (GH_LinkedParamAttributes)staticInput.Attributes;
                    ((GH_StateTagList)typeof(GH_LinkedParamAttributes).GetField("m_renderTags", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj))?.RenderStateTags(graphics);
                }
            }
            farCenter = GH_TextRenderingConstants.NearCenter;
            foreach (IGH_Param staticOutput in gH_SwitcherComponent.StaticData.StaticOutputs)
            {
                RectangleF bounds2 = staticOutput.Attributes.Bounds;
                if (bounds2.Width >= 1f)
                {
                    graphics.DrawString(staticOutput.NickName, StandardFont.Font(), solidBrush, bounds2, farCenter);
                    GH_LinkedParamAttributes obj2 = (GH_LinkedParamAttributes)staticOutput.Attributes;
                    ((GH_StateTagList)typeof(GH_LinkedParamAttributes).GetField("m_renderTags", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj2))?.RenderStateTags(graphics);
                }
            }
            solidBrush.Dispose();
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            GH_ObjectResponse gH_ObjectResponse = composedCollection.RespondToMouseUp(sender, e);
            switch (gH_ObjectResponse)
            {
                case GH_ObjectResponse.Capture:
                    ExpireLayout();
                    sender.Invalidate();
                    return gH_ObjectResponse;
                default:
                    ExpireLayout();
                    sender.Invalidate();
                    return GH_ObjectResponse.Release;
                case GH_ObjectResponse.Ignore:
                    return base.RespondToMouseUp(sender, e);
            }
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            GH_ObjectResponse gH_ObjectResponse = composedCollection.RespondToMouseDoubleClick(sender, e);
            if (gH_ObjectResponse != 0)
            {
                ExpireLayout();
                sender.Refresh();
                return gH_ObjectResponse;
            }
            return base.RespondToMouseDoubleClick(sender, e);
        }

        public override GH_ObjectResponse RespondToKeyDown(GH_Canvas sender, KeyEventArgs e)
        {
            GH_ObjectResponse gH_ObjectResponse = composedCollection.RespondToKeyDown(sender, e);
            if (gH_ObjectResponse != 0)
            {
                ExpireLayout();
                sender.Refresh();
                return gH_ObjectResponse;
            }
            return base.RespondToKeyDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            GH_ObjectResponse gH_ObjectResponse = composedCollection.RespondToMouseMove(sender, e);
            if (gH_ObjectResponse != 0)
            {
                ExpireLayout();
                sender.Refresh();
                return gH_ObjectResponse;
            }
            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            GH_ObjectResponse gH_ObjectResponse = composedCollection.RespondToMouseDown(sender, e);
            if (gH_ObjectResponse != 0)
            {
                ExpireLayout();
                sender.Refresh();
                return gH_ObjectResponse;
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override bool IsTooltipRegion(PointF pt)
        {
            _activeToolTip = null;
            bool flag = base.IsTooltipRegion(pt);
            if (flag)
            {
                return flag;
            }
            if (m_innerBounds.Contains(pt))
            {
                GH_Attr_Widget gH_Attr_Widget = collection.IsTtipPoint(pt);
                if (gH_Attr_Widget != null)
                {
                    _activeToolTip = gH_Attr_Widget;
                    return true;
                }
            }
            return false;
        }

        public bool GetActiveTooltip(PointF pt)
        {
            GH_Attr_Widget gH_Attr_Widget = composedCollection.IsTtipPoint(pt);
            if (gH_Attr_Widget != null)
            {
                _activeToolTip = gH_Attr_Widget;
                return true;
            }
            return false;
        }

        public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
        {
            GetActiveTooltip(canvasPoint);
            if (_activeToolTip != null)
            {
                _activeToolTip.TooltipSetup(canvasPoint, e);
                return;
            }
            e.Title = PathName;
            e.Text = base.Owner.Description;
            e.Description = base.Owner.InstanceDescription;
            e.Icon = base.Owner.Icon_24x24;
            if (base.Owner is IGH_Param)
            {
                IGH_Param obj = (IGH_Param)base.Owner;
                string text = obj.TypeName;
                if (obj.Access == GH_ParamAccess.list)
                {
                    text += "[…]";
                }
                if (obj.Access == GH_ParamAccess.tree)
                {
                    text += "{…;…;…}";
                }
                e.Title = $"{PathName} ({text})";
            }
        }
    }
}