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
using TNuGetResolver = UtilPack.NuGet.AssemblyLoading.NuGetRestorerWrapper;
using NuGet.ProjectModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using System.Threading;

namespace UtilPack.NuGet.AssemblyLoading
{
   /// <summary>
   /// This interface provides uniform way of dynamically load assemblies from NuGet packages and file paths.
   /// Instances of this interface are created by <see cref="NuGetAssemblyResolverFactory"/> class.
   /// </summary>
   public interface NuGetAssemblyResolver : IDisposable
   {
      /// <summary>
      /// Asynchronously loads assemblies from given NuGet packages. 
      /// The packages are restored first, by using whatever <see cref="global::NuGet.Configuration.ISettings"/> provided to underlying <see cref="NuGet.BoundRestoreCommandUser"/>.
      /// The assemblies of packages on which <paramref name="packageIDs"/> depend on will be loaded as they are needed.
      /// </summary>
      /// <param name="packageIDs">The IDs of the packages to restore and load assembly from.</param>
      /// <param name="packageVersions">The versions of the packages to restore and load assembly from.</param>
      /// <param name="assemblyPaths">The assembly paths within the package to load.</param>
      /// <returns>Task which on completion has array of loaded <see cref="Assembly"/> objects for each given package ID in <paramref name="packageIDs"/>.</returns>
      /// <remarks>
      /// All arrays <paramref name="packageIDs"/>, <paramref name="packageVersions"/>, and <paramref name="assemblyPaths"/> must have matching length.
      /// The elements in <paramref name="packageVersions"/> and <paramref name="assemblyPaths"/> may be <c>null</c> or empty.
      /// See <see cref="BoundRestoreCommandUser.RestoreIfNeeded"/> documentation on behaviour for situations when package version is left out.
      /// If assembly path is left out and package only has one assembly, it will be used.
      /// If package has more than one assembly, then the assembly path must be specified.
      /// </remarks>
      Task<Assembly[]> LoadNuGetAssemblies(
         String[] packageIDs,
         String[] packageVersions,
         String[] assemblyPaths
         );

      /// <summary>
      /// Loads assembly from disk given the path.
      /// Note that dependencies will not be resolved.
      /// </summary>
      /// <param name="assemblyPath">The assembly path where to load assembly from.</param>
      /// <returns>Assembly loaded from disk, or cached assembly if it was previously already loaded by this <see cref="NuGetAssemblyResolver"/>.</returns>
      Assembly LoadOtherAssembly(
         String assemblyPath
         );

      /// <summary>
      /// This event occurs whenever assembly has been successfully loaded by this <see cref="NuGetAssemblyResolver"/>.
      /// </summary>
      /// <seealso cref="AssemblyLoadSuccessEventArgs"/>
      event Action<AssemblyLoadSuccessEventArgs> OnAssemblyLoadSuccess;

      /// <summary>
      /// This event occurs whenever assembly failed to resolve or load by this <see cref="NuGetAssemblyResolver"/>.
      /// </summary>
      /// <seealso cref="AssemblyLoadFailedEventArgs"/>
      event Action<AssemblyLoadFailedEventArgs> OnAssemblyLoadFail;

      Assembly TryResolveFromPreviouslyLoaded( AssemblyName assemblyName );
   }

   /// <summary>
   /// This is abstract base class for <see cref="AssemblyLoadSuccessEventArgs"/> and <see cref="AssemblyLoadFailedEventArgs"/> types.
   /// </summary>
   public abstract class AbstractAssemblyResolveArgs
   {
      /// <summary>
      /// Initializes new instance of <see cref="AbstractAssemblyResolveArgs"/>.
      /// </summary>
      /// <param name="assemblyName">The <see cref="System.Reflection.AssemblyName"/> related to current event.</param>
      public AbstractAssemblyResolveArgs(
         AssemblyName assemblyName
         )
      {
         this.AssemblyName = assemblyName;
      }

