﻿using System;
using System.Windows;
using Context;
using Common.Enums;
using System.Linq;
using Common.Classes;
using Converter;

namespace TableToCSharp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public SqlContext context;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void OnLoad(object sender, RoutedEventArgs e)
		{
			this.cboObjectTypes.ItemsSource = Enum.GetValues(typeof(ClassType)).Cast<ClassType>();
			this.cboModifiers.ItemsSource = Enum.GetValues(typeof(Modifier)).Cast<Modifier>();

			this.cboObjectTypes.SelectedIndex = 0;
			this.cboModifiers.SelectedIndex = 0;
		}

		private async void Button_Connect_Click(object sender, RoutedEventArgs e)
		{
			this.btnConnect.IsEnabled = false;
			this.context = new SqlContext(this.txtConnection.Text);
			this.cboTables.ItemsSource = await this.context.GetTables();
			this.btnConnect.IsEnabled = true;
		}

		private async void Button_GenerateClass_Click(object sender, RoutedEventArgs e)
		{
			this.btnGenerateClass.IsEnabled = false;

			var classOptions = new ConvertOptions
			{
				TableName = this.cboTables.Text,
				Modifier = (Modifier)Enum.Parse(typeof(Modifier), this.cboModifiers.SelectedValue.ToString()),
				ClassType = (ClassType)Enum.Parse(typeof(ClassType), this.cboObjectTypes.SelectedValue.ToString()),
				ShowForeignKey = this.chkShowForeignKey.IsChecked ?? false,
				ShowForeignProperty = this.chkShowForeignProperty.IsChecked ?? false,
				ShowMaxLength = this.chkShowMaxLength.IsChecked ?? false,
				ShowPrimaryKey = this.chkShowPrimaryKey.IsChecked ?? false,
				ShowTableName = this.chkShowTableName.IsChecked ?? false,
				EnumerateSimilarForeignKeyProperties = this.chkEnumerateSimilarFKProperties.IsChecked ?? false
			};

			context.SetTableName(this.cboTables.Text);

			var convert = new TableConverter(classOptions);
			var tableSchemaResult = await context.GetTableData<TableSchemaResult>();

			convert.TableSchama = tableSchemaResult;

			var @class = convert.GetHighlightedCSharpClass();

			this.htmlDisplay.NavigateToString(@class);
			this.btnGenerateClass.IsEnabled = true;
		}
	}
}