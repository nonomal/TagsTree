﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagsTreeWinUI3.ViewModels;

namespace TagsTreeWinUI3.Models
{
	public class RelationsDataTable : DataTable
	{
		private readonly Dictionary<int, DataRow> _rowsDict = new();
		public bool this[FileModel fileModel, TagViewModel tag]
		{
			get => (bool)_rowsDict[fileModel.Id][tag.Id];
			set => _rowsDict[fileModel.Id][tag.Id.ToString()] = value;
		}
		public DataRow RowAt(FileModel rowKey) => _rowsDict[rowKey.Id];

		public IEnumerable<string> GetTags(FileModel file)
		{
			for (var i = 1; i < Columns.Count; i++)
				if ((bool)_rowsDict[file.Id][Columns[i]])
					yield return App.Tags.TagsDictionary[Convert.ToInt32(Columns[i].ColumnName)].Name;
		}
		private readonly struct Part
		{
			public readonly int Num;
			public readonly FileViewModel File;

			public Part(int num, FileViewModel file)
			{
				Num = num;
				File = file;
			}
		}

		public static ObservableCollection<FileViewModel> FuzzySearchName(string input, IEnumerable<FileViewModel> range)
		{   //大小写不敏感
			var precise = new List<FileViewModel>(); //完整包含搜索内容
			var fuzzy = new List<FileViewModel>(); //有序并全部包含所有字符
			var part = new List<Part>(); //包含任意一个字符
			var fuzzyRegex = new Regex(Regex.Replace(input, "(.)", ".+$1", RegexOptions.IgnoreCase));
			var partRegex = new Regex($"[{input}]", RegexOptions.IgnoreCase);
			foreach (var fileViewModel in range)
			{
				if (fileViewModel.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
					precise.Add(fileViewModel);
				else if (fuzzyRegex.IsMatch(fileViewModel.Name))
					fuzzy.Add(fileViewModel);
				else
				{
					var matches = partRegex.Matches(fileViewModel.Name);
					if (matches.Count != 0)
						part.Add(new Part(matches.Count, fileViewModel));
				}
			}
			precise.AddRange(fuzzy);
			part.Sort((x, y) => x.Num.CompareTo(y.Num));
			precise.AddRange(part.Select(item => item.File));
			var temp = new ObservableCollection<FileViewModel>();
			foreach (var fileModel in precise)
				temp.Add(fileModel);
			return temp;
		}
		public IEnumerable<FileModel> GetFileModels(List<PathTagModel>? tags = null)
		{
			if (tags is null || tags.Count == 0)
				return App.IdFile.Values.ToList();
			var validTags = new Dictionary<PathTagModel, bool>();
			foreach (var tag in tags.Where(tag => !validTags.ContainsKey(tag)))
				validTags[tag] = true;
			var enumerator = tags.GetEnumerator();
			_ = enumerator.MoveNext();
			return GetFileModels(enumerator).Select(row => App.IdFile[(int)row[0]]).ToList();
		}
		private List<DataRow> GetFileModels(IEnumerator<PathTagModel> tags)
		{
			var tag = tags.Current;
			var lastRange = tags.MoveNext() ? GetFileModels(tags) : _rowsDict.Values.ToList();
			if (App.Tags.TagsDictionary.GetValueOrDefault(tag.Name) is { } tagModel)
			{
				var dataRows = lastRange.Where(row => (bool)row[tagModel.Id.ToString()]).ToList();
				dataRows.AddRange(App.Tags.TagsDictionaryValues.Where(childTag => tagModel.HasChildTag(childTag))
					.SelectMany(_ => lastRange, (childTag, row) => new { childTag, row })
					.Where(t => (bool)t.row[t.childTag.Id.ToString()])
					.Select(t => t.row));
				return dataRows;
			}
			if (App.AppConfigurations.PathTags) //唯一需要判断是否能使用路径作为标签的地方
				return lastRange
					.SelectMany(dataRow => App.IdFile[(int)dataRow[0]].PartialPath[4..].Split('\\', StringSplitOptions.RemoveEmptyEntries), (dataRow, pathTag) => new { dataRow, pathTag })
					.Where(t => t.pathTag == tag.Name)
					.Select(t => t.dataRow).ToList();
			return new List<DataRow>();
		}
		public void NewRow(FileModel fileModel)
		{
			var newRow = NewRow();
			newRow[0] = fileModel.Id;
			_rowsDict[(int)newRow[0]] = newRow;
			Rows.Add(newRow);
		}
		public void NewColumn(int id)
		{
			var column = new DataColumn //不拎出来会因为"False"无法转化为bool类型而抛异常
			{
				AllowDBNull = false,
				AutoIncrement = false,
				ColumnName = id.ToString(),
				Caption = id.ToString(),
				DataType = typeof(bool),
				ReadOnly = false,
				Unique = false,
				DefaultValue = false
			};
			Columns.Add(column);
		}
		public void DeleteColumn(int id)
		{
			foreach (DataColumn column in Columns)
				if (column.ColumnName == id.ToString())
				{
					Columns.Remove(column);
					return;
				}
		}

		public RelationsDataTable() => TableName = "Relations";


		public void RefreshRowsDict()
		{
			_rowsDict.Clear();
			foreach (DataRow row in Rows)
				_rowsDict[(int)row[0]] = row; //row[0]即为row["FileId"]
		}

		public void Load(string path)
		{
			try
			{
				ReadXml(path);
			}
			catch (Exception)
			{
				Clear();
				Columns.Clear();
				Columns.Add(new DataColumn
				{
					AllowDBNull = false,
					AutoIncrement = false,
					ColumnName = "FileId",
					Caption = "FileId",
					DataType = typeof(int),
					ReadOnly = false,
					Unique = true
				});
				foreach (var tag in App.Tags.TagsDictionaryValues)
					NewColumn(tag.Id);
				foreach (var fileModel in App.IdFile.Values)
					NewRow(fileModel);
			}
			RefreshRowsDict();
			Save(path);
		}

		public async void Save(string path) => await Task.Run(() => WriteXml(path, XmlWriteMode.WriteSchema));
	}
}