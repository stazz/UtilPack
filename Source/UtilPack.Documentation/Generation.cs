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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using TPropertyInfo = System.ValueTuple<System.Reflection.PropertyInfo, System.String>;

namespace UtilPack
{
   using TGroupInfoValues = ValueTuple<List<TPropertyInfo>, List<TPropertyInfo>>;

   namespace Documentation
   {
      using TGroupInfoDictionary = Dictionary<String, TGroupInfoValues>;

      public sealed class NamedParameterGroup : ParameterGroupOrFixedParameterImpl
      {
         private static readonly IComparer<TPropertyInfo> _DefaultOptionalComparer = ComparerFromFunctions.NewComparer<TPropertyInfo>( ( x, y ) => StringComparer.Ordinal.Compare( x.Item2, y.Item2 ) );

         private static readonly IComparer<TPropertyInfo> _DefaultRequiredComparer = ComparerFromFunctions.NewComparer<TPropertyInfo>( ( x, y ) =>
         {
            var retVal = ( x.Item1.GetCustomAttribute<RequiredAttribute>()?.Conditional ?? false ).CompareTo( y.Item1.GetCustomAttribute<RequiredAttribute>()?.Conditional ?? false );
            return retVal == 0 ?
               StringComparer.Ordinal.Compare( x.Item2, y.Item2 ) :
               retVal;
         } );
         public NamedParameterGroup(
            Boolean isOptional,
            String name,
            String description = null,
            IComparer<TPropertyInfo> requiredParametersComparer = null,
            IComparer<TPropertyInfo> optionalParametersComparer = null
            ) : base( isOptional )
         {
            this.Name = ArgumentValidator.ValidateNotEmpty( nameof( name ), name );
            this.Description = description;
            this.RequiredParametersComparer = requiredParametersComparer ?? _DefaultRequiredComparer;
            this.OptionalParametersComparer = optionalParametersComparer ?? _DefaultOptionalComparer;
         }

         public String Name { get; }
         public String Description { get; }
         public IComparer<TPropertyInfo> RequiredParametersComparer { get; }
         public IComparer<TPropertyInfo> OptionalParametersComparer { get; }
      }

      public sealed class GroupContainer : ParameterGroupOrFixedParameterImpl
      {
         public GroupContainer(
            Boolean isOptional,
            IEnumerable<ParameterGroupOrFixedParameter> children
            ) : base( isOptional )
         {

            this.ChildGroups = children?.Where( c => c != null )?.ToArray() ?? Empty<ParameterGroupOrFixedParameter>.Array;
         }

         public ParameterGroupOrFixedParameter[] ChildGroups { get; }
      }

      public sealed class FixedParameter : ParameterGroupOrFixedParameterImpl
      {
         public FixedParameter(
            Boolean isOptional,
            String parameter
            ) : base( isOptional )
         {
            this.Parameter = ArgumentValidator.ValidateNotEmpty( nameof( parameter ), parameter );
         }

         public String Parameter { get; }
      }

      public class CommandLineArgumentsDocumentationGenerator
      {
         public virtual String GenerateParametersDocumentation(
            IEnumerable<ParameterGroupOrFixedParameter> parameterGroups,
            Type documentationType,
            String execName,
            String purpose,
            String defaultGroup = null
            )
         {
            var groups = parameterGroups.ToArray();
            var namedGroups = groups
               .SelectMany( group => group.AsDepthFirstEnumerable( g => ( g as GroupContainer )?.ChildGroups ?? Empty<ParameterGroupOrFixedParameter>.Enumerable ) )
               .OfType<NamedParameterGroup>()
               .ToArray();
            var groupInfo = this.CreateGroupInfo( documentationType, namedGroups, defaultGroup );
            return
   $@"Usage: {execName} {String.Join( " ", groups.Select( g => this.CreateGroupDescriptionString( g ) ) )}

{purpose}

{String.Join( "\n\n", namedGroups.Select( named => this.CreateGroupParameterString( groupInfo, named ) ) )}";
         }

