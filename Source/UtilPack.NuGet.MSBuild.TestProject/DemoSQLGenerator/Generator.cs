using SQLGenerator;
using SQLGenerator.Usage;
using System;
using System.Collections.Generic;

namespace MyGenerator
{
   public class Generator : AbstractSQLGeneratorUser<SQLVendor>
   {
      private const String TEST_SCHEMA = "test_schema";
      private const String TEST_TABLE = "test_table";
      private const String TEST_TABLE_ID_COLUMN = "global_id";
      private const String TEST_TABLE_VERSION_COLUMN = "local_version";
      public override IEnumerable<String> GenerateSQL( SQLVendor vendor )
      {
         var df = vendor.DefinitionFactory;
         var cf = vendor.CommonFactory;

         yield return df.NewSchemaDefinition( TEST_SCHEMA ).ToString();

         // database_info
         var dbInfoDirect = cf.TableNameDirect( TEST_SCHEMA, TEST_TABLE );
         yield return df.NewTableDefinition(
            dbInfoDirect,
            df.NewTableElementList(
               df.NewColumnDefinition(
                  TEST_TABLE_ID_COLUMN,
                  df.UserDefined( "UUID" ),
                  mayBeNull: false
               ),
               df.NewColumnDefinition(
                  TEST_TABLE_VERSION_COLUMN,
                  df.Integer(),
                  mayBeNull: false
               ),
               df.NewTableConstraintDefinition(
                  df.NewUniqueConstraint( UniqueSpecification.PrimaryKey, TEST_TABLE_ID_COLUMN ),
                  ConstraintCharacteristics.NotDeferrable
                  )
               )
            ).ToString();
      }
   }
}
