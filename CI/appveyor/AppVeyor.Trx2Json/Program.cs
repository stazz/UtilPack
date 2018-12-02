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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppVeyor.Trx2Json
{
   class Program
   {
      static async Task Main( String[] args )
      {
         using ( var source = new CancellationTokenSource() )
         {
            void Console_CancelKeyPress( Object sender, ConsoleCancelEventArgs e )
            {
               source.Cancel();
            }
            Console.CancelKeyPress += Console_CancelKeyPress;
            try
            {
               var inputs = args.Skip( 1 ).ToArray();
               if (inputs.Length == 1 && Directory.Exists(inputs[0])) {
                 inputs = Directory.EnumerateFiles(inputs[0], "*", SearchOption.TopDirectoryOnly).ToArray();
               }
               await TransformAll( inputs, args[0], source.Token );
            }
            finally
            {
               Console.CancelKeyPress -= Console_CancelKeyPress;
            }
         }
      }

      private static async Task TransformAll(
         String[] inputs,
         String output,
         CancellationToken token
         )
      {
         var transform = new Trx2JsonTransformer();
         var arrays = await Task.WhenAll( inputs.Select( input => transform.TransformAsync( input, token ) ).ToArray() );
         using ( var fs = File.Open( output, FileMode.Create, FileAccess.Write, FileShare.Read ) )
         using ( var streamWriter = new StreamWriter( fs ) )
         using ( var jsonWriter = new JsonTextWriter( streamWriter ) )
         {
            jsonWriter.Formatting = Formatting.Indented;
            await new JArray( arrays.SelectMany( array => array ) ).WriteToAsync( jsonWriter, token );
         }
      }
   }
}
