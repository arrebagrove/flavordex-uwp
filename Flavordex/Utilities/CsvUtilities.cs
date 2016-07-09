﻿using Flavordex.Models;
using Flavordex.Models.Data;
using Flavordex.UI.Controls;
using Flavordex.Utilities.CSV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace Flavordex.Utilities
{
    public class CsvUtilities
    {
        /// <summary>
        /// Container for CSV records.
        /// </summary>
        private class CsvRecord
        {
            public string uuid { get; set; }
            public string title { get; set; }
            public string cat { get; set; }
            public string maker { get; set; } = "";
            public string origin { get; set; } = "";
            public string price { get; set; } = "";
            public string location { get; set; } = "";
            public string date { get; set; }
            public double rating { get; set; } = 0.0;
            public string notes { get; set; } = "";
            public string extras { get; set; }
            public string flavors { get; set; }
            public string photos { get; set; }
        }

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
                    using (var csv = new CsvWriter<CsvRecord>(writer))
                    {
                        foreach (var entryId in entryIds)
                        {
                            var entry = await DatabaseHelper.GetEntryAsync(entryId);
                            var record = new CsvRecord()
                            {
                                uuid = entry.UUID,
                                title = entry.Title,
                                cat = entry.Category,
                                maker = entry.Maker,
                                origin = entry.Origin,
                                price = entry.Price,
                                location = entry.Location,
                                date = entry.Date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm'Z'"),
                                rating = entry.Rating,
                                notes = entry.Notes,
                                extras = await SerializeExtrasAsync(entry.ID),
                                flavors = await SerializeFlavorsAsync(entry.ID),
                                photos = await SerializePhotosAsync(entry.ID)
                            };
                            csv.WriteRecord(record);
                        }
                    }
                }

                return true;
            }

            return false;
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
                        var records = new Collection<ImportRecord>();
                        while (csv.Read())
                        {
                            var row = csv.GetRecord<CsvRecord>();

                            if (string.IsNullOrWhiteSpace(row.title))
                            {
                                continue;
                            }

                            var entry = new Entry()
                            {
                                Title = row.title,
                                Category = row.cat,
                                Maker = row.maker,
                                Origin = row.origin,
                                Price = row.price,
                                Location = row.location,
                                Rating = row.rating,
                                Notes = row.notes
                            };

                            DateTime date;
                            entry.Date = DateTime.TryParse(row.date, out date) ? date : DateTime.Now;

                            var isDuplicate = await DatabaseHelper.EntryUuidExists(row.uuid);
                            if (!isDuplicate)
                            {
                                entry.UUID = row.uuid;
                            }

                            records.Add(new ImportRecord()
                            {
                                Entry = entry,
                                Extras = ParseExtras(row),
                                Flavors = ParseFlavors(row),
                                Photos = ParsePhotos(row),
                                IsDuplicate = isDuplicate
                            });
                        }

                        return await new ImportDialog(records).ShowAsync() == ContentDialogResult.Primary;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Parse the extra fields from a CSV record.
        /// </summary>
        /// <param name="record">The CsvRecord.</param>
        /// <returns>A Collection of EntryExtras.</returns>
        private static Collection<EntryExtra> ParseExtras(CsvRecord record)
        {
            var list = new Collection<EntryExtra>();
            foreach (var item in ParseJsonObject(record.extras))
            {
                list.Add(new EntryExtra()
                {
                    Name = item.Key,
                    Value = item.Value.GetString()
                });
            }
            return list;
        }

        /// <summary>
        /// Parse the flavors from a CSV record.
        /// </summary>
        /// <param name="record">The CsvRecord.</param>
        /// <returns>A Collection of EntryFlavors.</returns>
        private static Collection<EntryFlavor> ParseFlavors(CsvRecord record)
        {
            var list = new Collection<EntryFlavor>();
            foreach (var item in ParseJsonObject(record.flavors))
            {
                if (item.Value.ValueType == JsonValueType.Number)
                {
                    list.Add(new EntryFlavor()
                    {
                        Name = item.Key,
                        Value = (long)item.Value.GetNumber()
                    });
                }
            }
            return list;
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
                    if (pair.Length >= 2 && JsonValue.TryParse(pair[0], out name) && JsonValue.TryParse(pair[1], out value))
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
        /// <param name="record">The CsvRecord.</param>
        /// <returns>A Collection of Photos.</returns>
        private static Collection<Photo> ParsePhotos(CsvRecord record)
        {
            var list = new Collection<Photo>();
            if (!string.IsNullOrWhiteSpace(record.photos))
            {
                JsonArray json;
                if (JsonArray.TryParse(record.photos, out json))
                {
                    foreach (var item in json)
                    {
                        list.Add(new Photo()
                        {
                            Path = item.GetString()
                        });
                    }
                }
            }
            return list;
        }
    }
}