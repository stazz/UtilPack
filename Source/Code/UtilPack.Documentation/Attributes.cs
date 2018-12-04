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

namespace UtilPack.Documentation
{
   /// <summary>
   /// Use this attribute to mark properties which are required to be specified in the configuration.
   /// </summary>
   [AttributeUsage( AttributeTargets.Property )]
   public sealed class RequiredAttribute : Attribute
   {
      /// <summary>
      /// Get or sets whether this is required only in some special case, e.g. when some other property is specified or missing.
      /// </summary>
      /// <value>Whether this is required only in some special case, e.g. when some other property is specified or missing.</value>
      /// <remarks>
      /// By default, this is <c>false</c>, which means that the property is always required.
      /// </remarks>
      public Boolean Conditional { get; set; }
   }

   /// <summary>
   /// Use this attribute to add a description about property value.
   /// </summary>
   [AttributeUsage( AttributeTargets.Property )]
   public sealed class DescriptionAttribute : Attribute
   {
      /// <summary>
      /// Gets or sets description for what kind of value the property represents (e.g. a path in a filesystem, an url, or something else).
      /// </summary>
      /// <value>The description for what kind of value the property represents (e.g. a path in a filesystem, an url, or something else).</value>
      /// <remarks>
      /// The <see cref="CommandLineArgumentsDocumentationGenerator"/> by default knows to auto-fill this property for enum types and for <see cref="Boolean"/>.
      /// </remarks>
      public String ValueName { get; set; }

      /// <summary>
      /// Gets or sets detailed description about the meaning of the property, and how the value is interpreted in various scenarios.
      /// </summary>
      /// <value>The detailed description about the meaning of the property, and how the value is interpreted in various scenarios.</value>
      public String Description { get; set; }
   }

   /// <summary>
   /// Use this attribute to mark a property belonging to a certain named parameter group by default.
   /// </summary>
   [AttributeUsage( AttributeTargets.Property )]
   public sealed class ParameterGroupAttribute : Attribute
   {
      /// <summary>
      /// Gets or sets the name of the parameter group.
      /// </summary>
      public String Group { get; set; }
   }

   /// <summary>
   /// Use this attribute to mark a property which should be ignored by documentation.
   /// </summary>
   [AttributeUsage( AttributeTargets.Property )]
   public sealed class IgnoreInDocumentation : Attribute
   {

   }
}

