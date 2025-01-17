﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Classes;
using DBContext;
using Common.Enums;
using System.Threading.Tasks;
using Pluralize.NET.Core;

namespace Converter
{
	public class CSharpConverter : BaseConverter, IConverter, IDisposable
    {
		private ConvertOptions _classOptions;

		public CSharpConverter(string tableName) : base (Language.CSharp)
		{
			this._classOptions = new ConvertOptions() { TableName = tableName };
		}

		public CSharpConverter(ConvertOptions classOptions) : base(Language.CSharp)
		{
			this._classOptions = classOptions ?? new ConvertOptions();
		}

		public override async Task<string> GetClass(IContext context)
		{
			var colCharToRemove = new List<char>() { '#', ' ', ':', '?', '!', '@', '$', '%', '&', '*', '(', ')', '[', ']', '{', '}' };
			var nullableSqlTypes = new List<string> { "bigint, bit, date, datetime, datetime2, datetimeoffset, decimal, float, int, money, numeric, real, smalldatetime, smallint, smallmoney, time, tinyint, uniqueidentifier" };
			var stringBuilder = new StringBuilder();

			context.CommandString = GetTableDefinitoinCommandString();
			var tableSchama = await context.GetTableDataAsync<TableSchemaResult>();

			if (this._classOptions.ClassType == ClassType.Entity)
			{
				stringBuilder.AppendLine("using System.ComponentModel.DataAnnotations;");
				stringBuilder.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");				

				stringBuilder.AppendLine("");
				stringBuilder.AppendLine("-----");
				stringBuilder.AppendLine("");

				if (this._classOptions.ShowTableName)
                {
					stringBuilder.AppendLine($"[Table(\"{this._classOptions.TableName}\")]");
                }
			}

			stringBuilder.AppendLine($"{this._classOptions.Modifier.ToString().ToLower()} class {new Pluralizer().Singularize(this._classOptions.TableName)}");
			stringBuilder.AppendLine("{");

			tableSchama.ToList()
			.ForEach(td =>
			{
				var isInvalidColName = false;

				if (this._classOptions.ClassType == ClassType.Entity)
				{
					if (td.IsPrimaryKey && this._classOptions.ShowPrimaryKey)
					{
						stringBuilder.AppendLine("\t[Key]");
					}

					if (td.ConvertedType.Equals("string", StringComparison.OrdinalIgnoreCase) && this._classOptions.ShowMaxLength)
					{
						if (td.MaxLength > 0)
						{
							_ = stringBuilder.AppendLine($"\t[MaxLength({td.MaxLength})]");
						}
					}
				}

				if (td.ColumnName.Any(cn => colCharToRemove.Contains(cn)))
				{
					isInvalidColName = true;
					if (this._classOptions.ClassType == ClassType.Entity)
					{
						stringBuilder.AppendLine($"\t[Column(\"{td.ColumnName}\")]");
					}

					if (this._classOptions.ClassType == ClassType.Contract)
					{
						stringBuilder.AppendLine($"\t[JsonProperty(PropertyName =\"{td.ColumnName}\")]");
					}
				}

				stringBuilder.AppendLine($"\tpublic {td.ConvertedType}{(td.IsNullable && nullableSqlTypes.Where(st => st == td.TypeName) != null && !"string,byte[]".Contains(td.ConvertedType) ? "?" : "")} {(isInvalidColName ? string.Join("_", td.ColumnName.Split(colCharToRemove.ToArray())) : td.ColumnName)} {{ get; set; }}");

				if (this._classOptions.ShowForeignProperty)
				{
					if (td.HasReference)
					{
						var referenceClass = new Pluralizer().Singularize(td.ReferencedTableName);

						if (this._classOptions.ClassType == ClassType.Entity && this._classOptions.ShowForeignKey)
						{
							stringBuilder.AppendLine($"\t[ForeignKey(\"{td.ColumnName}\")]");
						}
						stringBuilder.AppendLine($"\tpublic virtual {referenceClass} {(this._classOptions.EnumerateSimilarForeignKeyProperties ? $"{referenceClass}_{td.ReferencedTableNumber}" : $"{td.ColumnName}_{referenceClass}")} {{ get; set; }}");
					}
				}

				isInvalidColName = false;
			});

			stringBuilder.AppendLine("}");

			return stringBuilder.ToString();
		}

		public override string GetTableDefinitoinCommandString()
		{
			var command = $@"DECLARE @tableName varchar(50) = '{this._classOptions.TableName}';

							SELECT c.name AS ColumnName
								 , stp.name AS TypeName
								 , CASE
									   WHEN stp.name IN
									   (   'nvarchar'
										 , 'nchar'
									   ) THEN c.max_length / 2
									   ELSE c.max_length
								   END AS MaxLength
								 , i.is_primary_key AS IsPrimaryKey
								 , Cast(CASE
											WHEN fkc.parent_column_id IS NULL THEN 0
											ELSE 1
										END AS bit) AS HasReference
								 , c.is_nullable AS IsNullable
								 , t2.name AS ReferencedTableName
								 , Cast(CASE
											WHEN t2.name IS NOT NULL THEN Row_Number() OVER (PARTITION BY t2.name ORDER BY c.column_id)
											ELSE NULL
										END AS int) AS ReferencedTableNumber
								 , CASE stp.name
									   WHEN 'bigint' THEN 'long'
									   WHEN 'binary' THEN 'byte[]'
									   WHEN 'bit' THEN 'bool'
									   WHEN 'char' THEN 'string'
									   WHEN 'date' THEN 'DateTime'
									   WHEN 'datetime' THEN 'DateTime'
									   WHEN 'datetime2' THEN 'DateTime'
									   WHEN 'datetimeoffset' THEN 'DateTimeOffset'
									   WHEN 'decimal' THEN 'decimal'
									   WHEN 'float' THEN 'double'
									   WHEN 'image' THEN 'byte[]'
									   WHEN 'int' THEN 'int'
									   WHEN 'money' THEN 'decimal'
									   WHEN 'nchar' THEN 'string'
									   WHEN 'ntext' THEN 'string'
									   WHEN 'numeric' THEN 'decimal'
									   WHEN 'nvarchar' THEN 'string'
									   WHEN 'real' THEN 'float'
									   WHEN 'smalldatetime' THEN 'DateTime'
									   WHEN 'smallint' THEN 'short'
									   WHEN 'smallmoney' THEN 'decimal'
									   WHEN 'text' THEN 'string'
									   WHEN 'time' THEN 'TimeSpan'
									   WHEN 'timestamp' THEN 'long'
									   WHEN 'tinyint' THEN 'byte'
									   WHEN 'uniqueidentifier' THEN 'Guid'
									   WHEN 'varbinary' THEN 'byte[]'
									   WHEN 'varchar' THEN 'string'
									   ELSE 'UNKNOWN_' + stp.name
								   END AS ConvertedType
							FROM sys.columns c
								 INNER JOIN sys.tables t ON t.object_id = c.object_id
								 INNER JOIN sys.types stp ON stp.system_type_id = c.system_type_id
								 LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_column_id = c.column_id
																		   AND fkc.parent_object_id = t.object_id
								 LEFT JOIN sys.tables t2 ON t2.object_id = fkc.referenced_object_id
								 LEFT JOIN sys.indexes i ON i.object_id = c.object_id
															 AND i.index_id = c.column_id
															 AND i.is_primary_key = 1
							WHERE t.name = @tableName
								AND stp.system_type_id = stp.user_type_id
							ORDER BY c.column_id;";

			return command;

		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				this._classOptions = null;
			}
		}
	}
}
