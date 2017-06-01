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
   /// This class provides methods to resolve assembly file paths from <see cref="LocalPackageInfo"/>s, and to recursively resolve dependencies as well.
   /// </summary>
   public class NuGetPathResolver
   {
      private static readonly IEqualityComparer<LocalPackageInfo> _PackageIDEqualityComparer;

      private static readonly IEqualityComparer<LocalPackageInfo> _PackageIDAndVersionEqualityComparer;

      static NuGetPathResolver()
      {
         _PackageIDEqualityComparer = ComparerFromFunctions.NewEqualityComparer<LocalPackageInfo>(
         ( x, y ) => ReferenceEquals( x, y ) || ( x != null && y != null && String.Equals( x?.Id, y?.Id, StringComparison.OrdinalIgnoreCase ) ),
         x => x?.Id?.ToUpperInvariant()?.GetHashCode() ?? 0
         );

         _PackageIDAndVersionEqualityComparer = ComparerFromFunctions.NewEqualityComparer<LocalPackageInfo>(
         ( x, y ) => ReferenceEquals( x, y ) || ( x != null && y != null && _PackageIDEqualityComparer.Equals( x, y ) && x.Version.Equals( y.Version ) ),
         x => x?.Id?.ToUpperInvariant()?.GetHashCode() ?? 0
         );
      }

      /// <summary>
      /// Gets the <see cref="IEqualityComparer{T}"/> for <see cref="LocalPackageInfo"/> which only uses <see cref="LocalPackageInfo.Id"/> property to determine equality between two <see cref="LocalPackageInfo"/>s.
      /// </summary>
      /// <value>The <see cref="IEqualityComparer{T}"/> for <see cref="LocalPackageInfo"/> which only uses <see cref="LocalPackageInfo.Id"/> property to determine equality between two <see cref="LocalPackageInfo"/>s.</value>
      public static IEqualityComparer<LocalPackageInfo> PackageIDEqualityComparer
      {
         get
         {
            return _PackageIDEqualityComparer;
         }
      }

      /// <summary>
      /// Gets the <see cref="IEqualityComparer{T}"/> for <see cref="LocalPackageInfo"/> which uses <see cref="LocalPackageInfo.Id"/> and <see cref="LocalPackageInfo.Version"/> properties to determine equality between two <see cref="LocalPackageInfo"/>s.
      /// </summary>
      /// <value>The <see cref="IEqualityComparer{T}"/> for <see cref="LocalPackageInfo"/> which uses <see cref="LocalPackageInfo.Id"/> and <see cref="LocalPackageInfo.Version"/> properties to determine equality between two <see cref="LocalPackageInfo"/>s.</value>
      public static IEqualityComparer<LocalPackageInfo> PackageIDAndVersionEqualityComparer
      {
         get
         {
            return _PackageIDAndVersionEqualityComparer;
         }
      }

      private readonly IDictionary<String, FrameworkSpecificGroup[]> _readerCache;
      private readonly Func<PackageFolderReader, IEnumerable<FrameworkSpecificGroup>> _readerItemProducer;

      /// <summary>
      /// Creates a new instance of <see cref="NuGetPathResolver"/>.
      /// </summary>
      /// <param name="readerItemProducer">The optional callback to get enumerable of <see cref="FrameworkSpecificGroup"/> from <see cref="PackageFolderReader"/>. By default, <see cref="IPackageContentReader.GetLibItems"/> is used.</param>
      public NuGetPathResolver(
         Func<PackageFolderReader, IEnumerable<FrameworkSpecificGroup>> readerItemProducer = null
         )
      {
         this._readerCache = new Dictionary<String, FrameworkSpecificGroup[]>();
         this._readerItemProducer = readerItemProducer ?? ( r => r.GetLibItems() );
      }

      /// <summary>
      /// Gets the assembly paths for a single local NuGet package most suitable for given NuGet framework.
      /// </summary>
      /// <param name="package">This NuGet package.</param>
      /// <param name="thisFramework">Optional framework against which to match assemblies. By default, the framework of this assembly will be used.</param>
      /// <returns>An array of paths of suitable assemblies for a given NuGet framework. Will return <c>null</c> if no suitable framework is found for this package.</returns>
      /// <remarks>This method does not restore packages - it only sees those packages, which are currently locally installed.</remarks>
      /// <exception cref="ArgumentNullException">If this <see cref="LocalPackageInfo"/> is <c>null</c>.</exception>
      public String[] GetSingleNuGetPackageAssemblies(
         LocalPackageInfo package,
         NuGetFramework thisFramework = null
         )
      {
         ArgumentValidator.ValidateNotNull( nameof( package ), package );
         var suitableLibraryItem = this.GetAssembliesForSinglePackage( package, ref thisFramework );

         return suitableLibraryItem == null ?
            null :
            GetAssemblyPaths( package, suitableLibraryItem ).ToArray();
      }

      /// <summary>
      /// Gets the paths of given local NuGet package and all of its transitive dependencies.
      /// </summary>
      /// <param name="package">This NuGet package.</param>
      /// <param name="thisFramework">The framework for this package or process.</param>
      /// <param name="repositories">The repositories to use when searching for dependencies. If none specified or is <c>null</c>, a default repository (folder of <see cref="NuGetFolderPath.NuGetHome"/> combined with <c>"packages"</c>) will be used.</param>
      /// <returns>A dictionary, where each local package (including this) has assembly paths.Will return <c>null</c> if no suitable framework is found for this package.</returns>
      /// <exception cref="ArgumentNullException">If this <see cref="LocalPackageInfo"/> is <c>null</c>.</exception>
      public IDictionary<LocalPackageInfo, String[]> GetNuGetPackageAssembliesAndDependencies(
         LocalPackageInfo package,
         NuGetFramework thisFramework = null,
         IEnumerable<NuGetv3LocalRepository> repositories = null
         )
      {
         ArgumentValidator.ValidateNotNull( nameof( package ), package );

         var suitableLibraryItem = this.GetAssembliesForSinglePackage( package, ref thisFramework );
         IDictionary<LocalPackageInfo, String[]> retVal;

         if ( suitableLibraryItem != null )
         {
            // Check repositories
            var repos = repositories?.ToArray();
            if ( repos.IsNullOrEmpty() )
            {
               // Let's not deduce repo path based on package.ExpandedPath, since that logic may change some day.
               repos = new[] { new NuGetv3LocalRepository( Path.Combine( NuGetEnvironment.GetFolderPath( NuGetFolderPath.NuGetHome ), "packages" ) ) };
            }

            // We consider dependency chain item visited when there are two same packages (same id + same version) - the local assembly info is actually ignored in such equality comparison
            var allLoadableInfo = (package, suitableLibraryItem.TargetFramework, suitableLibraryItem).AsDepthFirstEnumerableWithLoopDetection(
             curTuple => this.GetSinglePackageDependencies( thisFramework, repositories, curTuple ),
             returnHead: true,
             equalityComparer: ComparerFromFunctions.NewEqualityComparer<(LocalPackageInfo, NuGetFramework, FrameworkSpecificGroup)>(
                ( x, y ) => PackageIDAndVersionEqualityComparer.Equals( x.Item1, y.Item1 ),
                x => PackageIDAndVersionEqualityComparer.GetHashCode( x.Item1 )
                )
             );

            // Take only the newest versions of each package id
            var multiplePackages = new Dictionary<LocalPackageInfo, (NuGetVersion, FrameworkSpecificGroup)>( PackageIDEqualityComparer );
            foreach ( var info in allLoadableInfo )
            {
               if ( info.Item3 != null )
               {
                  if ( !multiplePackages.TryGetValue( info.Item1, out var cur ) )
                  {
                     multiplePackages.Add( info.Item1, (info.Item1.Version, info.Item3) );
                  }
                  else if ( info.Item1.Version > cur.Item1 )
                  {
                     // Remove the item, since it is used as a key
                     multiplePackages.Remove( info.Item1 );
                     // Then add
                     multiplePackages.Add( info.Item1, (info.Item1.Version, info.Item3) );
                  }
               }
            }

            // Now construct the dictionary to return
            retVal = multiplePackages.ToDictionary(
               kvp => kvp.Key,
               kvp => GetAssemblyPaths( kvp.Key, kvp.Value.Item2 ).ToArray(),
               PackageIDEqualityComparer
               );
         }
         else
         {
            retVal = null;
         }
         return retVal;
      }

      /// <summary>
      /// Clears cached data of this <see cref="NuGetPathResolver"/>.
      /// </summary>
      /// <param name="packagePath">The specific package path (value of <see cref="LocalPackageInfo.ExpandedPath"/>). If not supplied, cached data of all currently seen package paths is cleared.</param>
      public void ClearCache( String packagePath = null )
      {
         if ( String.IsNullOrEmpty( packagePath ) )
         {
            this._readerCache.Clear();
         }
         else
         {
            this._readerCache.Remove( packagePath );
         }
      }

      private FrameworkSpecificGroup GetAssembliesForSinglePackage(
         LocalPackageInfo package,
         ref NuGetFramework thisFramework
         )
      {
         // We need to detect which lib folder is most suitable for current runtime
         // For that, we need to know the NuGetFramework of current runtime
         if ( thisFramework == null )
         {
            // Try auto-detect (No Assembly.GetEntryAssembly() for .NET Standard 1.3, only appears in 1.5 )
            thisFramework = typeof( UtilPackExtensions ).GetTypeInfo().Assembly.GetNuGetFrameworkFromAssembly();
         }

         return this.GetAssembliesForSinglePackage( package, thisFramework );
      }

      private FrameworkSpecificGroup GetAssembliesForSinglePackage( LocalPackageInfo package, NuGetFramework framework )
      {
         var items = this._readerCache.GetOrAdd_NotThreadSafe( package.ExpandedPath, path =>
         {
            using ( var reader = new PackageFolderReader( path ) )
            {
               return this._readerItemProducer( reader ).ToArray();
            }
         } );

         return NuGetFrameworkUtility.GetNearest(
            items,
            framework,
            li => li.TargetFramework
            );
      }

      private IEnumerable<(LocalPackageInfo Package, NuGetFramework TargetFW, FrameworkSpecificGroup Assemblies)> GetSinglePackageDependencies(
         NuGetFramework thisRuntimeFW,
         IEnumerable<NuGetv3LocalRepository> repositories,
         (LocalPackageInfo Package, NuGetFramework TargetFW, FrameworkSpecificGroup Assemblies) package
      )
      {
         var dependencies = NuGetFrameworkUtility.GetNearest(
            package.Package.Nuspec.GetDependencyGroups(),
            package.TargetFW,
            d => d.TargetFramework
            );
         if ( dependencies != null )
         {
            foreach ( var dependencyPackage in dependencies.Packages )
            {
               var currentDependency = dependencyPackage;
               // NuGetv3LocalRepositoryUtility.GetPackage takes exactly one version, and here we have a version range
               var suitableLocalPackage = repositories
                  .SelectMany( repo => repo.FindPackagesById( currentDependency.Id ) )
                  .FindBestMatch( currentDependency.VersionRange, l => l?.Version );
               if ( suitableLocalPackage != null )
               {
                  var suitableLibraryFolder = this.GetAssembliesForSinglePackage( suitableLocalPackage, thisRuntimeFW );
                  yield return (suitableLocalPackage, suitableLibraryFolder?.TargetFramework ?? package.TargetFW, suitableLibraryFolder);
               }
            }
         }

      }

      private static IEnumerable<String> GetAssemblyPaths(
         LocalPackageInfo package,
         FrameworkSpecificGroup group
         )
      {
         return group.Items
            .Where( li => String.Equals( Path.GetExtension( li ), ".dll", StringComparison.OrdinalIgnoreCase ) )
            .Select( relPath => Path.GetFullPath( Path.Combine( package.ExpandedPath, relPath ) ) );
      }
   }

   /// <summary>
   /// This class contains extension method which are for types not contained in this library.
   /// </summary>
   public static partial class UtilPackExtensions
   {
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
              : NuGetFramework.ParseFrameworkName( thisFrameworkString, new DefaultFrameworkNameProvider() );
      }
   }
}

