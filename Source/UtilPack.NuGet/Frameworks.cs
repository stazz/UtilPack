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
using NuGet.Repositories;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;
using NuGet.Common;
using UtilPack.NuGet;
using UtilPack;
using NuGet.Configuration;
using NuGet.ProjectModel;

namespace UtilPack.NuGet
{

   /// <summary>
   /// This class contains extension method which are for types not contained in this library.
   /// </summary>
   public static partial class UtilPackNuGetUtility
   {

      private static readonly NuGetFramework NETCOREAPP20 = new NuGetFramework( FrameworkConstants.FrameworkIdentifiers.NetCoreApp, new Version( 2, 0, 0, 0 ) );

      /// <summary>
      /// Gets the matching assembly path from set of assembly paths, the expanded home path of the package, and optional assembly path from "outside world", e.g. configuration.
      /// </summary>
      /// <param name="assemblyPaths">All assembly paths to be considered from this package.</param>
      /// <param name="packageExpandedPath">The package home directory path.</param>
      /// <param name="optionalGivenAssemblyPath">Optional assembly path from "outside world", e.g. configuration.</param>
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
         else if (
          assemblyPaths.Length > 1
          && !String.IsNullOrEmpty( ( assemblyPath = ( optionalGivenAssemblyPath ?? ( packageID + ".dll" ) ) ) ) // AssemblyPath task property was given
          )
         {
            assemblyPath = assemblyPaths
               .FirstOrDefault( ap => String.Equals( Path.GetFullPath( ap ), Path.GetFullPath( Path.Combine( Path.GetDirectoryName( ap ), assemblyPath ) ) ) );
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
      /// Gets the package ID of the SDK package for given framework. If the optional override is supplied, always returns that.
      /// </summary>
      /// <param name="framework">This <see cref="NuGetFramework"/>.</param>
      /// <param name="givenID">The optional override.</param>
      /// <returns>The value of <paramref name="givenID"/>, if it is non-<c>null</c> and not empty; otherwise tries to deduce the value from this <see cref="NuGetFramework"/>. Currently, returns value of <see cref="SDK_PACKAGE_NETSTANDARD"/> for .NET Standard and .NET Desktop frameworks, and <see cref="SDK_PACKAGE_NETCORE"/> for .NET Core frameworks.</returns>
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
            if (
               (
                  String.Equals( id, FrameworkConstants.FrameworkIdentifiers.Net, StringComparison.OrdinalIgnoreCase )
                  && framework.Version >= new Version( 4, 5 )
               ) ||
               String.Equals( id, FrameworkConstants.FrameworkIdentifiers.NetStandard, StringComparison.OrdinalIgnoreCase )
               )
            {
               id = SDK_PACKAGE_NETSTANDARD;
            }
            else if ( String.Equals( id, FrameworkConstants.FrameworkIdentifiers.NetCoreApp, StringComparison.OrdinalIgnoreCase ) )
            {
               id = SDK_PACKAGE_NETCORE;
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
                  retVal = "1.6.1";
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
                           retVal = "2.0.0";
                           break;
                        default:
                           retVal = null;
                           break;
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

      /// <summary>
      /// This is helper method to try and deduce the <see cref="NuGetFramework"/> representing the currently running process.
      /// If optional framework information is specified as parameter, this method will always return that information as wrapped around <see cref="NuGetFramework"/>.
      /// Otherwise, it will try deduce the required information from entry point assembly <see cref="System.Runtime.Versioning.TargetFrameworkAttribute"/> on desktop, and from <see cref="P:System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription"/> on core.
      /// </summary>
      /// <param name="givenInformation">Optional framework information passed from "outside world", e.g. configuration.</param>
      /// <returns>The deduced <see cref="NuGetFramework"/>, or <see cref="NuGetFramework.AnyFramework"/> if automatic deduce failed.</returns>
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
#if NET45
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
                     if ( netCoreVersion.Build == 25211 )
                     {
                        retVal = FrameworkConstants.CommonFrameworks.NetCoreApp11;
                     }
                     else
                     {
                        // NET Core 2.0, except that we don't see it as field of FrameworkConstants.CommonFrameworks, since we are targeting NuGet.Client package 4.0.0
                        retVal = NETCOREAPP20;
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

         String retVal;
         if ( !String.IsNullOrEmpty( givenRID ) )
         {
            retVal = givenRID;
         }
         else
         {
            // I wish these constants were in NuGet.Client library
            const String WIN = "win";
            const String UNIX = "unix";
            const String OSX = "osx";
#if NET45
            switch ( Environment.OSVersion.Platform )
            {
               case PlatformID.Win32NT:
               case PlatformID.Win32S:
               case PlatformID.Win32Windows:
               case PlatformID.WinCE:
                  retVal = WIN;
                  break;
               case PlatformID.Unix:
                  retVal = UNIX;
                  break;
               case PlatformID.MacOSX:
                  retVal = OSX;
                  break;
               default:
                  retVal = null;
                  break;
            }
#else

            if ( System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( System.Runtime.InteropServices.OSPlatform.Windows ) )
            {
               retVal = WIN;
            }
            else if ( System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( System.Runtime.InteropServices.OSPlatform.Linux ) )
            {
               retVal = UNIX;
            }
            else if ( System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( System.Runtime.InteropServices.OSPlatform.OSX ) )
            {
               retVal = OSX;
            }
            else
            {
               retVal = null;
            }
#endif

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