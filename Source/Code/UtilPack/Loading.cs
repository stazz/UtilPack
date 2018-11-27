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
#if false //NETSTANDARD1_5 (System.Runtime.Loader does not exist on any .NET -> need to rethink and possibly redesign
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using UtilPack;

namespace UtilPack
{
   /// <summary>
   /// This class allows to explicitly load assembly from given location (e.g. file path), and keep track of loaded assemblies.
   /// Furthermore, the load of dependent assemblies (which must be done since <see cref="System.Runtime.Loader.AssemblyLoadContext"/> loads assemblies lazily) is done in controlled and customizable way.
   /// </summary>
   /// <remarks>
   /// This class is not safe to be used concurrently.
   /// </remarks>
   public class ExplicitAssemblyLoader
   {
      private sealed class LoadedAssemblyInfo
      {
         public LoadedAssemblyInfo(
            String originalPath,
            String loadedPath,
            Assembly assembly
            )
         {
            this.OriginalPath = originalPath;
            this.LoadedPath = loadedPath;
            this.Assembly = assembly;
         }

         public String OriginalPath { get; }

         public String LoadedPath { get; }

         public Assembly Assembly { get; }
      }

      private readonly AssemblyLoadContext _assemblyLoader;
      private readonly IDictionary<String, LoadedAssemblyInfo> _assembliesByOriginalPath;
      private readonly Func<String, String> _assemblyPathProcessor;
      private readonly Func<String, AssemblyName, IEnumerable<String>> _candidatePathGetter;

      /// <summary>
      /// Creates new instance of <see cref="ExplicitAssemblyLoader"/> with given optional customizers.
      /// </summary>
      /// <param name="assemblyPathProcessor">The callback to process the assembly path from which to actually load assembly. Only invoked when the file exists. Receives original assembly path as argument.</param>
      /// <param name="candidatePathGetter">The callback to scan through potential assembly locations. Receives referencing assembly path and assembly name reference as arguments. By default, <see cref="GetDefaultAssemblyReferenceCandidatePaths"/> is used.</param>
      /// <param name="assemblyLoadContext">The actual <see cref="AssemblyLoadContext"/> to use. By default the <see cref="AssemblyLoadContext.Default"/> is used.</param>
      /// <remarks>
      /// The <paramref name="assemblyPathProcessor"/> may e.g. copy the assembly file from original location to some other location and return the copied path, thus behaving somewhat like shadow copying.
      /// The <paramref name="candidatePathGetter"/> may e.g. file paths in NuGet repository. 
      /// Not all paths returned by <paramref name="candidatePathGetter"/> need to exist - this loader will check whether paths exist.
      /// </remarks>
      public ExplicitAssemblyLoader(
         Func<String, String> assemblyPathProcessor = null,
         Func<String, AssemblyName, IEnumerable<String>> candidatePathGetter = null,
         AssemblyLoadContext assemblyLoadContext = null
         )
      {
         this._assemblyPathProcessor = assemblyPathProcessor;
         this._candidatePathGetter = candidatePathGetter ?? GetDefaultAssemblyReferenceCandidatePaths;
         this._assembliesByOriginalPath = new Dictionary<String, LoadedAssemblyInfo>();
         this._assemblyLoader = assemblyLoadContext ?? AssemblyLoadContext.Default;
      }

      /// <summary>
      /// Loads the assembly from given location, if it is not already loaded.
      /// </summary>
      /// <param name="location">The location to load assembly from. Is case-sensitive.</param>
      /// <param name="loadDependencies">Whether to load dependencies, if assembly was loaded.</param>
      /// <returns>Loaded or cached assembly. If assembly was loaded, and <paramref name="loadDependencies"/> is <c>true</c>, the second tuple item will be non-<c>null</c> containing recursive dependency load information.</returns>
      /// <remarks>
      /// This method recursively loads all dependencies of the assembly, if actual load is performed.
      /// </remarks>
      public (Assembly LoadedAssembly, IDictionary<AssemblyName, Assembly> LoadedDependencies) LoadAssemblyFrom(
         String location,
         Boolean loadDependencies = true
         )
      {
         var assemblyPath = System.IO.Path.GetFullPath( location );

         var assembly = this.LoadLibraryAssembly( assemblyPath, out Boolean actuallyLoaded );

         IDictionary<AssemblyName, Assembly> loadedDeps;
         if ( actuallyLoaded && loadDependencies )
         {
            // Load recursively all dependant assemblies right here, since the loader is lazy, and if the dependant assembly load happens later, our callback will be gone.
            loadedDeps = this.LoadDependenciesRecursively( assembly );
         }
         else
         {
            loadedDeps = null;
         }

         return (assembly.Assembly, loadedDeps);
      }

