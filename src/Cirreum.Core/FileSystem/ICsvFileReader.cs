namespace Cirreum.FileSystem;

using CsvHelper.Configuration;
using System.Collections.Generic;

public interface ICsvFileReader {

	IEnumerable<T> ReadCsvFile<T>(string fileName, CsvOptions options);

	IEnumerable<TRecord> ReadCsvFile<TRecord, TClassMap>(string fileName, CsvOptions options) where TClassMap : ClassMap<TRecord>;

}