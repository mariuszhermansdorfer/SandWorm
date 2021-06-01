using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;

namespace SandWorm
{
    public abstract class GH_ExtendableComponent : GH_Component
    {
        public enum GH_ComponentState
        {
            COMPUTED,
            EMPTY
        }

        private GH_RuntimeMessageLevel m_state;

        protected internal GH_ExtendableComponent(string sName, string sAbbreviation, string sDescription, string sCategory, string sSubCategory)
            : base(sName, sAbbreviation, sDescription, sCategory, sSubCategory)
        {
            base.Phase = GH_SolutionPhase.Blank;
        }

        protected virtual void Setup(GH_ExtendableComponentAttributes attr)
        {
        }

        protected virtual void OnComponentLoaded()
        {
        }

        protected virtual void OnComponentReset(GH_ExtendableComponentAttributes attr)
        {
        }



        /// <summary>
        /// Updates a reference based on the output. Use me in a lambda like:
        /// _levels.ValueChanged += (s, e) => ValueUpdate(s, e, ref levels);
        /// </summary>
        /// <typeparam name="T">double, int or enum</typeparam>
        /// <param name="sender"></param>
        /// <param name="reference">Can be any object that can be converted to double, int or enum</param>
        public void ValueUpdate<T>(object sender, System.EventArgs e, ref T reference) where T : IConvertible
        {
            if (sender is MenuSlider sl)
            {
                if (typeof(T).IsAssignableFrom(typeof(double)))
                    reference = (T)(object)sl.Value;
                else if (typeof(T).IsAssignableFrom(typeof(int)))
                    reference = (T)(object)Convert.ToInt32(Math.Round(sl.Value));
            }

            else if (sender is MenuDropDown down)
            {
                if (typeof(T).IsEnum)
                {
                    reference = (T)Enum.Parse(typeof(T), down.Items[down.Value].name);
                    base.Params.OnParametersChanged();
                }
                else if (typeof(T) == typeof(string))
                {
                    reference = (T)(object)down.Items[down.Value].name;
                }

            }
            else if (sender is MenuRadioButtonGroup rbg && rbg.GetActiveInt().Count > 0)
            {
                
                if (typeof(T) == typeof(bool))
                {
                    
                    foreach (var radio in rbg.GetActive())
                    {
                        if (radio.Tag.ToLower() == "no" || radio.Tag.ToLower() == "false" || radio.Tag.ToLower() == "off")
                        {
                            reference = (T)(object)false;
                            
                            break;

                        }
                        if (radio.Tag.ToLower() == "yes" || radio.Tag.ToLower() == "true" || radio.Tag.ToLower() == "on")
                        {
                            reference = (T)(object)true;
                            
                            break;
                        }
                    }
                }
                else if (typeof(T) == typeof(int))
                {
                    
                    reference = (T)(object)rbg.GetActiveInt()[0];
                }
            }

            else
            {
                throw new NotImplementedException($"Trying to update a type that is not yet implemented in GH_ExtendableComponent.cs/ValueUpdate<T>. Type {typeof(T)} sender: {sender}");
            }

            setModelProps();
        }

        /// <summary>
        /// Added to GH_ExtendableComponent. Do feel free to use this one or override to your own.
        /// </summary>
        protected void setModelProps()
        {
            ExpireSolution(recompute: true);
        }


        public override void ComputeData()
        {
            base.ComputeData();
            if (m_state != RuntimeMessageLevel && RuntimeMessageLevel == GH_RuntimeMessageLevel.Warning)
            {
                List<IGH_Param> input = base.Params.Input;
                int count = input.Count;
                bool flag = true;
                for (int i = 0; i < count; i++)
                {
                    if (input[i].SourceCount == 0 && !input[i].Optional && input[i].VolatileData.IsEmpty)
                    {
                        flag = false;
                        break;
                    }
                }
                if (!flag && RuntimeMessageLevel == GH_RuntimeMessageLevel.Warning)
                {
                    OnComponentReset((GH_ExtendableComponentAttributes)base.Attributes);
                }
            }
            m_state = RuntimeMessageLevel;
        }

        public override bool Read(GH_IReader reader)
        {
            bool result = base.Read(reader);
            OnComponentLoaded();
            return result;
        }

        public override void CreateAttributes()
        {
            Setup((GH_ExtendableComponentAttributes)(m_attributes = new GH_ExtendableComponentAttributes(this)));
        }

        public bool OutputConnected(int ind)
        {
            return base.Params.Output[ind].Recipients.Count != 0;
        }

        public bool OutputInUse(int ind)
        {
            if (base.Params.Output[ind] is IGH_PreviewObject && !base.Hidden)
            {
                return true;
            }
            if (base.Params.Output[ind].Recipients.Count != 0)
            {
                return true;
            }
            return false;
        }

        public bool OutputInUse()
        {
            for (int i = 0; i < base.Params.Output.Count; i++)
            {
                if (OutputInUse(i))
                {
                    return true;
                }
            }
            return false;
        }

        public string GetMenuDescription()
        {
            return ((GH_ExtendableComponentAttributes)base.Attributes).GetMenuDescription();
        }
    }
}