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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace UtilPackILGenerator
{
   public sealed class XMLDocGenerator
   {
      private readonly String _targetXMLFile;

      public XMLDocGenerator( String targetXMLFile )
      {
         this._targetXMLFile = targetXMLFile;
      }

      public void GenerateXMLDocument()
      {
         var targetLocation = this._targetXMLFile;
         var targetDocument = XDocument.Load( targetLocation );
         var members = targetDocument.Element( "doc" )?.Element( "members" );
         if ( members == null )
         {
            throw new Exception( "Failed to find members element in target XML documentation file." );
         }

         members.Add( this.GenerateDocumentation() );

         using ( var fs = File.Open( targetLocation, FileMode.Create, FileAccess.Write, FileShare.None ) )
         {
            targetDocument.Save( fs );
         }
      }

      private IEnumerable<XElement> GenerateDocumentation()
      {
         const String MEMBER = "member";
         const String NAME = "name";
         const String SUMMARY = "summary";
         const String PARAM = "param";
         const String TYPEPARAM = "typeparam";
         const String RETURNS = "returns";
         const String PARAMREF = "paramref";
         const String EXCEPTION = "exception";
         const String CREF = "cref";
         const String SEE = "see";
         const String REMARKS = "remarks";
         const String TYPEPARAMREF = "typeparamref";

         var NULL = new XElement( "c", "null" );
         var TRUE = new XElement( "c", "true" );
         var FALSE = new XElement( "c", "false" );

         yield return new XElement( "member",
            new XAttribute( NAME, $"T:{ILGenerator.SIZE_OF}" ),
            new XElement( SUMMARY, "Provides methods that describe sizes of various things during runtime." )
            );

         yield return new XElement( MEMBER,
            new XAttribute( NAME, $"M:{ILGenerator.SIZE_OF}.{ILGenerator.SIZE_OF_TYPE}``1" ),
            new XElement( SUMMARY, "Gets the runtime size of the given type, in bytes." ),
            new XElement( TYPEPARAM,
               new XAttribute( NAME, ILGenerator.SIZE_OF_TYPE_TTYPE ),
               "The type to calculate size of."
               )
            );

         yield return new XElement( MEMBER,
            new XAttribute( NAME, $"M:{ ILGenerator.EXTENSIONS }.{ ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS }``1(``0,System.Action{{``0}},System.Boolean)" ),
            new XElement( SUMMARY, "Invokes all event handlers one by one, even if some of them throw exception." ),
            new XElement( TYPEPARAM, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_TDELEGATE ), "The type of the event." ),
            new XElement( PARAM, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_DEL ), "The value of the event field." ),
            new XElement( PARAM, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_INVOKER ), "The lambda to invoke non-", NULL, " event." ),
            new XElement( PARAM, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_THROW_EXCEPTIONS ), "Whether this method should throw exceptions that are thrown by event handlers." ),
            new XElement( RETURNS, TRUE, " if ", new XElement( PARAMREF, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_DEL ) ), " was non-", NULL, "; ", FALSE, " otherwise." ),
            new XElement( EXCEPTION, new XAttribute( CREF, "T:System.AggregateException" ), "If ", new XElement( PARAMREF, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_THROW_EXCEPTIONS ) ), " is ", TRUE, " and any of the event handler throws an exception. The exception(s) will be given to the", new XElement( SEE, new XAttribute( CREF, "T:System.AggregateException" ) ), " constructor." ),
            new XElement( REMARKS, "If ", new XElement( PARAMREF, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_THROW_EXCEPTIONS ) ), " is ", TRUE, " and first exception is thrown by last event handler, then that exception is re-thrown instead of throwing ", new XElement( SEE, new XAttribute( CREF, "T:System.AggregateException" ) ) )
            );

         yield return new XElement( MEMBER,
            new XAttribute( NAME, $"M:{ ILGenerator.EXTENSIONS }.{ ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS }``1(``0,System.Action{{``0}},System.Exception[]@)" ),
            new XElement( SUMMARY, "Invokes all event handlers one by one, even if some of them throw exception." ),
            new XElement( TYPEPARAM, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_TDELEGATE ), "The type of the event." ),
            new XElement( PARAM, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_DEL ), "The value of the event field." ),
            new XElement( PARAM, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_INVOKER ), "The lambda to invoke non-", NULL, " event." ),
            new XElement( PARAM, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_OCCURRED_EXCEPTIONS ), "This will hold all exceptions thrown by event handlers. Will be ", NULL, " if no exceptions were thrown." ),
            new XElement( RETURNS, TRUE, " if ", new XElement( PARAMREF, new XAttribute( NAME, ILGenerator.EXTENSIONS_INVOKE_ALL_HANDLERS_PARAM_DEL ) ), " was non-", NULL, "; ", FALSE, " otherwise." )
            );

         yield return new XElement( MEMBER,
            new XAttribute( NAME, $"T:{ ILGenerator.DEL_MULTIPLEXER }" ),
            new XElement( SUMMARY, "This class implements ", new XElement( SEE, new XAttribute( CREF, "T:UtilPack.Multiplexer`2" ) ), " with ", new XElement( SEE, new XAttribute( CREF, "T:System.Delegate" ) ), " as constraint for values." ),
            new XElement( TYPEPARAM, new XAttribute( NAME, ILGenerator.DEL_MULTIPLEXER_TKEY ), "The key type." ),
            new XElement( TYPEPARAM, new XAttribute( NAME, ILGenerator.DEL_MULTIPLEXER_TVALUE ), "The type of delegate." ),
            new XElement( REMARKS, "This class is very useful when one multiplexes a single event, e.g. when having one ", new XElement( "c", "PropertyChanged" ), "event based on property name, but allowing to register to events fired on specific property change. In that case, the ", new XElement( TYPEPARAMREF, new XAttribute( NAME, ILGenerator.DEL_MULTIPLEXER_TKEY ) ), " would be ", new XElement( SEE, new XAttribute( CREF, "T:System.String" ) ), " and ", new XElement( TYPEPARAMREF, new XAttribute( NAME, ILGenerator.DEL_MULTIPLEXER_TVALUE ) ), " would be the type of event." )
            );

         yield return new XElement( MEMBER,
            new XAttribute( NAME, $"M:{ ILGenerator.DEL_MULTIPLEXER }.#ctor(System.Collections.Generic.IEqualityComparer{{`0}})" ),
            new XElement( SUMMARY, "Creates a new instance of ", new XElement( SEE, new XAttribute( CREF, $"T:{ ILGenerator.DEL_MULTIPLEXER }" ) ), "with optional custom equality comparer for keys." ),
            new XElement( PARAM, new XAttribute( NAME, ILGenerator.DEL_MULTIPLEXER_CTOR_PARAM_EQ_COMPARER ), "The optional custom equality comparer for keys." )
            );

         yield return new XElement( MEMBER,
            new XAttribute( NAME, $"M:{ ILGenerator.DEL_MULTIPLEXER }.{ ILGenerator.DEL_MULTIPLEXER_COMBINE }(`1,`1)" ),
            new XElement( "inheritdoc" )
            );

         yield return new XElement( MEMBER,
            new XAttribute( NAME, $"M:{ ILGenerator.DEL_MULTIPLEXER }.{ ILGenerator.DEL_MULTIPLEXER_REMOVE }(`1,`1)" ),
            new XElement( "inheritdoc" )
            );
      }
   }
}