      /// <summary>
      /// Loads all the dependencies of given assembly, if it is loaded by this <see cref="ExplicitAssemblyLoader"/>.
      /// </summary>
      /// <param name="assembly">The assembly to load dependencies of.</param>
      /// <returns>Possibly empty dictionary of assembly dependencies. Will return <c>null</c> if <paramref name="assembly"/> was not loaded by this <see cref="ExplicitAssemblyLoader"/>.</returns>
      public IDictionary<AssemblyName, Assembly> LoadAllDependenciesOf( Assembly assembly )
      {
         return this._assembliesByOriginalPath.TryGetValue( assembly.Location, out LoadedAssemblyInfo info ) ?
            this.LoadDependenciesRecursively( info ) :
            null;
      }

      /// <summary>
      /// Checks whether the assembly at given location has been loaded by this <see cref="ExplicitAssemblyLoader"/>.
      /// </summary>
      /// <param name="location">The location of the assembly.</param>
      /// <returns><c>true</c> if assebmly at given location has been loaded by this <see cref="ExplicitAssemblyLoader"/>; <c>false</c> otherwise.</returns>
      public Boolean IsAssemblyLoaded( String location )
      {
         return this._assembliesByOriginalPath.ContainsKey( location );
      }

      private IDictionary<AssemblyName, Assembly> LoadDependenciesRecursively(
         LoadedAssemblyInfo loadedAssembly
         )
      {
         var assemblies = this._assembliesByOriginalPath;

         var assembliesToProcess = new List<LoadedAssemblyInfo>
         {
            loadedAssembly
         };
         var addedThisRound = new List<LoadedAssemblyInfo>();
         var loadedDeps = new Dictionary<AssemblyName, Assembly>(
            ComparerFromFunctions.NewEqualityComparer<AssemblyName>(
               ( a1, a2 ) => ReferenceEquals( a1, a2 ) || ( a1 != null && a2 != null && String.Equals( a1.Name, a2.Name ) && String.Equals( a1.CultureName, a2.CultureName ) && a1.Version.Equals( a2.Version ) && ArrayEqualityComparer<Byte>.ArrayEquality( a1.GetPublicKey(), a2.GetPublicKey() ) ),
               a => a.Name.GetHashCodeSafe()
               )
            );
         do
         {
            addedThisRound.Clear();

            foreach ( var loadedInfo in assembliesToProcess )
            {

               foreach ( var aRef in loadedInfo.Assembly.GetReferencedAssemblies() )
               {
                  var curRef = aRef;
                  var oldCount = assemblies.Count;
                  var originalPath = loadedInfo.OriginalPath;
                  var assemblyPath = this.GetFirstExistingAssemblyPath( originalPath, curRef );

                  // We *must* use loading by assembly name here - otherwise we end up with multiple loaded assemblies with the same assembly name!
                  Assembly curAssembly = null;
                  try
                  {
                     curAssembly = this.LoadAssemblyReference(
                        originalPath,
                        curRef
                        );
                  }
                  catch
                  {
                     // Ignore
                     loadedDeps.Add( aRef, null );
                  }

                  if ( curAssembly != null && System.IO.File.Exists( assemblyPath ) )
                  {
                     var newCount = assemblies.Count;

                     if ( newCount > oldCount )
                     {
                        addedThisRound.Add( this._assembliesByOriginalPath[assemblyPath] );
                        loadedDeps.Add( aRef, curAssembly );
                     }
                  }

               }
            }

            assembliesToProcess.Clear();
            assembliesToProcess.AddRange( addedThisRound );
         } while ( addedThisRound.Count > 0 );

         return loadedDeps;
      }

