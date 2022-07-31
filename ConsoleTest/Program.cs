﻿using Common.Classes;
using Common.Enums;
using Context;
using Converter;
using System;
using System.Threading.Tasks;

namespace ConsoleTest
{
    class Program
	{
		static async Task Main(string[] args)
		{
			var connectinoString = @"Data Source=(local);Integrated Security=SSPI;Initial Catalog=DatabaseName";
			var tableName = "WorkOrder";
			var context = new SqlContext(connectinoString);

			context.SetTableName(tableName);

			var classOptions = new ConvertOptions
			{
				TableName = tableName,
				Modifier = Modifier.Public,
				ClassType = ClassType.Entity,
				ShowForeignKey = true,
				ShowForeignProperty = true,
				ShowMaxLength = true,
				ShowPrimaryKey = true,
				ShowTableName = true,
				EnumerateSimilarForeignKeyProperties = false
			};
			var highlightColors = new CSharpHighlightColor() { KeywordColor = "1d44a7" };

			//var convert = new TableConverter(tableName);
			var converter = new CSharpConverter(classOptions);
			//var convert = new TableConverter(classOptions, highlightColors);

			var tableSchemaResult = await context.GetTableData<TableSchemaResult>();

			converter.TableSchama = tableSchemaResult;

			var @class = converter.GetClass();
			//var classHtmlDocument = convert.GetHighlightedCSharpClass();

			Console.WriteLine(@class);
			//Console.WriteLine(classHtmlDocument);
			Console.ReadLine();
		}
	}
}