      /// <summary>
      /// Gets the <see cref="System.Reflection.AssemblyName"/> related to current event.
      /// </summary>
      /// <value>The <see cref="System.Reflection.AssemblyName"/> related to current event.</value>
      public AssemblyName AssemblyName { get; }
   }

   /// <summary>
   /// This class contains information for <see cref="NuGetAssemblyResolver.OnAssemblyLoadSuccess"/> event.
   /// </summary>
   public class AssemblyLoadSuccessEventArgs : AbstractAssemblyResolveArgs
   {
      /// <summary>
      /// Creates new instance of <see cref="AssemblyLoadSuccessEventArgs"/>
      /// </summary>
      /// <param name="assemblyName">The <see cref="System.Reflection.AssemblyName"/> related to current event.</param>
      /// <param name="originalPath">The original path where assembly resides.</param>
      /// <param name="actualPath">The path where assembly is loaded from.</param>
      public AssemblyLoadSuccessEventArgs(
         AssemblyName assemblyName,
         String originalPath,
         String actualPath
         ) : base( assemblyName )
      {
         this.OriginalPath = originalPath;
         this.ActualPath = actualPath;
      }

      /// <summary>
      /// Gets the original path where assembly resides.
      /// </summary>
      /// <value>The original path where assembly resides.</value>
      public String OriginalPath { get; }

      /// <summary>
      /// Gets the path where assembly is loaded from.
      /// </summary>
      /// <value>The path where assembly is loaded from.</value>
      public String ActualPath { get; }
   }

   /// <summary>
   /// This class contains information for <see cref="NuGetAssemblyResolver.OnAssemblyLoadFail"/> event.
   /// </summary>
   public class AssemblyLoadFailedEventArgs : AbstractAssemblyResolveArgs
   {
      /// <summary>
      /// Creates new instance of <see cref="AssemblyLoadFailedEventArgs"/>
      /// </summary>
      /// <param name="assemblyName">The <see cref="System.Reflection.AssemblyName"/> related to current event.</param>
      public AssemblyLoadFailedEventArgs(
         AssemblyName assemblyName
         ) : base( assemblyName )
      {

      }

   }

