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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UtilPack.NuGet;
using UtilPack.NuGet.AssemblyLoading;
using TResolveResult = System.Collections.Generic.IDictionary<System.String, UtilPack.NuGet.AssemblyLoading.ResolvedPackageInfo>;
using TNuGetResolver = UtilPack.NuGet.AssemblyLoading.NuGetResolverWrapper;
using NuGet.ProjectModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using System.Threading;

namespace UtilPack.NuGet.AssemblyLoading
{
   // This interface should not expose any UtilPack members in order to avoid app domain to load utilpack assembly
   public interface NuGetAssemblyResolver : IDisposable
   {
      // TODO maybe expose members which do not use tuples?

      Task<Assembly[]> LoadNuGetAssemblies(
         String[] packageIDs,
         String[] packageVersions,
         String[] assemblyPaths
         );

      Assembly LoadOtherAssembly(
         String assemblyPath
         );

      event Action<String> ResolveLogEvent;
   }



   public static class NuGetAssemblyResolverFactory
   {
      public static NuGetAssemblyResolver NewNuGetAssemblyResolver(
         BoundRestoreCommandUser restorer,
#if NET45
         AppDomainSetup appDomainSetup,
#else 
         LockFile thisFrameworkRestoreResult,
#endif
         out
#if NET45
         AppDomain
#else
         System.Runtime.Loader.AssemblyLoadContext
#endif
         createdLoader,
#if NET45
         Func<AssemblyName, String> overrideLocation = null,
#else
         Func<AssemblyName, Boolean> additionalCheckForDefaultLoader = null, // return true if nuget-based assembly loading should not be used
#endif
         GetFileItemsDelegate defaultGetFiles = null,
         Func<String, String> pathProcessor = null
         )
      {
         if ( defaultGetFiles == null )
         {
            defaultGetFiles = ( targetLib, libs ) => targetLib.RuntimeAssemblies.Select( i => i.Path );
         }
         var resolver = new NuGetResolverWrapper( restorer, ( targetLib, libs ) => defaultGetFiles( targetLib, libs ).FilterUnderscores() );

         NuGetAssemblyResolver retVal;
#if NET45
         // It would be nice to do as in .NET Core - to create NuGetAssemblyResolverImpl right here.
         // However, using it would be quite annoying - since it would reside in this domain, it would need to do assembly loading in target domain.
         // That would mean serializing assembly as it would move cross-app-domains... it would simply not work.
         // Therefore, we *must* create instance of NuGetAssemblyResolverImpl in target appdomain, if we are provided with app domain setup.
         // Because of this, we have to be very careful not to load any extra assemblies in there.
         createdLoader = appDomainSetup == null ? AppDomain.CurrentDomain : AppDomain.CreateDomain( "NuGet assembly loader domain", AppDomain.CurrentDomain.Evidence, appDomainSetup );
         var cbWrapper = overrideLocation == null ? null : new CallbackWrapper( overrideLocation );
         var ppWrapper = pathProcessor == null ? null : new PathProcessorWrapper( pathProcessor );
         retVal = appDomainSetup == null ?
            new NuGetAssemblyResolverImpl( resolver, cbWrapper, ppWrapper ) :
            (NuGetAssemblyResolver) createdLoader.CreateInstanceFromAndUnwrap(
               Path.GetFullPath( new Uri( typeof( NuGetAssemblyResolverFactory ).Assembly.CodeBase ).LocalPath ),
               typeof( NuGetAssemblyResolverImpl ).FullName,
               false,
               0,
               null,
               new Object[] { resolver, cbWrapper, ppWrapper },
               null,
               null
            );
#else
         var loader = new NuGetAssemblyLoadContext( restorer, thisFrameworkRestoreResult, additionalCheckForDefaultLoader );
         retVal = loader.SetResolver( new NuGetAssemblyResolverImpl( resolver, loader, pathProcessor ) );
         createdLoader = loader;
#endif

         return retVal;

      }
   }

