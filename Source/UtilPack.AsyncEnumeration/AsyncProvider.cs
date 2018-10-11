using System;
using System.Collections.Generic;
using System.Text;
using UtilPack.AsyncEnumeration;
using UtilPack.AsyncEnumeration.Enumerables;

namespace UtilPack.AsyncEnumeration
{
   public partial interface IAsyncProvider
   {

   }

   public sealed partial class DefaultAsyncProvider : IAsyncProvider
   {
      public static IAsyncProvider Instance { get; } = new DefaultAsyncProvider();

      private DefaultAsyncProvider()
      {

      }

   }
}