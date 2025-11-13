namespace Cirreum.FileSystem;

public class CsvOptions {

	public bool HasHeaderRecord { get; set; }

	public string Delimiter { get; set; } = "";

	public bool IgnoreMissingFields { get; set; }

}