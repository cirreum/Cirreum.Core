namespace Cirreum.FileSystem;

using CsvHelper.Configuration;
using System.Collections.Generic;

public interface ICsvFileBuilder {

	byte[] BuildFile<TRecord>(IEnumerable<TRecord> records, string delimiter = ",");

	byte[] BuildFile<TRecord>(IEnumerable<TRecord> records, CsvConfiguration configuration);

	byte[] BuildFile<TRecord, TClassMap>(IEnumerable<TRecord> records) where TClassMap : ClassMap<TRecord>;

	byte[] BuildFile<TRecord, TClassMap>(IEnumerable<TRecord> records, CsvConfiguration configuration) where TClassMap : ClassMap<TRecord>;

}