      private LoadedAssemblyInfo LoadLibraryAssembly(
         String originalPathParam,
         out Boolean actuallyLoaded
         )
      {
         var assemblies = this._assembliesByOriginalPath;
         var oldCount = assemblies.Count;
         var retVal = assemblies.GetOrAdd_NotThreadSafe( originalPathParam, originalPath =>
         {
            var processed = this._assemblyPathProcessor?.Invoke( originalPath );
            if ( String.IsNullOrEmpty( processed ) )
            {
               processed = originalPath;
            }

            return new LoadedAssemblyInfo(
               originalPath,
               processed,
               this.LoadAssemblyReference( originalPath, processed )
               );
         } );
         actuallyLoaded = assemblies.Count > oldCount;

         return retVal;
      }

      private Assembly LoadAssemblyReference(
         String referencingAssemblyOriginalPath,
         EitherOr<String, AssemblyName> assemblyRef
         )
      {
         Func<AssemblyLoadContext, AssemblyName, Assembly> eventHandler = ( ctx, aName ) =>
         {
            var assemblyPath = this.GetFirstExistingAssemblyPath( referencingAssemblyOriginalPath, aName );

            return String.IsNullOrEmpty( assemblyPath ) ?
               null :
               this.LoadLibraryAssembly( assemblyPath, out Boolean actuallyLoaded ).Assembly;
         };

         var loader = this._assemblyLoader;
         loader.Resolving += eventHandler;
         try
         {
            return assemblyRef.IsFirst ?
               loader.LoadFromAssemblyPath( assemblyRef.First ) :
               loader.LoadFromAssemblyName( assemblyRef.Second );
         }
         finally
         {
            loader.Resolving -= eventHandler;
         }
      }

      /// <summary>
      /// This is default callback which returns the string of file located in same directory as <paramref name="referencingAssemblyPath"/> and having file name of <see cref="AssemblyName.Name"/> property of <paramref name="assemblyName"/>.
      /// Currently, only <c>.dll</c> file extension is used.
      /// </summary>
      /// <param name="referencingAssemblyPath">The path of the assembly which needs this reference.</param>
      /// <param name="assemblyName">The <see cref="AssemblyName"/> of the reference.</param>
      /// <returns>The file path representing to the <c>.dll</c> reference to assembly in the same directory as <paramref name="referencingAssemblyPath"/>.</returns>
      public static IEnumerable<String> GetDefaultAssemblyReferenceCandidatePaths(
         String referencingAssemblyPath,
         AssemblyName assemblyName
         )
      {
         var assemblyBasePath = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( referencingAssemblyPath ), assemblyName.Name );

         // TODO something else than .dll in the future...? 
         yield return assemblyBasePath + ".dll";
      }

      private String GetFirstExistingAssemblyPath(
         String referencingAssemblyOriginalPath,
         AssemblyName assemblyName
         )
      {
         return this._candidatePathGetter( referencingAssemblyOriginalPath, assemblyName ).FirstOrDefault( aPath => System.IO.File.Exists( aPath ) );
      }

   }
}

