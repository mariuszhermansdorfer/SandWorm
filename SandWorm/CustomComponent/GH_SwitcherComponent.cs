using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino;

namespace SandWorm
{ 
public abstract class GH_SwitcherComponent : GH_Component
{
	protected EvaluationUnitManager evalUnits;

	protected EvaluationUnit activeUnit;

	protected RuntimeComponentData staticData;

	public RuntimeComponentData StaticData => staticData;

	public List<EvaluationUnit> EvalUnits => evalUnits.Units;

	public EvaluationUnit ActiveUnit => activeUnit;

	protected virtual string DefaultEvaluationUnit => null;

	public virtual string UnitMenuName => "Evaluation Units";

	public virtual string UnitMenuHeader => "Select evaluation unit";

	public virtual bool UnitlessExistence => false;

	protected internal GH_SwitcherComponent(string sName, string sAbbreviation, string sDescription, string sCategory, string sSubCategory)
		: base(sName, sAbbreviation, sDescription, sCategory, sSubCategory)
	{
		base.Phase = GH_SolutionPhase.Blank;
		SetupEvaluationUnits();
	}

	protected override void PostConstructor()
	{
		evalUnits = new EvaluationUnitManager(this);
		RegisterEvaluationUnits(evalUnits);
		base.PostConstructor();
		staticData = new RuntimeComponentData(this);
	}

	private void SetupEvaluationUnits()
	{
		if (activeUnit != null)
		{
			throw new ArgumentException("Invalid switcher state. No evaluation unit must be active at this point.");
		}
		EvaluationUnit evaluationUnit = GetUnit(DefaultEvaluationUnit);
		if (evaluationUnit == null && !UnitlessExistence)
		{
			if (EvalUnits.Count == 0)
			{
				throw new ArgumentException("Switcher has no evaluation units registered and UnitlessExistence is false.");
			}
			evaluationUnit = EvalUnits[0];
		}
		if (OnPingDocument() != null)
		{
			RhinoApp.WriteLine("Component belongs to a document at a stage where it should not belong to one.");
		}
		SwitchUnit(evaluationUnit, recompute: false, recordEvent: false);
	}

	public EvaluationUnit GetUnit(string name)
	{
		return evalUnits.GetUnit(name);
	}

	protected override void SolveInstance(IGH_DataAccess DA)
	{
		SolveInstance(DA, activeUnit);
	}

	protected abstract void SolveInstance(IGH_DataAccess DA, EvaluationUnit unit);

	public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
	{
		base.AppendAdditionalMenuItems(menu);
		if (evalUnits.Units.Count <= 0)
		{
			return;
		}
		GH_DocumentObject.Menu_AppendSeparator(menu);
		ToolStripMenuItem toolStripMenuItem = GH_DocumentObject.Menu_AppendItem(menu, "Units");
		foreach (EvaluationUnit unit in evalUnits.Units)
		{
			GH_DocumentObject.Menu_AppendItem(toolStripMenuItem.DropDown, unit.Name, Menu_ActivateUnit, null, enabled: true, unit.Active).Tag = unit;
		}
		GH_DocumentObject.Menu_AppendSeparator(menu);
	}

