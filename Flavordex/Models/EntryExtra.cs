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
using Flavordex.Models.Data;
using Flavordex.Utilities.Databases;

namespace Flavordex.Models
{
    /// <summary>
    /// Model representing the value of an extra field.
    /// </summary>
    public class EntryExtra : Model
    {
        /// <summary>
        /// The primary ID of the journal entry this extra field is associated with.
        /// </summary>
        public long EntryID
        {
            get { return _data.GetLong(Tables.EntriesExtras.ENTRY); }
            set { _data.SetLong(Tables.EntriesExtras.ENTRY, value); }
        }

        /// <summary>
        /// The primary ID of the extra field.
        /// </summary>
        public long ExtraID
        {
            get { return _data.GetLong(Tables.EntriesExtras.EXTRA); }
            set { _data.SetLong(Tables.EntriesExtras.EXTRA, value); }
        }

        /// <summary>
        /// The globally unique identifier string.
        /// </summary>
        public string UUID
        {
            get { return _data.GetString(Tables.Extras.UUID); }
            set { _data.SetString(Tables.Extras.UUID, value); }
        }

        /// <summary>
        /// The name of the extra field.
        /// </summary>
        public string Name
        {
            get { return _data.GetString(Tables.Extras.NAME); }
            set { _data.SetString(Tables.Extras.NAME, value); }
        }

        /// <summary>
        /// The sorting position in the list of fields.
        /// </summary>
        public long Position
        {
            get { return _data.GetLong(Tables.Extras.POS); }
            set { _data.SetLong(Tables.Extras.POS, value); }
        }

        /// <summary>
        /// The value of the extra field.
        /// </summary>
        public string Value
        {
            get { return _data.GetString(Tables.EntriesExtras.VALUE); }
            set { _data.SetString(Tables.EntriesExtras.VALUE, value); }
        }

        /// <summary>
        /// Whether this is a preset field.
        /// </summary>
        public bool IsPreset
        {
            get { return _data.GetBool(Tables.Extras.PRESET); }
            set { _data.SetBool(Tables.Extras.PRESET, value); }
        }

        /// <summary>
        /// Whether this extra field has been deleted from the category.
        /// </summary>
        public bool IsDeleted
        {
            get { return _data.GetBool(Tables.Extras.DELETED); }
            set { _data.SetBool(Tables.Extras.DELETED, value); }
        }
    }
}