   public static class NuGetAssemblyResolverUtility
   {
      public static String GetAssemblyPathFromNuGetAssemblies(
         String[] assemblyPaths,
         String packageExpandedPath,
         String optionalGivenAssemblyPath
         )
      {
         String assemblyPath = null;
         if ( assemblyPaths.Length == 1 || (
               assemblyPaths.Length > 1 // There is more than 1 possible assembly
               && !String.IsNullOrEmpty( ( assemblyPath = optionalGivenAssemblyPath ) ) // AssemblyPath task property was given
               && ( assemblyPath = Path.GetFullPath( ( Path.Combine( packageExpandedPath, assemblyPath ) ) ) ).StartsWith( packageExpandedPath ) // The given assembly path truly resides in the package folder
               ) )
         {
            // TODO maybe check that assembly path is in possibleAssemblies array?
            if ( assemblyPath == null )
            {
               assemblyPath = assemblyPaths[0];
            }
         }
         return assemblyPath;
      }
   }

#if !NET45
   internal sealed class NuGetAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
   {
      private readonly ISet<String> _frameworkAssemblySimpleNames;
      private readonly System.Runtime.Loader.AssemblyLoadContext _defaultLoadContext;
      private readonly ConcurrentDictionary<AssemblyName, Lazy<Assembly>> _loadedAssemblies; // We will get multiple request for same assembly name, so let's cache them
      private readonly Func<AssemblyName, Boolean> _additionalCheckForDefaultLoader;
      private NuGetAssemblyResolverImpl _resolver;


      public NuGetAssemblyLoadContext(
         BoundRestoreCommandUser restorer,
         LockFile thisFrameworkRestoreResult,
         Func<AssemblyName, Boolean> additionalCheckForDefaultLoader
         )
      {
         this._defaultLoadContext = GetLoadContext( this.GetType().GetTypeInfo().Assembly );
         this._loadedAssemblies = new ConcurrentDictionary<AssemblyName, Lazy<Assembly>>(
            ComparerFromFunctions.NewEqualityComparer<AssemblyName>(
               ( x, y ) => String.Equals( x.Name, y.Name ) && String.Equals( x.CultureName, y.CultureName ) && x.Version.Equals( y.Version ) && ArrayEqualityComparer<Byte>.ArrayEquality( x.GetPublicKeyToken(), y.GetPublicKeyToken() ),
               x => x.Name.GetHashCode()
               )
            );
         // .NET Core is package-based framework, so we need to find out which packages are part of framework, and which ones are actually client ones.
         this._frameworkAssemblySimpleNames = new HashSet<String>(
            restorer.ExtractAssemblyPaths( thisFrameworkRestoreResult, ( lib, libs ) => lib.CompileTimeAssemblies.Select( i => i.Path ).FilterUnderscores() ).Values
               .SelectMany( p => p.Assemblies )
               .Select( p => Path.GetFileNameWithoutExtension( p ) ) // For framework assemblies, we can assume that file name without extension = assembly name
            );
         this._additionalCheckForDefaultLoader = additionalCheckForDefaultLoader;
      }

      internal NuGetAssemblyResolver SetResolver( NuGetAssemblyResolverImpl resolver )
      {
         if ( resolver != null )
         {
            System.Threading.Interlocked.CompareExchange( ref this._resolver, resolver, null );
         }
         return resolver;
      }

      protected override Assembly Load( AssemblyName assemblyName )
      {
         return this._loadedAssemblies.GetOrAdd( assemblyName, an => new Lazy<Assembly>( () =>
         {
            Assembly retVal = null;
            if ( this._frameworkAssemblySimpleNames.Contains( an.Name ) || ( this._additionalCheckForDefaultLoader?.Invoke( assemblyName ) ?? false ) )
            {
               // We use default loader for framework assemblies, in order to avoid loading from different path for same assembly name.
               try
               {
                  retVal = this._defaultLoadContext.LoadFromAssemblyName( assemblyName );
               }
               catch
               {
                  // Ignore
               }
            }
            return retVal ?? this._resolver.PerformAssemblyResolve( an );
         }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) ).Value;
      }
   }
#endif

   // This class will exist in separate app domain in .NET Desktop, so no UtilPack stuff here.

   internal sealed class NuGetAssemblyResolverImpl :
#if NET45
      MarshalByRefObject,
#endif

