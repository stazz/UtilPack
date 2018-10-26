using System;

namespace UtilPack.NuGet.NuGetExec.Entrypoint
{
   [AttributeUsage( AttributeTargets.Assembly | AttributeTargets.Method )]
   public sealed class ConfiguredEntryPointAttribute : Attribute
   {
      public ConfiguredEntryPointAttribute()
         : this( null, null )
      {

      }

      public ConfiguredEntryPointAttribute(
         Type entryPointType
         ) : this( entryPointType, null )
      {

      }

      public ConfiguredEntryPointAttribute(
         String entryPointMethodName
         ) : this( null, entryPointMethodName )
      {

      }

      public ConfiguredEntryPointAttribute(
         Type entryPointType,
         String entryPointMethodName
         )
      {
         this.EntryPointType = entryPointType;
         this.EntryPointMethodName = entryPointMethodName;
      }

      public Type EntryPointType { get; }

      public String EntryPointMethodName { get; }
   }
}
