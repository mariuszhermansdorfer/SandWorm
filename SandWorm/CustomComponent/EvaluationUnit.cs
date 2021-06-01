using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;

namespace SandWorm
{ 
public class EvaluationUnit
{
	private GH_SwitcherComponent component;

	private string name;

	private string displayName;

	private string description;

	private List<ExtendedPlug> inputs;

	private List<ExtendedPlug> outputs;

	private bool active;

	private Bitmap icon;

	private bool keepLinks;

	private EvaluationUnitContext cxt;

	public GH_SwitcherComponent Component
	{
		get
		{
			return component;
		}
		set
		{
			component = value;
		}
	}

	public List<ExtendedPlug> Inputs => inputs;

	public List<ExtendedPlug> Outputs => outputs;

	public string Name
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

	public string DisplayName
	{
		get
		{
			return displayName;
		}
		set
		{
			displayName = value;
		}
	}

	public string Description
	{
		get
		{
			return description;
		}
		set
		{
			description = value;
		}
	}

	public bool Active
	{
		get
		{
			return active;
		}
		set
		{
			active = value;
		}
	}

	public bool KeepLinks
	{
		get
		{
			return keepLinks;
		}
		set
		{
			keepLinks = value;
		}
	}

	public Bitmap Icon
	{
		get
		{
			return icon;
		}
		set
		{
			icon = value;
		}
	}

	public EvaluationUnitContext Context => cxt;

	public EvaluationUnit(string name, string displayName, string description, Bitmap icon = null)
	{
		this.name = name;
		this.displayName = displayName;
		this.description = description;
		inputs = new List<ExtendedPlug>();
		outputs = new List<ExtendedPlug>();
		keepLinks = false;
		this.icon = icon;
		cxt = new EvaluationUnitContext(this);
	}

	private static Type GetGenericType(Type generic, Type toCheck)
	{
		while (toCheck != null && toCheck != typeof(object))
		{
			Type right = (toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck);
			if (generic == right)
			{
				return toCheck;
			}
			toCheck = toCheck.BaseType;
		}
		return null;
	}

	public void RegisterInputParam(IGH_Param param, string name, string nickName, string description, GH_ParamAccess access, IGH_Goo defaultValue)
	{
		param.Name = name;
		param.NickName = nickName;
		param.Description = description;
		param.Access = access;
		try
		{
			if (defaultValue != null && typeof(IGH_Goo).IsAssignableFrom(param.Type))
			{
				Type genericType = GetGenericType(typeof(GH_PersistentParam<>), param.GetType());
				if (genericType != null)
				{
					Type[] genericArguments = genericType.GetGenericArguments();
					if (genericArguments.Length != 0)
					{
						_ = genericArguments[0];
						genericType.GetMethod("SetPersistentData", BindingFlags.Instance | BindingFlags.Public, null, new Type[1]
						{
							genericArguments[0]
						}, null).Invoke(param, new object[1]
						{
							defaultValue
						});
					}
				}
			}
		}
		catch (Exception)
		{
		}
		ExtendedPlug extendedPlug = new ExtendedPlug(param);
		extendedPlug.Unit = this;
		inputs.Add(extendedPlug);
	}

	public void RegisterInputParam(IGH_Param param, string name, string nickName, string description, GH_ParamAccess access)
	{
		RegisterInputParam(param, name, nickName, description, access, null);
	}

	public void RegisterOutputParam(IGH_Param param, string name, string nickName, string description)
	{
		param.Name = name;
		param.NickName = nickName;
		param.Description = description;
		ExtendedPlug extendedPlug = new ExtendedPlug(param);
		extendedPlug.Unit = this;
		outputs.Add(extendedPlug);
	}

	public void NewParameterIds()
	{
		foreach (ExtendedPlug input in inputs)
		{
			input.Parameter.NewInstanceGuid();
		}
		foreach (ExtendedPlug output in outputs)
		{
			output.Parameter.NewInstanceGuid();
		}
	}

	public void AddMenu(GH_ExtendableMenu menu)
	{
		Context.Collection.AddMenu(menu);
	}

	public bool Write(GH_IWriter writer)
	{
		writer.SetString("name", Name);
		GH_IWriter gH_IWriter = writer.CreateChunk("params");
		GH_IWriter gH_IWriter2 = gH_IWriter.CreateChunk("input");
		gH_IWriter2.SetInt32("index", 0);
		gH_IWriter2.SetInt32("count", Inputs.Count);
		for (int i = 0; i < inputs.Count; i++)
		{
			if (inputs[i].Parameter.Attributes != null)
			{
				GH_IWriter writer2 = gH_IWriter2.CreateChunk("p", i);
				inputs[i].Parameter.Write(writer2);
			}
		}
		GH_IWriter gH_IWriter3 = gH_IWriter.CreateChunk("output");
		gH_IWriter3.SetInt32("index", 0);
		gH_IWriter3.SetInt32("count", Outputs.Count);
		for (int j = 0; j < outputs.Count; j++)
		{
			if (outputs[j].Parameter.Attributes != null)
			{
				GH_IWriter writer3 = gH_IWriter3.CreateChunk("p", j);
				outputs[j].Parameter.Write(writer3);
			}
		}
		GH_IWriter writer4 = writer.CreateChunk("context");
		return cxt.Collection.Write(writer4);
	}

	public bool Read(GH_IReader reader)
	{
		if (reader.ChunkExists("params"))
		{
			GH_IReader gH_IReader = reader.FindChunk("params");
			if (gH_IReader.ChunkExists("input") && inputs != null)
			{
				GH_IReader gH_IReader2 = gH_IReader.FindChunk("input");
				int value = -1;
				if (gH_IReader2.TryGetInt32("count", ref value) && inputs.Count == value)
				{
					for (int i = 0; i < value; i++)
					{
						if (gH_IReader2.ChunkExists("p", i))
						{
							inputs[i].Parameter.Read(gH_IReader2.FindChunk("p", i));
						}
						else if (gH_IReader2.ChunkExists("param", i))
						{
							inputs[i].Parameter.Read(gH_IReader2.FindChunk("param", i));
						}
					}
				}
			}
			if (gH_IReader.ChunkExists("output") && outputs != null)
			{
				GH_IReader gH_IReader3 = gH_IReader.FindChunk("output");
				int value2 = -1;
				if (gH_IReader3.TryGetInt32("count", ref value2) && outputs.Count == value2)
				{
					for (int j = 0; j < value2; j++)
					{
						if (gH_IReader3.ChunkExists("p", j))
						{
							outputs[j].Parameter.Read(gH_IReader3.FindChunk("p", j));
						}
						else if (gH_IReader3.ChunkExists("param", j))
						{
							outputs[j].Parameter.Read(gH_IReader3.FindChunk("param", j));
						}
					}
				}
			}
		}
		try
		{
			GH_IReader gH_IReader4 = reader.FindChunk("context");
			if (gH_IReader4 != null)
			{
				cxt.Collection.Read(gH_IReader4);
			}
		}
		catch (Exception ex)
		{
			RhinoApp.WriteLine("Component error:" + ex.Message + "\n" + ex.StackTrace);
		}
		return true;
	}

	public string GetMenuDescription()
	{
		return Context.Collection.GetMenuDescription();
	}
}
}