   /// <summary>
   /// This class provides method to create new instances of <see cref="NuGetAssemblyResolver"/>.
   /// </summary>
   public static class NuGetAssemblyResolverFactory
   {
#if NET45
      /// <summary>
      /// Creates new instance of <see cref="NuGetAssemblyResolver"/> which will reside in a new <see cref="AppDomain"/> if <paramref name="appDomainSetup"/> is specified.
      /// </summary>
      /// <param name="restorer">The <see cref="BoundRestoreCommandUser"/> to use when restoring packages.</param>
      /// <param name="appDomainSetup">The <see cref="AppDomainSetup"/> to use when creating new <see cref="AppDomain"/>. Specify <c>null</c> if creating <see cref="NuGetAssemblyResolver"/> in this app domain.</param>
      /// <param name="createdLoader">This parameter will contain the newly created <see cref="AppDomain"/>, or <see cref="AppDomain.CurrentDomain"/> if <paramref name="appDomainSetup"/> is not specified.</param>
      /// <param name="overrideLocation">The optional callback to override location of assembly to be loaded. If it is called, then <paramref name="pathProcessor"/> will not be used.</param>
      /// <param name="defaultGetFiles">The optional callback to give to <see cref="M:E_UtilPack.ExtractAssemblyPaths{TResult}(BoundRestoreCommandUser, LockFile, Func{string, IEnumerable{string}, TResult}, GetFileItemsDelegate)"/> method.</param>
      /// <param name="pathProcessor">The optional callback to process assembly path just before it is loaded. It can e.g. copy assembly to some temp folder in order to avoid locking assembly in package repository.</param>
      /// <returns>A new instance of <see cref="NuGetAssemblyResolver"/>.</returns>
      /// <remarks>
      /// Please be aware that if <paramref name="appDomainSetup"/> is specified, a new app domain will be created!
      /// This app domain and other resources will be unloaded and cleared on calling <see cref="IDisposable.Dispose"/> method on returned <see cref="NuGetAssemblyResolver"/>.
      /// </remarks>
#else
      /// <summary>
      /// Creates new instance of <see cref="NuGetAssemblyResolver"/> and <see cref="System.Runtime.Loader.AssemblyLoadContext"/> to be used to load assemblies of NuGet packages.
      /// </summary>
      /// <param name="restorer">The <see cref="BoundRestoreCommandUser"/> to use when restoring packages.</param>
      /// <param name="thisFrameworkRestoreResult">The <see cref="LockFile"/> obtained by restoring the framework package. For .NET Core, this framework package is the one with ID <c>Microsoft.NETCore.App</c>.</param>
      /// <param name="createdLoader">This parameter will contain the newly created <see cref="System.Runtime.Loader.AssemblyLoadContext"/>.</param>
      /// <param name="additionalCheckForDefaultLoader">The optional callback to check whether some assembly needs to be loaded using parent loader (the loader which loaded this assembly).</param>
      /// <param name="defaultGetFiles">The optional callback to give to <see cref="M:E_UtilPack.ExtractAssemblyPaths{TResult}(BoundRestoreCommandUser, LockFile, Func{string, IEnumerable{string}, TResult}, GetFileItemsDelegate)"/> method.</param>
      /// <param name="pathProcessor">The optional callback to process assembly path just before it is loaded. It can e.g. copy assembly to some temp folder in order to avoid locking assembly in package repository.</param>
      /// <returns>A new instance of <see cref="NuGetAssemblyResolver"/>.</returns>
      /// <remarks>
      /// The created <see cref="System.Runtime.Loader.AssemblyLoadContext"/> and other resources will be cleared on calling <see cref="IDisposable.Dispose"/> method on returned <see cref="NuGetAssemblyResolver"/>.
      /// </remarks>
#endif
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
#if !NET45
         , OtherLoadersRegistration loadersRegistration = OtherLoadersRegistration.None
#endif
         )
      {
         if ( defaultGetFiles == null )
         {
            defaultGetFiles = UtilPackNuGetUtility.GetRuntimeAssembliesDelegate;
         }
         var resolver = new NuGetRestorerWrapper(
            restorer,
            ( rid, targetLib, libs ) => defaultGetFiles( rid, targetLib, libs ).FilterUnderscores(),
#if NET45
            null
#else
            thisFrameworkRestoreResult.Libraries.Select( lib => lib.Name )
#endif
            );

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
               Path.GetFullPath( new Uri( typeof( NuGetAssemblyResolverImpl ).Assembly.CodeBase ).LocalPath ),
               typeof( NuGetAssemblyResolverImpl ).FullName,
               false,
               0,
               null,
               new Object[] { resolver, cbWrapper, ppWrapper },
               null,
               null
            );
#else
         var loader = new NuGetAssemblyLoadContext( restorer, thisFrameworkRestoreResult, additionalCheckForDefaultLoader, loadersRegistration );
         retVal = loader.SetResolver( new NuGetAssemblyResolverImpl( resolver, loader, pathProcessor ) );
         createdLoader = loader;
#endif

         return retVal;

      }
   }

