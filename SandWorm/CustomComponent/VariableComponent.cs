﻿using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace SandWorm
{
    public class VariableComponent
    {
        /// <summary>
        /// Add input parameters to the GH component. You might be able to get the input based on the 'name'.
        /// </summary>
        /// <param name="component">this</param>
        /// <param name="menu">the sender MenuDropDownMenu</param>
        /// <param name="values">which integer items of the MenuDropDown will activate this input?</param>
        /// <param name="name">Name of the input</param>
        /// <param name="nickName">Nickname of the input</param>
        /// <param name="description">Description of the input</param>
        public static void AddOrRemoveVariableParameters(GH_Component component, MenuDropDown menu, List<int> values, string name, string nickName, string description)
        {
            if (values.Contains(menu.Value))
            {
                foreach (int value in values)
                {
                    foreach (var p in component.Params.Input)
                    {
                        if (p.Name == name) return;
                    }
                    component.Params.RegisterInputParam(new Param_Number
                    {
                        Name = name,
                        NickName = nickName,
                        Description = description,
                        Access = GH_ParamAccess.list,
                        Optional = true
                    });
                    component.Params.OnParametersChanged();
                    return;
                }
            }
            else
            {
                for (int i = 0; i < component.Params.Input.Count; i++)
                {
                    if (component.Params.Input[i].Name == name)
                    {
                        component.Params.Input[i].RemoveAllSources();
                        component.Params.UnregisterInputParameter(component.Params.Input[i]);
                    }
                }
                component.Params.OnParametersChanged();
            }
        }
    }
}