         protected virtual TGroupInfoDictionary CreateGroupInfo(
            Type type,
            NamedParameterGroup[] namedGroups,
            String defaultGroup
            )
         {
            var retVal = new TGroupInfoDictionary();
            if ( String.IsNullOrEmpty( defaultGroup ) && namedGroups.Length == 1 )
            {
               defaultGroup = namedGroups[0].Name;
            }
            foreach ( var propInfo in default( TPropertyInfo ).AsDepthFirstEnumerableWithLoopDetection( p => this.GetNestedProperties( p, type ), returnHead: false ).Where( p => this.CanBeSpecifiedViaCommandLine( p.Item1.PropertyType ) ) )
            {
               var prop = propInfo.Item1;
               var lists = retVal.GetOrAdd_NotThreadSafe( prop.GetCustomAttribute<ParameterGroupAttribute>()?.Group ?? defaultGroup, ignored => (new List<TPropertyInfo>(), new List<TPropertyInfo>()) );
               ( prop.GetCustomAttribute<RequiredAttribute>() == null ? lists.Item2 : lists.Item1 )
                  .Add( propInfo );
            }

            return retVal;
         }

         protected virtual IEnumerable<TPropertyInfo> GetNestedProperties(
            TPropertyInfo propertyInfo,
            Type startingType
            )
         {
            var property = propertyInfo.Item1;
            Type propertyType = null;
            String currentContext = null;
            if ( property == null )
            {
               propertyType = startingType;
            }
            else if ( property.GetCustomAttribute<IgnoreInDocumentation>() == null )
            {
               // We could check for interface which have properties with same name + signature...
               // But that would maybe encourage 'bad' behaviour and make code overly complex - how to e.g. merge description attributes present in both class property and interface/base type property...?
               // Instead, it maybe would be better idea to expose common documentation of properties via e.g. constant fields.
               propertyType = property.PropertyType;
               currentContext = propertyInfo.Item2;
            }
            return propertyType == null ? Empty<TPropertyInfo>.Enumerable : this.GetNestedProperties( propertyType, currentContext );
         }

         protected virtual IEnumerable<TPropertyInfo> GetNestedProperties(
            Type propertyType,
            String currentContext
            )
         {
            if ( !String.IsNullOrEmpty( currentContext ) )
            {
               currentContext += ":";
            }

            // From documentation: The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single. 
            // That still leaves out string and enums
            return this.CanBeSpecifiedViaCommandLine( propertyType ) ?
               Empty<TPropertyInfo>.Enumerable :
               propertyType.GetRuntimeProperties()
                  .Where( p =>
                  {
                     var getter = p.
#if NET40
                        GetGetMethod( true )
#else
                        GetMethod
#endif
                     ;
                     var setter = p.
#if NET40
                        GetSetMethod( true )
#else
                        SetMethod
#endif
                     ;

                     return getter != null && ( setter?.IsPublic ?? false ) && !setter.IsStatic && p.GetCustomAttribute<IgnoreInDocumentation>() == null;
                  } )
                  .Select( p => (p, currentContext + p.Name) );
         }

         protected virtual Boolean CanBeSpecifiedViaCommandLine(
            Type propertyType
            )
         {
            while ( propertyType.IsArray || propertyType.IsByRef )
            {
               propertyType = propertyType.GetElementType();
            }

            var t = propertyType.GetTypeInfo();
            return t != null
               && ( t.IsPrimitive
#if NET40
               ()
#endif
               || Equals( typeof( String ), propertyType )
               || t.IsEnum
#if NET40
               ()
#endif
               || propertyType.IsPointer );
         }

         protected virtual String CreateGroupDescriptionString(
            ParameterGroupOrFixedParameter group
            )
         {
            var sb = new StringBuilder();
            this.CreateGroupDescriptionString( group, sb );
            return sb.ToString();
         }