#if !NET45

   [Flags]
   public enum OtherLoadersRegistration
   {
      None = 0,
      Default = 1,
      Current = 2
   }

   internal sealed class NuGetAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext, IDisposable
   {
      private readonly ISet<String> _frameworkAssemblySimpleNames;
      private readonly System.Runtime.Loader.AssemblyLoadContext _parentLoadContext;
      private readonly ConcurrentDictionary<AssemblyName, Lazy<Assembly>> _loadedAssemblies; // We will get multiple request for same assembly name, so let's cache them
      private readonly Func<AssemblyName, Boolean> _additionalCheckForDefaultLoader;
      private NuGetAssemblyResolverImpl _resolver;


      public NuGetAssemblyLoadContext(
         BoundRestoreCommandUser restorer,
         LockFile thisFrameworkRestoreResult,
         Func<AssemblyName, Boolean> additionalCheckForDefaultLoader,
         OtherLoadersRegistration loadersRegistration
         )
      {
         var parentLoader = GetLoadContext( this.GetType().GetTypeInfo().Assembly );
         this._parentLoadContext = parentLoader;
         this._loadedAssemblies = new ConcurrentDictionary<AssemblyName, Lazy<Assembly>>(
            ComparerFromFunctions.NewEqualityComparer<AssemblyName>(
               ( x, y ) => ReferenceEquals( x, y ) || ( x != null && y != null && String.Equals( x.Name, y.Name )
                  && String.Equals( x.CultureName, y.CultureName )
                  && ( ReferenceEquals( x.Version, y.Version ) || ( x.Version?.Equals( y.Version ) ?? false ) )
                  && NuGetAssemblyResolverImpl.AssemblyNameComparer.SafeEqualsWhenNullsAreEmptyArrays( x.GetPublicKeyToken(), y.GetPublicKeyToken() )
                  ),
               x => x?.Name?.GetHashCode() ?? 0
               )
            );
         // .NET Core is package-based framework, so we need to find out which packages are part of framework, and which ones are actually client ones.
         this._frameworkAssemblySimpleNames = new HashSet<String>(
            restorer.ExtractAssemblyPaths(
                  thisFrameworkRestoreResult,
                  ( rid, lib, libs ) => lib.CompileTimeAssemblies.Select( i => i.Path ).FilterUnderscores(),
                  null
                  ).Values
               .SelectMany( p => p.Assemblies )
               .Select( p => Path.GetFileNameWithoutExtension( p ) ) // For framework assemblies, we can assume that file name without extension = assembly name
            );
         this._additionalCheckForDefaultLoader = additionalCheckForDefaultLoader;

         // We do this to catch scenarios like using Type.GetType(String) method.
         var defaultLoader = Default;
         var registerDefault = loadersRegistration.HasFlag( OtherLoadersRegistration.Default );
         if ( registerDefault )
         {
            defaultLoader.Resolving += this.OtherResolving;
         }
         if ( loadersRegistration.HasFlag( OtherLoadersRegistration.Current )
            && ( !registerDefault || !ReferenceEquals( parentLoader, defaultLoader ) )
            )
         {
            parentLoader.Resolving += this.OtherResolving;
         }
      }

      private Assembly OtherResolving( System.Runtime.Loader.AssemblyLoadContext loadContext, AssemblyName assemblyName )
      {
         return this._resolver?.TryResolveFromPreviouslyLoaded( assemblyName );
      }

      public void Dispose()
      {
         Default.Resolving -= this.OtherResolving;
         this._parentLoadContext.Resolving -= this.OtherResolving;
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
            if ( this._frameworkAssemblySimpleNames.Contains( an.Name ) || ( this._additionalCheckForDefaultLoader?.Invoke( an ) ?? false ) )
            {
               // We use default loader for framework assemblies, in order to avoid loading from different path for same assembly name.
               try
               {
                  retVal = this._parentLoadContext.LoadFromAssemblyName( an );
               }
               catch
               {
                  // Ignore
               }
            }
            return retVal ?? this._resolver.TryResolveFromPreviouslyLoaded( an );
         }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) ).Value;
      }
   }
#endif


   internal sealed class NuGetAssemblyResolverImpl :
#if NET45
      MarshalByRefObject,
