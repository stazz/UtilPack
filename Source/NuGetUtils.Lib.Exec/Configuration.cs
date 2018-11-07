/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
 *
 * Licensed  under the  Apache License,  Version 2.0  (the "License");
 * you may not use  this file  except in  compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed  under the  License is distributed on an "AS IS" BASIS,
 * WITHOUT  WARRANTIES OR CONDITIONS  OF ANY KIND, either  express  or
 * implied.
 *
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */
using System;

namespace NuGetUtils.Lib.Exec
{
   /// <summary>
   /// Similar to <see cref="Restore.NuGetUsageConfiguration"/>, this data interface provides required information to execute a method within assembly located in NuGet package.
   /// </summary>
   /// <remarks>All properties except <see cref="PackageID"/> are optional.</remarks>
   /// <seealso cref="E_NuGetUtils.ExecuteMethodWithinNuGetAssemblyAsync"/>
   public interface NuGetExecutionConfiguration
   {
      /// <summary>
      /// Gets the package ID of the NuGet package containing assembly with the method to execute.
      /// </summary>
      /// <value>The package ID of the NuGet package containing assembly with the method to execute.</value>
      /// <remarks>This property is required.</remarks>
      String PackageID { get; }

      /// <summary>
      /// Gets the package version of the NuGet package containing assembly with the method to execute.
      /// </summary>
      /// <value>The package version of the NuGet package containing assembly with the method to execute.</value>
      /// <remarks>If this property is <c>null</c> or empty string, then NuGet source will be queried for the newest version.</remarks>
      String PackageVersion { get; }

      /// <summary>
      /// Gets the path within the target folder (e.g. <c>"lib/netstandard1.3"</c>) of the NuGet package pointing to the assembly containing method to execute.
      /// </summary>
      /// <value>The path within the target folder (e.g. <c>"lib/netstandard1.3"</c>) of the NuGet package pointing to the assembly containing method to execute.</value>
      /// <remarks>This is required only when the NuGet package contains more than one assembly in its target folder, and the method to execute is not located in assembly named as package ID.</remarks>
      String AssemblyPath { get; }

      /// <summary>
      /// Gets the full name of the type within assembly which contains the method to execute.
      /// </summary>
      /// <value>The full name of the type within assembly which contains the method to execute.</value>
      /// <remarks>This is needed, together with <see cref="EntrypointMethodName"/>, when the assembly does not have <see cref="EntryPoint.ConfiguredEntryPointAttribute"/> applied to, and it is also lacking the entrypoint information within DLL file; or when the method to be executed is neither of those.</remarks>
      /// <seealso cref="EntryPoint.ConfiguredEntryPointAttribute"/>
      String EntrypointTypeName { get; }

      /// <summary>
      /// Gets the name of the method to execute.
      /// </summary>
      /// <value>The name of the method to execute.</value>
      /// <remarks>This is needed, optionally together with <see cref="EntrypointTypeName"/>, when the assembly does not have <see cref="EntryPoint.ConfiguredEntryPointAttribute"/> applied to, and it is also lacking the entrypoint information within DLL file; or when the method to be executed is neither of those.</remarks>
      /// <seealso cref="EntryPoint.ConfiguredEntryPointAttribute"/>
      String EntrypointMethodName { get; }
   }
}
