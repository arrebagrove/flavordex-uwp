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
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

namespace Flavordex
{
    /// <summary>
    /// A Page containing a welcome message.
    /// </summary>
    public sealed partial class WelcomePage : Page
    {
        /// <summary>
        /// Gets the version message.
        /// </summary>
        private string Version
        {
            get
            {
                var format = ResourceLoader.GetForCurrentView().GetString("Message/Version");
                var version = Package.Current.Id.Version;
                return string.Format(format, version.Major, version.Minor, version.Build,
                    version.Revision);
            }
        }

        /// <summary>
        /// Gets the copyright message.
        /// </summary>
        private string Copyright
        {
            get
            {
                var format = ResourceLoader.GetForCurrentView().GetString("Message/Copyright");
                return string.Format(format, DateTime.Now.Year);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public WelcomePage()
        {
            InitializeComponent();
        }
    }
}
