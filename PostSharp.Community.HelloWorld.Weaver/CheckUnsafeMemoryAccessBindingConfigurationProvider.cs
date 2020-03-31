using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Binding;

namespace PostSharp.Community.UnsafeMemoryChecker.Weaver
{
    internal class CheckUnsafeMemoryAccessBindingConfigurationProvider 
    {
        public string ProjectAssetsPath { get; set; }

        public string TargetFramework { get; set; }

        public void ConfigureBinding(BindingConfiguration bindingConfiguration, bool configureTargetVersion)
        {
            bindingConfiguration.ReferenceContext.AssemblyLocatorPolicies.Add(new ProjectAssetsPolicy(this.ProjectAssetsPath, this.TargetFramework, BindingContext.Reference));
        }

        public class ProjectAssetsPolicy : IAssemblyLocatorPolicy
        {
            private Dictionary<string, IAssemblyIdentity> referenceLibraries;
            private BindingContext bindingContext;

            public ProjectAssetsPolicy(string projectAssetsPath, string targetFramework, BindingContext bindingContext )
            {
                this.bindingContext = bindingContext;
                this.referenceLibraries = new Dictionary<string, IAssemblyIdentity>();

                var projectAssetsText = File.ReadAllText(projectAssetsPath);
                var projectAssetsToken = JToken.Parse(projectAssetsText);
                var targetFrameworkName = NuGetFramework.Parse(targetFramework);

                string[] packageFolders = ((JObject)projectAssetsToken["packageFolders"]).Properties().Select(x => x.Name).ToArray();
                
                foreach(var packageProperty in ((JObject)projectAssetsToken["targets"][targetFrameworkName.DotNetFrameworkName]).Properties())
                {
                    if (packageProperty.Value["type"].Value<string>() != "package")
                        continue;

                    string[] packageIdParts = packageProperty.Name.Split('/');
                    string packageName = packageIdParts[0];
                    string packageVersion = packageIdParts[1];

                    var compileSection = packageProperty.Value["compile"];

                    if (compileSection == null)
                        continue;

                    foreach (var libraryProperty in ((JObject)compileSection).Properties())
                    {
                        string libraryRelativePath = libraryProperty.Name;

                        if (libraryRelativePath.EndsWith("_._"))
                            continue;

                        string tfmDir = Path.GetFileName(Path.GetDirectoryName(libraryRelativePath));
                        NuGetFramework tfm = NuGetFramework.ParseFolder(tfmDir);

                        foreach (string packageFolder in packageFolders)
                        {
                            string candidatePath = Path.Combine(packageFolder, packageName, packageVersion, libraryRelativePath);

                            if (File.Exists(candidatePath))
                            {
                                var identity = AssemblyIdentityReader.GetAssemblyBindingIdentity(candidatePath, this.bindingContext, new FrameworkName(tfm.DotNetFrameworkName));

                                if (identity != null)
                                {
                                    referenceLibraries[identity.Name] = identity;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            public IAssemblyIdentity FindAssembly( IAssemblyName requestedAssemblyName )
            {
                if (!this.referenceLibraries.TryGetValue(requestedAssemblyName.Name, out var identity))
                    return null;

                return identity;
            }
        }
    }
}