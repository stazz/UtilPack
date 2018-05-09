/*
 * Copyright 2014 Stanislav Muhametsin. All rights Reserved.
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
using System.Text;

namespace UtilPack
{
   /// <summary>
   /// This is helper class to invoke code finally block by using <c>using</c> word in C#.
   /// </summary>
   public struct UsingHelper : IDisposable
   {
      private readonly Action _action;

      /// <summary>
      /// Creates a new instance of <see cref="UsingHelper"/> with specified action to invoke during disposing.
      /// </summary>
      /// <param name="action">The action to invoke during disposing. May be <c>null</c>.</param>
      /// <remarks>The <paramref name="action"/> is invoked only if <see cref="IDisposable.Dispose"/> method is called. Otherwise, e.g. when garbage collecting this object, the delegate will not be invoked.</remarks>
      public UsingHelper( Action action )
      {
         this._action = action;
      }

      /// <inheritdoc />
      public void Dispose()
      {
         this._action?.Invoke();
      }
   }
}
