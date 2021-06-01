using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;

namespace SandWorm
{ 
public class GH_SwitchAction : GH_UndoAction
{
	private class GH_SwitcherConnectivity
	{
		private Guid componentId;

		private bool noUnit;

		private string unit;

		private List<GH_SwitcherParamState> inputs;

		private List<GH_SwitcherParamState> outputs;

		public string Unit
		{
			get
			{
				return unit;
			}
			set
			{
				unit = value;
			}
		}

		public List<GH_SwitcherParamState> Inputs
		{
			get
			{
				return inputs;
			}
			set
			{
				inputs = value;
			}
		}

		public List<GH_SwitcherParamState> Outputs
		{
			get
			{
				return outputs;
			}
			set
			{
				outputs = value;
			}
		}

		public Guid ComponentId => componentId;

		public GH_SwitcherConnectivity()
		{
			inputs = new List<GH_SwitcherParamState>();
			outputs = new List<GH_SwitcherParamState>();
		}

		public static GH_SwitcherConnectivity Create(GH_SwitcherComponent component)
		{
			GH_SwitcherConnectivity gH_SwitcherConnectivity = new GH_SwitcherConnectivity();
			gH_SwitcherConnectivity.componentId = component.InstanceGuid;
			EvaluationUnit activeUnit = component.ActiveUnit;
			if (activeUnit != null)
			{
				gH_SwitcherConnectivity.noUnit = false;
				gH_SwitcherConnectivity.unit = activeUnit.Name;
			}
			else if (component.UnitlessExistence)
			{
				gH_SwitcherConnectivity.noUnit = true;
			}
			foreach (IGH_Param item in component.Params.Input)
			{
				GH_SwitcherParamState gH_SwitcherParamState = new GH_SwitcherParamState(GH_ParameterSide.Input, item.InstanceGuid);
				gH_SwitcherConnectivity.inputs.Add(gH_SwitcherParamState);
				foreach (IGH_Param source in item.Sources)
				{
					gH_SwitcherParamState.Targets.Add(source.InstanceGuid);
				}
			}
			foreach (IGH_Param item2 in component.Params.Output)
			{
				GH_SwitcherParamState gH_SwitcherParamState2 = new GH_SwitcherParamState(GH_ParameterSide.Output, item2.InstanceGuid);
				gH_SwitcherConnectivity.outputs.Add(gH_SwitcherParamState2);
				foreach (IGH_Param recipient in item2.Recipients)
				{
					gH_SwitcherParamState2.Targets.Add(recipient.InstanceGuid);
				}
			}
			return gH_SwitcherConnectivity;
		}

		public void Apply(GH_SwitcherComponent component, GH_Document document)
		{
			if (noUnit)
			{
				component.ClearUnit(recompute: true, recordEvent: false);
			}
			else
			{
				component.SwitchUnit(unit, recompute: true, recordEvent: false);
			}
			for (int i = 0; i < inputs.Count; i++)
			{
				GH_SwitcherParamState gH_SwitcherParamState = inputs[i];
				int num = component.Params.IndexOfInputParam(gH_SwitcherParamState.ParameterId);
				if (num == -1)
				{
					continue;
				}
				IGH_Param iGH_Param = component.Params.Input[num];
				for (int j = 0; j < gH_SwitcherParamState.Targets.Count; j++)
				{
					IGH_Param iGH_Param2 = document.FindParameter(gH_SwitcherParamState.Targets[j]);
					if (iGH_Param2 != null)
					{
						iGH_Param.AddSource(iGH_Param2);
					}
				}
			}
			for (int k = 0; k < outputs.Count; k++)
			{
				GH_SwitcherParamState gH_SwitcherParamState2 = outputs[k];
				int num2 = component.Params.IndexOfOutputParam(gH_SwitcherParamState2.ParameterId);
				if (num2 != -1)
				{
					IGH_Param source = component.Params.Output[num2];
					for (int l = 0; l < gH_SwitcherParamState2.Targets.Count; l++)
					{
						document.FindParameter(gH_SwitcherParamState2.Targets[l])?.AddSource(source);
					}
				}
			}
		}
	}

	private class GH_SwitcherParamState
	{
		private GH_ParameterSide side;

		private Guid paramId;

		private List<Guid> targets;

		public Guid ParameterId
		{
			get
			{
				return paramId;
			}
			set
			{
				paramId = value;
			}
		}

		public List<Guid> Targets
		{
			get
			{
				return targets;
			}
			set
			{
				targets = value;
			}
		}

		public GH_SwitcherParamState(GH_ParameterSide side, Guid paramId)
		{
			this.side = side;
			this.paramId = paramId;
			targets = new List<Guid>();
		}
	}

	private GH_SwitcherConnectivity oldState;

	private string newUnit;

	public override bool ExpiresSolution => true;

	public GH_SwitchAction(GH_SwitcherComponent component, string newUnit)
	{
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		oldState = GH_SwitcherConnectivity.Create(component);
		this.newUnit = newUnit;
	}

	protected override void Internal_Redo(GH_Document doc)
	{
		IGH_DocumentObject iGH_DocumentObject = doc.FindObject(oldState.ComponentId, topLevelOnly: true);
		if (iGH_DocumentObject == null || !(iGH_DocumentObject is GH_SwitcherComponent))
		{
			throw new GH_UndoException("Switcher component with id[" + oldState.ComponentId.ToString() + "] not found");
		}
		GH_SwitcherComponent gH_SwitcherComponent = (GH_SwitcherComponent)iGH_DocumentObject;
		if (newUnit != null)
		{
			gH_SwitcherComponent.SwitchUnit(newUnit, recompute: true, recordEvent: false);
		}
		else
		{
			gH_SwitcherComponent.ClearUnit(recompute: false);
		}
	}

	protected override void Internal_Undo(GH_Document doc)
	{
		IGH_DocumentObject iGH_DocumentObject = doc.FindObject(oldState.ComponentId, topLevelOnly: true);
		if (iGH_DocumentObject == null || !(iGH_DocumentObject is GH_SwitcherComponent))
		{
			throw new GH_UndoException("Switcher component with id[" + oldState.ComponentId.ToString() + "] not found");
		}
		GH_SwitcherComponent component = (GH_SwitcherComponent)iGH_DocumentObject;
		oldState.Apply(component, doc);
	}
}
}