using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Parquet.Schema;
using Parquet.Serialization;
using Xunit;

namespace Parquet.Test.Serialisation {
    public class SchemaReflectorTest : TestBase {


        /// <summary>
        /// Essentially all the test cases are this class' fields
        /// </summary>
        class PocoClass {
            public int Id { get; set; }

            [ParquetColumn("AltId")] public int AnnotatedId { get; set; }
            public float? NullableFloat { get; set; }

            public int[]? IntArray { get; set; }
        }

        [Fact]
        public void I_can_infer_different_types() {
            ParquetSchema schema = typeof(PocoClass).GetParquetSchema(true);

            Assert.NotNull(schema);
            Assert.Equal(4, schema.Fields.Count);

            // verify

            DataField id = (DataField)schema[0];
            Assert.Equal("Id", id.Name);
            Assert.Equal(typeof(int), id.ClrType);
            Assert.False(id.IsNullable);
            Assert.False(id.IsArray);

            DataField altId = (DataField)schema[1];
            Assert.Equal("AltId", altId.Name);
            Assert.Equal(typeof(int), id.ClrType);
            Assert.False(id.IsNullable);
            Assert.False(id.IsArray);

            DataField nullableFloat = (DataField)schema[2];
            Assert.Equal("NullableFloat", nullableFloat.Name);
            Assert.Equal(typeof(float), nullableFloat.ClrType);
            Assert.True(nullableFloat.IsNullable);
            Assert.False(nullableFloat.IsArray);

            DataField intArray = (DataField)schema[3];
            Assert.Equal("IntArray", intArray.Name);
            Assert.Equal(typeof(int), intArray.ClrType);
            Assert.False(intArray.IsNullable);
            Assert.True(intArray.IsArray);
        }


        class PocoSubClass : PocoClass {
            public int ExtraProperty { get; set; }
        }

        [Fact]
        public void I_can_recognize_inherited_properties() {
            ParquetSchema schema = typeof(PocoSubClass).GetParquetSchema(true);
            Assert.Equal(5, schema.Fields.Count);
            DataField extraProperty = (DataField)schema[0];
            Assert.Equal("ExtraProperty", extraProperty.Name);
            Assert.Equal(typeof(int), extraProperty.ClrType);
            Assert.False(extraProperty.IsNullable);
            Assert.False(extraProperty.IsArray);

            DataField id = (DataField)schema[1];
            Assert.Equal("Id", id.Name);
            Assert.Equal(typeof(int), id.ClrType);
            Assert.False(id.IsNullable);
            Assert.False(id.IsArray);

            DataField altId = (DataField)schema[2];
            Assert.Equal("AltId", altId.Name);
            Assert.Equal(typeof(int), id.ClrType);
            Assert.False(id.IsNullable);
            Assert.False(id.IsArray);

            DataField nullableFloat = (DataField)schema[3];
            Assert.Equal("NullableFloat", nullableFloat.Name);
            Assert.Equal(typeof(float), nullableFloat.ClrType);
            Assert.True(nullableFloat.IsNullable);
            Assert.False(nullableFloat.IsArray);

            DataField intArray = (DataField)schema[4];
            Assert.Equal("IntArray", intArray.Name);
            Assert.Equal(typeof(int), intArray.ClrType);
            Assert.False(intArray.IsNullable);
            Assert.True(intArray.IsArray);

            var extraProp = (DataField)schema[0];
            Assert.Equal("ExtraProperty", extraProp.Name);
            Assert.Equal(typeof(int), extraProp.ClrType);
            Assert.False(extraProp.IsNullable);
            Assert.False(extraProp.IsArray);
        }

        class AliasedPoco {

            [ParquetColumn(Name = "ID1")]
            public int _id1 { get; set; }

            [JsonPropertyName("ID2")]
            public int _id2 { get; set; }
        }

        [Fact]
        public void AliasedProperties() {
            ParquetSchema schema = typeof(AliasedPoco).GetParquetSchema(true);

            Assert.Equal(new ParquetSchema(
                new DataField<int>("ID1"),
                new DataField<int>("ID2")
                ), schema);
        }

#pragma warning disable CS0618 // Type or member is obsolete

        class IgnoredPoco {

            public int NotIgnored { get; set; }

            [ParquetIgnore]
            public int Ignored1 { get; set; }

            [JsonIgnore]
            public int Ignored2 { get; set; }
        }
#pragma warning restore CS0618 // Type or member is obsolete


        [Fact]
        public void IgnoredProperties() {
            ParquetSchema schema = typeof(IgnoredPoco).GetParquetSchema(true);

            Assert.Equal(new ParquetSchema(
                new DataField<int>("NotIgnored")), schema);
        }

        class SimpleMapPoco {
            public int? Id { get; set; }

            public Dictionary<string, int> Tags { get; set; } = new Dictionary<string, int>();
        }

        [Fact]
        public void SimpleMap() {
            ParquetSchema schema = typeof(SimpleMapPoco).GetParquetSchema(true);

            Assert.Equal(new ParquetSchema(
                new DataField<int?>("Id"),
                new MapField("Tags", 
                    new DataField<string>("Key"),
                    new DataField<int>("Value"))), schema);
        }

        class StructMemberPoco {
            public string? FirstName { get; set; }

            public string? LastName { get; set; }
        }

        class StructMasterPoco {
            public int Id { get; set; }

            public StructMemberPoco? Name { get; set; }
        }

        [Fact]
        public void SimpleStruct() {
            ParquetSchema schema = typeof(StructMasterPoco).GetParquetSchema(true);

            Assert.Equal(new ParquetSchema(
                new DataField<int>("Id"),
                new StructField("Name",
                    new DataField<string>("FirstName"),
                    new DataField<string>("LastName")
                )), schema);
        }

        class ListOfStructsPoco {
            public int Id { get; set; }

            public List<StructMemberPoco>? Members { get; set; }
        }

        [Fact]
        public void ListOfStructs() {
            ParquetSchema actualSchema = typeof(ListOfStructsPoco).GetParquetSchema(true);

            var expectedSchema = new ParquetSchema(
                new DataField<int>("Id"),
                new ListField("Members",
                    new StructField("element",
                        new DataField<string>("FirstName"),
                        new DataField<string>("LastName"))));

            Assert.True(
                expectedSchema.Equals(actualSchema),
                expectedSchema.GetNotEqualsMessage(actualSchema, "expected", "actual"));
        }
    }
}