	private void Menu_ActivateUnit(object sender, EventArgs e)
	{
		try
		{
			EvaluationUnit evaluationUnit = (EvaluationUnit)((ToolStripMenuItem)sender).Tag;
			if (evaluationUnit != null)
			{
				SwitchUnit(evaluationUnit);
			}
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	private void SetReadState()
	{
		if (activeUnit != null)
		{
			staticData.UnregisterUnit(activeUnit);
			activeUnit.Active = false;
			activeUnit = null;
		}
		GH_Document gH_Document = OnPingDocument();
		if (gH_Document != null)
		{
			gH_Document.Modified();
			gH_Document.DestroyAttributeCache();
			gH_Document.DestroyObjectTable();
		}
		if (base.Attributes != null)
		{
			((GH_SwitcherComponentAttributes)base.Attributes).OnSwitchUnit();
		}
		Name = staticData.CachedName;
		NickName = staticData.CachedNickname;
		Description = staticData.CachedDescription;
		SetIconOverride(staticData.CachedIcon);
		if (base.Attributes != null)
		{
			base.Attributes.ExpireLayout();
		}
	}

	public void ClearUnit(bool recompute = true, bool recordEvent = true)
	{
		if (!UnitlessExistence)
		{
			return;
		}
		if (activeUnit != null)
		{
			if (recordEvent)
			{
				RecordUndoEvent("Switch Unit", new GH_SwitchAction(this, null));
			}
			staticData.UnregisterUnit(activeUnit);
			activeUnit.Active = false;
			activeUnit = null;
		}
		GH_Document gH_Document = OnPingDocument();
		if (gH_Document != null)
		{
			gH_Document.Modified();
			gH_Document.DestroyAttributeCache();
			gH_Document.DestroyObjectTable();
		}
		if (base.Attributes != null)
		{
			((GH_SwitcherComponentAttributes)base.Attributes).OnSwitchUnit();
		}
		Name = staticData.CachedName;
		NickName = staticData.CachedNickname;
		Description = staticData.CachedDescription;
		SetIconOverride(staticData.CachedIcon);
		if (base.Attributes != null)
		{
			base.Attributes.ExpireLayout();
		}
		if (recompute)
		{
			ExpireSolution(recompute: true);
		}
	}

	public virtual void SwitchUnit(string unitName, bool recompute = true, bool recordEvent = true)
	{
		EvaluationUnit unit = evalUnits.GetUnit(unitName);
		if (unit != null)
		{
			SwitchUnit(unit, recompute, recordEvent);
		}
	}

	protected virtual void SwitchUnit(EvaluationUnit unit, bool recompute = true, bool recordEvent = true)
	{
		if (unit != null && (activeUnit == null || activeUnit != unit))
		{
			if (recordEvent)
			{
				RecordUndoEvent("Switch Unit", new GH_SwitchAction(this, unit.Name));
			}
			if (activeUnit != null)
			{
				staticData.UnregisterUnit(activeUnit);
				activeUnit.Active = false;
				activeUnit = null;
			}
			staticData.RegisterUnit(unit);
			activeUnit = unit;
			activeUnit.Active = true;
			GH_Document gH_Document = OnPingDocument();
			if (gH_Document != null)
			{
				gH_Document.Modified();
				gH_Document.DestroyAttributeCache();
				gH_Document.DestroyObjectTable();
			}
			if (base.Attributes != null)
			{
				((GH_SwitcherComponentAttributes)base.Attributes).OnSwitchUnit();
			}
			if (unit.DisplayName != null)
			{
				SetIconOverride(unit.Icon);
			}
			if (base.Attributes != null)
			{
				base.Attributes.ExpireLayout();
			}
			if (recompute)
			{
				ExpireSolution(recompute: true);
			}
		}
	}

	protected virtual void RegisterEvaluationUnits(EvaluationUnitManager mngr)
	{
	}

	private void _Setup()
	{
		Setup((GH_SwitcherComponentAttributes)base.Attributes);
	}

	protected virtual void Setup(GH_SwitcherComponentAttributes attr)
	{
	}

	protected virtual void OnComponentLoaded()
	{
	}

	protected virtual void OnComponentReset(GH_ExtendableComponentAttributes attr)
	{
	}

	public override bool Write(GH_IWriter writer)
	{
		staticData.PrepareWrite(activeUnit);
		bool result = base.Write(writer);
		staticData.RestoreWrite(activeUnit);
		if (activeUnit != null)
		{
			writer.CreateChunk("ActiveUnit").SetString("unitname", activeUnit.Name);
		}
		try
		{
			GH_IWriter gH_IWriter = writer.CreateChunk("EvalUnits");
			gH_IWriter.SetInt32("count", evalUnits.Units.Count);
			for (int i = 0; i < evalUnits.Units.Count; i++)
			{
				EvaluationUnit evaluationUnit = evalUnits.Units[i];
				GH_IWriter writer2 = gH_IWriter.CreateChunk("unit", i);
				evaluationUnit.Write(writer2);
			}
			return result;
		}
		catch (Exception ex)
		{
			RhinoApp.WriteLine(ex.Message + "\n" + ex.StackTrace);
			throw ex;
		}
	}

	public override bool Read(GH_IReader reader)
	{
		bool flag = true;
		SetReadState();
		flag &= base.Read(reader);
		string text = null;
		if (reader.ChunkExists("ActiveUnit"))
		{
			text = reader.FindChunk("ActiveUnit").GetString("unitname");
		}
		if (reader.ChunkExists("EvalUnits"))
		{
			GH_IReader gH_IReader = reader.FindChunk("EvalUnits");
			int value = -1;
			if (gH_IReader.TryGetInt32("count", ref value))
			{
				for (int i = 0; i < value; i++)
				{
					if (gH_IReader.ChunkExists("unit", i))
					{
						GH_IReader gH_IReader2 = gH_IReader.FindChunk("unit", i);
						string @string = gH_IReader2.GetString("name");
						if (text != null)
						{
							@string.Equals(text);
						}
						evalUnits.GetUnit(@string)?.Read(gH_IReader2);
					}
				}
			}
		}
		if (text != null)
		{
			GetUnit(text);
			SwitchUnit(GetUnit(text), recompute: false, recordEvent: false);
		}
		for (int j = 0; j < EvalUnits.Count; j++)
		{
			if (!EvalUnits[j].Active)
			{
				EvalUnits[j].NewParameterIds();
			}
		}
		OnComponentLoaded();
		return flag;
	}

	public override void CreateAttributes()
	{
		Setup((GH_SwitcherComponentAttributes)(m_attributes = new GH_SwitcherComponentAttributes(this)));
	}
}
}