#endif

   NuGetAssemblyResolver, IDisposable
   {
      internal sealed class AssemblyNameComparer : IEqualityComparer<AssemblyName>
      {

         private AssemblyNameComparer()
         {

         }
         bool IEqualityComparer<AssemblyName>.Equals( AssemblyName x, AssemblyName y )
         {
            // Compare just name + public key, as we might get different version assembly
            var retVal = String.Equals( x?.Name, y?.Name );
            if ( retVal && x != null && y != null )
            {
               retVal = SafeEqualsWhenNullsAreEmptyArrays( x.GetPublicKeyToken(), y.GetPublicKeyToken() );
            }

            return retVal;
         }

         Int32 IEqualityComparer<AssemblyName>.GetHashCode( AssemblyName obj )
         {
            return obj?.Name?.GetHashCode() ?? 0;
         }

         internal static readonly IEqualityComparer<AssemblyName> Instance = new AssemblyNameComparer();

         internal static Boolean SafeEqualsWhenNullsAreEmptyArrays( Byte[] x, Byte[] y ) =>
            ArrayEqualityComparer<Byte>.ArrayEquality( x ?? Empty<Byte>.Array, y ?? Empty<Byte>.Array );
      }

      private sealed class AssemblyInformation
      {
         private const Int32 INITIAL = 0;
         private const Int32 AFTER_VALUE_CREATION = 1;

         private Int32 _state;
         private readonly Lazy<Assembly> _assembly;
         private readonly NuGetAssemblyResolverImpl _resolver;
         private AssemblyLoadSuccessEventArgs _loadArgs;

         public AssemblyInformation(
            String path,
            NuGetAssemblyResolverImpl resolver,
            Boolean skipPathProcessor = false
            )
         {
            this.Path = path = System.IO.Path.GetFullPath( path );
            this._resolver = resolver;
            this._assembly = new Lazy<Assembly>( () =>
            {
               String processed;
               var actualPath = skipPathProcessor || String.IsNullOrEmpty( ( processed = resolver._pathProcessor?.Invoke( path ) ) ) ?
                  path :
                  processed;
               var retVal = resolver._fromPathLoader( actualPath );
               Interlocked.Exchange( ref this._loadArgs, new AssemblyLoadSuccessEventArgs( retVal.GetName(), path, actualPath ) );
               return retVal;
            }, LazyThreadSafetyMode.ExecutionAndPublication );
         }

         public String Path { get; }

         public Assembly Assembly
         {
            get
            {
               Assembly retVal;
               if ( this._state == INITIAL
                  && Interlocked.CompareExchange( ref this._state, AFTER_VALUE_CREATION, INITIAL ) == INITIAL
                  )
               {
                  retVal = this._assembly.Value;
                  // We don't want to invoke that event inside lazy factory, so have to do some tricks in order to invoke it here
                  try
                  {
                     this._resolver.OnAssemblyLoadSuccess?.Invoke( this._loadArgs );
                  }
                  catch
                  {
                     // Ignore
                  }
               }
               else
               {
                  retVal = this._assembly.Value;
               }
               return retVal;
            }
         }
      }

      private readonly ConcurrentDictionary<AssemblyName, AssemblyInformation> _assemblies;
      private readonly ConcurrentDictionary<String, Lazy<AssemblyName>> _assemblyNames;
      private readonly TNuGetResolver _resolver;
      private readonly Func<String, Assembly> _fromPathLoader;
      private readonly Func<String, String> _pathProcessor;
#if NET45
      private readonly CallbackWrapper _callbackWrapper;
#else
      private readonly NuGetAssemblyLoadContext _loader;
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
         NuGetAssemblyLoadContext loader,
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
         AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#endif

#if !NET45
         this._loader = ArgumentValidator.ValidateNotNull( nameof( loader ), loader );
#endif
         this._fromPathLoader =
#if NET45
            Assembly.LoadFile
#else
            path => loader.LoadFromAssemblyPath( path )
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
         if ( this._assemblies == null )
         {
            // Happens when casting constructor parameters
            retVal = this.GetType().Assembly;
         }
         else
         {
            String overrideLocation;
            if ( !String.IsNullOrEmpty( overrideLocation = this._callbackWrapper?.OverrideLocation( name ) ) )
            {
               retVal = this._assemblies.AddOrUpdate(
                  name,
                  an => new AssemblyInformation( overrideLocation, this, true ),
                  ( an, existing ) => new AssemblyInformation( overrideLocation, this, true )
                  ).Assembly;
            }
            else
            {
               retVal = this.TryResolveFromPreviouslyLoaded( name ); // this._fromNameLoader( name );
            }
         }
         return retVal;
      }

