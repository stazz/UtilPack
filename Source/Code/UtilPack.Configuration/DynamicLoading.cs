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
   /// <summary>
   /// This delegate provides signature for callback used by <see cref="DynamicConfigurableTypeLoader"/> to load a type from <see cref="IConfiguration"/> containing whatever information necessary to load a type dynamically.
   /// </summary>
   /// <param name="config">The <see cref="IConfiguration"/> containing information to load type dynamically.</param>
   /// <param name="targetType">The type which must be parent of the type to be loaded.</param>
   /// <returns>The potentially asynchronous operation which results in <see cref="TypeInfo"/> of dynamically loaded type.</returns>
   public delegate ValueTask<TypeInfo> TypeLoaderDelegate( IConfiguration config, TypeInfo targetType );

   /// <summary>
   /// This delegate provides signature for callback used by <see cref="DynamicConfigurableTypeLoader"/> to extract <see cref="IConfigurationSection"/> containing information to pass to constructor of the type that was dynamically loaded by <see cref="DynamicConfigurableTypeLoader"/>.
   /// </summary>
   /// <param name="config">The <see cref="IConfiguration"/> containing information to load type dynamically.</param>
   /// <param name="loadedType">The type that was loaded from information in <paramref name="config"/>.</param>
   /// <returns>The <see cref="IConfigurationSection"/> containing information for type constructor.</returns>
   /// <seealso cref="ConfigurationTypeAttribute"/>
   public delegate IConfigurationSection ConstructorConfigurationLoaderDelegate( IConfiguration config, TypeInfo loadedType );

   /// <summary>
   /// This class uses <see cref="TypeLoaderDelegate"/> and <see cref="ConstructorConfigurationLoaderDelegate"/> callbacks to walk through the <see cref="IConfiguration"/> and potentially recursively dynamically load and instantiate objects.
   /// </summary>
   public class DynamicConfigurableTypeLoader
   {
      private readonly TypeLoaderDelegate _typeLoader;
      private readonly ConstructorConfigurationLoaderDelegate _constructorArgumentsLoader;

      /// <summary>
      /// Creates new instance of <see cref="DynamicConfigurableTypeLoader"/> with given callbacks.
      /// </summary>
      /// <param name="typeLoader">The type loader callback.</param>
      /// <param name="constructorConfigLoader">The optional constructor configuration section getter callback. If not supplised, the default behaviour is to get section called "Configuration" from passed <see cref="IConfiguration"/>.</param>
      public DynamicConfigurableTypeLoader(
         TypeLoaderDelegate typeLoader,
         ConstructorConfigurationLoaderDelegate constructorConfigLoader = null
         )
      {
         this._typeLoader = typeLoader ?? throw new ArgumentNullException( nameof( typeLoader ) );
         this._constructorArgumentsLoader = constructorConfigLoader ?? ( ( cfg, type ) => cfg.GetSection( "Configuration" ) );
      }

      /// <summary>
      /// Potentially asynchronously instantiates an object from type information provided in given <see cref="IConfiguration"/>.
      /// </summary>
      /// <param name="config">The configuration containing type information.</param>
      /// <param name="targetType">The type which should be parent type of the object to be instantiated.</param>
      /// <returns>The potentially asynchronous operation which results in instantiated object, or <c>null</c>.</returns>
      /// <remarks>
      /// This method takes <see cref="ConfigurationTypeAttribute"/> and <see cref="NestedDynamicConfigurationAttribute"/> attributes into account when instantiating and traversing object.
      /// This method is only asynchronous if <see cref="TypeLoaderDelegate"/> callback provided to this <see cref="DynamicConfigurableTypeLoader"/> is asynchronous.
      /// </remarks>
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
                     if ( nestedSection != null && nestedSection.GetChildren().Any() ) // String.IsNullOrEmpty( nestedSection?.Value ) )
                     {
                        // This is complex value (not string/number), iterate...
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

   /// <summary>
   /// The types that are dynamically loaded by <see cref="DynamicConfigurableTypeLoader.InstantiateWithConfiguration"/> method can either have parameterless constructor, or use this attribute to specify the arguments for constructor.
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
   /// Sometimes the configuration type specified by <see cref="ConfigurationTypeAttribute"/> contains properties which should be loaded using <see cref="DynamicConfigurableTypeLoader"/>.
   /// In such case, this attribute should be used on those properties, indicating the name of the configuration section which will be passed to <see cref="DynamicConfigurableTypeLoader.InstantiateWithConfiguration"/>.
   /// </summary>
   [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
   public class NestedDynamicConfigurationAttribute : Attribute
   {
      /// <summary>
      /// Creates a new instance of <see cref="NestedDynamicConfigurationAttribute"/> with given section name to pass to <see cref="DynamicConfigurableTypeLoader.InstantiateWithConfiguration"/>.
      /// </summary>
      /// <param name="configurationSectionName">The name of the configuration section name pass to <see cref="DynamicConfigurableTypeLoader.InstantiateWithConfiguration"/>.</param>
      public NestedDynamicConfigurationAttribute( String configurationSectionName )
      {
         this.ConfigurationSectionName = configurationSectionName;
      }

      /// <summary>
      /// Gets the name of configuration section for dynamically loaded type.
      /// </summary>
      /// <value>The name of configuration section for dynamically loaded type.</value>
      public String ConfigurationSectionName { get; }

   }
}