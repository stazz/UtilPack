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
using System;

namespace NuGet.Utils.Exec.Entrypoint
{
   /// <summary>
   /// This attribute can be used to mark entry point information for <c>nuget-exec</c> global tool.
   /// </summary>
   [AttributeUsage( AttributeTargets.Assembly | AttributeTargets.Method )]
   public sealed class ConfiguredEntryPointAttribute : Attribute
   {
      /// <summary>
      /// This constructor should be used on methods to indicate that the actual entrypoint is another method within the same type, with the same name as this method, but with different signature.
      /// </summary>
      public ConfiguredEntryPointAttribute()
         : this( null, null )
      {

      }

      /// <summary>
      /// This constructor should be used on assemblies or methods to narrow down the type containing potential entry point methods to a given type.
      /// </summary>
      /// <param name="entryPointType">The type where to search for entry point methods.</param>
      public ConfiguredEntryPointAttribute(
         Type entryPointType
         ) : this( entryPointType, null )
      {

      }

      /// <summary>
      /// This constructor should be used on e.g. assembly entry point methods to indicate the name of the method within the same type which is actual entry point method.
      /// </summary>
      /// <param name="entryPointMethodName">The name of the actual entry point method within same type.</param>
      public ConfiguredEntryPointAttribute(
         String entryPointMethodName
         ) : this( null, entryPointMethodName )
      {

      }

      /// <summary>
      /// This constructor should be used on assemblies or methods to narrow down both type and name of the entry point method.
      /// </summary>
      /// <param name="entryPointType">The type containing entry point method.</param>
      /// <param name="entryPointMethodName">The name of the entry point method.</param>
      public ConfiguredEntryPointAttribute(
         Type entryPointType,
         String entryPointMethodName
         )
      {
         this.EntryPointType = entryPointType;
         this.EntryPointMethodName = entryPointMethodName;
      }

      /// <summary>
      /// Gets the type where the entry point method is declared. May be <c>null</c>.
      /// </summary>
      /// <value>The type where the entry point method is declared.</value>
      public Type EntryPointType { get; }

      /// <summary>
      /// Gets the name of the entry point method. May be <c>null</c>.
      /// </summary>
      /// <value>The name of the entry point method.</value>
      public String EntryPointMethodName { get; }
   }
}
