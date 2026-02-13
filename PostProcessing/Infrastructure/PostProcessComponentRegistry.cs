using System;
using System.Collections.Generic;
using System.Reflection;
using AngusHCY.Unity.Rendering.PostProcessing.Contract;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    public class PostProcessComponentRegistry
    {
        public static Type[] GetAllComponents()
        {
            Type baseType = typeof(PostProcessComponentBase);
            Assembly assembly = baseType.Assembly;
            Type[] allTypes = assembly.GetTypes();
            List<Type> components = new List<Type>();

            foreach (Type type in allTypes)
            {
                // 跳過抽象類別與介面
                if (type.IsAbstract)
                {
                    continue;
                }

                // 判斷是否繼承 PostProcessComponentBase
                if (baseType.IsAssignableFrom(type))
                {
                    components.Add(type);
                }
            }

            return components.ToArray();
        }
    }

}