      NuGetAssemblyResolver, IDisposable
   {
      private sealed class AssemblyNameComparer : IEqualityComparer<AssemblyName>
      {

         private AssemblyNameComparer()
         {

         }
         bool IEqualityComparer<AssemblyName>.Equals( AssemblyName x, AssemblyName y )
         {
            // Compare just name + public key, as we might get different version assembly
            Boolean AllMatch( Byte[] first, Byte[] second )
            {
               var len = first.Length;
               for ( var i = 0; i < len; ++i )
               {
                  if ( first[i] != second[i] )
                  {
                     return false;
                  }
               }

               return true;
            }

            var retVal = String.Equals( x?.Name, y?.Name );
            if ( retVal && x != null && y != null )
            {
               var xpk = x.GetPublicKeyToken();
               var ypk = y.GetPublicKeyToken();
               retVal = ( ( xpk == null || xpk.Length == 0 ) && ( ypk == null || ypk.Length == 0 ) )
                  || ( xpk != null && ypk != null && xpk.Length == ypk.Length && AllMatch( xpk, ypk ) );
            }

            return retVal;
         }

         Int32 IEqualityComparer<AssemblyName>.GetHashCode( AssemblyName obj )
         {
            return obj?.Name?.GetHashCode() ?? 0;
         }

         internal static readonly IEqualityComparer<AssemblyName> Instance = new AssemblyNameComparer();
      }

      private sealed class AssemblyInformation
      {
         public AssemblyInformation(
            String path,
            Func<String, Assembly> fromPathLoader,
            Func<String, String> pathProcessor
            )
         {
            this.Path = path;
            this.Assembly = new Lazy<System.Reflection.Assembly>( () =>
            {
               String processed;
               return fromPathLoader( String.IsNullOrEmpty( ( processed = pathProcessor?.Invoke( path ) ) ) ?
                  path :
                  processed );
            }, LazyThreadSafetyMode.ExecutionAndPublication );
         }
         public String Path { get; }
         public Lazy<Assembly> Assembly { get; }
      }

      private readonly ConcurrentDictionary<AssemblyName, AssemblyInformation> _assemblies;
      private readonly ConcurrentDictionary<String, Lazy<AssemblyName>> _assemblyNames;
      private readonly TNuGetResolver _resolver;
      private readonly Func<String, Assembly> _fromPathLoader;
      private readonly Func<AssemblyName, Assembly> _fromNameLoader;
      private readonly Func<String, String> _pathProcessor;
#if NET45
      private readonly CallbackWrapper _callbackWrapper;
      private readonly ThreadLocal<Boolean> _insideResolve;
#endif

      public NuGetAssemblyResolverImpl(
#if NET45
         Object
#else
         TNuGetResolver
#endif
         resolver,
#if NET45
         Object callbackWrapper,
#else
         System.Runtime.Loader.AssemblyLoadContext loader,
#endif
#if NET45
         Object
#else
         Func<String, String>
#endif
         pathProcessor
         )
      {

#if NET45
         this._insideResolve = new ThreadLocal<Boolean>( () => false );
         AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#endif

#if !NET45
         ArgumentValidator.ValidateNotNull( nameof( loader ), loader );
#endif
         this._fromPathLoader =
#if NET45
            Assembly.LoadFile
#else
            path => loader.LoadFromAssemblyPath( path )
#endif
            ;
         this._fromNameLoader =
#if NET45
         Assembly.Load
#else
         name => loader.LoadFromAssemblyName( name )
#endif
         ;

         this._resolver =
#if NET45
            (TNuGetResolver)
#endif
            resolver ?? throw new ArgumentNullException( nameof( resolver ) );

#if NET45
         this._callbackWrapper = (CallbackWrapper) callbackWrapper;
#endif
         this._pathProcessor =
#if NET45
            pathProcessor == null ? (Func<String, String>) null : ( (PathProcessorWrapper) pathProcessor ).ProcessPath
#else
            pathProcessor
#endif
            ;
         this._assemblyNames = new ConcurrentDictionary<String, Lazy<AssemblyName>>();
         this._assemblies = new ConcurrentDictionary<AssemblyName, AssemblyInformation>( AssemblyNameComparer.Instance );
      }

#if NET45

      private Assembly CurrentDomain_AssemblyResolve( Object sender, ResolveEventArgs args )
      {
         Assembly retVal;
         var name = new AssemblyName( args.Name );
         // We must use direct call to resolve method only if we are already inside nested resolving.
         // Otherwise, we must always try to resolve by name - in order to avoid loading System.Threading.Tasks.Extensions and System.ValueTuple twice.
         if ( this._insideResolve.Value )
         {
            retVal = this.PerformAssemblyResolve( name );
         }
         else
         {
            this._insideResolve.Value = true;
            try
            {
               String overrideLocation;
               if ( !String.IsNullOrEmpty( overrideLocation = this._callbackWrapper?.OverrideLocation( name ) ) )
               {
                  retVal = this._assemblies.AddOrUpdate(
                     name,
                     an => new AssemblyInformation( overrideLocation, this._fromPathLoader, this._pathProcessor ),
                     ( an, existing ) => new AssemblyInformation( overrideLocation, this._fromPathLoader, this._pathProcessor )
                     ).Assembly.Value;
               }
               else
               {
                  retVal = this._fromNameLoader( name );
               }
            }
            finally
            {
               this._insideResolve.Value = false;
            }
         }
         return retVal;
      }

#endif

      public void Dispose()
      {
#if NET45
         AppDomain.CurrentDomain.AssemblyResolve -= this.CurrentDomain_AssemblyResolve;
#endif
         this._assemblies.Clear();
      }

      public async Task<Assembly[]> LoadNuGetAssemblies(
         String[] packageIDs,
         String[] packageVersions,
         String[] assemblyPaths
         )
      {
         Assembly[] retVal;
         if ( packageIDs != null
            && packageVersions != null
            && assemblyPaths != null
            && packageIDs.Length > 0
            && packageIDs.Length == packageVersions.Length
            && packageIDs.Length == assemblyPaths.Length
            )
         {
            var assemblyInfos =
#if NET45
            await this.UseResolver( packageIDs, packageVersions )
#else
            this._resolver.Resolver.ExtractAssemblyPaths(
               await this._resolver.Resolver.RestoreIfNeeded( packageIDs.Select( ( p, idx ) => (p, packageVersions[idx]) ).ToArray() ),
               this._resolver.GetFiles
               )
#endif
            ;

            retVal = new Assembly[packageIDs.Length];
            if ( assemblyInfos != null )
            {
               var assemblies = this._assemblies;
               // TODO there is some performance optimizing here left to do, as not nearly all assembly names are actually used.
               // .NET Core uses default loader for all system assemblies, and in .NET Desktop system assemblies are loaded from GAC.
               var assemblyNames = assemblyInfos.Values
                  .SelectMany( v => v.Assemblies )
                  .Distinct()
                  .ToDictionary(
                     p => p,
                     p => this._assemblyNames.GetOrAdd( p, pp => new Lazy<AssemblyName>( () =>
#if !NET45
               System.Runtime.Loader.AssemblyLoadContext
#else
               AssemblyName
#endif
               .GetAssemblyName( pp ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) )
                     );

               foreach ( var kvp in assemblyNames )
               {
                  var curPath = kvp.Key;
                  assemblies.TryAdd(
                     kvp.Value.Value,
                     new AssemblyInformation( curPath, this._fromPathLoader, this._pathProcessor )
                     );
               }

               for ( var i = 0; i < packageIDs.Length; ++i )
               {
                  if ( assemblyInfos.TryGetValue( packageIDs[i], out var possibleAssemblyPaths ) )
                  {
                     var assemblyPath = NuGetAssemblyResolverUtility.GetAssemblyPathFromNuGetAssemblies(
                        possibleAssemblyPaths.Assemblies,
                        possibleAssemblyPaths.PackageDirectory,
                        assemblyPaths[i]
                        );
                     if ( !String.IsNullOrEmpty( assemblyPath ) )
                     {
                        var name = assemblyNames[assemblyPath].Value;
                        retVal[i] = this._fromNameLoader( name );
                     }
                  }
               }
            }
         }
         else
         {
            // Don't use Empty<..> as it will cause UtilPack to load in this app domain.
            retVal = new Assembly[0];
         }
         return retVal;
      }

      public Assembly LoadOtherAssembly(
         String assemblyPath
         )
      {
         assemblyPath = Path.GetFullPath( assemblyPath );
         var retVal = this._assemblies.Values.FirstOrDefault( v => String.Equals( v.Path, assemblyPath ) )?.Assembly?.Value;

         if ( retVal == null && File.Exists( assemblyPath ) )
         {
            var assemblyName = this._assemblyNames.GetOrAdd( assemblyPath, p => new Lazy<AssemblyName>( () =>
#if !NET45
               System.Runtime.Loader.AssemblyLoadContext
#else
               AssemblyName
#endif
               .GetAssemblyName( p ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) ).Value;
            this._assemblies.GetOrAdd(
               assemblyName,
               an => new AssemblyInformation( assemblyPath, this._fromPathLoader, this._pathProcessor )
               );

            retVal = this._fromNameLoader( assemblyName );
         }
         return retVal;
      }

      public event Action<String> ResolveLogEvent;

      internal Assembly PerformAssemblyResolve( AssemblyName assemblyName )
      {
         Assembly retVal;
         if ( this._assemblies.TryGetValue( assemblyName, out var assemblyInfo ) )
         {
            retVal = assemblyInfo.Assembly.Value;
            this.LogResolveMessage( $"Found \"{retVal.FullName}\" by name \"{assemblyName.Name}\" in \"{retVal.CodeBase}\"." );
         }
         else
         {
            this.LogResolveMessage( $"Failed to find \"{assemblyName}\" by simple name \"{assemblyName.Name}\"." );
            retVal = null;
         }
         return retVal;
      }

      private void LogResolveMessage( String message )
      {
         this.ResolveLogEvent?.Invoke( message );
      }




#if NET45

      private System.Threading.Tasks.Task<TResolveResult> UseResolver(
         String[] packageIDs,
         String[] packageVersions
         )
      {
         var setter = new MarshaledResultSetter<TResolveResult>();
         this._resolver.ResolveNuGetPackageAssemblies(
            packageIDs,
            packageVersions,
            setter
            );
         return setter.Task;
      }
#endif

   }

#if NET45
   internal sealed class CallbackWrapper : MarshalByRefObject
   {
      private readonly Func<AssemblyName, String> _callback;

      public CallbackWrapper( Func<AssemblyName, String> callback )
      {
         this._callback = callback;
      }

      public String OverrideLocation( AssemblyName assemblyName )
      {
         var retVal = this._callback?.Invoke( assemblyName );
         return String.IsNullOrEmpty( retVal ) ? null : retVal;
      }
   }

   internal sealed class PathProcessorWrapper : MarshalByRefObject
   {
      private readonly Func<String, String> _callback;

      public PathProcessorWrapper( Func<String, String> callback )
      {
         this._callback = callback;
      }

      public String ProcessPath( String originalPath )
      {
         return this._callback( originalPath );
      }
   }
#endif


   internal sealed class NuGetResolverWrapper
#if NET45
      : MarshalByRefObject
#endif
   {


      public NuGetResolverWrapper(
         BoundRestoreCommandUser resolver,
         GetFileItemsDelegate getFiles
         )
      {
         this.Resolver = resolver;
         this.GetFiles = getFiles;
      }

#if NET45

      internal void ResolveNuGetPackageAssemblies(
         String[] packageID,
         String[] packageVersion,
         MarshaledResultSetter<TResolveResult> setter
         )
      {
         var task = this.Resolver.RestoreIfNeeded( packageID.Select( ( pID, idx ) => (pID, packageVersion[idx]) ).ToArray() );
         task.ContinueWith( prevTask =>
         {
            try
            {
               var result = prevTask.Result;
               setter.SetResult( this.Resolver.ExtractAssemblyPaths( result, this.GetFiles ) );
            }
            catch
            {
               setter.SetResult( null );
            }
         } );
      }
#endif

#if NET45
      private
#else
      public
#endif
         BoundRestoreCommandUser Resolver
      { get; }

#if NET45
      private
#else
      public
#endif
         GetFileItemsDelegate GetFiles
      { get; }
   }


#if NET45
   internal sealed class MarshaledResultSetter<T> : MarshalByRefObject
   {
      private readonly TaskCompletionSource<T> _tcs;

      public MarshaledResultSetter()
      {
         this._tcs = new TaskCompletionSource<T>();
      }

      public void SetResult( T result ) => this._tcs.SetResult( result );
      public Task<T> Task => this._tcs.Task;
   }

#endif

#if NET45
   [Serializable] // We want to be serializable instead of MarshalByRef as we want to copy these objects
#endif
   internal sealed class ResolvedPackageInfo
   {
      public ResolvedPackageInfo( String packageDirectory, String[] assemblies )
      {
         this.PackageDirectory = packageDirectory;
         this.Assemblies = assemblies;
      }

      public String PackageDirectory { get; }
      public String[] Assemblies { get; }
   }
}

public static partial class E_UtilPack
{
   internal static TResolveResult ExtractAssemblyPaths(
      this BoundRestoreCommandUser restorer,
      LockFile lockFile,
      GetFileItemsDelegate fileGetter = null
   )
   {
      return restorer.ExtractAssemblyPaths(
         lockFile,
         ( packageFolder, filePaths ) => new ResolvedPackageInfo( packageFolder, filePaths.ToArray() ),
         fileGetter
         );
   }

   public static async Task<Assembly> LoadNuGetAssembly(
      this NuGetAssemblyResolver resolver,
      String packageID,
      String packageVersion,
      String assemblyPath = null
      )
   {
      return ( await resolver.LoadNuGetAssemblies( new[] { packageID }, new[] { packageVersion }, new[] { assemblyPath } ) )[0];
   }

   public static Task<Assembly[]> LoadNuGetAssemblies(
      this NuGetAssemblyResolver resolver,
      params (String PackageID, String PackageVersion, String AssemblyPath)[] packageInfo
      )
   {
      return resolver.LoadNuGetAssemblies(
         packageInfo.Select( p => p.PackageID ).ToArray(),
         packageInfo.Select( p => p.PackageVersion ).ToArray(),
         packageInfo.Select( p => p.AssemblyPath ).ToArray() );
   }

   internal static IEnumerable<String> FilterUnderscores( this IEnumerable<String> paths )
   {
      return paths?.Where( p => !p.EndsWith( "_._" ) );
   }
}