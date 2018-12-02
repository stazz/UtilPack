/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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
using AppVeyor.Trx2Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AppVeyor.Trx2Json
{
   public sealed class Trx2JsonTransformer
   {
      public const String TEST_FRAMEWORK = "MSTest";
      public const String TEST_NAMESPACE = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

      public JArray Transform(
         XElement root,
         String testFramework = TEST_FRAMEWORK,
         String testNS = TEST_NAMESPACE
         )
      {
         if ( String.IsNullOrEmpty( testFramework ) )
         {
            testFramework = TEST_FRAMEWORK;
         }

         var unitTestResults = root
            .Elements( XName.Get( "Results", testNS ) )
            .SelectMany( results => results.Elements( XName.Get( "UnitTestResult", testNS ) ) )
            .ToDictionary( unitTestResult => unitTestResult.Attribute( "executionId" ).Value );

         return new JArray( root
            .Elements( XName.Get( "TestDefinitions", testNS ) )
            .SelectMany( testDef => testDef.Elements( XName.Get( "UnitTest", testNS ) ) )
            .SelectMany( testDef => testDef.Elements( XName.Get( "TestMethod", testNS ) ).Select( testMethod => (testDef, testMethod) ) )
            .Select( tuple =>
            {
               (var testDef, var testMethod) = tuple;

               var testObject = new JObject(
                  new JProperty( "testName", testMethod.Attribute( "className" ).Value + "." + testMethod.Attribute( "name" ).Value ),
                  new JProperty( "testFramework", testFramework ),
                  new JProperty( "fileName", Path.GetFileName( testMethod.Attribute( "codeBase" ).Value ) )
                  );
               if ( testDef.Element( XName.Get( "Execution", testNS ) ).Attribute( "id" ).Value is String executionID
                  && unitTestResults.TryGetValue( executionID, out var result )
                     )
               {
                  testObject.AddAttributeIfPresent(
                     result,
                     "outcome",
                     null,
                     outcome =>
                     {
                        if ( !Char.IsUpper( outcome[0] ) )
                        {
                           // Capitalize first letter
                           var chars = outcome.ToCharArray();
                           chars[0] = Char.ToUpper( chars[0] );
                           outcome = new String( chars );
                        }
                        return outcome;
                     } );
                  testObject.AddAttributeIfPresent(
                     result,
                     "duration",
                     "durationMilliseconds",
                     duration => TimeSpan.Parse( duration ).TotalMilliseconds.ToString( "F0" )
                     );

                  var output = result.Element( XName.Get( "Output", testNS ) );
                  if ( output != null )
                  {
                     testObject.AddTextFromChild( output, "StdOut", testNS, null );
                     testObject.AddTextFromChild( output, "StdErr", testNS, null );
                     var errorInfo = output.Element( XName.Get( "ErrorInfo", testNS ) );
                     if ( errorInfo != null )
                     {
                        testObject.AddTextFromChild( errorInfo, "Message", testNS, "ErrorMessage" );
                        testObject.AddTextFromChild( errorInfo, "StackTrace", testNS, "ErrorStackTrace" );
                     }
                  }
               }

               return testObject;
            } )
         );
      }
   }
}

public static class E_Trx2Json
{
   public static async Task<JArray> TransformAsync(
      this Trx2JsonTransformer transformer,
      String filePath,
      CancellationToken token,
      String testFramework = Trx2JsonTransformer.TEST_FRAMEWORK,
      String testNS = Trx2JsonTransformer.TEST_NAMESPACE
      )
   {
      XDocument doc;
      using ( var fs = File.Open( filePath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
      {
         doc = await XDocument.LoadAsync( fs, LoadOptions.None, token );
      }

      return transformer.Transform( doc.Root );
   }

   public static void AddTextFromChild(
      this JObject jObject,
      XElement parent,
      String childXElementName,
      String childXElementNamespace,
      String childJObjectName
      )
   {
      var child = parent.Element( XName.Get( childXElementName, childXElementNamespace ) );
      if ( child != null )
      {
         if ( String.IsNullOrEmpty( childJObjectName ) )
         {
            childJObjectName = childXElementName;
         }

         jObject.Add( new JProperty( childJObjectName, child.Value ) );
      }
   }

   public static void AddAttributeIfPresent(
      this JObject jObject,
      XElement element,
      String attributeName,
      String childPropertyName,
      Func<String, String> converter = null
      )
   {
      var attribute = element.Attribute( attributeName )?.Value;
      if ( !String.IsNullOrEmpty( attribute ) )
      {
         if ( String.IsNullOrEmpty( childPropertyName ) )
         {
            childPropertyName = attributeName;
         }
         jObject.Add( new JProperty( childPropertyName, converter?.Invoke( attribute ) ?? attribute ) );
      }
   }
}