         protected virtual void CreateGroupDescriptionString(
            ParameterGroupOrFixedParameter group,
            StringBuilder sb
            )
         {
            var isOptional = group.IsOptional;
            if ( isOptional )
            {
               sb.Append( "[" );
            }
            switch ( group )
            {
               case NamedParameterGroup named:
                  sb.Append( named.Name );
                  break;
               case FixedParameter param:
                  sb.Append( param.Parameter );
                  break;
               case GroupContainer container:
                  var children = container.ChildGroups;
                  if ( children.Length > 0 )
                  {
                     foreach ( var child in children )
                     {
                        this.CreateGroupDescriptionString( child, sb );
                        sb.Append( " " );
                     }
                  }
                  break;
            }
            if ( isOptional )
            {
               sb.Append( "]" );
            }

         }

         protected virtual String CreateGroupParameterString(
            TGroupInfoDictionary groupInfo,
            NamedParameterGroup namedGroup
            )
         {

            return
$@"{namedGroup.Name}:
{( groupInfo.TryGetValue( namedGroup.Name, out var props ) ?
      this.CreatePropertyBasedParametersDescription( namedGroup, props ) :
      this.CreateFreeFormParameterGroupDescription( namedGroup )
)}";
         }

         protected virtual String CreatePropertyBasedParametersDescription(
            NamedParameterGroup namedGroup,
            TGroupInfoValues props
            )
         {
            var reqCount = props.Item1.Count;
            var allParameters = props.Item1
               .OrderBy( p => p, namedGroup.RequiredParametersComparer )
               .Concat( props.Item2.OrderBy( p => p, namedGroup.OptionalParametersComparer ) )
               .Select( p =>
               {
                  var desc = p.Item1.GetCustomAttribute<DescriptionAttribute>();
                  var req = p.Item1.GetCustomAttribute<RequiredAttribute>();
                  var v = desc?.ValueName;
                  if ( String.IsNullOrEmpty( v ) )
                  {
                     var propertyType = p.Item1.PropertyType;
                     if ( Equals( propertyType, typeof( Boolean ) ) || Equals( propertyType, typeof( Boolean? ) ) )
                     {
                        v = "true|false";
                     }
                     // TODO enum support
                  }
                  return (p.Item2, $"<{( String.IsNullOrEmpty( v ) ? "value" : v )}>", desc?.Description, req?.Conditional ?? false);
               } )
               .ToArray();
            var maxLength = allParameters.Max( t => t.Item1.Length + t.Item2.Length + 3 );
            return String.Join( "\n", allParameters.Select( ( p, idx ) => $@"{( idx < reqCount ? ( ( p.Item4 ? "+" : "*" ) + " " ) : "  " )}{( p.Item1 + " " + p.Item2 ).PadRight( maxLength, ' ' )}{p.Item3}" ) );
         }

         protected virtual String CreateFreeFormParameterGroupDescription(
            NamedParameterGroup namedGroup
            )
         {
            var desc = namedGroup.Description;
            return String.IsNullOrEmpty( desc ) ? "" : $"  {desc}";
         }
      }
   }
}


internal static class E_Reflection
{
   internal static T GetCustomAttribute<T>( this MemberInfo member )
      where T : Attribute
   {
      return member
         .GetCustomAttributes( typeof( T ), false )
         ?.FirstOrDefault()
         as T;
   }

#if NET40

   internal static IEnumerable<PropertyInfo> GetRuntimeProperties( this Type type )
   {
      return type.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance );
   }

   internal static Boolean IsEnum( this Type type )
   {
      return Equals( typeof( Enum ), type?.GetTypeInfo().BaseType );
   }

   internal static Boolean IsPrimitive( this Type type )
   {
      switch ( Type.GetTypeCode( type ) )
      {
         case TypeCode.Boolean:
         case TypeCode.Byte:
         case TypeCode.Char:
         case TypeCode.Double:
         case TypeCode.Int16:
         case TypeCode.Int32:
         case TypeCode.Int64:
         case TypeCode.SByte:
         case TypeCode.Single:
         case TypeCode.UInt16:
         case TypeCode.UInt32:
         case TypeCode.UInt64:
            return true;
         default:
            return Equals( typeof( IntPtr ), type ) || Equals( typeof( UIntPtr ), type );
      }
   }

#endif
}