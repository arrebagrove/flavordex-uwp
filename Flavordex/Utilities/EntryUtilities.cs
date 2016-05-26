﻿using CsvHelper;
using Flavordex.Models;
using Flavordex.Models.Data;
using Flavordex.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Data.Json;
using Windows.Storage.Pickers;

namespace Flavordex.Utilities
{
    /// <summary>
    /// Common methods for managing journal entries.
    /// </summary>
    public class EntryUtilities
    {
        /// <summary>
        /// The format string for the sharing subject.
        /// </summary>
        private static readonly string _shareSubjectFormat = ResourceLoader.GetForCurrentView().GetString("Share/Subject");

        /// <summary>
        /// The format string for the sharing body.
        /// </summary>
        private static readonly string _shareBodyFormat = ResourceLoader.GetForCurrentView().GetString("Share/Body");

        /// <summary>
        /// Opens the sharing UI for a journal entry.
        /// </summary>
        /// <param name="entry">The Entry to share.</param>
        public static void ShareEntry(Entry entry)
        {
            DataTransferManager.GetForCurrentView().DataRequested += (DataTransferManager sender, DataRequestedEventArgs args) =>
            {
                args.Request.Data.Properties.Title = string.Format(_shareSubjectFormat, entry.Title);
                args.Request.Data.SetText(string.Format(_shareBodyFormat, entry.Title, entry.Rating));
            };
            DataTransferManager.ShowShareUI();
        }

        /// <summary>
        /// Deletes a journal entry.
        /// </summary>
        /// <param name="entry">The journal entry.</param>
        public static async void DeleteEntry(Entry entry)
        {
            await DatabaseHelper.DeleteEntryAsync(entry);
            PhotoUtilities.DeleteThumbnail(entry.ID);
        }

        /// <summary>
        /// Container for CSV records.
        /// </summary>
        private class CsvRecord
        {
            public string uuid { get; set; }
            public string title { get; set; }
            public string cat { get; set; }
            public string maker { get; set; }
            public string origin { get; set; }
            public string price { get; set; }
            public string location { get; set; }
            public string date { get; set; }
            public double rating { get; set; }
            public string notes { get; set; }
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
                    var writer = new StreamWriter(stream);
                    using (var csv = new CsvWriter(writer))
                    {
                        csv.Configuration.QuoteAllFields = true;
                        csv.WriteHeader(typeof(CsvRecord));
                        foreach (var entryId in entryIds)
                        {
                            var entry = await DatabaseHelper.GetEntryAsync(entryId);
                            var model = entry.Model;
                            var record = new CsvRecord()
                            {
                                uuid = model.UUID,
                                title = model.Title,
                                cat = model.Category,
                                maker = model.Maker,
                                origin = model.Origin,
                                price = model.Price,
                                location = model.Location,
                                date = model.Date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm'Z'"),
                                rating = model.Rating,
                                notes = model.Notes,
                                extras = SerializeExtras(entry),
                                flavors = await SerializeFlavorsAsync(model.ID),
                                photos = await SerializePhotosAsync(model.ID)
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
        /// <param name="entry">The journal entry.</param>
        /// <returns>A JSON object string.</returns>
        private static string SerializeExtras(EntryViewModel entry)
        {
            var json = new JsonObject();
            foreach (var extra in entry.Extras)
            {
                json[extra.Model.Name] = JsonValue.CreateStringValue(extra.Model.Value);
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
                json[flavor.Model.Name] = JsonValue.CreateNumberValue(flavor.Model.Value);
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
                json.Add(JsonValue.CreateStringValue(photo.Model.Path));
            }
            return json.Stringify();
        }
    }
}
