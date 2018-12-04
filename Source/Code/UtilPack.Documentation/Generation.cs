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

      /// <summary>
      /// This is common base class for <see cref="NamedParameterGroup"/>, <see cref="GroupContainer"/>, and <see cref="FixedParameter"/>.
      /// Use instances of those classes to control how the parameters are printed in the description.
      /// </summary>
      /// <remarks>
      /// The <see cref="CommandLineArgumentsDocumentationGenerator"/> will print parameter summary (before detailed explanation of each parameter) using instances of this class as a model.
      /// So for example to print a string <c>required-options [optional-options] [[--] [other-options]]</c> one could use the following:
      /// <list type="number">
      /// <item><description>non-optional <see cref="NamedParameterGroup"/> with name <c>required-options</c>,</description></item>
      /// <item><description>optional <see cref="NamedParameterGroup"/> with name<c>optional-options</c>, and</description></item>
      /// <item><description>optional <see cref="GroupContainer"/> with the following children:
      /// <list type="number">
      /// <item><description>optional <see cref="FixedParameter"/> with value <c>--</c>,</description></item>
      /// <item><description>optional <see cref="NamedParameterGroup"/> with name <c>other-options</c>.</description></item>
      /// </list>
      /// </description></item>
      /// </list>
      /// </remarks>
      public abstract class ParameterGroupOrFixedParameter
      {
         internal ParameterGroupOrFixedParameter(
            Boolean isOptional
            )
         {
            this.IsOptional = isOptional;
         }

         /// <summary>
         /// Gets the value indicating whether this group or single parameter is optional.
         /// </summary>
         /// <value>The value indicating whether this group or single parameter is optional.</value>
         public Boolean IsOptional { get; }
      }

      /// <summary>
      /// This class represents a named parameter group.
      /// </summary>
      public sealed class NamedParameterGroup : ParameterGroupOrFixedParameter
      {
         private static readonly IComparer<TPropertyInfo> _DefaultOptionalComparer = ComparerFromFunctions.NewComparer<TPropertyInfo>( ( x, y ) => StringComparer.Ordinal.Compare( x.Item2, y.Item2 ) );

         private static readonly IComparer<TPropertyInfo> _DefaultRequiredComparer = ComparerFromFunctions.NewComparer<TPropertyInfo>( ( x, y ) =>
         {
            var retVal = ( x.Item1.GetCustomAttribute<RequiredAttribute>()?.Conditional ?? false ).CompareTo( y.Item1.GetCustomAttribute<RequiredAttribute>()?.Conditional ?? false );
            return retVal == 0 ?
               StringComparer.Ordinal.Compare( x.Item2, y.Item2 ) :
               retVal;
         } );

         /// <summary>
         /// Creates a new instance of <see cref="NamedParameterGroup"/> with given arguments.
         /// </summary>
         /// <param name="isOptional">whether this <see cref="NamedParameterGroup"/> is optional.</param>
         /// <param name="name">The name of this <see cref="NamedParameterGroup"/>.</param>
         /// <param name="description">The optional description of this <see cref="NamedParameterGroup"/>.</param>
         /// <param name="requiredParametersComparer">The optional <see cref="IComparer{T}"/> to sort required parameters. By default, <see cref="StringComparer.Ordinal"/> is used for property names, in such way that truly required parameters come first, and then only conditionally required ones.</param>
         /// <param name="optionalParametersComparer">The optional <see cref="IComparer{T}"/> to sort optional parameters. By default, <see cref="StringComparer.Ordinal"/> is used for property names.</param>
         /// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
         /// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
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

         /// <summary>
         /// Gets the name of this <see cref="NamedParameterGroup"/>.
         /// </summary>
         /// <value>The name of this <see cref="NamedParameterGroup"/>.</value>
         public String Name { get; }

         /// <summary>
         /// Gets the optional description of this <see cref="NamedParameterGroup"/>.
         /// </summary>
         /// <value>The optional description of this <see cref="NamedParameterGroup"/>.</value>
         public String Description { get; }

         /// <summary>
         /// Gets the <see cref="IComparer{T}"/> for required parameters.
         /// </summary>
         /// <value>The <see cref="IComparer{T}"/> for required parameters.</value>
         /// <remarks>This value is never <c>null</c>.</remarks>
         public IComparer<TPropertyInfo> RequiredParametersComparer { get; }

         /// <summary>
         /// Gets the <see cref="IComparer{T}"/> for optional parameters.
         /// </summary>
         /// <value>The <see cref="IComparer{T}"/> for optional parameters.</value>
         /// <remarks>This value is never <c>null</c>.</remarks>
         public IComparer<TPropertyInfo> OptionalParametersComparer { get; }
      }

      /// <summary>
      /// This class may be used to group several other <see cref="ParameterGroupOrFixedParameter"/>s into a single, potentially optional, parameter group.
      /// </summary>
      public sealed class GroupContainer : ParameterGroupOrFixedParameter
      {
         private readonly ParameterGroupOrFixedParameter[] _children;

         /// <summary>
         /// Creates a new instance of <see cref="GroupContainer"/> with given parameters.
         /// </summary>
         /// <param name="isOptional">Whether this <see cref="GroupContainer"/> is optional, as a whole.</param>
         /// <param name="children">The <see cref="ParameterGroupOrFixedParameter"/> instances that belong to this group. May be <c>null</c> or empty.</param>
         public GroupContainer(
            Boolean isOptional,
            IEnumerable<ParameterGroupOrFixedParameter> children
            ) : base( isOptional )
         {
            this._children = children?.Where( c => c != null )?.ToArray() ?? Empty<ParameterGroupOrFixedParameter>.Array;
         }

         /// <summary>
         /// Gets the children enumerable.
         /// </summary>
         /// <value>The children enumerable.</value>
         public IEnumerable<ParameterGroupOrFixedParameter> ChildGroups
         {
            get
            {
               foreach ( var child in this._children )
               {
                  yield return child;
               }
            }
         }
      }

      /// <summary>
      /// This class represents as a single parameter with fixed value, e.g. <c>--</c>.
      /// </summary>
      public sealed class FixedParameter : ParameterGroupOrFixedParameter
      {
         /// <summary>
         /// Creates new instance of <see cref="FixedParameter"/> with given parameters.
         /// </summary>
         /// <param name="isOptional">Whether this <see cref="FixedParameter"/> is optional</param>
         /// <param name="parameter">The parameter value.</param>
         /// <exception cref="ArgumentNullException">If <paramref name="parameter"/> is <c>null</c>.</exception>
         /// <exception cref="ArgumentException">If <paramref name="parameter"/> is empty.</exception>
         public FixedParameter(
            Boolean isOptional,
            String parameter
            ) : base( isOptional )
         {
            this.Parameter = ArgumentValidator.ValidateNotEmpty( nameof( parameter ), parameter );
         }

         /// <summary>
         /// Gets the fixed parameter value.
         /// </summary>
         /// <value>The fixed parameter value.</value>
         public String Parameter { get; }
      }

      /// <summary>
      /// This class is responsible for generating the string with documentation from model of <see cref="ParameterGroupOrFixedParameter"/> instances, with some additional information.
      /// The string is generated by invoking <see cref="GenerateParametersDocumentation"/> method.
      /// </summary>
      /// <seealso cref="GenerateParametersDocumentation"/>
      public class CommandLineArgumentsDocumentationGenerator
      {
         /// <summary>
         /// This method generates a documentation string with given model of <see cref="ParameterGroupOrFixedParameter"/> instances along with additional information.
         /// </summary>
         /// <param name="parameterGroups">The <see cref="ParameterGroupOrFixedParameter"/> instances, capturing information about parameter grouping. May be <c>null</c> or empty.</param>
         /// <param name="documentationType">The configuration type containing various attributes (e.g. <see cref="RequiredAttribute"/>, <see cref="DescriptionAttribute"/>, etc) to aid string generation. May be <c>null</c>.</param>
         /// <param name="execName">The name of the executable. May be <c>null</c> or empty, but no automatic deduction is done then.</param>
         /// <param name="purpose">The short description about the purpose of the executable. May be <c>null</c> or empty, but no automatic deduction is done then.</param>
         /// <param name="defaultGroup">Optional default group name for properties which are not marked with <see cref="ParameterGroupAttribute"/>. Only used when <paramref name="parameterGroups"/> recursively contain more than one <see cref="NamedParameterGroup"/>.</param>
         /// <returns>Generated documentation string.</returns>
         public virtual String GenerateParametersDocumentation(
            IEnumerable<ParameterGroupOrFixedParameter> parameterGroups,
            Type documentationType,
            String execName,
            String purpose,
            String defaultGroup = null
            )
         {
            var groups = parameterGroups?.ToArray() ?? Empty<ParameterGroupOrFixedParameter>.Array;
            var namedGroups = groups
               .SelectMany( group => group.AsDepthFirstEnumerable( g => ( g as GroupContainer )?.ChildGroups ?? Empty<ParameterGroupOrFixedParameter>.Enumerable ) )
               .OfType<NamedParameterGroup>()
               .ToArray();

            if ( String.IsNullOrEmpty( defaultGroup ) && namedGroups.Length == 1 )
            {
               defaultGroup = namedGroups[0].Name;
            }

            var groupInfo = this.CreateGroupInfo( documentationType, defaultGroup );
            return
$@"Usage: {execName} { groups.JoinToString( " ", g => this.CreateGroupDescriptionString( g ) ) }

{purpose}

{namedGroups.JoinToString( "\n\n", named => this.CreateGroupParameterString( groupInfo, named ) )}";
         }

         /// <summary>
         /// Subclasses may override this method to create custom information about named groups.
         /// </summary>
         /// <param name="type">The configuration type containing various attributes (e.g. <see cref="RequiredAttribute"/>, <see cref="DescriptionAttribute"/>, etc) to aid string generation. May be <c>null</c>.</param>
         /// <param name="defaultGroup">The default group. May be <c>null</c> or empty.</param>
         /// <returns>Dictionary with group information. Value type is tuple, where first list is required parameters, and second list is optional parameters.</returns>
         protected virtual TGroupInfoDictionary CreateGroupInfo(
            Type type,
            String defaultGroup
            )
         {
            var retVal = new TGroupInfoDictionary();
            if ( type != null )
            {
               foreach ( var propInfo in default( TPropertyInfo ).AsDepthFirstEnumerableWithLoopDetection( p => this.GetNestedProperties( p, type ), returnHead: false ).Where( p => this.CanBeSpecifiedViaCommandLine( p.Item1.PropertyType ) ) )
               {
                  var prop = propInfo.Item1;
                  var lists = retVal.GetOrAdd_NotThreadSafe( prop.GetCustomAttribute<ParameterGroupAttribute>()?.Group ?? defaultGroup, ignored => (new List<TPropertyInfo>(), new List<TPropertyInfo>()) );
                  ( prop.GetCustomAttribute<RequiredAttribute>() == null ? lists.Item2 : lists.Item1 )
                     .Add( propInfo );
               }
            }

            return retVal;
         }


         private IEnumerable<TPropertyInfo> GetNestedProperties(
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

         /// <summary>
         /// Subclasses may override this method with custom logic how to extract nested properties from single property.
         /// This method should not be recursive.
         /// </summary>
         /// <param name="propertyType">The type of the property value.</param>
         /// <param name="currentContext">Current context ('path') of the property.</param>
         /// <returns>This implementation returns empty enumerable if <see cref="CanBeSpecifiedViaCommandLine"/> returns <c>true</c> for <paramref name="propertyType"/>. Otherwise returns all properties of <paramref name="propertyType"/> which don't have <see cref="IgnoreInDocumentation"/> attribute applied to them.</returns>
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

         /// <summary>
         /// Subclasses may override this with custom logic on whether some type can be specified in command line.
         /// </summary>
         /// <param name="propertyType">The property value type.</param>
         /// <returns>This implementation returns <c>true</c> for primitive types or enum types.</returns>
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

         private String CreateGroupDescriptionString(
            ParameterGroupOrFixedParameter group
            )
         {
            var sb = new StringBuilder();
            this.CreateGroupDescriptionString( group, sb );
            return sb.ToString();
         }

         /// <summary>
         /// Subclasses may override this with custom logic on how to construct a string for single <see cref="ParameterGroupOrFixedParameter"/>.
         /// This method is allowed to be recursive.
         /// </summary>
         /// <param name="group">The <see cref="ParameterGroupOrFixedParameter"/>.</param>
         /// <param name="sb">The <see cref="StringBuilder"/> holding string being constructed.</param>
         /// <remarks>The default implementation surrounds the actual string with brackets (<c>[</c>, <c>]</c>) if the <see cref="ParameterGroupOrFixedParameter.IsOptional"/> is true for <paramref name="group"/>.</remarks>
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
                  foreach ( var child in container.ChildGroups )
                  {
                     this.CreateGroupDescriptionString( child, sb );
                     sb.Append( " " );
                  }
                  break;
            }
            if ( isOptional )
            {
               sb.Append( "]" );
            }

         }

         /// <summary>
         /// Subclasses may override this with custom logic on how to construct detailed parameter description string for one whole <see cref="NamedParameterGroup"/>.
         /// </summary>
         /// <param name="groupInfo">Group info dictionary created by <see cref="CreateGroupInfo"/></param>
         /// <param name="namedGroup">The <see cref="NamedParameterGroup"/>.</param>
         /// <returns>Current implementation calls <see cref="CreatePropertyBasedParametersDescription"/> if the <paramref name="groupInfo"/> contains value for <see cref="NamedParameterGroup.Name"/> of <paramref name="namedGroup"/>; otherwise returns result of <see cref="CreateFreeFormParameterGroupDescription"/>.</returns>
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

         /// <summary>
         /// Subclasses may override this with custom logic on how to construct detailed parameter description string for all properties related to single <see cref="NamedParameterGroup"/>.
         /// </summary>
         /// <param name="namedGroup">The <see cref="NamedParameterGroup"/>.</param>
         /// <param name="props">Information about required (first element) and optional (second element) parameters.</param>
         /// <returns>Current implementation sorts the parameters according to <see cref="NamedParameterGroup.RequiredParametersComparer"/> and <see cref="NamedParameterGroup.OptionalParametersComparer"/> comparers of <paramref name="namedGroup"/>, and then constructs one line for each parameter, checking for property type and <see cref="RequiredAttribute"/> and <see cref="DescriptionAttribute"/> possibly applied on property.</returns>
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
                     else if ( propertyType.
#if NETSTANDARD1_0
                        GetTypeInfo().
#endif
                     IsEnum )
                     {
                        v = Enum.GetValues( propertyType ).Cast<Object>().JoinToString( "|" );
                     }
                  }
                  return (p.Item2, $"<{( String.IsNullOrEmpty( v ) ? "value" : v )}>", desc?.Description, req?.Conditional ?? false);
               } )
               .ToArray();
            var maxLength = allParameters.Max( t => t.Item1.Length + t.Item2.Length + 3 );
            return allParameters.JoinToString( "\n", ( p, idx ) => $@"{( idx < reqCount ? ( ( p.Item4 ? "+" : "*" ) + " " ) : "  " )}{( p.Item1 + " " + p.Item2 ).PadRight( maxLength, ' ' )}{p.Item3}" );
         }

         /// <summary>
         /// Subclasses may override this method to provide custom logic in order to create description string for <see cref="NamedParameterGroup"/> which did not contain matching properties. 
         /// </summary>
         /// <param name="namedGroup">The <see cref="NamedParameterGroup"/>.</param>
         /// <returns>Current implementation returns <see cref="NamedParameterGroup.Description"/> of given <paramref name="namedGroup"/>, or empty string, if the <see cref="NamedParameterGroup.Description"/> is <c>null</c>.</returns>
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