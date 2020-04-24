﻿using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace PaintDotNet.Effects
{
    internal static class AssemblyUtil
    {
        private static Assembly[] referenceAssemblies;

        internal static Assembly[] ReferenceAssemblies
        {
            get
            {
                if (referenceAssemblies == null)
                {
                    List<Assembly> assemblyList = new List<Assembly>
                    {
                        typeof(Effect).Assembly,
                        typeof(Enumerable).Assembly // System.Core Assembly
                    };

                    foreach (AssemblyName assemblyName in typeof(Effect).Assembly.GetReferencedAssemblies())
                    {
                        if (assemblyName.Name == "PaintDotNet.Framework" ||
                            assemblyName.Name == "PaintDotNet.Resources" ||
                            assemblyName.Name == "PaintDotNet.SystemLayer")
                        {
                            continue;
                        }

                        try
                        {
                            assemblyList.Add(Assembly.Load(assemblyName));
                        }
                        catch
                        {
                            // Just don't crash. It wasn't that important anyway.
                        }
                    }

                    referenceAssemblies = assemblyList.Distinct().ToArray();
                }

                return referenceAssemblies;
            }
        }
    }
}
