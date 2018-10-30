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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.DependencyResolver;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Repositories;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace UtilPack.Tests.NuGet
{
   [TestClass]
   public class NuGetAssemblyResolveTest
   {

      //[TestMethod]
      //public async Task TestThatTypeGetTypeWorks()
      //{
      //   var nugetSettings = UtilPackNuGetUtility.GetNuGetSettingsWithDefaultRootDirectory( null );
      //   var restorer = new BoundRestoreCommandUser( nugetSettings );
      //   var assemblyResolver = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
      //      restorer,
      //      await restorer.RestoreIfNeeded( "Microsoft.NETCore.App", "1.1.2" ),
      //      out var loader,
      //      loadersRegistration: OtherLoadersRegistration.Default
      //      );
      //   var pushAssembly = await assemblyResolver.LoadNuGetAssembly( "UtilPack.NuGet.Push.MSBuild", "1.2.0" );
      //   const String PUSH_TASK = "UtilPack.NuGet.Push.MSBuild.PushTask";
      //   var taskType = Type.GetType( PUSH_TASK + ", " + pushAssembly.FullName );
      //   Assert.IsNotNull( taskType );
      //   Assert.IsTrue( ReferenceEquals( taskType, pushAssembly.GetType( PUSH_TASK ) ) );
      //   Assert.IsTrue( ReferenceEquals( taskType, Type.GetType( PUSH_TASK + ", " + pushAssembly.GetName().Name ) ) );
      //}

      //[TestMethod]
      //public async Task TestThatNoUtilPackAssembliesLoadedByAssemblyResolver()
      //{
      //   var nugetSettings = Settings.LoadDefaultSettings(
      //      Path.GetDirectoryName( new Uri( this.GetType().GetTypeInfo().Assembly.CodeBase ).LocalPath ),
      //      null,
      //      new XPlatMachineWideSetting()
      //      );
      //   var restorer = new BoundRestoreCommandUser(
      //      nugetSettings,
      //      Assembly.GetEntryAssembly().GetNuGetFrameworkFromAssembly(),
      //      new ConsoleLogger()
      //      );

      //   var cbam = await restorer.RestoreIfNeeded( "CBAM.SQL.MSBuild", "0.1.0-beta" );
      //   var cbamAssembly = restorer.ExtractAssemblyPaths( cbam, ( dir, assemblies ) => assemblies )["CBAM.SQL.MSBuild"].First();

      //   var resolver = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
      //      restorer,
      //      await restorer.RestoreIfNeeded( "Microsoft.NETCore.App", "1.1.2" ),
      //      out var createdLoader
      //      );

      //   var loadedAssembly = await resolver.LoadNuGetAssembly( "CBAM.SQL.MSBuild", "0.1.0-beta" );

      //   var ctx = AssemblyLoadContext.GetLoadContext( loadedAssembly );
      //}



      //   [TestMethod]
      //   public void TestSingleAssemblyResolve()
      //   {
      //      var repo = CreateDefaultLocalRepo();
      //      var nugetClientPackage = repo.FindPackagesById( "NuGet.Client" ).First();
      //      var assemblies = new NuGetPathResolver().GetSingleNuGetPackageAssemblies( nugetClientPackage );
      //      Assert.AreEqual( 1, assemblies.Length );
      //   }

      //   [TestMethod]
      //   public void TestAssemblyResolveWithDependencies()
      //   {
      //      var repo = CreateDefaultLocalRepo();
      //      var nugetClientPackage = repo.FindPackagesById( "NuGet.Client" ).First();
      //      var assemblies = new NuGetPathResolver().GetNuGetPackageAssembliesAndDependencies( nugetClientPackage, repo );
      //   }

      //   [TestMethod]
      //   public void TestNETCoreAppResolve()
      //   {
      //      var repo = CreateDefaultLocalRepo();
      //      var package = repo.FindPackage( "Microsoft.NETCore.App", NuGetVersion.Parse( "1.1.2" ) );
      //      var thisAssembly = Assembly.GetEntryAssembly();
      //      var thisFW = NuGetFramework.Parse( ".NETCoreApp1.1" ); // thisAssembly.GetNuGetFrameworkFromAssembly(); the test assembly is actually .NET Core App 1.0, even though the version in .csproj is 1.1.
      //      (var assemblyInfo, var missingDependencies) = new NuGetPathResolver( r =>
      //      {
      //         return r.GetLibItems( PackagingConstants.Folders.Ref ).Concat( r.GetLibItems() );
      //      } ).GetNuGetPackageAssembliesAndDependencies( package, thisFW, repo.Singleton() );

      //      Assert.AreEqual( 0, missingDependencies.Count );

      //      var resolvedAssemblies = assemblyInfo.Values
      //         .SelectMany( p => p )
      //         .Select( p => Path.GetFileName( p ) )
      //         .ToArray();

      //      var allDLLs = Directory.EnumerateFiles( @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\1.1.2" )
      //         .Where( fullFilePath =>
      //         {
      //            var file = Path.GetFileName( fullFilePath );
      //            var retVal = file.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase )
      //            && !file.EndsWith( ".ni.dll", StringComparison.OrdinalIgnoreCase )
      //            && file.IndexOf( "Private", StringComparison.OrdinalIgnoreCase ) == -1
      //            && !String.Equals( "mscorlib.dll", file, StringComparison.OrdinalIgnoreCase )
      //            && !String.Equals( "SOS.NETCore.dll", file, StringComparison.Ordinal )
      //            ;
      //            if ( retVal )
      //            {
      //               try
      //               {
      //                  System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName( fullFilePath );
      //               }
      //               catch ( BadImageFormatException )
      //               {
      //                  retVal = false;
      //               }
      //            }
      //            return retVal;
      //         } )
      //         .Select( fullFilePath => Path.GetFileName( fullFilePath ) )
      //         .ToArray();

      //      Assert.AreEqual( allDLLs.Length, resolvedAssemblies.Length );

      //      var set = new HashSet<String>( allDLLs );
      //      set.ExceptWith( resolvedAssemblies );
      //      Assert.AreEqual( 0, set.Count );
      //   }


      //   private static NuGetv3LocalRepository CreateDefaultLocalRepo()
      //   {
      //      return new NuGetv3LocalRepository( Path.Combine( NuGetEnvironment.GetFolderPath( NuGetFolderPath.NuGetHome ), "packages" ) );
      //   }
   }

   //public sealed class ConsoleLogger : ILogger
   //{

   //   public void LogDebug( String data )
   //   {
   //      Console.WriteLine( "[NuGet Debug]: " + data );
   //   }

   //   public void LogError( String data )
   //   {
   //      Console.WriteLine( "[NuGet Error]: " + data );
   //   }

   //   public void LogErrorSummary( String data )
   //   {
   //      Console.WriteLine( "[NuGet ErrorSummary]: " + data );
   //   }

   //   public void LogInformation( String data )
   //   {
   //      Console.WriteLine( "[NuGet Info]: " + data );
   //   }

   //   public void LogInformationSummary( String data )
   //   {
   //      Console.WriteLine( "[NuGet InfoSummary]: " + data );
   //   }

   //   public void LogMinimal( String data )
   //   {
   //      Console.WriteLine( "[NuGet Minimal]: " + data );
   //   }

   //   public void LogVerbose( String data )
   //   {
   //      Console.WriteLine( "[NuGet Verbose]: " + data );
   //   }

   //   public void LogWarning( String data )
   //   {
   //      Console.WriteLine( "[NuGet Warning]: " + data );
   //   }
   //}
}
