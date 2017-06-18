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

namespace UtilPack.NuGet
{

   /// <summary>
   /// This class contains extension method which are for types not contained in this library.
   /// </summary>
   public static partial class UtilPackNuGetUtility
   {
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
            retVal = Assembly.GetEntryAssembly().GetNuGetFrameworkFromAssembly();
#else
            var fwName = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            retVal = null;
            if ( !String.IsNullOrEmpty( fwName ) && fwName.StartsWith( ".NET Core" ) )
            {
               if ( Version.TryParse( fwName.Substring( 10 ), out var netCoreVersion ) )
               {
                  if ( netCoreVersion.Major == 4 )
                  {
                     if ( netCoreVersion.Minor == 0 )
                     {
                        retVal = FrameworkConstants.CommonFrameworks.NetCoreApp10;
                     }
                     else if ( netCoreVersion.Minor == 6 )
                     {
                        retVal = FrameworkConstants.CommonFrameworks.NetCoreApp11;
                     }
                  }
               }
            }
#endif
         }
         return retVal ?? NuGetFramework.AnyFramework;
      }


      /// <summary>
      /// Tries to parse the <see cref="System.Runtime.Versioning.TargetFrameworkAttribute"/> applied to this assembly into <see cref="NuGetFramework"/>.
      /// </summary>
      /// <param name="assembly">This <see cref="Assembly"/>.</param>
      /// <returns>A <see cref="NuGetFramework"/> parsed from <see cref="System.Runtime.Versioning.TargetFrameworkAttribute.FrameworkName"/>, or <see cref="NuGetFramework.AnyFramework"/> if no such attribute is applied to this assembly.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="Assembly"/> is <c>null</c>.</exception>
      public static NuGetFramework GetNuGetFrameworkFromAssembly( this Assembly assembly )
      {
         var thisFrameworkString = ArgumentValidator.ValidateNotNullReference( assembly ).GetCustomAttributes<System.Runtime.Versioning.TargetFrameworkAttribute>()
            .Select( x => x.FrameworkName )
            .FirstOrDefault();
         return thisFrameworkString == null
              ? NuGetFramework.AnyFramework
              : NuGetFramework.ParseFrameworkName( thisFrameworkString, DefaultFrameworkNameProvider.Instance );
      }
   }
}