/// <summary>
/// This class contains extension methods defined in UtilPack product family.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Gets the paths of given local NuGet package and all of its transitive dependencies.
   /// </summary>
   /// <param name="resolver">This <see cref="NuGetPathResolver"/>.</param>
   /// <param name="package">This NuGet package.</param>
   /// <param name="repositories">The repositories to use when searching for dependencies. If none specified, a default repository (folder of <see cref="NuGetFolderPath.NuGetHome"/> combined with <c>"packages"</c>) will be used.</param>
   /// <returns>A dictionary, where each local package (including this) has assembly paths.Will return <c>null</c> if no suitable framework is found for this package.</returns>
   /// <remarks>This method does not restore packages - it only sees those packages, which are currently locally installed.</remarks>
   /// <exception cref="NullReferenceException">If this <see cref="NuGetPathResolver"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="package"/> is <c>null</c>.</exception>
   public static IDictionary<LocalPackageInfo, String[]> GetNuGetPackageAssembliesAndDependencies(
      this NuGetPathResolver resolver,
      LocalPackageInfo package,
      params NuGetv3LocalRepository[] repositories
      )
   {
      return ArgumentValidator.ValidateNotNullReference( resolver )
         .GetNuGetPackageAssembliesAndDependencies( package, null, repositories );
   }
}