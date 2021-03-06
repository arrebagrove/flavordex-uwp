﻿/*
  The MIT License (MIT)
  Copyright © 2016 Steve Guidetti

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the “Software”), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
  THE SOFTWARE.
*/
using Flavordex.Models;
using Flavordex.Models.Data;
using Flavordex.UI.Controls;
using Flavordex.Utilities.CSV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Data.Json;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace Flavordex.Utilities
{
    /// <summary>
    /// Helpers for importing and exporting CSV files.
    /// </summary>
    public class CsvUtilities
    {
        /// <summary>
        /// The names of the fields common to all legacy formats.
        /// </summary>
        private static string[] LEGACY_FIELDS_COMMON = new string[]
        {
            "title",
            "maker",
            "origin",
            "location",
            "date",
            "price",
            "rating",
            "notes",
            "flavors",
            "photos"
        };

        /// <summary>
        /// The names of the fields of the Beer Journal legacy format.
        /// </summary>
        private static string[] LEGACY_FIELDS_BEER = new string[]
        {
            "style",
            "serving",
            "stats_ibu",
            "stats_abv",
            "stats_og",
            "stats_fg"
        };

        /// <summary>
        /// The names of the fields of the Coffee Journal legacy format.
        /// </summary>
        private static string[] LEGACY_FIELDS_COFFEE = new string[]
        {
            "roaster",
            "roast_date",
            "grind",
            "brew_method",
            "stats_dose",
            "stats_mass",
            "stats_temp",
            "stats_extime",
            "stats_tds",
            "stats_yield"
        };

        /// <summary>
        /// The names of the fields of the Whiskey Journal legacy format.
        /// </summary>
        private static string[] LEGACY_FIELDS_WHISKEY = new string[]
        {
            "style",
            "stats_age",
            "stats_abv"
        };

        /// <summary>
        /// The names of the fields of the Wine Journal legacy format.
        /// </summary>
        private static string[] LEGACY_FIELDS_WINE = new string[]
        {
            "varietal",
            "stats_vintage",
            "stats_abv"
        };

        /// <summary>
        /// Exports a list of journal entries to a CSV file.
        /// </summary>
        /// <param name="entryIds">
        /// The list of primary IDs for the journal entries to export.
        /// </param>
        /// <returns>Whether the export was completed successfully.</returns>
        public static async Task<bool> ExportEntriesAsync(Collection<long> entryIds)
        {
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add("Comma Separated Value", new List<string>() { ".csv" });
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = "flavordex_" + DateTime.Now.ToString("yyyy_MM_dd");
            picker.DefaultFileExtension = ".csv";
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    stream.SetLength(0);
                    var writer = new StreamWriter(stream);
                    using (var csv = new CsvWriter(writer))
                    {
                        WriteHeaders(csv);
                        foreach (var entryId in entryIds)
                        {
                            await WriteEntry(csv, await DatabaseHelper.GetEntryAsync(entryId));
                        }
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Write the header row to a CSV file.
        /// </summary>
        /// <param name="writer">The CsvWriter.</param>
        private static void WriteHeaders(CsvWriter writer)
        {
            string[] fields = new string[] {
                Tables.Entries.UUID,
                Tables.Entries.TITLE,
                Tables.Entries.CAT,
                Tables.Entries.MAKER,
                Tables.Entries.ORIGIN,
                Tables.Entries.PRICE,
                Tables.Entries.LOCATION,
                Tables.Entries.DATE,
                Tables.Entries.RATING,
                Tables.Entries.NOTES,
                Tables.Extras.TABLE_NAME,
                Tables.Flavors.TABLE_NAME,
                Tables.Photos.TABLE_NAME
            };
            writer.WriteRecord(fields);
        }

        /// <summary>
        /// Write an entry to the CSV file.
        /// </summary>
        /// <param name="writer">The CsvWriter.</param>
        /// <param name="entry">The Entry.</param>
        private static async Task WriteEntry(CsvWriter writer, Entry entry)
        {
            var record = new string[]
            {
                entry.UUID,
                entry.Title,
                entry.Category,
                entry.Maker,
                entry.Origin,
                entry.Price,
                entry.Location,
                entry.Date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm'Z'"),
                entry.Rating.ToString(),
                entry.Notes,
                await SerializeExtrasAsync(entry.ID),
                await SerializeFlavorsAsync(entry.ID),
                await SerializePhotosAsync(entry.ID)
            };
            writer.WriteRecord(record);
        }

        /// <summary>
        /// Serializes the extra fields for a journal entry into a JSON object string.
        /// </summary>
        /// <param name="entryId">The primary ID of the journal entry.</param>
        /// <returns>A JSON object string.</returns>
        private static async Task<string> SerializeExtrasAsync(long entryId)
        {
            var json = new JsonObject();
            foreach (var extra in await DatabaseHelper.GetEntryExtrasAsync(entryId))
            {
                json[extra.Name] = JsonValue.CreateStringValue(extra.Value);
            }
            return json.Stringify();
        }

        /// <summary>
        /// Serializes the flavors for a journal entry into a JSON object string.
        /// </summary>
        /// <param name="entryId">The primary ID of the journal entry.</param>
        /// <returns>A JSON object string.</returns>
        private static async Task<string> SerializeFlavorsAsync(long entryId)
        {
            var json = new JsonObject();
            foreach (var flavor in await DatabaseHelper.GetEntryFlavorsAsync(entryId))
            {
                json[flavor.Name] = JsonValue.CreateNumberValue(flavor.Value);
            }
            return json.Stringify();
        }

        /// <summary>
        /// Serializes the photos for a journal entry into a JSON array string.
        /// </summary>
        /// <param name="entryId">The primary ID of the journal entry.</param>
        /// <returns>A JSON array string.</returns>
        private static async Task<string> SerializePhotosAsync(long entryId)
        {
            var json = new JsonArray();
            foreach (var photo in await DatabaseHelper.GetEntryPhotosAsync(entryId))
            {
                json.Add(JsonValue.CreateStringValue(photo.Path));
            }
            return json.Stringify();
        }

        /// <summary>
        /// Imports journal entries from a CSV file.
        /// </summary>
        /// <returns>Whether the import was completed successfully.</returns>
        public static async Task<bool> ImportEntriesAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".csv");
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    var reader = new StreamReader(stream);
                    using (var csv = new CsvReader(reader))
                    {
                        if (csv.Read())
                        {
                            var fields = csv.GetRecord();
                            if (!fields.Contains(Tables.Entries.TITLE))
                            {
                                return false;
                            }

                            var collection = new ImportCollection();
                            DetectFormat(collection, fields);

                            var rowMap = new Dictionary<string, string>();
                            while (csv.Read())
                            {
                                rowMap.Clear();
                                var row = csv.GetRecord();
                                for (var i = 0; i < row.Length; i++)
                                {
                                    rowMap[fields[i]] = row[i];
                                }
                                await ReadRow(collection, rowMap);
                            }

                            var result = await new ImportDialog(collection).ShowAsync();
                            return result == ContentDialogResult.Primary;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines the format of the CSV file.
        /// </summary>
        /// <param name="collection">The ImportCollection.</param>
        /// <param name="fields">The list of field names.</param>
        private static void DetectFormat(ImportCollection collection, string[] fields)
        {
            if (fields.Intersect(LEGACY_FIELDS_COMMON).Count() == LEGACY_FIELDS_COMMON.Length)
            {
                var test = fields.Intersect(LEGACY_FIELDS_BEER);
                if (fields.Intersect(LEGACY_FIELDS_BEER).Count() == LEGACY_FIELDS_BEER.Length)
                {
                    collection.LegacyFormat = Constants.CAT_BEER;
                }
                else if (fields.Intersect(LEGACY_FIELDS_COFFEE).Count() ==
                    LEGACY_FIELDS_COFFEE.Length)
                {
                    collection.LegacyFormat = Constants.CAT_COFFEE;
                }
                else if (fields.Intersect(LEGACY_FIELDS_WHISKEY).Count() ==
                    LEGACY_FIELDS_WHISKEY.Length)
                {
                    collection.LegacyFormat = Constants.CAT_WHISKEY;
                }
                else if (fields.Intersect(LEGACY_FIELDS_WINE).Count() == LEGACY_FIELDS_WINE.Length)
                {
                    collection.LegacyFormat = Constants.CAT_WINE;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Reads a record from a CSV file into an ImportCollection object.
        /// </summary>
        /// <param name="collection">The ImportCollection.</param>
        /// <param name="rowMap">A map of column names to values.</param>
        private static async Task ReadRow(ImportCollection collection,
            Dictionary<string, string> rowMap)
        {
            string value;
            rowMap.TryGetValue(Tables.Entries.TITLE, out value);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var entry = new Entry();
            entry.Title = value;

            if (collection.LegacyFormat == null)
            {
                rowMap.TryGetValue(Tables.Entries.CAT, out value);
                entry.Category = value;
            }
            else
            {
                entry.Category = collection.LegacyFormat;
            }

            rowMap.TryGetValue(Tables.Entries.MAKER, out value);
            entry.Maker = value;
            rowMap.TryGetValue(Tables.Entries.ORIGIN, out value);
            entry.Origin = value;
            rowMap.TryGetValue(Tables.Entries.PRICE, out value);
            entry.Price = value;
            rowMap.TryGetValue(Tables.Entries.LOCATION, out value);
            entry.Location = value;

            if (rowMap.TryGetValue(Tables.Entries.RATING, out value))
            {
                double rating;
                double.TryParse(value, out rating);
                entry.Rating = rating;
            }

            if (rowMap.TryGetValue(Tables.Entries.DATE, out value))
            {
                DateTime date;
                entry.Date = DateTime.TryParse(value, out date) ? date : DateTime.Now;
            }
            else
            {
                entry.Date = DateTime.Now;
            }

            rowMap.TryGetValue(Tables.Entries.NOTES, out value);
            entry.Notes = value;

            var isDuplicate = false;
            if (rowMap.TryGetValue(Tables.Entries.UUID, out value))
            {
                isDuplicate = await DatabaseHelper.EntryUuidExists(value);
                if (!isDuplicate)
                {
                    entry.UUID = value;
                }
            }

            var record = new ImportRecord()
            {
                Entry = entry,
                IsDuplicate = isDuplicate
            };

            if (collection.LegacyFormat != null)
            {
                ParseLegacyExtras(record, rowMap, collection.LegacyFormat);
                ParseLegacyFlavors(record, rowMap, collection.LegacyFormat);
                ParseLegacyPhotos(record, rowMap);
            }
            else
            {
                ParseFlavors(record, rowMap);
                ParsePhotos(record, rowMap);
            }
            ParseExtras(record, rowMap);

            collection.Entries.Add(record);
        }

        /// <summary>
        /// Parse the extra fields from a CSV record.
        /// </summary>
        /// <param name="collection">The ImportCollection.</param>
        /// <param name="rowMap">A map of column names to values.</param>
        private static void ParseExtras(ImportRecord record, Dictionary<string, string> rowMap)
        {
            string value;
            if (!rowMap.TryGetValue(Tables.Extras.TABLE_NAME, out value))
            {
                return;
            }
            foreach (var item in ParseJsonObject(value))
            {
                record.Extras.Add(new EntryExtra()
                {
                    Name = item.Key,
                    Value = item.Value.GetString()
                });
            }
        }

        /// <summary>
        /// Reads the columns of a legacy format into extra fields.
        /// </summary>
        /// <param name="collection">The ImportCollection.</param>
        /// <param name="rowMap">A map of column names to values.</param>
        /// <param name="format">The legacy format.</param>
        private static void ParseLegacyExtras(ImportRecord record,
            Dictionary<string, string> rowMap, string format)
        {
            foreach (var item in rowMap)
            {
                if (LEGACY_FIELDS_COMMON.Contains(item.Key))
                {
                    continue;
                }
                switch (format)
                {
                    case Constants.CAT_BEER:
                        if (!LEGACY_FIELDS_BEER.Contains(item.Key))
                        {
                            continue;
                        }
                        break;
                    case Constants.CAT_COFFEE:
                        if (!LEGACY_FIELDS_COFFEE.Contains(item.Key))
                        {
                            continue;
                        }
                        break;
                    case Constants.CAT_WHISKEY:
                        if (!LEGACY_FIELDS_WHISKEY.Contains(item.Key))
                        {
                            continue;
                        }
                        break;
                    case Constants.CAT_WINE:
                        if (!LEGACY_FIELDS_WINE.Contains(item.Key))
                        {
                            continue;
                        }
                        break;
                    default:
                        return;
                }
                record.Extras.Add(new EntryExtra()
                {
                    Name = '_' + item.Key,
                    Value = item.Value
                });
            }
        }

        /// <summary>
        /// Parse the flavors from a CSV record.
        /// </summary>
        /// <param name="collection">The ImportCollection.</param>
        /// <param name="rowMap">A map of column names to values.</param>
        private static void ParseFlavors(ImportRecord record, Dictionary<string, string> rowMap)
        {
            string value;
            if (!rowMap.TryGetValue(Tables.Flavors.TABLE_NAME, out value))
            {
                return;
            }
            foreach (var item in ParseJsonObject(value))
            {
                if (item.Value.ValueType == JsonValueType.Number)
                {
                    record.Flavors.Add(new EntryFlavor()
                    {
                        Name = item.Key,
                        Value = (long)item.Value.GetNumber()
                    });
                }
            }
        }

        /// <summary>
        /// Reads the flavors from a legacy formatted CSV record.
        /// </summary>
        /// <param name="collection">The ImportCollection.</param>
        /// <param name="rowMap">A map of column names to values.</param>
        /// <param name="format">The legacy format.</param>
        private static void ParseLegacyFlavors(ImportRecord record,
            Dictionary<string, string> rowMap, string format)
        {
            string value;
            if (!rowMap.TryGetValue(Tables.Flavors.TABLE_NAME, out value)
                || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string resource;
            switch (format)
            {
                case Constants.CAT_BEER:
                    resource = "Beer";
                    break;
                case Constants.CAT_COFFEE:
                    resource = "Coffee";
                    break;
                case Constants.CAT_WHISKEY:
                    resource = "Whiskey";
                    break;
                case Constants.CAT_WINE:
                    resource = "Wine";
                    break;
                default:
                    return;
            }

            var names = ResourceLoader.GetForViewIndependentUse(resource).GetString("FlavorNames")
                .Split(';');
            var flavors = value.Split(',');
            if (flavors.Length != names.Length)
            {
                return;
            }

            for (var i = 0; i < flavors.Length; i++)
            {
                long flavorValue;
                long.TryParse(flavors[i], out flavorValue);
                flavorValue = Math.Max(0, Math.Min(5, flavorValue));
                record.Flavors.Add(new EntryFlavor()
                {
                    Name = names[i],
                    Value = flavorValue
                });
            }
        }

        /// <summary>
        /// Parses a JSON object into an ordered list of KeyValuePairs.
        /// </summary>
        /// <param name="input">The JSON object string.</param>
        /// <returns>An ordered list of KeyValuePairs</returns>
        private static IEnumerable<KeyValuePair<string, JsonValue>> ParseJsonObject(string input)
        {
            var list = new List<KeyValuePair<string, JsonValue>>();
            if (!string.IsNullOrWhiteSpace(input))
            {
                foreach (var item in input.Trim().Trim('{', '}').Split(','))
                {
                    var pair = item.Split(':');
                    JsonValue name, value;
                    if (pair.Length >= 2 && JsonValue.TryParse(pair[0], out name)
                        && JsonValue.TryParse(pair[1], out value))
                    {
                        list.Add(new KeyValuePair<string, JsonValue>(name.GetString(), value));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Parse the photos from a CSV record.
        /// </summary>
        /// <param name="collection">The ImportCollection.</param>
        /// <param name="rowMap">A map of column names to values.</param>
        private static void ParsePhotos(ImportRecord record, Dictionary<string, string> rowMap)
        {
            string value;
            if (!rowMap.TryGetValue(Tables.Photos.TABLE_NAME, out value)
                || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            JsonArray json;
            if (JsonArray.TryParse(value, out json))
            {
                foreach (var item in json)
                {
                    record.Photos.Add(new Photo()
                    {
                        Path = item.GetString()
                    });
                }
            }
        }

        /// <summary>
        /// Reads the photos from a legacy formatted CSV record.
        /// </summary>
        /// <param name="collection">The ImportCollection.</param>
        /// <param name="rowMap">A map of column names to values.</param>
        private static void ParseLegacyPhotos(ImportRecord record,
            Dictionary<string, string> rowMap)
        {
            string value;
            if (!rowMap.TryGetValue(Tables.Photos.TABLE_NAME, out value)
                || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            foreach (var photo in value.Split(','))
            {
                record.Photos.Add(new Photo()
                {
                    Path = photo.Trim().Substring(0, photo.IndexOf('|'))
                });
            }
        }
    }
}
