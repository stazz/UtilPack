using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetUtils.Lib.Deployment
{

   /// <summary>
   /// This configuration provides a way to get information for deploying a single NuGet package.
   /// </summary>
   /// <remarks>All properties except <see cref="PackageID"/> are optional.</remarks>
   /// <seealso cref="E_NuGetUtils.DeployAsync"/>
   public interface NuGetDeploymentConfiguration
   {
      /// <summary>
      /// Gets the package ID of the package to be deployed.
      /// </summary>
      /// <value>The package ID of the package to be deployed.</value>
      String PackageID { get; }

      /// <summary>
      /// Gets the package version of the package to be deployed.
      /// </summary>
      /// <value>The package version of the package to be deployed.</value>
      /// <remarks>
      /// If this property is <c>null</c> or empty string, then NuGet source will be queried for the newest version.
      /// </remarks>
      String PackageVersion { get; }

      /// <summary>
      /// Gets the path within the target folder (e.g. <c>"lib/netstandard1.3"</c>) of the NuGet package pointing to the assembly containing method to execute.
      /// </summary>
      /// <value>The path within the target folder (e.g. <c>"lib/netstandard1.3"</c>) of the NuGet package pointing to the assembly containing method to execute.</value>
      /// <remarks>This is required only when the NuGet package contains more than one assembly in its target folder, and the method to execute is not located in assembly named as package ID.</remarks>
      String AssemblyPath { get; }

      /// <summary>
      /// Gets the package ID of the SDK of the framework of the NuGet package.
      /// </summary>
      /// <value>The package ID of the SDK of the framework of the NuGet package.</value>
      /// <remarks>
      /// If this property is <c>null</c> or empty string, then automatic detection of SDK package ID will be used.
      /// </remarks>
      String PackageSDKFrameworkPackageID { get; }

      /// <summary>
      /// Gets the package version of the SDK of the framework of the NuGet package.
      /// </summary>
      /// <value>The package version of the SDK of the framework of the NuGet package.</value>
      /// <remarks>
      /// If this property is <c>null</c> or empty string, then automatic detection of SDK package version will be used.
      /// </remarks>
      String PackageSDKFrameworkPackageVersion { get; }

      /// <summary>
      /// Gets the deployment kind.
      /// </summary>
      /// <value>The deployment kind.</value>
      /// <seealso cref="Deployment.DeploymentKind"/>
      DeploymentKind DeploymentKind { get; }

      /// <summary>
      /// Gets the information about SDK NuGet package (e.g. <c>"Microsoft.NETCore.App"</c>) related to how the SDK assemblies are relayed.
      /// </summary>
      /// <value>The information about SDK NuGet package (e.g. <c>"Microsoft.NETCore.App"</c>) related to how the SDK assemblies are relayed.</value>
      /// <remarks>
      /// Setting this to <c>true</c> will force the SDK package logic to assume that all compile time assemblies are package IDs of SDK sub-packages, thus affecting SDK package resolving logic.
      /// Setting this to <c>false</c> will force the SDK package logic to assume that main SDK package only has the assemblies that are exposed via NuGet package dependency chain.
      /// Leaving this unset (<c>null</c>) will use auto-detection (which will use <c>true</c> when deploying for .NET Core 2.0+, and will use <c>false</c> when deploying for other frameworks).
      /// </remarks>
      Boolean? PackageFrameworkIsPackageBased { get; }

      /// <summary>
      /// Gets the target directory where the NuGet package should be deployed to.
      /// </summary>
      /// <value>The target directory where the NuGet package should be deployed to.</value>
      /// <remarks>A randomly named directory within the system's temporary directory will be used if this is <c>null</c> or empty.</remarks>
      String TargetDirectory { get; }
   }

   /// <summary>
   /// This enumeration controls which files are copied and generated during deployment process of <see cref="E_NuGetUtils.DeployAsync"/>.
   /// </summary>
   public enum DeploymentKind
   {
      /// <summary>
      /// This value indicates that only the entrypoint assembly will be copied to the target directory, and <c>.deps.json</c> file will be generated, along with <c>.runtimeconfig.json</c> file.
      /// Those files will contain required information so that dotnet process will know to resolve dependency assemblies.
      /// This way the IO load by the deployment process will be kept at minimum.
      /// However, the dotnet process will then lock the DLL files in your package repository, as they are loaded directly from there.
      /// </summary>
      GenerateConfigFiles,

      /// <summary>
      /// This value indicates that entrypoint assembly along with all the non-SDK dependencies will be copied to the target folder.
      /// The <c>.deps.json</c> file will not be generated, but the <c>.runtimeconfig.json</c> file for .NET Core and <c>.exe.config</c> file for the .NET Desktop will be generated.
      /// The IO load may become heavy in this scenario, since possibly a lot of files may need to be copied.
      /// But with this deployment kind, the dotnet won't lock DLL files in your package repository.
      /// </summary>
      CopyNonSDKAssemblies
   }
}
