/*
 * Copyright 2017 Stanislav Muhametsin. All rights Reserved.
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
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.Repositories;
using NuGet.Versioning;
using NuGetUtils.Lib.Restore;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UtilPack;

namespace NuGetUtils.Lib.Restore
{

   /// <summary>
   /// This class contains extension method which are for types not contained in this library.
   /// </summary>
   public static partial class UtilPackNuGetUtility
   {

#if NUGET_430
      private static readonly NuGetFramework NETCOREAPP20 = new NuGetFramework( FrameworkConstants.FrameworkIdentifiers.NetCoreApp, new Version( 2, 0, 0, 0 ) );
#endif
#if NUGET_430 || NUGET_440 || NUGET_450
      private static readonly NuGetFramework NETCOREAPP21 = new NuGetFramework( FrameworkConstants.FrameworkIdentifiers.NetCoreApp, new Version( 2, 1, 0, 0 ) );
#endif

      /// <summary>
      /// Gets the matching assembly path from set of assembly paths, the expanded home path of the package, and optional assembly path from "outside world", e.g. configuration.
      /// </summary>
      /// <param name="packageID">This package ID. It will be used as assembly name without an extension, if <paramref name="optionalGivenAssemblyPath"/> is <c>null</c> or empty.</param>
      /// <param name="assemblyPaths">All assembly paths to be considered from this package.</param>
      /// <param name="optionalGivenAssemblyPath">Optional assembly path from "outside world", e.g. configuration.</param>
      /// <param name="suitableAssemblyPathChecker">The callback to check whether the assembly path is suitable (e.g. file exists on disk).</param>
      /// <returns>Best matched assembly path, or <c>null</c>.</returns>
      public static String GetAssemblyPathFromNuGetAssemblies(
         String packageID,
         String[] assemblyPaths,
         String optionalGivenAssemblyPath,
         Func<String, Boolean> suitableAssemblyPathChecker
         )
      {
         String assemblyPath;
         if ( assemblyPaths.Length == 1 )
         {
            assemblyPath = assemblyPaths[0];
            if ( suitableAssemblyPathChecker != null && !suitableAssemblyPathChecker( assemblyPath ) )
            {
               assemblyPath = null;
            }
         }
         else if ( assemblyPaths.Length > 1 )
         {
            if ( String.IsNullOrEmpty( optionalGivenAssemblyPath ) )
            {
               optionalGivenAssemblyPath = packageID + ".dll";
            }

            assemblyPath = assemblyPaths
               .FirstOrDefault( ap => String.Equals( Path.GetFullPath( ap ), Path.GetFullPath( Path.Combine( Path.GetDirectoryName( ap ), optionalGivenAssemblyPath ) ) )
               && ( suitableAssemblyPathChecker?.Invoke( ap ) ?? true ) );
         }
         else
         {
            assemblyPath = null;
         }


         return assemblyPath;
      }

      /// <summary>
      /// The package ID for SDK package of framework <c>.NET Core App</c>. The value is <c>Microsoft.NETCore.App</c>.
      /// </summary>
      public const String SDK_PACKAGE_NETCORE = "Microsoft.NETCore.App";

      /// <summary>
      /// The package ID for SDK package of framework <c>.NET Standard</c>. The value is <c>NETStandard.Library</c>.
      /// </summary>
      public const String SDK_PACKAGE_NETSTANDARD = "NETStandard.Library";

      /// <summary>
      /// The package ID for sdk package of framework <c>ASP.NETCore</c>. The value is <c>Microsoft.AspNetCore.App</c>.
      /// </summary>
      public const String SDK_PACKAGE_ASPNETCORE = "Microsoft.AspNetCore.App";

      /// <summary>
      /// Gets the package ID of the SDK package for given framework. If the optional override is supplied, always returns that.
      /// </summary>
      /// <param name="framework">This <see cref="NuGetFramework"/>.</param>
      /// <param name="givenID">The optional override.</param>
      /// <returns>The value of <paramref name="givenID"/>, if it is non-<c>null</c> and not empty; otherwise tries to deduce the value from this <see cref="NuGetFramework"/>. Currently, returns value of <see cref="SDK_PACKAGE_NETSTANDARD"/> for .NET Standard and .NET Desktop frameworks, and <see cref="SDK_PACKAGE_NETCORE"/> for .NET Core framework, and <see cref="SDK_PACKAGE_ASPNETCORE"/> for ASP.NETCore framework.</returns>
      /// <seealso cref="GetSDKPackageVersion"/>
      public static String GetSDKPackageID( this NuGetFramework framework, String givenID = null )
      {
         // NuGet library should really have something like this method, or this information should be somewhere in repository
         String id;
         if ( !String.IsNullOrEmpty( givenID ) )
         {
            id = givenID;
         }
         else
         {
            id = framework.Framework;
            if ( String.Equals( id, FrameworkConstants.FrameworkIdentifiers.NetCoreApp, StringComparison.OrdinalIgnoreCase ) )
            {
               id = SDK_PACKAGE_NETCORE;
            }
            else if ( String.Equals( id, FrameworkConstants.FrameworkIdentifiers.AspNetCore, StringComparison.OrdinalIgnoreCase ) )
            {
               id = SDK_PACKAGE_ASPNETCORE;
            }
            else if (
               (
                  String.Equals( id, FrameworkConstants.FrameworkIdentifiers.Net, StringComparison.OrdinalIgnoreCase )
                  && framework.Version >= new Version( 4, 5 )
               ) ||
               String.Equals( id, FrameworkConstants.FrameworkIdentifiers.NetStandard, StringComparison.OrdinalIgnoreCase )
               )
            {
               id = SDK_PACKAGE_NETSTANDARD;
            }
            else
            {
               id = null;
            }
         }
         return id;
      }

      /// <summary>
      /// Gets the package version of the SDK package for given framework. If the optional override is supplied, always returns that.
      /// </summary>
      /// <param name="framework">This <see cref="NuGetFramework"/>.</param>
      /// <param name="sdkPackageID">The package ID of the SDK package.</param>
      /// <param name="givenVersion">The optional override.</param>
      /// <returns>The value of <paramref name="givenVersion"/>, if it is non-<c>null</c> and not empty; otherwise tries to deduce the value from this <see cref="NuGetFramework"/> and <paramref name="sdkPackageID"/>.</returns>
      /// <seealso cref="GetSDKPackageID"/>
      public static String GetSDKPackageVersion( this NuGetFramework framework, String sdkPackageID, String givenVersion = null )
      {
         String retVal;
         if ( !String.IsNullOrEmpty( givenVersion ) )
         {
            retVal = givenVersion;
         }
         else
         {
            switch ( sdkPackageID )
            {
               case SDK_PACKAGE_NETSTANDARD:
                  retVal = "2.0.3";
                  //if ( String.Equals( framework.Framework, FrameworkConstants.FrameworkIdentifiers.NetStandard, StringComparison.OrdinalIgnoreCase ) )
                  //{
                  //   retVal = framework.Version.ToString();
                  //}
                  //else
                  //{
                  //   // .NETFramework compatibility, see https://docs.microsoft.com/en-gb/dotnet/standard/net-standard
                  //   var version = framework.Version;
                  //   var minor = version.Minor;
                  //   var build = version.Build;
                  //   switch ( minor )
                  //   {
                  //      case 5:
                  //         retVal = build == 0 ? "1.1" : "1.2";
                  //         break;
                  //      case 6:
                  //         retVal = build == 0 ? "1.3" : ( build == 1 ? "1.4" : "1.5" );
                  //         break;
                  //      default:
                  //         retVal = null;
                  //         break;
                  //   }
                  //}
                  break;
               case SDK_PACKAGE_NETCORE:
                  {
                     retVal = TryDetectSDKPackgeIDFromPaths( sdkPackageID );
                     if ( String.IsNullOrEmpty( retVal ) )
                     {
                        var version = framework.Version;
                        switch ( version.Major )
                        {
                           case 1:
                              switch ( version.Minor )
                              {
                                 case 0:
                                    retVal = "1.0.5";
                                    break;
                                 case 1:
                                    retVal = "1.1.2";
                                    break;
                                 default:
                                    retVal = null;
                                    break;
                              }
                              break;
                           case 2:
                              switch ( version.Minor )
                              {
                                 case 0:
                                    retVal = "2.0.9";
                                    break;
                                 case 1:
                                    retVal = "2.1.5";
                                    break;
                                 default:
                                    retVal = null;
                                    break;
                              }
                              break;
                           default:
                              retVal = null;
                              break;
                        }
                     }
                  }
                  break;
               default:
                  retVal = null;
                  break;
            }
         }
         return retVal;
      }

      private static String TryDetectSDKPackgeIDFromPaths(
         String sdkPackageID
         )
      {
         String retVal = null;
         try
         {
            // When one runs normal dotnet command, the DLLs will be visible as e.g. in C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.1.5
            // When one runs e.g. dotnet build, the DLLs will be visible as e.g. in C:\Program Files\dotnet\sdk\2.1.403
            // Therefore, we must match both version and SDK package ID
            var dir = Path.GetDirectoryName( new Uri( typeof( Object ).GetTypeInfo().Assembly.CodeBase ).LocalPath );
            var maybeVersion = Path.GetFileName( dir );
            var maybePackageID = Path.GetFileName( Path.GetDirectoryName( dir ) );
            if ( Version.TryParse( maybeVersion, out var ignored ) && String.Equals( sdkPackageID, maybePackageID, StringComparison.OrdinalIgnoreCase ) )
            {
               retVal = maybeVersion;
            }
         }
         catch
         {

         }

         return retVal;
      }

      /// <summary>
      /// This is helper method to try and deduce the <see cref="NuGetFramework"/> representing the currently running process.
      /// If optional framework information is specified as parameter, this method will always return that information as wrapped around <see cref="NuGetFramework"/>.
      /// Otherwise, it will try deduce the required information from entry point assembly <see cref="System.Runtime.Versioning.TargetFrameworkAttribute"/> on desktop, and from <see cref="P:System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription"/> on core.
      /// </summary>
      /// <param name="givenInformation">Optional framework information passed from "outside world", e.g. configuration.</param>
      /// <returns>The deduced <see cref="NuGetFramework"/>, or <see cref="NuGetFramework.AnyFramework"/> if automatic deduce failed.</returns>
      /// <remarks>This method never returns <c>null</c>.</remarks>
      public static NuGetFramework TryAutoDetectThisProcessFramework(
         (String FrameworkName, String FrameworkVersion)? givenInformation = null
         )
      {
         NuGetFramework retVal;
         if (
            givenInformation.HasValue
            && !String.IsNullOrEmpty( givenInformation.Value.FrameworkName )
            && !String.IsNullOrEmpty( givenInformation.Value.FrameworkVersion )
            && Version.TryParse( givenInformation.Value.FrameworkVersion, out var version )
            )
         {
            retVal = new NuGetFramework( givenInformation.Value.FrameworkName, version );
         }
         else
         {
#if NET45 || NET46
            var epAssembly = Assembly.GetEntryAssembly();
            if ( epAssembly == null )
            {
               // Deduct entrypoint assembly as the top-most assembly in current stack trace with TargetFramework attribute
               var fwString = new System.Diagnostics.StackTrace()
                  .GetFrames()
                  .Select( f => f.GetMethod().DeclaringType.Assembly.GetNuGetFrameworkStringFromAssembly() )
                  .LastOrDefault( fwName => !String.IsNullOrEmpty( fwName ) );
               retVal = fwString == null
                 ? NuGetFramework.AnyFramework
                 : NuGetFramework.ParseFrameworkName( fwString, DefaultFrameworkNameProvider.Instance );
            }
            else
            {
               retVal = epAssembly.GetNuGetFrameworkFromAssembly();
            }
#else
            var fwName = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            retVal = null;
            if ( !String.IsNullOrEmpty( fwName )
               && fwName.StartsWith( ".NET Core" )
               && Version.TryParse( fwName.Substring( 10 ), out var netCoreVersion )
               )
            {
               if ( netCoreVersion.Major == 4 )
               {
                  if ( netCoreVersion.Minor == 0 )
                  {
                     retVal = FrameworkConstants.CommonFrameworks.NetCoreApp10;
                  }
                  else if ( netCoreVersion.Minor == 6 )
                  {
                     // The strings are a bit messed up, e.g.:
                     // Core 1.1: ".NET Core 4.6.25211.01"
                     // Core 2.0: ".NET Core 4.6.00001.0"
                     // Core 2.1: ".NET Core 4.6.26614.01" (and also ".NET Core 4.6.26919.02")
                     switch ( netCoreVersion.Build )
                     {
                        case 25211:
                           retVal = FrameworkConstants.CommonFrameworks.NetCoreApp11;
                           break;
                        case 00001:
                           retVal =
#if NUGET_430
                           NETCOREAPP20
#else
                           FrameworkConstants.CommonFrameworks.NetCoreApp20
#endif
                           ;
                           break;
                        default:
                           retVal =
#if !NUGET_430 && !NUGET_440 && !NUGET_450
                              FrameworkConstants.CommonFrameworks.NetCoreApp21
#else
                              NETCOREAPP21
#endif
                              ;
                           break;
                     }
                  }
               }

            }
#endif
         }
         return retVal ?? NuGetFramework.AnyFramework;
      }

      /// <summary>
      /// Tries to automatically detect the runtime identifier of currently running process.
      /// </summary>
      /// <param name="givenRID">The optional override.</param>
      /// <returns>The value of <paramref name="givenRID"/>, if it is non-<c>null</c> and not empty; otherwise tries to deduce the RID using framework library methods. In such cahse, the result is always one of <c>"win"</c>, <c>"linux"</c>, <c>"osx"</c>, or <c>null</c>.</returns>
      public static String TryAutoDetectThisProcessRuntimeIdentifier(
         String givenRID = null
         )
      {
         // I wish these constants were in NuGet.Client library
         String retVal;
         if ( !String.IsNullOrEmpty( givenRID ) )
         {
            retVal = givenRID;
         }
         else
         {
#if NET45 || NET46
            switch ( Environment.OSVersion.Platform )
            {
               case PlatformID.Win32NT:
               case PlatformID.Win32S:
               case PlatformID.Win32Windows:
               case PlatformID.WinCE:
                  retVal = RID_WINDOWS;
                  break;
               // We will most likely never go into cases below, but one never knows...
               case PlatformID.Unix:
                  retVal = RID_UNIX;
                  break;
               case PlatformID.MacOSX:
                  retVal = RID_OSX;
                  break;
               default:
                  retVal = null;
                  break;
            }
#else

            if ( System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( System.Runtime.InteropServices.OSPlatform.Windows ) )
            {
               retVal = RID_WINDOWS;
            }
            else if ( System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( System.Runtime.InteropServices.OSPlatform.Linux ) )
            {
               retVal = RID_LINUX;
            }
            else if ( System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( System.Runtime.InteropServices.OSPlatform.OSX ) )
            {
               retVal = RID_OSX;
            }
            else
            {
               retVal = null;
            }
#endif

         }

         if ( !String.IsNullOrEmpty( retVal ) )
         {
            const Char ARCHITECTURE_SEPARATOR = '-';
            var numericChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            var architectureIndex = retVal.IndexOf( ARCHITECTURE_SEPARATOR );
            var versionIndex = retVal.IndexOfAny( numericChars );
            // Append version, unless we have generic UNIX/LINUX RID
            if ( !String.Equals( retVal, RID_LINUX, StringComparison.OrdinalIgnoreCase )
               && !String.Equals( retVal, RID_UNIX, StringComparison.OrdinalIgnoreCase )
               && ( versionIndex < 0 || architectureIndex < 0 || versionIndex > architectureIndex )
               )
            {
               Version osVersion;
#if NET45 || NET46
               osVersion = Environment.OSVersion.Version;
#else
               // Append version. This is a bit tricky...
               // OS Description is typically something like "Microsoft Windows x.y.z"
               // And we need to extract the x.y.z out of it.
               var osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
               var idx = osDescription.IndexOfAny( numericChars );
               if ( idx > 0 )
               {
                  Version.TryParse( osDescription.Substring( idx ), out osVersion );
               }
               else
               {
                  osVersion = null;
               }
#endif

               if ( osVersion != null )
               {
                  // Append version: More 'fun' special cases.
                  // On Windows, it is "majorminor" (without dot), unless minor is 0, then it is just "major"
                  // On others, it is ".major.minor" (with the dots)
                  String versionSuffix;
                  if ( String.Equals( retVal, RID_WINDOWS ) )
                  {
                     if ( osVersion.Minor == 0 )
                     {
                        versionSuffix = "" + osVersion.Major;
                     }
                     else
                     {
                        versionSuffix = "" + osVersion.Major + "" + osVersion.Minor;
                     }
                  }
                  else
                  {
                     versionSuffix = "." + osVersion.Major + "." + osVersion.Minor;
                  }

                  if ( architectureIndex < 0 )
                  {
                     // Can append directly
                     retVal += versionSuffix;
                  }
                  else
                  {
                     // Insert version before architecture separator
                     retVal = retVal.Substring( 0, architectureIndex ) + versionSuffix + retVal.Substring( architectureIndex );
                  }
               }
            }

            // Append architecture
            if ( architectureIndex < 0 )
            {
               String architectureString;
#if NET45 || NET46
               architectureString = Environment.Is64BitProcess ? "x64" : "x86";
#else
               architectureString = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
#endif
               retVal += ARCHITECTURE_SEPARATOR + architectureString;
            }
         }

         return retVal;
      }

      /// <summary>
      /// Helper method to get NuGet <see cref="ISettings"/> object from multiple potential NuGet configuration file locations.
      /// </summary>
      /// <param name="potentialConfigFileLocations">The potential configuration file locations. Will be traversed in given order.</param>
      /// <returns>A <see cref="ISettings"/> loaded from first specified configuration file location, or <see cref="ISettings"/> loaded using defaults if no potential configuration file locations are specified (array is <c>null</c>, empty, or contains only <c>null</c>s).</returns>
      public static ISettings GetNuGetSettings(
         params String[] potentialConfigFileLocations
         ) => GetNuGetSettingsWithDefaultRootDirectory( null, potentialConfigFileLocations );

      /// <summary>
      /// Helper method to get Nuget <see cref="ISettings"/> object from multiple potential NuGet configuration file locations, and use specified root directory if none of them work.
      /// </summary>
      /// <param name="rootDirectory">The root directory if none of the <paramref name="potentialConfigFileLocations"/> are valid.</param>
      /// <param name="potentialConfigFileLocations">The potential configuration file locations. Will be traversed in given order.</param>
      /// <returns>A <see cref="ISettings"/> loaded from first specified configuration file location, or <see cref="ISettings"/> loaded using defaults if no potential configuration file locations are specified (array is <c>null</c>, empty, or contains only <c>null</c>s).</returns>
      public static ISettings GetNuGetSettingsWithDefaultRootDirectory(
         String rootDirectory,
         params String[] potentialConfigFileLocations
         )
      {
         ISettings nugetSettings = null;
         if ( !potentialConfigFileLocations.IsNullOrEmpty() )
         {
            for ( var i = 0; i < potentialConfigFileLocations.Length && nugetSettings == null; ++i )
            {
               var curlocation = potentialConfigFileLocations[i];
               if ( !String.IsNullOrEmpty( curlocation ) )
               {
                  var fp = Path.GetFullPath( curlocation );
                  nugetSettings = Settings.LoadSpecificSettings( Path.GetDirectoryName( fp ), Path.GetFileName( fp ) );
               }
            }
         }

         if ( nugetSettings == null )
         {
            nugetSettings = Settings.LoadDefaultSettings( rootDirectory, null, new XPlatMachineWideSetting() );
         }

         return nugetSettings;
      }


      /// <summary>
      /// Tries to parse the <see cref="System.Runtime.Versioning.TargetFrameworkAttribute"/> applied to this assembly into <see cref="NuGetFramework"/>.
      /// </summary>
      /// <param name="assembly">This <see cref="Assembly"/>.</param>
      /// <returns>A <see cref="NuGetFramework"/> parsed from <see cref="System.Runtime.Versioning.TargetFrameworkAttribute.FrameworkName"/>, or <see cref="NuGetFramework.AnyFramework"/> if no such attribute is applied to this assembly.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="Assembly"/> is <c>null</c>.</exception>
      public static NuGetFramework GetNuGetFrameworkFromAssembly( this Assembly assembly )
      {
         var thisFrameworkString = assembly.GetNuGetFrameworkStringFromAssembly();
         return thisFrameworkString == null
              ? NuGetFramework.AnyFramework
              : NuGetFramework.ParseFrameworkName( thisFrameworkString, DefaultFrameworkNameProvider.Instance );
      }

      /// <summary>
      /// Tries to get the framework string from <see cref="System.Runtime.Versioning.TargetFrameworkAttribute"/> possibly applied to this assembly.
      /// </summary>
      /// <param name="assembly">This <see cref="Assembly"/>.</param>
      /// <returns>The value of <see cref="System.Runtime.Versioning.TargetFrameworkAttribute.FrameworkName"/> possibly applied to this assembly, or <c>null</c> if no such attribute was found.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="Assembly"/> is <c>null</c>.</exception>
      public static String GetNuGetFrameworkStringFromAssembly( this Assembly assembly )
      {
         return ArgumentValidator.ValidateNotNullReference( assembly ).GetCustomAttributes<System.Runtime.Versioning.TargetFrameworkAttribute>()
            .Select( x => x.FrameworkName )
            .FirstOrDefault();
      }
   }
}