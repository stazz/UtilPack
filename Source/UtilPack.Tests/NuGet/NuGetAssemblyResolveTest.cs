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
using NuGet.Common;
using NuGet.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UtilPack.NuGet;

namespace UtilPack.Tests.NuGet
{
   [TestClass]
   public class NuGetAssemblyResolveTest
   {
      [TestMethod]
      public void TestSingleAssemblyResolve()
      {
         var repo = CreateDefaultLocalRepo();
         var nugetClientPackage = repo.FindPackagesById( "NuGet.Client" ).First();
         var assemblies = nugetClientPackage.GetSingleNuGetPackageAssemblies();
         Assert.AreEqual( 1, assemblies.Length );
      }

      [TestMethod]
      public void TestAssemblyResolveWithDependencies()
      {
         var repo = CreateDefaultLocalRepo();
         var nugetClientPackage = repo.FindPackagesById( "NuGet.Client" ).First();
         var assemblies = nugetClientPackage.GetNuGetPackageAssembliesAndDependencies( repo );
      }

      private static NuGetv3LocalRepository CreateDefaultLocalRepo()
      {
         return new NuGetv3LocalRepository( Path.Combine( NuGetEnvironment.GetFolderPath( NuGetFolderPath.NuGetHome ), "packages" ) );
      }
   }
}
