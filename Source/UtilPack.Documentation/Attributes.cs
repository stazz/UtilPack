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
   [AttributeUsage( AttributeTargets.Property )]
   public sealed class RequiredAttribute : Attribute
   {
      public Boolean Conditional { get; set; }

      //public String AdditionalInformation { get; set; }
   }

   [AttributeUsage( AttributeTargets.Property )]
   public sealed class DescriptionAttribute : Attribute
   {
      public String ValueName { get; set; }

      public String Description { get; set; }
   }

   [AttributeUsage( AttributeTargets.Property )]
   public sealed class ParameterGroupAttribute : Attribute
   {

      public String Group { get; set; }
   }

   [AttributeUsage( AttributeTargets.Property )]
   public sealed class IgnoreInDocumentation : Attribute
   {

   }

   public interface ParameterGroupOrFixedParameter
   {
      Boolean IsOptional { get; }
   }

   public abstract class ParameterGroupOrFixedParameterImpl : ParameterGroupOrFixedParameter
   {
      public ParameterGroupOrFixedParameterImpl(
         Boolean isOptional
         )
      {
         this.IsOptional = isOptional;
      }

      public Boolean IsOptional { get; }
   }

}

