﻿// Copyright (c) SharpCrafters s.r.o. This file is not open source. It is released under a commercial
// source-available license. Please see the LICENSE.md file in the repository root for details.

 using System.Collections.Generic;
 using System.IO;
 using PostSharp.AddIn.CheckUnsafeMemoryAccess;
 using PostSharp.Sdk.CodeModel.Binding;
 using PostSharp.Sdk.Extensibility;
 using PostSharp.Sdk.Extensibility.Configuration;
 using PostSharp.Sdk.Extensibility.Tasks;

 namespace PostSharp.Community.UnsafeMemoryChecker.Weaver
{
    [ExportProject( ProjectName = "CheckUnsafeMemoryAccess")]
    public sealed class CheckUnsafeMemoryAccessProject : IProjectConfigurationProvider, IBindingConfigurationProvider
    {
        public ProjectConfiguration GetProjectConfiguration()
        {
            return new ProjectConfiguration
                       {
                           SearchPath = new SearchPathConfigurationCollection
                                            {
                                              new SearchPathConfiguration("{$SearchPath}")   
                                            },
                           TaskFactories = new Dictionary<string, CreateTaskDelegate>
                                               {
                                                   {
                                                       "CheckUnsafeMemoryAccess",
                                                       project => new CheckUnsafeMemoryAccessTask()
                                                   },
                                                   {
                                                       "Compile",
                                                       project =>
                                                           {
                                                               string output = project.Evaluate( "{$Output}", true );
                                                               string privateKeyLocation = project.Evaluate( "{$PrivateKeyLocation}", true );
                                                               if ( output == null )
                                                               {
                                                                   return null;
                                                               }

                                                               return
                                                                   new CompileTask
                                                                       {
                                                                           TargetFile = output,
                                                                           IntermediateDirectory = Path.GetDirectoryName( output ),
                                                                           CleanIntermediate = false,
                                                                           SignAssembly = true,
                                                                           DelaySign = false,
                                                                           PrivateKeyLocation = privateKeyLocation
                                                                       };
                                                           }
                                                       }
                                               }
                       };
        }

        void IBindingConfigurationProvider.ConfigureBinding(Project p)
        {
            string targetFramework =
                p.Evaluate("{$LibraryTargetFramework}");
            if (targetFramework == null)
                return;

            string projectAssetsPath =
                p.Evaluate("{$LibraryProjectAssetsPath}");
            if (projectAssetsPath == null)
                return;

            new CheckUnsafeMemoryAccessBindingConfigurationProvider
            {
                TargetFramework = targetFramework,
                ProjectAssetsPath = projectAssetsPath,
            }.ConfigureBinding(p.BindingConfiguration, false);
        }
    }
}