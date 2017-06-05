///*
// * Copyright 2017 Stanislav Muhametsin. All rights Reserved.
// *
// * Licensed  under the  Apache License,  Version 2.0  (the "License");
// * you may not use  this file  except in  compliance with the License.
// * You may obtain a copy of the License at
// *
// *   http://www.apache.org/licenses/LICENSE-2.0
// *
// * Unless required by applicable law or agreed to in writing, software
// * distributed  under the  License is distributed on an "AS IS" BASIS,
// * WITHOUT  WARRANTIES OR CONDITIONS  OF ANY KIND, either  express  or
// * implied.
// *
// * See the License for the specific language governing permissions and
// * limitations under the License. 
// */
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using NuGet.Common;
//using NuGet.Configuration;
//using NuGet.DependencyResolver;
//using NuGet.Frameworks;
//using NuGet.Packaging;
//using NuGet.Repositories;
//using NuGet.Versioning;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using UtilPack.NuGet;
//using NuGet.Protocol.Core.Types;
//using NuGet.Protocol;
//using NuGet.LibraryModel;
//using System.Threading.Tasks;
//using NuGet.ProjectModel;
//using NuGet.Commands;

//namespace UtilPack.Tests.NuGet
//{
//   [TestClass]
//   public class NuGetAssemblyResolveTest
//   {
//      [TestMethod]
//      public void TestSingleAssemblyResolve()
//      {
//         var repo = CreateDefaultLocalRepo();
//         var nugetClientPackage = repo.FindPackagesById( "NuGet.Client" ).First();
//         var assemblies = new NuGetPathResolver().GetSingleNuGetPackageAssemblies( nugetClientPackage );
//         Assert.AreEqual( 1, assemblies.Length );
//      }

//      [TestMethod]
//      public void TestAssemblyResolveWithDependencies()
//      {
//         var repo = CreateDefaultLocalRepo();
//         var nugetClientPackage = repo.FindPackagesById( "NuGet.Client" ).First();
//         var assemblies = new NuGetPathResolver().GetNuGetPackageAssembliesAndDependencies( nugetClientPackage, repo );
//      }

//      [TestMethod]
//      public void TestNETCoreAppResolve()
//      {
//         var repo = CreateDefaultLocalRepo();
//         var package = repo.FindPackage( "Microsoft.NETCore.App", NuGetVersion.Parse( "1.1.2" ) );
//         var thisAssembly = Assembly.GetEntryAssembly();
//         var thisFW = NuGetFramework.Parse( ".NETCoreApp1.1" ); // thisAssembly.GetNuGetFrameworkFromAssembly(); the test assembly is actually .NET Core App 1.0, even though the version in .csproj is 1.1.
//         (var assemblyInfo, var missingDependencies) = new NuGetPathResolver( r =>
//         {
//            return r.GetLibItems( PackagingConstants.Folders.Ref ).Concat( r.GetLibItems() );
//         } ).GetNuGetPackageAssembliesAndDependencies( package, thisFW, repo.Singleton() );

//         Assert.AreEqual( 0, missingDependencies.Count );

//         var resolvedAssemblies = assemblyInfo.Values
//            .SelectMany( p => p )
//            .Select( p => Path.GetFileName( p ) )
//            .ToArray();

//         var allDLLs = Directory.EnumerateFiles( @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\1.1.2" )
//            .Where( fullFilePath =>
//            {
//               var file = Path.GetFileName( fullFilePath );
//               var retVal = file.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase )
//               && !file.EndsWith( ".ni.dll", StringComparison.OrdinalIgnoreCase )
//               && file.IndexOf( "Private", StringComparison.OrdinalIgnoreCase ) == -1
//               && !String.Equals( "mscorlib.dll", file, StringComparison.OrdinalIgnoreCase )
//               && !String.Equals( "SOS.NETCore.dll", file, StringComparison.Ordinal )
//               ;
//               if ( retVal )
//               {
//                  try
//                  {
//                     System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName( fullFilePath );
//                  }
//                  catch ( BadImageFormatException )
//                  {
//                     retVal = false;
//                  }
//               }
//               return retVal;
//            } )
//            .Select( fullFilePath => Path.GetFileName( fullFilePath ) )
//            .ToArray();

//         Assert.AreEqual( allDLLs.Length, resolvedAssemblies.Length );

//         var set = new HashSet<String>( allDLLs );
//         set.ExceptWith( resolvedAssemblies );
//         Assert.AreEqual( 0, set.Count );
//      }


//      private static NuGetv3LocalRepository CreateDefaultLocalRepo()
//      {
//         return new NuGetv3LocalRepository( Path.Combine( NuGetEnvironment.GetFolderPath( NuGetFolderPath.NuGetHome ), "packages" ) );
//      }
//   }
//}