public static partial class E_UtilPack
{
   /// <summary>
   /// Tries to create an instance of class which is either specified by <paramref name="assemblyLocation"/> and <paramref name="typeName"/> pair, or by assembly-qualified <paramref name="typeName"/>.
   /// </summary>
   /// <typeparam name="T">The type of the class.</typeparam>
   /// <param name="loader">This <see cref="ExplicitAssemblyLoader"/>.</param>
   /// <param name="typeName">The name of the type. It should be the full name if <paramref name="assemblyLocation"/> is specified, and assembly-qualified name if <paramref name="assemblyLocation"/> is not specified. It can be <c>null</c> if <paramref name="assemblyLocation"/> is specified; in that case the first suitable type is returned.</param>
   /// <param name="assemblyLocation">The location of the assembly file. If it is specified, this <see cref="ExplicitAssemblyLoader"/> is not used. Instead, the type is loaded using <see cref="Type.GetType(string, bool)"/>, if <paramref name="typeName"/> is specified.</param>
   /// <param name="instanceCreator">The optional callback to create an instance once the type has been acquired. By default, the <see cref="Activator.CreateInstance(Type)"/> is used.</param>
   /// <returns>An instance of type <typeparamref name="T"/>, or <c>null</c>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="assemblyLocation"/> is specified, and this <see cref="ExplicitAssemblyLoader"/> is <c>null</c>.</exception>
   public static T TryLoadClassInstanceFromAssembly<T>(
      this ExplicitAssemblyLoader loader,
      String typeName,
      String assemblyLocation,
      Func<Type, Object> instanceCreator = null
      )
      where T : class
   {
      return (T) ( loader.TryLoadInstanceFromAssembly(
         typeName,
         assemblyLocation,
         requiredParentType: typeof( T ).GetTypeInfo(),
         additionalTypeCheck: t => t.GetTypeInfo().IsClass,
         instanceCreator: instanceCreator
         ) );
   }

   /// <summary>
   /// Tries to create an instance of struct which is either specified by <paramref name="assemblyLocation"/> and <paramref name="typeName"/> pair, or by assembly-qualified <paramref name="typeName"/>.
   /// </summary>
   /// <typeparam name="T">The type of the struct.</typeparam>
   /// <param name="loader">This <see cref="ExplicitAssemblyLoader"/>.</param>
   /// <param name="typeName">The name of the type. It should be the full name if <paramref name="assemblyLocation"/> is specified, and assembly-qualified name if <paramref name="assemblyLocation"/> is not specified. It can be <c>null</c> if <paramref name="assemblyLocation"/> is specified; in that case the first suitable type is returned.</param>
   /// <param name="assemblyLocation">The location of the assembly file. If it is specified, this <see cref="ExplicitAssemblyLoader"/> is not used. Instead, the type is loaded using <see cref="Type.GetType(string, bool)"/>, if <paramref name="typeName"/> is specified.</param>
   /// <param name="instanceCreator">The optional callback to create an instance once the type has been acquired. By default, the <see cref="Activator.CreateInstance(Type)"/> is used.</param>
   /// <returns>An instance of type <typeparamref name="T"/>, or <c>null</c>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="assemblyLocation"/> is specified, and this <see cref="ExplicitAssemblyLoader"/> is <c>null</c>.</exception>
   public static T? TryLoadStructInstanceFromAssembly<T>(
      this ExplicitAssemblyLoader loader,
      String typeName,
      String assemblyLocation,
      Func<Type, Object> instanceCreator = null
      )
      where T : struct
   {
      return (T?) ( loader.TryLoadInstanceFromAssembly(
         typeName,
         assemblyLocation,
         additionalTypeCheck: t => !t.GetTypeInfo().IsClass,
         instanceCreator: instanceCreator
         ) );
   }

   /// <summary>
   /// Tries to create an instance of type which is either specified by <paramref name="assemblyLocation"/> and <paramref name="typeName"/> pair, or by assembly-qualified <paramref name="typeName"/>.
   /// </summary>
   /// <param name="loader">This <see cref="ExplicitAssemblyLoader"/>.</param>
   /// <param name="typeName">The name of the type. It should be the full name if <paramref name="assemblyLocation"/> is specified, and assembly-qualified name if <paramref name="assemblyLocation"/> is not specified. It can be <c>null</c> if <paramref name="assemblyLocation"/> and <paramref name="requiredParentType"/> are both specified; in that case the first suitable type is returned.</param>
   /// <param name="assemblyLocation">The location of the assembly file. If it is specified, this <see cref="ExplicitAssemblyLoader"/> is not used. Instead, the type is loaded using <see cref="Type.GetType(string, bool)"/>, if <paramref name="typeName"/> is specified.</param>
   /// <param name="requiredParentType">Optional parent type which the loaded type must be assignable from.</param>
   /// <param name="additionalTypeCheck">Optional additional callback to check loaded type.</param>
   /// <param name="instanceCreator">The optional callback to create an instance once the type has been acquired. By default, the <see cref="Activator.CreateInstance(Type)"/> is used.</param>
   /// <returns>An instance of given type, or <c>null</c>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="assemblyLocation"/> is specified, and this <see cref="ExplicitAssemblyLoader"/> is <c>null</c>.</exception>
   public static Object TryLoadInstanceFromAssembly(
      this ExplicitAssemblyLoader loader,
      String typeName,
      String assemblyLocation,
      TypeInfo requiredParentType = null,
      Func<Type, Boolean> additionalTypeCheck = null,
      Func<Type, Object> instanceCreator = null
      )
   {
      var type = loader.TryLoadTypeFromAssembly(
         typeName,
         assemblyLocation,
         requiredParentType: requiredParentType,
         additionalTypeCheck: additionalTypeCheck
         );
      return type == null ? null : ( instanceCreator?.Invoke( type ) ?? Activator.CreateInstance( type ) );
   }