#endif

      public void Dispose()
      {
#if NET45
         AppDomain.CurrentDomain.AssemblyResolve -= this.CurrentDomain_AssemblyResolve;
#else
         this._loader.DisposeSafely();
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
               this._resolver.GetFiles,
               this._resolver.SDKPackageIDs
               )
#endif
            ;

            retVal = new Assembly[packageIDs.Length];
            if ( assemblyInfos != null )
            {
               var assemblyNames = assemblyInfos.Values
                  .SelectMany( v => v.Assemblies )
                  .Where( p => !String.IsNullOrEmpty( p ) )
                  .Select( p => Path.GetFullPath( p ) )
                  .Distinct()
                  .ToDictionary(
                     p => p,
                     p => this._assemblyNames.GetOrAdd( p, this.LoadAssemblyNameFromPath )
                     );

               foreach ( var kvp in assemblyNames )
               {
                  var curPath = kvp.Key;
                  var assembly = kvp.Value.Value;
                  if ( assembly != null )
                  {
                     this._assemblies.TryAdd(
                        assembly,
                        new AssemblyInformation( curPath, this )
                        );
                  }
               }

               for ( var i = 0; i < packageIDs.Length; ++i )
               {
                  var packageID = packageIDs[i];
                  if ( assemblyInfos.TryGetValue( packageID, out var possibleAssemblyPaths ) )
                  {
                     var assemblyPath = UtilPackNuGetUtility.GetAssemblyPathFromNuGetAssemblies(
                        packageID,
                        possibleAssemblyPaths.Assemblies,
                        assemblyPaths[i],
                        ap => File.Exists( ap )
                        );
                     AssemblyName name;
                     if ( !String.IsNullOrEmpty( assemblyPath ) && ( name = assemblyNames[assemblyPath = Path.GetFullPath( assemblyPath )].Value ) != null )
                     {
                        retVal[i] = this._assemblies[name].Assembly;
                     }
                     else
                     {
                        this._resolver
#if !NET45
                           .Resolver
#endif

                           .LogAssemblyPathResolveError( packageID, possibleAssemblyPaths.Assemblies, assemblyPaths[i], assemblyPath );
                     }
                  }
               }
            }
         }
         else
         {
            retVal = Empty<Assembly>.Array;
         }
         return retVal;
      }

      public Assembly LoadOtherAssembly(
         String assemblyPath
         )
      {
         assemblyPath = Path.GetFullPath( assemblyPath );
         var retVal = this._assemblies.Values.FirstOrDefault( v => String.Equals( v.Path, assemblyPath ) )?.Assembly;

         if ( retVal == null && File.Exists( assemblyPath ) )
         {
            var assemblyName = this._assemblyNames.GetOrAdd( assemblyPath, this.LoadAssemblyNameFromPath ).Value;
            retVal = assemblyName == null ? null : this._assemblies.GetOrAdd(
               assemblyName,
               an => new AssemblyInformation( assemblyPath, this )
               ).Assembly;
         }
         return retVal;
      }

      public event Action<AssemblyLoadSuccessEventArgs> OnAssemblyLoadSuccess;
      public event Action<AssemblyLoadFailedEventArgs> OnAssemblyLoadFail;

      public Assembly TryResolveFromPreviouslyLoaded( AssemblyName assemblyName )
      {
         Assembly retVal;
         if (
            assemblyName != null
            && ( this._assemblies.TryGetValue( assemblyName, out var assemblyInfo ) // We already have directly matching assembly loaded
            || ( CanIgnoreVersionAndToken( assemblyName ) && ( assemblyInfo = this._assemblies.FirstOrDefault( kvp => String.Equals( kvp.Key.Name, assemblyName.Name ) ).Value ) != null ) // We already have indirectly matching assembly loaded.
            ) )
         {
            try
            {
               retVal = assemblyInfo.Assembly;
            }
            catch
            {
               retVal = null;
            }
         }
         else
         {
            retVal = null;
         }
         if ( retVal == null )
         {
            try
            {
               this.OnAssemblyLoadFail?.Invoke( new AssemblyLoadFailedEventArgs( assemblyName ) );
            }
            catch
            {
               // Ignore
            }
         }
         return retVal;
      }

      private Lazy<AssemblyName> LoadAssemblyNameFromPath( String path )
      {
         return new Lazy<AssemblyName>( () =>
         {
            AssemblyName an = null;
            try
            {
               an =
#if !NET45
                           System.Runtime.Loader.AssemblyLoadContext
#else
                           AssemblyName
#endif
               .GetAssemblyName( path );
            }
            catch ( Exception exc )
            {
               this._resolver
#if !NET45
                           .Resolver
#endif
                           .LogAssemblyNameLoadError( path, exc.Message );
               // Ignore
            }

            return an;
         }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication );
      }

      private static Boolean CanIgnoreVersionAndToken( AssemblyName assemblyName )
      {
         return assemblyName.Flags.HasFlag( AssemblyNameFlags.Retargetable ) // retargetable referenace
            || ( assemblyName.Version == null && assemblyName.GetPublicKeyToken().IsNullOrEmpty() ); // Version and token not specified
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


   internal sealed class NuGetRestorerWrapper
#if NET45
      : MarshalByRefObject
#endif
   {


      public NuGetRestorerWrapper(
         BoundRestoreCommandUser resolver,
         GetFileItemsDelegate getFiles,
         IEnumerable<String> sdkPackages
         )
      {
         this.Resolver = resolver;
         this.GetFiles = getFiles;
         this.SDKPackageIDs = sdkPackages?.ToList();
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
               setter.SetResult( this.Resolver.ExtractAssemblyPaths( result, this.GetFiles, this.SDKPackageIDs ) );
            }
            catch
            {
               setter.SetResult( null );
            }
         } );
      }

      internal void LogAssemblyPathResolveError( String packageID, String[] possiblePaths, String pathHint, String seenAssemblyPath ) =>
         this.Resolver.LogAssemblyPathResolveError( packageID, possiblePaths, pathHint, seenAssemblyPath );

      internal void LogAssemblyNameLoadError( String path, String message ) =>
         this.Resolver.NuGetLogger.LogWarning( $"Error when loading assembly name from {path}: {message}" );
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

      public IReadOnlyList<String> SDKPackageIDs { get; }
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

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_UtilPack
{
   internal static TResolveResult ExtractAssemblyPaths(
      this BoundRestoreCommandUser restorer,
      LockFile lockFile,
      GetFileItemsDelegate fileGetter,
      IEnumerable<String> sdkPackages
   )
   {
      return restorer.ExtractAssemblyPaths(
         lockFile,
         ( packageFolder, filePaths ) => new ResolvedPackageInfo( packageFolder, filePaths.ToArray() ),
         fileGetter: fileGetter,
         filterablePackages: sdkPackages
         );
   }

   /// <summary>
   /// Convenience method to load one assembly from one NuGet package.
   /// </summary>
   /// <param name="resolver">This <see cref="NuGetAssemblyResolver"/>.</param>
   /// <param name="packageID">The ID of the package from which to load assembly from.</param>
   /// <param name="packageVersion">The optional version of the package from thich to load assembly from.</param>
   /// <param name="assemblyPath">The optional assembly path within the package.</param>
   /// <returns>Task which will have loaded <see cref="System.Reflection.Assembly"/> object or <c>null</c> on completion.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NuGetAssemblyResolver"/> is <c>null</c>.</exception>
   public static async Task<Assembly> LoadNuGetAssembly(
      this NuGetAssemblyResolver resolver,
      String packageID,
      String packageVersion,
      String assemblyPath = null
      )
   {
      // Don't use ArgumentValidator, as this may be executing in other app domain
      // (Same reason we don't use value tuples here)
      return ( await ( resolver ?? throw new NullReferenceException() ).LoadNuGetAssemblies( new[] { packageID }, new[] { packageVersion }, new[] { assemblyPath } ) )[0];
   }

   /// <summary>
   /// Conveience method to load multiple assembleis from multiple NuGet packages, and specifying parameters using value tuples.
   /// </summary>
   /// <param name="resolver">This <see cref="NuGetAssemblyResolver"/>.</param>
   /// <param name="packageInfo">The information about packages as value tuple.</param>
   /// <returns>Task which on completion has array of loaded <see cref="Assembly"/> objects for each given package ID in <paramref name="packageInfo"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NuGetAssemblyResolver"/> is <c>null</c>.</exception>
   public static Task<Assembly[]> LoadNuGetAssemblies(
      this NuGetAssemblyResolver resolver,
      params (String PackageID, String PackageVersion, String AssemblyPath)[] packageInfo
      )
   {
      return ( resolver ?? throw new NullReferenceException() ).LoadNuGetAssemblies(
         packageInfo.Select( p => p.PackageID ).ToArray(),
         packageInfo.Select( p => p.PackageVersion ).ToArray(),
         packageInfo.Select( p => p.AssemblyPath ).ToArray() );
   }

   public static Type TryLoadTypeFromPreviouslyLoadedAssemblies( this NuGetAssemblyResolver resolver, String typeName )
   {
      if ( resolver == null )
      {
         throw new NullReferenceException();
      }

      Type retVal = null;
      if ( !String.IsNullOrEmpty( typeName ) )
      {
         var separator = typeName.IndexOf( ", " );
         if ( separator > 0 && separator < typeName.Length - 1 )
         {
            // There is room for both type name and assembly name
            // Assembly name is what follows after ", "
            Assembly assembly = null;
            try
            {
               assembly = resolver.TryResolveFromPreviouslyLoaded( new AssemblyName( typeName.Substring( separator + 2 ) ) );
            }
            catch
            {
               // Ignore
            }

            if ( assembly != null )
            {
               // Now try load type
               retVal = assembly.GetType( typeName.Substring( 0, separator ), false, false );
            }
         }
      }

      return retVal;
   }

   internal static IEnumerable<String> FilterUnderscores( this IEnumerable<String> paths )
   {
      return paths?.Where( p => !p.EndsWith( "_._" ) );
   }

   internal static void LogAssemblyPathResolveError( this BoundRestoreCommandUser restorer, String packageID, String[] possiblePaths, String pathHint, String seenAssemblyPath )
   {
      restorer.NuGetLogger.LogError( $"Failed to resolve assemblies for \"{packageID}\"{( String.IsNullOrEmpty( seenAssemblyPath ) ? "" : ( " from \"" + seenAssemblyPath + "\"" ) )}, considered {String.Join( ";", possiblePaths.Select( pp => "\"" + pp + "\"" ) )}, with path hint of \"{pathHint}\"." );
   }

   internal static void LogAssemblyNameLoadError( this BoundRestoreCommandUser restorer, String path, String message ) =>
      restorer.NuGetLogger.LogWarning( $"Error when loading assembly name from {path}: {message}" );

}