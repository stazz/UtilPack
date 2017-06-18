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
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace UtilPack.Configuration
{
   public delegate ValueTask<TypeInfo> TypeLoaderDelegate( IConfiguration config, TypeInfo targetType );
   public delegate IConfigurationSection ConstructorConfigurationLoaderDelegate( IConfiguration config, TypeInfo loadedType );

   public class DynamicConfigurableTypeLoader
   {
      private readonly TypeLoaderDelegate _typeLoader;
      private readonly ConstructorConfigurationLoaderDelegate _constructorArgumentsLoader;

      public DynamicConfigurableTypeLoader(
         TypeLoaderDelegate typeLoader,
         ConstructorConfigurationLoaderDelegate constructorConfigLoader = null
         )
      {
         this._typeLoader = typeLoader ?? throw new ArgumentNullException( nameof( typeLoader ) );
         this._constructorArgumentsLoader = constructorConfigLoader ?? ( ( cfg, type ) => cfg.GetSection( "Configuration" ) );
      }

      public async ValueTask<Object> InstantiateWithConfiguration(
         IConfiguration config,
         TypeInfo targetType
         )
      {
         var type = await this._typeLoader( config, targetType );

         Object retVal = null;
         if ( type != null && !type.IsInterface && !type.IsAbstract )
         {
            // Type load successful - now figure out constructor and arguments to it
            (var ctor, var ctorArgs) = this.DeduceConstructor( type, this._constructorArgumentsLoader( config, type ) );
            if ( ctor != null )
            {
               Object[] actualCtorArgs;
               if ( ctorArgs != null )
               {
                  var tuples = ctorArgs( ctor.GetParameters() ).ToArray();
                  await Task.WhenAll( tuples
                     .Select( tuple => this.ScanForNestedConfigs( tuple.Item1, tuple.Item2 ).AsTask() )
                     .ToArray() );
                  actualCtorArgs = tuples.Select( t => t.Item2 ).ToArray();

               }
               else
               {
                  actualCtorArgs = null;
               }

               retVal = ctor.Invoke( actualCtorArgs );
            }
         }

         return retVal;
      }

      private (ConstructorInfo, Func<ParameterInfo[], IEnumerable<(IConfigurationSection, Object)>>) DeduceConstructor(
         TypeInfo loadedType,
         IConfigurationSection ctorConfig
         )
      {
         ConstructorInfo suitableCtor;
         Func<ParameterInfo[], IEnumerable<(IConfigurationSection, Object)>> ctorArgs;
         var possibleCtors = loadedType.DeclaredConstructors.Where( ctor => ctor.IsPublic && !ctor.IsStatic );
         Type singleConfigType;
         if ( ( singleConfigType = loadedType.GetCustomAttribute<ConfigurationTypeAttribute>( true )?.ConfigurationType ) != null )
         {
            suitableCtor = possibleCtors.FirstOrDefault( ctor =>
            {
               var paramz = ctor.GetParameters();
               return paramz.Length == 1 && paramz[0].ParameterType.GetTypeInfo().IsAssignableFrom( singleConfigType.GetTypeInfo() );
            } );
            ctorArgs = paramz => GetConfigTypeCtorArg( ctorConfig, singleConfigType );
         }
         else
         {
            var children = ctorConfig.GetChildren().ToArray();
            if ( children.Length > 0 )
            {
               if ( children.All( child => Int32.TryParse( child.Key, out var ignored ) ) )
               {
                  // This is an array - need to find constructor which accepts given amount of parameters
                  suitableCtor = possibleCtors
                     .Where( ctor =>
                     {
                        var paramz = ctor.GetParameters();
                        return paramz.Length >= children.Length
                             && paramz.Skip( children.Length ).All( p => p.IsOptional );
                     } )
                     .OrderBy( c => c.GetParameters().Length )
                     .FirstOrDefault();
                  ctorArgs = paramz => children.Select( ( c, idx ) => (c, c.Get( paramz[idx].ParameterType )) );
               }
               else
               {
                  // This is an object - try to find constructor with all required parameters
                  suitableCtor = possibleCtors
                     .Where( ctor =>
                     {
                        var paramz = ctor.GetParameters();
                        return paramz.All( p => !String.IsNullOrEmpty( ctorConfig.GetSection( p.Name )?.Value ) );
                     } )
                     .FirstOrDefault();
                  ctorArgs = paramz => paramz.Select( p =>
                  {
                     var thisSection = ctorConfig.GetSection( p.Name );
                     return (thisSection, thisSection.Get( p.ParameterType ));
                  } );
               }
            }
            else
            {
               // Try to find default constructor
               suitableCtor = possibleCtors.FirstOrDefault( ctor => ctor.GetParameters().Length == 0 );
               ctorArgs = null;
            }
         }

         return (suitableCtor, ctorArgs);
      }

      private static IEnumerable<(IConfigurationSection, Object)> GetConfigTypeCtorArg( IConfigurationSection config, Type configType )
      {
         yield return (config, config.Get( configType ));
      }

      private async ValueTask<Boolean> ScanForNestedConfigs(
         IConfigurationSection section,
         Object configurationInstance
         )
      {
         if ( section != null && configurationInstance != null )
         {
            var configType = configurationInstance.GetType();
            foreach ( var configProp in configurationInstance.GetType().GetRuntimeProperties() )
            {
               if ( configProp.GetMethod != null
                  && configProp.SetMethod != null
                  && !configProp.GetMethod.IsStatic )
               {

                  var nestedConfigAttribute = configProp.GetCustomAttribute<NestedDynamicConfigurationAttribute>( true );
                  if ( nestedConfigAttribute == null )
                  {
                     // This is normal property - process recursively
                     var nestedSection = section.GetSection( configProp.Name );
                     if ( !String.IsNullOrEmpty( nestedSection?.Value ) )
                     {

                        Object nestedConfigInstance = null;
                        try
                        {
                           nestedConfigInstance = configProp.GetMethod.Invoke( configurationInstance, null );
                        }
                        catch
                        {
                           // Ignore..
                        }

                        await ForSingleOrArray(
                           nestedSection,
                           configProp.PropertyType,
                           nestedConfigInstance,
                           async ( curConfig, curType, curInstance ) =>
                           {
                              if ( !typeof( IFormattable ).GetTypeInfo().IsAssignableFrom( curType.GetTypeInfo() )
                                && !Equals( curType, typeof( String ) )
                              )
                              {
                                 return await this.ScanForNestedConfigs( curConfig, curInstance );
                              }

                              return null;
                           }
                           );
                     }
                  }
                  else
                  {
                     // This is nested dynamic configuration property - try to create from configuration
                     var sectionName = nestedConfigAttribute.ConfigurationSectionName;
                     if ( !String.IsNullOrEmpty( sectionName ) )
                     {
                        var nestedConfigInstance = await ForSingleOrArray(
                              section.GetSection( sectionName ),
                              configProp.PropertyType,
                              null,
                              async ( curConfig, curType, curObject ) => await this.InstantiateWithConfiguration( curConfig, curType.GetTypeInfo() )
                              );
                        if ( nestedConfigInstance != null )
                        {
                           try
                           {
                              configProp.SetMethod.Invoke( configurationInstance, new[] { nestedConfigInstance } );
                           }
                           catch
                           {
                              // Ignore, for now...
                           }
                        }

                     }
                  }
               }
            }
         }

         return true;
      }

      private static async ValueTask<Object> ForSingleOrArray(
         IConfigurationSection config,
         Type type,
         Object thisObject,
         Func<IConfigurationSection, Type, Object, ValueTask<Object>> singleAction
         )
      {
         Object retVal;
         if ( config == null )
         {
            retVal = null;
         }
         else
         {
            if ( IsListType( type, out var elemType, out var addMethod ) )
            {
               var list = new List<Object>();
               var thisArray = (Array) thisObject;
               {
                  var i = 0;
                  foreach ( var actualSection in config.GetChildren() )
                  {
                     retVal = ForSingleOrArray( actualSection, elemType, thisArray?.GetValue( i ), singleAction );
                     if ( retVal != null )
                     {
                        list.Add( retVal );
                     }
                     ++i;
                  }
               }

               if ( thisObject == null )
               {
                  if ( addMethod == null )
                  {
                     var array = Array.CreateInstance( elemType, list.Count );
                     for ( var i = 0; i < list.Count; ++i )
                     {
                        array.SetValue( list[i], i );
                     }
                     retVal = array;
                  }
                  else
                  {
                     retVal = typeof( List<> )
                        .MakeGenericType( elemType ).GetTypeInfo()
                        .DeclaredConstructors
                        .First( c => c.GetParameters().Length == 1 && Equals( c.GetParameters()[0].ParameterType, typeof( Int32 ) ) )
                        .Invoke( new Object[] { list.Count } );
                     foreach ( var item in list )
                     {
                        addMethod.Invoke( retVal, new[] { item } );
                     }
                  }
               }
               else
               {
                  retVal = null;
               }
            }
            else
            {
               retVal = await singleAction( config, type, thisObject );
            }
         }

         return retVal;
      }

      private static Boolean IsListType( Type type, out Type elementType, out MethodInfo addMethod )
      {
         addMethod = null;
         if ( type.IsArray )
         {
            elementType = type.GetElementType();
         }
         else if ( type.GenericTypeArguments.Length == 1
            && typeof( IList<> ).MakeGenericType( type.GenericTypeArguments[0] ).GetTypeInfo().IsAssignableFrom( type.GetTypeInfo() ) )
         {
            elementType = type.GenericTypeArguments[0];
            addMethod = typeof( ICollection<> ).MakeGenericType( elementType ).GetRuntimeMethod( "Add", new[] { elementType } );
         }
         else
         {
            elementType = null;
         }

         return elementType != null;
      }
   }










   ///// <summary>
   ///// This is base class for configuration expressing loading of object, type of which is specified in configuration.
   ///// See <see cref="ConfigurationTypeAttribute"/> to learn how to customize what is given for a constructor of that type.
   ///// </summary>
   //public class DynamicElementConfiguration
   //{
   //   public TypeLoadInformation Type { get; set; }

   //}

   //public class TypeLoadInformation
   //{
   //   public String Location { get; set; }
   //   public String Name { get; set; }

   //   public override String ToString()
   //   {
   //      return String.Format( "{0}@{1}", this.Name, this.Location );
   //   }
   //}

   /// <summary>
   /// The types that are dynamically loaded by <see cref="E_DynamicTypeSpecification.TryLoadDynamicElement(IConfiguration, DynamicElementConfiguration, Func{string, Type}, TypeInfo, out object, out string, string)"/> method can either have parameterless constructor, or use this attribute to specify the argument for constructor.
   /// That constructor should take single parameter of type specified in this attribute.
   /// </summary>
   [AttributeUsage( AttributeTargets.Class, AllowMultiple = false )]
   public class ConfigurationTypeAttribute : Attribute
   {
      /// <summary>
      /// Creates new instance of <see cref="ConfigurationTypeAttribute"/> with given configuration type.
      /// </summary>
      /// <param name="configurationType">The type of configuration that constructor accepts as parameter.</param>
      public ConfigurationTypeAttribute( Type configurationType )
      {
         this.ConfigurationType = configurationType;
      }

      /// <summary>
      /// Gets the type of configuration that constructor accepts as parameter.
      /// </summary>
      /// <value>The type of configuration that constructor accepts as parameter.</value>
      public Type ConfigurationType { get; }

   }

   /// <summary>
   /// Sometimes the configuration type specified by <see cref="ConfigurationTypeAttribute"/> contains properties which should be loaded using <see cref="DynamicElementConfiguration"/>.
   /// In such case, this attribute should be used on those properties, indicating the name of the configuration section bindable to <see cref="DynamicElementConfiguration"/> type (using <see cref="ConfigurationBinder.Get(IConfiguration, Type)"/> method).
   /// </summary>
   [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
   public class NestedDynamicConfigurationAttribute : Attribute
   {
      /// <summary>
      /// Creates a new instance of <see cref="NestedDynamicConfigurationAttribute"/> with given section name for configuration of <see cref="DynamicElementConfiguration"/> type.
      /// </summary>
      /// <param name="configurationSectionName">The name of the configuration section bindable to see <see cref="DynamicElementConfiguration"/> type.</param>
      public NestedDynamicConfigurationAttribute( String configurationSectionName, String loadedTypeConfigurationSectionName = null ) //, Type configType = null )
      {
         this.ConfigurationSectionName = configurationSectionName;
         //this.LoadedTypeConfigurationSectionName = loadedTypeConfigurationSectionName;
         //this.ConfigurationType = configType;
      }

      /// <summary>
      /// Gets the name of configuration section for dynamically loaded type.
      /// </summary>
      /// <value>The name of configuration section for dynamically loaded type.</value>
      public String ConfigurationSectionName { get; }


      ///// <summary>
      ///// Gets the name of configuration section within the <see cref="ConfigurationSectionName"/> that should hold the configuration for dynamically loaded type. By default, it is <code>Configuration</code>.
      ///// </summary>
      ///// <value>The name of configuration section within the <see cref="ConfigurationSectionName"/> that should hold the configuration for dynamically loaded type.</value>
      //public String LoadedTypeConfigurationSectionName { get; }

      //public Type ConfigurationType { get; }
   }


}

//public static partial class E_DynamicTypeSpecification
//{
//   public const String SECTION_FOR_TARGET_CONFIGURATION = "Configuration";

//   //private static readonly ISet<Type> _configPrimitiveTypes = new HashSet<Type>()
//   //{
//   //   typeof(Object),
//   //   typeof(String),
//   //   typeof(SByte),
//   //   typeof(Byte),
//   //   typeof(Int16),
//   //   typeof(UInt16),
//   //   typeof(Int32),
//   //   typeof(UInt32),
//   //   typeof(Int64),
//   //   typeof(UInt64),
//   //   typeof(Single),
//   //   typeof(Double),
//   //   typeof(Decimal),
//   //   typeof(DateTime),
//   //   typeof(TimeSpan),
//   //};

//   public static IEnumerable<TElement> LoadDynamicElements<TElement>(
//      this IConfiguration config,
//      TypeLoader typeLoader,
//      Func<DynamicElementConfiguration, String> dynamicTargetConfigurationSectionName = null
//      )
//      where TElement : class
//   {
//      return config.EnumerateDynamicElements<TElement, DynamicElementConfiguration, TElement>( typeLoader, ( el, cfg ) => el, dynamicTargetConfigurationSectionName );
//   }
//   public static IEnumerable<Tuple<TElement, TConfig>> LoadDynamicElementsAndConfigurations<TElement, TConfig>(
//      this IConfiguration config,
//      TypeLoader typeLoader,
//      Func<TConfig, String> dynamicTargetConfigurationSectionName = null
//   )
//      where TElement : class
//      where TConfig : DynamicElementConfiguration
//   {
//      return config.EnumerateDynamicElements<TElement, TConfig, Tuple<TElement, TConfig>>( typeLoader, ( el, cfg ) => Tuple.Create( el, cfg ), dynamicTargetConfigurationSectionName );
//   }

//   private static IEnumerable<T> EnumerateDynamicElements<TElement, TConfig, T>(
//      this IConfiguration config,
//      TypeLoader typeLoader,
//      Func<TElement, TConfig, T> transformer,
//      Func<TConfig, String> dynamicTargetConfigurationSectionName
//      )
//      where TElement : class
//      where TConfig : DynamicElementConfiguration
//   {
//      var creatorsConfigObjects = config.Get<IEnumerable<TConfig>>();
//      var i = 0;
//      foreach ( var curConfig in creatorsConfigObjects )
//      {
//         TElement element;
//         String errorMsg;
//         if ( TryLoadDynamicElement(
//            config.GetSection( i.ToString() ),
//            curConfig,
//            typeLoader,
//            out element,
//            out errorMsg,
//            dynamicTargetConfigurationSectionName?.Invoke( curConfig )
//            ) )
//         {
//            yield return transformer( element, curConfig );
//         }
//         ++i;
//      }
//   }

//   public static TElement LoadDynamicElementOrNull<TElement>(
//      this IConfiguration config,
//      DynamicElementConfiguration dynamicConfig,
//      TypeLoader typeLoader,
//      String configurationSectionName = SECTION_FOR_TARGET_CONFIGURATION
//      )
//      where TElement : class
//   {
//      TElement retVal; String errorMsg;
//      return config.TryLoadDynamicElement( dynamicConfig, typeLoader, out retVal, out errorMsg, configurationSectionName ) ?
//         retVal :
//         null;
//   }


//   public static Boolean TryLoadDynamicElement<TElement>(
//      this IConfiguration config,
//      DynamicElementConfiguration dynamicConfig,
//      TypeLoader typeLoader,
//      out TElement element,
//      out String errorMessage,
//      String configurationSectionName = SECTION_FOR_TARGET_CONFIGURATION
//      )
//      where TElement : class
//   {
//      Object elementObj;
//      var retVal = config.TryLoadDynamicElement( dynamicConfig, typeLoader, typeof( TElement ).GetTypeInfo(), out elementObj, out errorMessage, configurationSectionName );
//      // The TryLoadDynamicElement will give an error if it is of wrong type, so it's safe to cast it here directly.
//      element = (TElement) elementObj;
//      return retVal;
//   }

//   public static Boolean TryLoadDynamicElement(
//      this IConfiguration config,
//      DynamicElementConfiguration dynamicConfig,
//      TypeLoader typeLoader,
//      TypeInfo elementType,
//      out Object element,
//      out String errorMessage,
//      String configurationSectionName = SECTION_FOR_TARGET_CONFIGURATION
//      )
//   {
//      element = null;
//      if ( config != null && dynamicConfig != null && elementType != null )
//      {
//         try
//         {
//            var typeName = dynamicConfig.Type;
//            var type = typeLoader( typeName )?.GetTypeInfo();
//            if ( type == null )
//            {
//               errorMessage = String.Format( "Could not load type: \"{0}\".", typeName );
//            }
//            else
//            {
//               if ( type.IsAbstract )
//               {
//                  errorMessage = String.Format( "The type {0} is marked as abstract.", typeName );
//               }
//               else
//               {
//                  if ( !elementType.IsAssignableFrom( type ) )
//                  {
//                     errorMessage = String.Format( "Given dynamic element type \"{0}\" must be same or subtype of \"{1}\".", typeName, elementType );
//                  }
//                  else
//                  {
//                     var configType = type.GetCustomAttribute<ConfigurationTypeAttribute>( true )?.ConfigurationType;
//                     Object configForConstructor = null;
//                     ConstructorInfo ctor;
//                     if ( configType == null )
//                     {
//                        ctor = type.FindInstanceConstructor( null ) ?? type.FindInstanceConstructor( new TypeInfo[] { null } );
//                     }
//                     else if ( configType.IsArray || configType.IsByRef || configType.IsPointer )
//                     {
//                        ctor = null;
//                     }
//                     else
//                     {
//                        ctor = type.FindInstanceConstructor( new[] { configType.GetTypeInfo() } ) ?? type.FindInstanceConstructor( null );
//                        var configSection = config.GetSection( configurationSectionName ?? SECTION_FOR_TARGET_CONFIGURATION );
//                        configForConstructor = configSection.Get( configType );
//                        configSection.ProcessInstancedConfigurationForNestedConfigurations( configForConstructor, typeLoader );
//                     }



//                     if ( ctor != null )
//                     {
//                        try
//                        {
//                           element = ctor.Invoke( ctor.GetParameters().Length == 0 ? null : new[] { configForConstructor } );

//                           errorMessage = null;
//                        }
//                        catch ( Exception exc )
//                        {
//                           errorMessage = String.Format( "Error when invoking constructor for \"{0}\": {1}", typeName, exc.Message );
//                        }
//                     }
//                     else
//                     {
//                        errorMessage = String.Format( "The configuration type \"{0}\" for \"{1}\" is invalid", configType, type );
//                     }
//                  }
//               }
//            }
//         }
//         catch ( Exception exc )
//         {
//            errorMessage = String.Format( "Malformed configuration ({0})", exc.Message );
//         }
//      }
//      else
//      {
//         errorMessage = "One or more parameters were null";
//      }

//      return element != null;
//   }

//   private static void ProcessInstancedConfigurationForNestedConfigurations(
//      this IConfiguration thisConfig,
//      Object configurationInstance,
//      TypeLoader typeLoader
//      )
//   {
//      if ( thisConfig != null && configurationInstance != null )
//      {
//         var configType = configurationInstance.GetType();
//         foreach ( var configProp in configurationInstance.GetType().GetRuntimeProperties() )
//         {
//            if ( configProp.GetMethod != null
//               && configProp.SetMethod != null
//               && !configProp.GetMethod.IsStatic )
//            {

//               var nestedConfigAttribute = configProp.GetCustomAttribute<NestedDynamicConfigurationAttribute>( true );
//               if ( nestedConfigAttribute == null )
//               {
//                  // This is normal property - process recursively

//                  var nestedSection = thisConfig.GetSection( configProp.Name );

//                  if ( nestedSection != null )
//                  {

//                     Object nestedConfigInstance = null;
//                     try
//                     {
//                        nestedConfigInstance = configProp.GetMethod.Invoke( configurationInstance, null );
//                     }
//                     catch
//                     {
//                        // Ignore..
//                     }

//                     nestedSection.ForSingleOrArray(
//                        configProp.PropertyType,
//                        nestedConfigInstance,
//                        ( curConfig, curType, curInstance ) =>
//                        {
//                           if ( !typeof( IFormattable ).GetTypeInfo().IsAssignableFrom( curType.GetTypeInfo() )
//                             && !Equals( curType, typeof( String ) )
//                           )
//                           {
//                              curConfig.ProcessInstancedConfigurationForNestedConfigurations( curInstance, typeLoader );
//                           }

//                           return null;
//                        }
//                        );
//                  }


//               }
//               else
//               {
//                  // This is nested dynamic configuration property - try to create from configuration
//                  var sectionName = nestedConfigAttribute.ConfigurationSectionName;
//                  if ( !String.IsNullOrEmpty( sectionName ) )
//                  {
//                     String nestedErrorMsg;
//                     Object nestedConfigInstance;
//                     var nestedConfigSectionName = nestedConfigAttribute.LoadedTypeConfigurationSectionName;
//                     nestedConfigInstance = thisConfig
//                        .GetSection( sectionName )
//                        .ForSingleOrArray(
//                           configProp.PropertyType,
//                           null,
//                           ( curConfig, curType, curObject ) =>
//                           {
//                              curConfig.TryLoadDynamicElement(
//                                 curConfig.Get<DynamicElementConfiguration>(),
//                                 typeLoader,
//                                 out nestedConfigInstance,
//                                 out nestedErrorMsg,
//                                 nestedConfigSectionName
//                              );
//                              return nestedConfigInstance;
//                           } );
//                     if ( nestedConfigInstance != null )
//                     {
//                        try
//                        {
//                           configProp.SetMethod.Invoke( configurationInstance, new[] { nestedConfigInstance } );
//                        }
//                        catch
//                        {
//                           // Ignore, for now...
//                        }
//                     }

//                  }
//               }
//            }
//         }
//      }
//   }

//   private static Object ForSingleOrArray(
//      this IConfiguration config,
//      Type type,
//      Object thisObject,
//      Func<IConfiguration, Type, Object, Object> singleAction
//      )
//   {
//      Object retVal;
//      if ( config == null )
//      {
//         retVal = null;
//      }
//      else
//      {
//         if ( type.IsArray )
//         {
//            var list = new List<Object>();
//            var elemType = type.GetElementType();
//            var thisArray = (Array) thisObject;
//            {
//               var i = 0;
//               foreach ( var actualSection in config.GetChildren() )
//               {
//                  retVal = actualSection.ForSingleOrArray( elemType, thisArray?.GetValue( i ), singleAction );
//                  if ( retVal != null )
//                  {
//                     list.Add( retVal );
//                  }
//                  ++i;
//               }
//            }

//            if ( thisObject == null )
//            {
//               var array = Array.CreateInstance( elemType, list.Count );
//               for ( var i = 0; i < list.Count; ++i )
//               {
//                  array.SetValue( list[i], i );
//               }
//               retVal = array;
//            }
//            else
//            {
//               retVal = null;
//            }
//         }
//         else
//         {
//            retVal = singleAction( config, type, thisObject );
//         }
//      }

//      return retVal;
//   }

//   private static ConstructorInfo FindInstanceConstructor( this TypeInfo type, TypeInfo[] paramTypes )
//   {
//      Func<ConstructorInfo, Boolean> checker;
//      if ( paramTypes == null || paramTypes.Length == 0 || paramTypes.Any( pType => pType == null ) )
//      {
//         var len = paramTypes?.Length ?? 0;
//         checker = ctor => !ctor.IsStatic && ctor.GetParameters().Length == len;
//      }
//      else
//      {
//         checker = ctor =>
//         {
//            ParameterInfo[] paramz = null;
//            var retVal = !ctor.IsStatic && ( paramz = ctor.GetParameters() ).Length == paramTypes.Length;
//            if ( retVal )
//            {
//               retVal = paramz
//                  .Where( ( parameter, idx ) => parameter.ParameterType.GetTypeInfo().IsAssignableFrom( paramTypes[idx] ) )
//                  .Count() == paramTypes.Length;
//            }

//            return retVal;
//         };
//      }
//      return type.DeclaredConstructors.FirstOrDefault( checker );
//   }
//}