   /// <summary>
   /// Tries to load type which is either specified by <paramref name="assemblyLocation"/> and <paramref name="typeName"/> pair, or by assembly-qualified <paramref name="typeName"/>.
   /// </summary>
   /// <param name="loader">This <see cref="ExplicitAssemblyLoader"/>.</param>
   /// <param name="typeName">The name of the type. It should be the full name if <paramref name="assemblyLocation"/> is specified, and assembly-qualified name if <paramref name="assemblyLocation"/> is not specified. It can be <c>null</c> if <paramref name="assemblyLocation"/> and <paramref name="requiredParentType"/> are both specified; in that case the first suitable type is returned.</param>
   /// <param name="assemblyLocation">The location of the assembly file. If it is specified, this <see cref="ExplicitAssemblyLoader"/> is not used. Instead, the type is loaded using <see cref="Type.GetType(string, bool)"/>, if <paramref name="typeName"/> is specified.</param>
   /// <param name="requiredParentType">Optional parent type which the loaded type must be assignable from.</param>
   /// <param name="additionalTypeCheck">Optional additional callback to check loaded type.</param>
   /// <returns>The loaded type, or <c>null</c>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="assemblyLocation"/> is specified, and this <see cref="ExplicitAssemblyLoader"/> is <c>null</c>.</exception>
   public static Type TryLoadTypeFromAssembly(
      this ExplicitAssemblyLoader loader,
      String typeName,
      String assemblyLocation,
      TypeInfo requiredParentType = null,
      Func<Type, Boolean> additionalTypeCheck = null
      )
   {
      var providerTypeNameSpecified = !String.IsNullOrEmpty( typeName );
      Type providerType;
      //String errorMessage;
      if ( String.IsNullOrEmpty( assemblyLocation ) )
      {
         if ( providerTypeNameSpecified )
         {
            providerType = Type.GetType( typeName, false );
            //errorMessage = $"Failed to load type \"{providerTypeName}\", make sure the name is assembly-qualified.";
         }
         else
         {
            providerType = null;
            //errorMessage = $"The task must receive {nameof( ConnectionPoolProviderAssemblyLocation )} and/or {nameof( ConnectionPoolProviderTypeName )} parameters.";
         }
      }
      else
      {
         var providerAssembly = ArgumentValidator.ValidateNotNullReference( loader ).LoadAssemblyFrom( assemblyLocation, true ).LoadedAssembly;
         if ( providerTypeNameSpecified )
         {
            providerType = providerAssembly.GetType( typeName, false );
            //errorMessage = $"No type \"{providerTypeName}\" in assembly located in \"{providerAssemblyLocation}\".";
         }
         else if ( requiredParentType != null )
         {
            providerType = providerAssembly.DefinedTypes.FirstOrDefault( t => requiredParentType.IsAssignableFrom( t ) )?.AsType();
            //errorMessage = $"Failed to find any type implementing \"{providerBaseType.AssemblyQualifiedName}\" in assembly located in \"{providerAssemblyLocation}\".";
         }
         else
         {
            providerType = null;
         }
      }

      return providerType != null
         && ( requiredParentType?.IsAssignableFrom( providerType ) ?? true )
         && ( additionalTypeCheck?.Invoke( providerType ) ?? true ) ?
         providerType :
         null;
   }
}
#endif