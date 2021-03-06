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
using Flavordex.Utilities;
using Flavordex.Utilities.Databases;
using Flavordex.ViewModels;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Flavordex
{
    /// <summary>
    /// Page to display the list of journal entries grouped by category. The Page also shows the
    /// details of the selected item when the screen is wide.
    /// </summary>
    public sealed partial class EntryListPage : Page
    {
        /// <summary>
        /// The EntryListViewModel backing the Page.
        /// </summary>
        private EntryListViewModel List { get; } = new EntryListViewModel();

        /// <summary>
        /// Gets or sets whether the Export Button is enabled.
        /// </summary>
        public bool EnableExport
        {
            get { return (bool)GetValue(EnableExportProperty); }
            set { SetValue(EnableExportProperty, value); }
        }
        public static readonly DependencyProperty EnableExportProperty =
            DependencyProperty.Register("EnableExport", typeof(bool), typeof(EntryListPage),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether the Settings pane is open.
        /// </summary>
        private bool _isSettingsOpen;

        /// <summary>
        /// Constructor. Loads the list of Categories.
        /// </summary>
        public EntryListPage()
        {
            InitializeComponent();
            CheckDefaultSortButton();

            if (Window.Current.Bounds.Width < 720)
            {
                VisualStateManager.GoToState(this, "NarrowState", false);
            }
            Window.Current.SizeChanged += OnWindowSizeChanged;
            (Application.Current as App).SearchSubmitted += OnSearchSubmitted;
        }

        /// <summary>
        /// Sets the selected item to the parameter if provided when the Page is navigated to.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.BackRequested += OnBackRequested;
            if (List.SelectedEntry != null)
            {
                systemNavigationManager.AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Visible;
            }

            Zoom.IsZoomedInViewActive = List.IsCategorySelected;
            Zoom.ViewChangeStarted += OnListViewChange;

            if (e.Parameter is long)
            {
                List.SelectedEntryId = (long)e.Parameter;
            }

            SplitView.IsPaneOpen = _isSettingsOpen;
            _isSettingsOpen = false;
        }

        /// <summary>
        /// Removes event handlers when the Page is navigated away from.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;

            Zoom.ViewChangeStarted -= OnListViewChange;
            Zoom.IsZoomedInViewActive = true;

            _isSettingsOpen = SplitView.IsPaneOpen;
            SplitView.IsPaneOpen = false;
        }

        /// <summary>
        /// Sets the VisualState when the Window size changes.
        /// </summary>
        /// <param name="sender">The Window.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            if (e.Size.Width >= 720)
            {
                VisualStateManager.GoToState(this, "WideState", false);
            }
            else
            {
                VisualStateManager.GoToState(this,
                    DetailFrame.SourcePageType != typeof(WelcomePage)
                    ? "NarrowSelectedState" : "NarrowState", false);
            }
        }

        /// <summary>
        /// Moves CommandBar items between the primary and secondary commands depending on the
        /// width of the CommandBar when its size changes.
        /// </summary>
        /// <param name="sender">The ListCommandBar.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCommandBarSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var threshold = 400;
            if (e.NewSize.Width < threshold
                && (e.PreviousSize.Width == 0 || e.PreviousSize.Width >= threshold))
            {
                if (ListCommandBar.PrimaryCommands.Remove(SearchButton))
                {
                    ListCommandBar.PrimaryCommands.Remove(SortButton);
                    ListCommandBar.SecondaryCommands.Insert(0, SortButton);
                    ListCommandBar.SecondaryCommands.Insert(0, SearchButton);
                }
            }
            else if (e.NewSize.Width > threshold && e.PreviousSize.Width <= threshold)
            {
                if (ListCommandBar.SecondaryCommands.Remove(SearchButton))
                {
                    ListCommandBar.SecondaryCommands.Remove(SortButton);
                    ListCommandBar.PrimaryCommands.Add(SearchButton);
                    ListCommandBar.PrimaryCommands.Add(SortButton);
                    SearchButton.IsEnabled = SortButton.IsEnabled = true;
                }
            }

        }

        /// <summary>
        /// Deselects the selected item when the back button is pressed.
        /// </summary>
        /// <param name="sender">The SystemNavigationManager.</param>
        /// <param name="e">The event arguments.</param>
        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (List.SelectedEntryId > -1)
            {
                List.SelectedEntryId = -1;
                e.Handled = true;
            }
            else if (DetailFrame.SourcePageType != typeof(WelcomePage))
            {
                DetailFrame.Navigate(typeof(WelcomePage));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Loads the details for the selected Entry in the DetailFrame when the selected list item
        /// changes.
        /// </summary>
        /// <param name="sender">The MasterList.</param>
        /// <param name="e">The event arguments.</param>
        private void OnItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (List.ExportMode)
            {
                EnableExport = MasterList.SelectedItems.Count > 0;
            }
            else
            {
                if (MasterList.SelectedItem != null)
                {
                    var entryId = (MasterList.SelectedItem as EntryItemViewModel).Model.ID;
                    if (entryId != List.SelectedEntryId)
                    {
                        List.SelectedEntryId = entryId;
                    }

                    if (DetailFrame.SourcePageType == typeof(ViewEntryPage))
                    {
                        DetailFrame.Navigate(typeof(ViewEntryPage), entryId,
                            new SuppressNavigationTransitionInfo());
                    }
                    else
                    {
                        DetailFrame.Navigate(typeof(ViewEntryPage), entryId);
                    }
                }
                else
                {
                    DetailFrame.Navigate(typeof(WelcomePage));
                }
            }
        }

        /// <summary>
        /// Selects a Category when the SemanticZoomView changes views.
        /// </summary>
        /// <param name="sender">The SemanticZoomView.</param>
        /// <param name="e">The event arguments.</param>
        private void OnListViewChange(object sender, SemanticZoomViewChangedEventArgs e)
        {
            if (!e.IsSourceZoomedInView)
            {
                if (e.SourceItem.Item == null)
                {
                    Settings.ListCategory = 0;
                    return;
                }

                var category = e.SourceItem.Item as CategoryItemViewModel;
                if (Settings.ListCategory != category.Model.ID)
                {
                    Settings.ListCategory = category.Model.ID;
                }
            }
            else
            {
                (Application.Current as App).Search = null;
                Settings.ListCategory = -1;
            }
        }

        /// <summary>
        /// Updates the VisualState of the Page according to the SourcePageType when the
        /// DetailFrame is navigated.
        /// </summary>
        /// <param name="sender">The DetailFrame.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDetailFrameNavigated(object sender, NavigationEventArgs e)
        {
            DetailFrame.BackStack.Clear();
            var visibility = e.SourcePageType != typeof(WelcomePage)
                ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = visibility;
            if (Window.Current.Bounds.Width < 720)
            {
                VisualStateManager.GoToState(this,
                    e.SourcePageType != typeof(WelcomePage)
                    ? "NarrowSelectedState" : "NarrowState", true);
            }
        }

        /// <summary>
        /// Updates the list sorting parameters when one of the sorting buttons is clicked.
        /// </summary>
        /// <param name="sender">The clicked ToggleMenuFlyoutItem.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSortButtonClick(object sender, RoutedEventArgs e)
        {
            Settings.SortField field;
            if (sender == SortDateButton)
            {
                field = Settings.SortField.Date;

                SortNameButton.IsChecked = false;
                SortDateButton.IsChecked = true;
                SortRatingButton.IsChecked = false;
            }
            else if (sender == SortRatingButton)
            {
                field = Settings.SortField.Rating;

                SortNameButton.IsChecked = false;
                SortDateButton.IsChecked = false;
                SortRatingButton.IsChecked = true;
            }
            else
            {
                field = Settings.SortField.Name;

                SortNameButton.IsChecked = true;
                SortDateButton.IsChecked = false;
                SortRatingButton.IsChecked = false;
            }

            Settings.ListSortDescending =
                field == Settings.ListSortField ? !Settings.ListSortDescending : false;
            Settings.ListSortField = field;
        }

        /// <summary>
        /// Navigates to the EditCategoryPage to add a new Category when the Add Category button is
        /// pressed.
        /// </summary>
        /// <param name="sender">The AppBarButton.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAddCategory(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EditCategoryPage));
        }

        /// <summary>
        /// Navigates to the AddEntryPage to add a new Entry when the Add Entry button is pressed.
        /// </summary>
        /// <param name="sender">The AppBarButton.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAddEntry(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddEntryPage), Settings.ListCategory);
        }

        /// <summary>
        /// Opens the MenuFlyout for a Category list item when it is right-clicked or held.
        /// </summary>
        /// <param name="sender">The list item.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRequestCategoryContextMenu(object sender, RoutedEventArgs e)
        {
            if (GetItem(sender).ID > 0)
            {
                FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
            }
        }

        /// <summary>
        /// Opens the MenuFlyout for an Entry list item when it is right-clicked or held.
        /// </summary>
        /// <param name="sender">The list item.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRequestEntryContextMenu(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        /// <summary>
        /// Navigates to the EditCategoryPage when the Edit Category menu item is clicked.
        /// </summary>
        /// <param name="sender">The MenuFlyoutItem.</param>
        /// <param name="e">The event arguments.</param>
        private void OnEditCategory(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EditCategoryPage), GetItem(sender).ID);
        }

        /// <summary>
        /// Deletes a Category after prompting the user for confirmation.
        /// </summary>
        /// <param name="sender">The MenuFlyoutItem.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnDeleteCategory(object sender, RoutedEventArgs e)
        {
            var category = GetItem(sender) as Category;
            if (category.IsPreset)
            {
                return;
            }

            var result = await new DeleteCategoryDialog(category).ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await DatabaseHelper.DeleteCategoryAsync(category);
            }
        }

        /// <summary>
        /// Opens the sharing UI when the Share Entry menu item is clicked.
        /// </summary>
        /// <param name="sender">The MenuFlyoutItem.</param>
        /// <param name="e">The event arguments.</param>
        private void OnShareEntry(object sender, RoutedEventArgs e)
        {
            EntryUtilities.ShareEntry(GetItem(sender) as Entry);
        }

        /// <summary>
        /// Navigates to the EditEntryPage when the Edit Entry menu item is clicked.
        /// </summary>
        /// <param name="sender">The MenuFlyoutItem.</param>
        /// <param name="e">The event arguments.</param>
        private void OnEditEntry(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EditEntryPage), GetItem(sender).ID);
        }

        /// <summary>
        /// Deletes a journal entry after prompting the user for confirmation.
        /// </summary>
        /// <param name="sender">The MenuFlyoutItem.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnDeleteEntry(object sender, RoutedEventArgs e)
        {
            var entry = GetItem(sender) as Entry;
            var result = await new DeleteEntryDialog(entry).ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                EntryUtilities.DeleteEntry(entry);
            }
        }

        /// <summary>
        /// Opens the list filtering dialog when the Filter button is pressed, then updates the
        /// list filtering parameters according to the result.
        /// </summary>
        /// <param name="sender">The AppBarButton.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            if (DetailFrame.CurrentSourcePageType != typeof(EntrySearchPage))
            {
                List.SelectedEntryId = -1;
                DetailFrame.Navigate(typeof(EntrySearchPage));
            }
        }

        /// <summary>
        /// Updates the current list search parameters when a search query is submitted.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSearchSubmitted(object sender, EventArgs e)
        {
            List.Search = (Application.Current as App).Search;
            if (PageVisualStates.CurrentState == NarrowSelectedState)
            {
                DetailFrame.Navigate(typeof(WelcomePage));
            }
        }

        /// <summary>
        /// Gets the Model backing list item.
        /// </summary>
        /// <param name="sender">The list item.</param>
        /// <returns>The primary ID of the list item.</returns>
        private static Model GetItem(object sender)
        {
            var context = (sender as FrameworkElement).DataContext;
            if (context is CategoryItemViewModel)
            {
                return (context as CategoryItemViewModel).Model;
            }
            return (context as EntryItemViewModel).Model;
        }

        /// <summary>
        /// Checks the default sorting option of the menu.
        /// </summary>
        private void CheckDefaultSortButton()
        {
            switch (Settings.ListSortField)
            {
                case Settings.SortField.Date:
                    SortDateButton.IsChecked = true;
                    break;
                case Settings.SortField.Rating:
                    SortRatingButton.IsChecked = true;
                    break;
                default:
                    SortNameButton.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// Sets export mode and changes the list selection settings accordingly.
        /// </summary>
        private void SetExportMode(bool exportMode)
        {
            List.ExportMode = exportMode;

            if (exportMode)
            {
                FindName("ExportBar");
                MasterList.SelectionMode = ListViewSelectionMode.Multiple;
                MasterList.IsItemClickEnabled = false;
            }
            else
            {
                MasterList.SelectionMode = ListViewSelectionMode.Single;
            }
        }

        /// <summary>
        /// Enables export mode when the Export menu option is clicked.
        /// </summary>
        /// <param name="sender">The MenuFlyoutItem.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStartExport(object sender, RoutedEventArgs e)
        {
            ListCommandBar.IsOpen = false;
            if (!List.IsCategorySelected)
            {
                Settings.ListCategory = 0;
            }
            SetExportMode(true);
        }

        /// <summary>
        /// Disables export mode when the Cancel button is clicked.
        /// </summary>
        /// <param name="sender">The Cancel Button.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCancelExport(object sender, RoutedEventArgs e)
        {
            SetExportMode(false);
        }

        /// <summary>
        /// Selects all list items when the Check All button is clicked.
        /// </summary>
        /// <param name="sender">The Check All Button.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCheckAll(object sender, RoutedEventArgs e)
        {
            MasterList.SelectAll();
        }

        /// <summary>
        /// Deselects all list items when the Uncheck All button is clicked.
        /// </summary>
        /// <param name="sender">The Uncheck All Button.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUncheckAll(object sender, RoutedEventArgs e)
        {
            MasterList.DeselectRange(new ItemIndexRange(0, (uint)MasterList.Items.Count));
        }

        /// <summary>
        /// Exports the selected list items when the Export button is clicked.
        /// </summary>
        /// <param name="sender">The Export Button.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnExport(object sender, RoutedEventArgs e)
        {
            var items = new Collection<long>();
            foreach (var item in MasterList.SelectedItems)
            {
                items.Add((item as EntryItemViewModel).Model.ID);
            }
            SetExportMode(!await CsvUtilities.ExportEntriesAsync(items));
        }

        /// <summary>
        /// Starts the import process when the Import menu option is clicked.
        /// </summary>
        /// <param name="sender">The MenuFlyoutItem.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnStartImport(object sender, RoutedEventArgs e)
        {
            ListCommandBar.IsOpen = false;
            await CsvUtilities.ImportEntriesAsync();
        }

        /// <summary>
        /// Opens the settings pane when the Settings button is pressed.
        /// </summary>
        /// <param name="sender">The AppBarButton.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            FindName("SettingsPane");
            SplitView.IsPaneOpen = true;
        }

        /// <summary>
        /// Returns the list to the categories list when the back arrow button is pressed.
        /// </summary>
        /// <param name="sender">The back arrow Button.</param>
        /// <param name="e">The event arguments.</param>
        private void OnListBackArrowClick(object sender, RoutedEventArgs e)
        {
            Settings.ListCategory = -1;
        }
    }
}
